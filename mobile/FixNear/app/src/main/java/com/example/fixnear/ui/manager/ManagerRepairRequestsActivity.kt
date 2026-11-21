
package com.example.fixnear.ui.manager

import android.content.Intent
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
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

class ManagerRepairRequestsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityManagerRepairRequestsBinding
    private lateinit var requestAdapter: ManagerRepairAdapter

    private var shopId = -1
    private var filter = "all"

    private var allItems = emptyList<ManagerRepairItem>()
    private var isLoading = false
    private var hasLoaded = false
    private var hasResumedOnce = false

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

        setupRecyclerView()
        setupFilters()
        setupRefresh()

        // Nhận bộ lọc từ ManagerHomeActivity.
        val initialFilter = intent
            .getStringExtra("INITIAL_FILTER")
            ?.lowercase()
            .orEmpty()

        selectFilter(
            when (initialFilter) {
                "pending" -> "pending"
                "processing" -> "processing"
                "completed" -> "completed"
                else -> "all"
            }
        )

        loadRepairRequests()
    }

    override fun onResume() {
        super.onResume()

        // Không GET hai lần khi Activity vừa được tạo.
        // Khi quay lại từ màn chi tiết/phân công sẽ GET mới.
        if (hasResumedOnce) {
            loadRepairRequests()
        } else {
            hasResumedOnce = true
        }
    }

    private fun setupRecyclerView() {

        requestAdapter = ManagerRepairAdapter { item ->

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

        binding.rvRepairRequests.apply {
            layoutManager = LinearLayoutManager(
                this@ManagerRepairRequestsActivity
            )

            adapter = requestAdapter
        }
    }

    private fun setupFilters() {

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
    }

    private fun setupRefresh() {

        binding.btnRefresh.setOnClickListener {
            loadRepairRequests()
        }
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

    // ============================================
    // GET /api/manager/repair-requests?shopId=...
    // ============================================

    private fun loadRepairRequests() {

        if (isLoading) return

        if (shopId <= 0) {
            showError(
                "ShopId không hợp lệ. Hãy mở từ Manager Home."
            )
            return
        }

        val token = authorization()

        if (token == null) {
            showError(
                "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại."
            )
            return
        }

        isLoading = true

        binding.progressLoading.visibility = View.VISIBLE
        binding.tvState.visibility = View.VISIBLE

        binding.tvState.text =
            "Đang tải yêu cầu sửa chữa..."

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

                    val message = when (response.code()) {

                        401 ->
                            "Phiên đăng nhập hết hạn."

                        403 ->
                            "Tài khoản không có quyền xem yêu cầu của Shop này."

                        404 ->
                            "Không tìm thấy cửa hàng."

                        else ->
                            "Không tải được yêu cầu sửa chữa. " +
                                    "HTTP ${response.code()}"
                    }

                    showError(message)
                    return@launch
                }

                val items = extractArray(response.body())

                if (items == null) {
                    showError(
                        "Backend trả về danh sách không hợp lệ."
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
                    "Không thể kết nối backend:\n" +
                            (e.localizedMessage ?: "Lỗi không xác định")
                )

            } finally {

                isLoading = false

                binding.progressLoading.visibility =
                    View.GONE

                binding.btnRefresh.isEnabled = true
            }
        }
    }

    // ============================================
    // ĐỌC RESPONSE { message, data }
    // ============================================

    private fun extractArray(
        body: JsonElement?
    ): List<JsonObject>? {

        if (body == null || body.isJsonNull) {
            return null
        }

        val array = when {

            body.isJsonArray -> {
                body.asJsonArray
            }

            body.isJsonObject -> {

                val root = body.asJsonObject
                val data = root.get("data")

                when {

                    data?.isJsonArray == true -> {
                        data.asJsonArray
                    }

                    data?.isJsonObject == true -> {

                        val nested = data.asJsonObject

                        val list =
                            nested.get("items")
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

        return array.mapNotNull { element ->

            if (element.isJsonObject) {
                element.asJsonObject
            } else {
                null
            }
        }
    }

    // ============================================
    // HELPER LẤY GIÁ TRỊ JSON
    // ============================================

    private fun JsonObject.textOf(
        vararg keys: String
    ): String {

        for (key in keys) {

            val value = entrySet()
                .firstOrNull { entry ->
                    entry.key.equals(
                        key,
                        ignoreCase = true
                    )
                }
                ?.value

            if (
                value != null &&
                !value.isJsonNull &&
                value.isJsonPrimitive
            ) {
                return value.asString
            }
        }

        return ""
    }

    private fun JsonObject.customerNameText(): String {

        val directName = textOf(
            "customerName",
            "customerFullName"
        )

        if (directName.isNotBlank()) {
            return directName
        }

        val customer = get("customer")

        if (customer?.isJsonObject == true) {

            val nestedName = customer.asJsonObject.textOf(
                "fullName",
                "customerName",
                "name"
            )

            if (nestedName.isNotBlank()) {
                return nestedName
            }
        }

        val customerId = textOf("customerId")

        return if (customerId.isNotBlank()) {
            "Khách hàng #$customerId"
        } else {
            "Chưa có thông tin"
        }
    }

    private fun statusText(status: String): String {

        return when (status.trim().toIntOrNull()) {

            1 -> "Chờ tiếp nhận"
            2 -> "Đã tiếp nhận"
            3 -> "Đã từ chối"
            4 -> "Đã phân công"
            5 -> "Đang khảo sát"
            6 -> "Đã báo giá"
            7 -> "Khách đã duyệt giá"
            8 -> "Đang sửa chữa"
            9 -> "Đã sửa xong"
            10 -> "Đã thanh toán"
            11 -> "Đã hủy"

            else -> "Chưa xác định trạng thái"
        }
    }

    private fun formatDate(value: String): String {

        if (value.isBlank()) {
            return ""
        }

        return value
            .replace("T", " ")
            .substringBefore(".")
            .take(19)
    }

    // ============================================
    // MAP BACKEND -> MODEL RECYCLERVIEW
    // ============================================

    private fun parseRepairItem(
        item: JsonObject
    ): ManagerRepairItem? {

        val id = item.textOf(
            "repairRequestId",
            "id"
        ).toIntOrNull() ?: return null

        if (id <= 0) return null

        val status = item.textOf(
            "status",
            "repairStatus"
        )

        val customerName = item.customerNameText()

        val description = item.textOf(
            "description",
            "issueDescription"
        ).ifBlank {
            "Chưa có mô tả sự cố"
        }

        return ManagerRepairItem(
            repairRequestId = id,
            customerName = customerName,
            description = description,
            statusCode = status,
            statusLabel = statusText(status),
            createdAt = formatDate(
                item.textOf(
                    "createdAt",
                    "requestedAt"
                )
            )
        )
    }

    // ============================================
    // FILTER
    // ============================================

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
                if (selected) {
                    Typeface.BOLD
                } else {
                    Typeface.NORMAL
                }
            )
        }

        applyFilter()
    }

    private fun statusGroup(status: String): String {

        return when (
            status.trim().toIntOrNull()
        ) {

            1 -> "pending"

            2, 4, 5, 6, 7, 8 -> "processing"

            9, 10 -> "completed"

            // Từ chối và hủy vẫn hiển thị ở "Tất cả".
            3, 11 -> "closed"

            else -> "unknown"
        }
    }

    private fun applyFilter() {

        if (!::requestAdapter.isInitialized) {
            return
        }

        val filtered = if (filter == "all") {

            allItems

        } else {

            allItems.filter { item ->
                statusGroup(item.statusCode) == filter
            }
        }

        requestAdapter.submitItems(filtered)

        binding.tvResultCount.text =
            "Danh sách yêu cầu (${filtered.size})"

        if (!hasLoaded) {
            return
        }

        binding.tvState.visibility =
            if (filtered.isEmpty()) {
                View.VISIBLE
            } else {
                View.GONE
            }

        binding.tvState.text = when {

            allItems.isEmpty() ->
                "Cửa hàng chưa có yêu cầu sửa chữa."

            filtered.isNotEmpty() ->
                ""

            filter != "all" &&
                    allItems.any {
                        statusGroup(it.statusCode) == "unknown"
                    } ->
                "Có trạng thái chưa được nhận diện. " +
                        "Hãy mở tab Tất cả để xem."

            else ->
                "Không có yêu cầu phù hợp bộ lọc."
        }
    }

    private fun showError(message: String) {

        binding.tvState.visibility = View.VISIBLE
        binding.tvState.text = message

        allItems = emptyList()
        hasLoaded = false

        if (::requestAdapter.isInitialized) {
            requestAdapter.submitItems(emptyList())
        }

        binding.tvResultCount.text =
            "Danh sách yêu cầu (0)"
    }

    // ============================================
    // GET CHI TIẾT - GIỮ LẠI NẾU CẦN POPUP
    // ============================================

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
                        "Không tải được chi tiết: " +
                                "HTTP ${response.code()}"
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

                    toast(
                        "Chi tiết từ backend không hợp lệ."
                    )

                    return@launch
                }

                // Không cần parse response thêm lần nữa.
                showDetailDialog(repairRequestId, data)

            } catch (e: Exception) {

                toast(
                    "Lỗi tải chi tiết: " +
                            (e.localizedMessage ?: "Không xác định")
                )
            }
        }
    }

    // ============================================
    // POPUP CHI TIẾT - KHÔNG HIỂN THỊ JSON THÔ
    // ============================================

    private fun showDetailDialog(
        repairRequestId: Int,
        data: JsonObject
    ) {

        val density = resources.displayMetrics.density

        fun dp(value: Int): Int {
            return (value * density).toInt()
        }

        val container = LinearLayout(this).apply {

            orientation = LinearLayout.VERTICAL

            setPadding(
                dp(20),
                dp(22),
                dp(20),
                dp(22)
            )

            setBackgroundResource(
                R.drawable.fixnear_dialog_bg
            )
        }

        container.addView(
            TextView(this).apply {

                text = "Yêu cầu #$repairRequestId"
                textSize = 21f
                setTextColor(dark)

                setTypeface(
                    null,
                    Typeface.BOLD
                )
            }
        )

        fun addDetail(label: String, value: String) {

            if (value.isBlank()) {
                return
            }

            container.addView(
                TextView(this).apply {

                    text = label
                    textSize = 12f
                    setTextColor(brown)

                },
                LinearLayout.LayoutParams(
                    -1,
                    -2
                ).apply {
                    topMargin = dp(15)
                }
            )

            container.addView(
                TextView(this).apply {

                    text = value
                    textSize = 15f
                    setTextColor(dark)

                },
                LinearLayout.LayoutParams(
                    -1,
                    -2
                ).apply {
                    topMargin = dp(4)
                }
            )
        }

        // Tên khách hàng có fallback theo CustomerId.
        addDetail(
            "Khách hàng",
            data.customerNameText()
        )

        addDetail(
            "Mô tả sự cố",
            data.textOf(
                "description",
                "issueDescription"
            ).ifBlank {
                "Chưa có mô tả sự cố"
            }
        )

        val status = data.textOf(
            "status",
            "repairStatus"
        )

        addDetail(
            "Trạng thái",
            statusText(status)
        )

        val repairMethod = data.textOf(
            "repairMethod"
        )

        if (repairMethod.isNotBlank()) {
            addDetail(
                "Phương thức sửa chữa",
                "Phương thức $repairMethod"
            )
        }

        addDetail(
            "Thời gian tạo",
            formatDate(
                data.textOf("createdAt")
            )
        )

        val address = data.textOf(
            "address",
            "addressLine"
        )

        if (address.isNotBlank()) {
            addDetail("Địa chỉ", address)
        }

        val serviceCount =
            data.get("services")
                ?.takeIf { it.isJsonArray }
                ?.asJsonArray
                ?.size()

        if (serviceCount != null) {
            addDetail(
                "Dịch vụ yêu cầu",
                "$serviceCount dịch vụ"
            )
        }

        val imageCount =
            data.get("images")
                ?.takeIf { it.isJsonArray }
                ?.asJsonArray
                ?.size()

        if (imageCount != null) {
            addDetail(
                "Hình ảnh",
                "$imageCount hình ảnh"
            )
        }

        // Không còn GsonBuilder.toJson(data).
        // Không hiển thị JSON debug.

        val dialog = AlertDialog.Builder(this)
            .setView(
                ScrollView(this).apply {
                    isFillViewport = true
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

        dialog.getButton(
            AlertDialog.BUTTON_POSITIVE
        )?.setTextColor(caramel)
    }

    private fun toast(message: String) {

        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
