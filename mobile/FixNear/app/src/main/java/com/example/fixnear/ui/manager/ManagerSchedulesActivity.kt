package com.example.fixnear.ui.manager

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.ManagerScheduleItem
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityManagerSchedulesBinding
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

class ManagerSchedulesActivity : AppCompatActivity() {

    private lateinit var binding: ActivityManagerSchedulesBinding
    private lateinit var scheduleAdapter: ManagerScheduleAdapter

    private var shopId = -1
    private var includeHistory = false
    private var loading = false

    private val caramel = 0xFFD8975B.toInt()
    private val inactive = 0xFFC7B9AF.toInt()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityManagerSchedulesBinding.inflate(
            layoutInflater
        )
        setContentView(binding.root)

        shopId = intent.getIntExtra("SHOP_ID", -1)

        binding.tvShopInfo.text =
            if (shopId > 0) {
                "Cửa hàng #$shopId"
            } else {
                "Chưa xác định cửa hàng"
            }

        scheduleAdapter = ManagerScheduleAdapter { item ->
            if (item.repairRequestId <= 0) {
                toast("Lịch này chưa có mã yêu cầu sửa chữa.")
            } else {
                startActivity(
                    Intent(
                        this,
                        ManagerRepairDetailActivity::class.java
                    ).apply {
                        putExtra("SHOP_ID", shopId)
                        putExtra(
                            "REPAIR_REQUEST_ID",
                            item.repairRequestId
                        )
                    }
                )
            }
        }

        binding.rvSchedules.apply {
            layoutManager = LinearLayoutManager(
                this@ManagerSchedulesActivity
            )
            adapter = scheduleAdapter
        }

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.tabCurrent.setOnClickListener {
            includeHistory = false
            updateTabs()
            loadSchedules()
        }

        binding.tabHistory.setOnClickListener {
            includeHistory = true
            updateTabs()
            loadSchedules()
        }

        binding.btnRefresh.setOnClickListener {
            loadSchedules()
        }

        updateTabs()
        loadSchedules()
    }

    private fun getToken(): String? {
        val raw = SessionManager(this)
            .getToken()
            ?.trim()
            ?.takeIf { it.isNotBlank() }
            ?: return null

        return if (raw.startsWith("Bearer ", true)) {
            raw
        } else {
            "Bearer $raw"
        }
    }

    private fun updateTabs() {
        binding.tabCurrent.setTextColor(
            if (!includeHistory) caramel else inactive
        )

        binding.tabHistory.setTextColor(
            if (includeHistory) caramel else inactive
        )
    }

    private fun JsonObject.field(
        vararg names: String
    ): String {
        for (name in names) {
            val value = entrySet()
                .firstOrNull {
                    it.key.equals(name, true)
                }
                ?.value

            if (value != null &&
                !value.isJsonNull &&
                value.isJsonPrimitive
            ) {
                return value.asString
            }
        }

        return ""
    }

    private fun extractItems(
        root: JsonElement?
    ): List<JsonObject>? {
        if (root == null || root.isJsonNull) {
            return null
        }

        val data = when {
            root.isJsonArray -> root

            root.isJsonObject -> {
                root.asJsonObject.get("data")
            }

            else -> null
        } ?: return null

        val array = when {
            data.isJsonArray -> data.asJsonArray

            data.isJsonObject -> {
                val nested = data.asJsonObject
                val items = nested.get("items")
                    ?: nested.get("schedules")
                    ?: nested.get("results")

                if (items?.isJsonArray == true) {
                    items.asJsonArray
                } else {
                    return null
                }
            }

            else -> return null
        }

        return array.mapNotNull {
            if (it.isJsonObject) it.asJsonObject
            else null
        }
    }

    private fun toSchedule(
        json: JsonObject
    ): ManagerScheduleItem? {

        val id = json.field(
            "repairScheduleId",
            "scheduleId",
            "id"
        ).toIntOrNull() ?: return null

        return ManagerScheduleItem(
            scheduleId = id,

            repairRequestId = json.field(
                "repairRequestId"
            ).toIntOrNull() ?: -1,

            technicianName = json.field(
                "technicianName"
            ),

            customerName = json.field(
                "customerName"
            ),

            description = json.field(
                "description",
                "repairDescription"
            ),

            startTime = json.field(
                "startTime"
            ),

            endTime = json.field(
                "endTime"
            ),

            status = json.field(
                "status",
                "scheduleStatus"
            )
        )
    }

    private fun loadSchedules() {
        if (loading) return

        if (shopId <= 0) {
            showError("ShopId không hợp lệ.")
            return
        }

        val token = getToken()

        if (token == null) {
            showError("Vui lòng đăng nhập lại.")
            return
        }

        loading = true

        binding.progressLoading.visibility = View.VISIBLE
        binding.tvState.visibility = View.VISIBLE
        binding.tvState.text = "Đang tải lịch sửa chữa..."
        binding.btnRefresh.isEnabled = false

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerRepairApi
                        .getRepairSchedules(
                            token = token,
                            shopId = shopId,
                            includeHistory = includeHistory
                        )

                if (!response.isSuccessful) {
                    showError(
                        when (response.code()) {
                            401 -> "Phiên đăng nhập hết hạn."
                            403 -> "Không có quyền xem lịch Shop này."
                            404 -> "Không tìm thấy cửa hàng."
                            else ->
                                "Lỗi tải lịch HTTP ${response.code()}"
                        }
                    )
                    return@launch
                }

                val rawItems = extractItems(response.body())

                if (rawItems == null) {
                    showError(
                        "Cấu trúc JSON lịch không đúng dạng danh sách."
                    )
                    return@launch
                }

                val schedules = rawItems.mapNotNull {
                    toSchedule(it)
                }

                scheduleAdapter.submitItems(schedules)

                binding.tvScheduleCount.text =
                    "Danh sách lịch (${schedules.size})"

                binding.tvState.visibility =
                    if (schedules.isEmpty()) {
                        View.VISIBLE
                    } else {
                        View.GONE
                    }

                binding.tvState.text =
                    "Cửa hàng chưa có lịch sửa chữa phù hợp."

            } catch (e: Exception) {
                showError(
                    "Lỗi kết nối: ${e.localizedMessage}"
                )
            } finally {
                loading = false
                binding.progressLoading.visibility = View.GONE
                binding.btnRefresh.isEnabled = true
            }
        }
    }

    private fun showError(message: String) {
        if (::scheduleAdapter.isInitialized) {
            scheduleAdapter.submitItems(emptyList())
        }

        binding.tvScheduleCount.text = "Danh sách lịch"
        binding.tvState.text = message
        binding.tvState.visibility = View.VISIBLE
        binding.progressLoading.visibility = View.GONE
    }

    private fun toast(text: String) {
        Toast.makeText(
            this,
            text,
            Toast.LENGTH_SHORT
        ).show()
    }
}
