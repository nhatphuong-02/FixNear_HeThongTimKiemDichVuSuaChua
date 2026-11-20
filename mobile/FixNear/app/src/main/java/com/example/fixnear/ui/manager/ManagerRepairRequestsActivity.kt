package com.example.fixnear.ui.manager

import android.content.res.ColorStateList
import android.graphics.Typeface
import android.os.Bundle
import android.view.View
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.R
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.ManagerRepairItem
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityManagerRepairRequestsBinding
import com.google.gson.GsonBuilder
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import kotlinx.coroutines.launch
import java.util.Locale

class ManagerRepairRequestsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityManagerRepairRequestsBinding
    private lateinit var requestAdapter: ManagerRepairAdapter

    private var shopId = -1
    private var filter = "all"
    private var allItems = emptyList<ManagerRepairItem>()
    private var isLoading = false
    private var hasLoaded = false

    private val caramel = 0xFFD8975B.toInt()
    private val inactive = 0xFFC7B9AF.toInt()
    private val brown = 0xFF66442F.toInt()
    private val dark = 0xFF251A14.toInt()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityManagerRepairRequestsBinding.inflate(
            layoutInflater
        )
        setContentView(binding.root)

        shopId = intent.getIntExtra("SHOP_ID", -1)

        binding.tvShopName.text =
            if (shopId > 0) {
                "Cửa hàng #$shopId"
            } else {
                "Chưa xác định cửa hàng"
            }

        binding.btnBack.setOnClickListener {
            finish()
        }

        requestAdapter = ManagerRepairAdapter { item ->
            loadRepairDetail(item.repairRequestId)
        }

        binding.rvRepairRequests.apply {
            layoutManager = LinearLayoutManager(
                this@ManagerRepairRequestsActivity
            )
            adapter = requestAdapter
        }

        binding.tabAll.setOnClickListener {
            selectFilter("all")
        }

        binding.tabPending.setOnClickListener {
            selectFilter("pending")
        }

        binding.tabProcessing.setOnClickListener {
            selectFilter("processing")
        }

        binding.tabCompleted.setOnClickListener {
            selectFilter("completed")
        }

        binding.btnRefresh.setOnClickListener {
            loadRepairRequests()
        }

        selectFilter("all")
        loadRepairRequests()
    }

    private fun authorization(): String? {
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

    // ==========================================
    // GET /api/manager/repair-requests
    // ==========================================

    private fun loadRepairRequests() {
        if (isLoading) return

        if (shopId <= 0) {
            showError("ShopId không hợp lệ. Hãy mở từ Manager Home.")
            return
        }

        val token = authorization()

        if (token == null) {
            showError("Phiên đăng nhập không hợp lệ.")
            return
        }

        isLoading = true
        binding.progressLoading.visibility = View.VISIBLE
        binding.tvState.visibility = View.VISIBLE
        binding.tvState.text = "Đang tải yêu cầu sửa chữa..."
        binding.btnRefresh.isEnabled = false

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerRepairApi
                        .getRepairRequests(
                            token = token,
                            shopId = shopId
                        )

                if (!response.isSuccessful) {
                    showError(
                        when (response.code()) {
                            401 -> "Phiên đăng nhập hết hạn."
                            403 -> "Tài khoản không có quyền xem yêu cầu của Shop này."
                            404 -> "Không tìm thấy dữ liệu cửa hàng."
                            else ->
                                "Không tải được yêu cầu: HTTP ${response.code()}"
                        }
                    )
                    return@launch
                }

                val items = extractArray(response.body())

                if (items == null) {
                    showError(
                        "Backend trả JSON không đúng cấu trúc danh sách."
                    )
                    return@launch
                }

                allItems = items.mapNotNull {
                    parseRepairItem(it)
                }

                hasLoaded = true
                applyFilter()

            } catch (e: Exception) {
                showError(
                    "Lỗi kết nối: ${e.localizedMessage}"
                )
            } finally {
                isLoading = false
                binding.progressLoading.visibility = View.GONE
                binding.btnRefresh.isEnabled = true
            }
        }
    }

    // ==========================================
    // ĐỌC RESPONSE { message, data }
    // ==========================================

    private fun extractArray(
        body: JsonElement?
    ): List<JsonObject>? {

        if (body == null || body.isJsonNull) {
            return null
        }

        val array = when {
            body.isJsonArray -> body.asJsonArray

            body.isJsonObject -> {
                val root = body.asJsonObject
                val data = root.get("data")

                when {
                    data?.isJsonArray == true ->
                        data.asJsonArray

                    data?.isJsonObject == true -> {
                        val nested = data.asJsonObject

                        val list = nested.get("items")
                            ?: nested.get("results")
                            ?: nested.get("repairRequests")

                        if (list?.isJsonArray == true) {
                            list.asJsonArray
                        } else {
                            return null
                        }
                    }

                    else -> return null
                }
            }

            else -> return null
        }

        return array.mapNotNull {
            if (it.isJsonObject) it.asJsonObject else null
        }
    }

    private fun JsonObject.textOf(
        vararg keys: String
    ): String {
        for (key in keys) {
            val value = entrySet().firstOrNull {
                it.key.equals(key, ignoreCase = true)
            }?.value

            if (value != null &&
                !value.isJsonNull &&
                value.isJsonPrimitive
            ) {
                return value.asString
            }
        }
        return ""
    }

    private fun parseRepairItem(
        item: JsonObject
    ): ManagerRepairItem? {

        val id = item.textOf(
            "repairRequestId", "id"
        ).toIntOrNull() ?: return null

        val status = item.textOf(
            "status", "repairStatus"
        )

        val customerName = item.textOf(
            "customerName"
        ).ifBlank {
            val customer = item.get("customer")
            if (customer?.isJsonObject == true) {
                customer.asJsonObject.textOf(
                    "fullName", "customerName", "name"
                )
            } else {
                ""
            }
        }

        return ManagerRepairItem(
            repairRequestId = id,
            customerName = customerName,
            description = item.textOf(
                "description", "issueDescription"
            ),
            statusCode = status,
            statusLabel = if (status.isBlank()) {
                "Chưa rõ trạng thái"
            } else {
                "Trạng thái: $status"
            },
            createdAt = item.textOf(
                "createdAt", "requestedAt"
            ).replace("T", " ").take(19)
        )
    }

    // ==========================================
    // LỌC DANH SÁCH
    // ==========================================

    private fun selectFilter(value: String) {
        filter = value

        listOf(
            binding.tabAll to "all",
            binding.tabPending to "pending",
            binding.tabProcessing to "processing",
            binding.tabCompleted to "completed"
        ).forEach { (tab, key) ->

            val selected = key == value

            tab.setTextColor(
                if (selected) caramel else inactive
            )
            tab.setTypeface(
                null,
                if (selected) Typeface.BOLD
                else Typeface.NORMAL
            )
        }

        applyFilter()
    }

    private fun statusGroup(status: String): String {
        return when (
            status.trim()
                .lowercase(Locale.ROOT)
                .replace("_", "")
                .replace(" ", "")
        ) {
            "pending", "waiting",
            "requested", "chờxửlý",
            "chờtiếpnhận" -> "pending"

            "accepted", "processing",
            "inprogress", "đangxửlý",
            "repairing" -> "processing"

            "completed", "paid",
            "hoàntất", "đãhoànthành" -> "completed"

            else -> "unknown"
        }
    }

    private fun applyFilter() {
        if (!::requestAdapter.isInitialized) return

        val filtered = if (filter == "all") {
            allItems
        } else {
            allItems.filter {
                statusGroup(it.statusCode) == filter
            }
        }

        requestAdapter.submitItems(filtered)

        binding.tvResultCount.text =
            "Danh sách yêu cầu (${filtered.size})"

        if (!hasLoaded) return

        binding.tvState.visibility =
            if (filtered.isEmpty()) View.VISIBLE else View.GONE

        binding.tvState.text =
            if (allItems.isEmpty()) {
                "Cửa hàng chưa có yêu cầu sửa chữa."
            } else if (
                filter != "all" &&
                allItems.any {
                    statusGroup(it.statusCode) == "unknown"
                }
            ) {
                "Chưa ánh xạ trạng thái số của backend. " +
                        "Xem tab Tất cả để không bỏ sót yêu cầu."
            } else {
                "Không có yêu cầu phù hợp bộ lọc."
            }
    }

    private fun showError(message: String) {
        binding.tvState.visibility = View.VISIBLE
        binding.tvState.text = message

        // Không giữ lại danh sách cũ khi request thất bại.
        allItems = emptyList()
        hasLoaded = false
        requestAdapter.submitItems(emptyList())
        binding.tvResultCount.text = "Danh sách yêu cầu"
    }

    // ==========================================
    // GET CHI TIẾT YÊU CẦU
    // ==========================================

    private fun loadRepairDetail(repairRequestId: Int) {
        val token = authorization() ?: run {
            toast("Vui lòng đăng nhập lại.")
            return
        }

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerRepairApi
                        .getRepairDetail(
                            token = token,
                            repairRequestId = repairRequestId,
                            shopId = shopId
                        )

                if (!response.isSuccessful) {
                    toast(
                        "Không tải được chi tiết: HTTP ${response.code()}"
                    )
                    return@launch
                }

                val body = response.body()

                val data = when {
                    body?.isJsonObject == true -> {
                        val root = body.asJsonObject
                        val nested = root.get("data")

                        if (nested?.isJsonObject == true) {
                            nested.asJsonObject
                        } else {
                            root
                        }
                    }

                    else -> null
                }

                if (data == null) {
                    toast("Chi tiết từ backend không hợp lệ.")
                    return@launch
                }

                showDetailDialog(repairRequestId, data)

            } catch (e: Exception) {
                toast("Lỗi tải chi tiết: ${e.localizedMessage}")
            }
        }
    }

    private fun showDetailDialog(
        repairRequestId: Int,
        data: JsonObject
    ) {
        val density = resources.displayMetrics.density
        fun dp(value: Int) = (value * density).toInt()

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(
                dp(20), dp(22), dp(20), dp(22)
            )
            setBackgroundResource(R.drawable.fixnear_dialog_bg)
        }

        container.addView(
            TextView(this).apply {
                text = "Yêu cầu #$repairRequestId"
                textSize = 21f
                setTextColor(dark)
                setTypeface(null, Typeface.BOLD)
            }
        )

        fun addDetail(label: String, value: String) {
            if (value.isBlank()) return

            container.addView(
                TextView(this).apply {
                    text = label
                    textSize = 12f
                    setTextColor(brown)
                },
                LinearLayout.LayoutParams(-1, -2).apply {
                    topMargin = dp(15)
                }
            )

            container.addView(
                TextView(this).apply {
                    text = value
                    textSize = 15f
                    setTextColor(dark)
                },
                LinearLayout.LayoutParams(-1, -2).apply {
                    topMargin = dp(4)
                }
            )
        }

        addDetail(
            "Khách hàng",
            data.textOf("customerName")
        )

        addDetail(
            "Mô tả sự cố",
            data.textOf("description", "issueDescription")
        )

        addDetail(
            "Trạng thái backend",
            data.textOf("status", "repairStatus")
        )

        addDetail(
            "Phương thức sửa chữa",
            data.textOf("repairMethod")
        )

        addDetail(
            "Thời gian tạo",
            data.textOf("createdAt").replace("T", " ")
        )

        addDetail(
            "Địa chỉ",
            data.textOf("address", "addressLine")
        )

        // Các trường chưa có nhãn UI được giữ nguyên để
        // không giấu dữ liệu backend khi kiểm thử.
        container.addView(
            TextView(this).apply {
                text = "Dữ liệu chi tiết từ backend"
                textSize = 12f
                setTextColor(brown)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(20)
            }
        )

        container.addView(
            TextView(this).apply {
                text = GsonBuilder()
                    .setPrettyPrinting()
                    .create()
                    .toJson(data)
                textSize = 11f
                setTextColor(brown)
                setTextIsSelectable(true)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(8)
            }
        )

        val dialog = AlertDialog.Builder(this)
            .setView(
                ScrollView(this).apply {
                    addView(container)
                }
            )
            .setPositiveButton("Đóng", null)
            .create()

        dialog.show()

        dialog.window?.setBackgroundDrawableResource(
            android.R.color.transparent
        )

        dialog.window?.setLayout(
            (resources.displayMetrics.widthPixels * 0.90f).toInt(),
            (resources.displayMetrics.heightPixels * 0.75f).toInt()
        )

        dialog.getButton(AlertDialog.BUTTON_POSITIVE)
            ?.setTextColor(caramel)
    }

    private fun toast(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
