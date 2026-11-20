
package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.customer.ChatConversationCreate
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerRepairDetailBinding
import com.google.gson.JsonArray
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

class CustomerRepairDetailActivity : AppCompatActivity() {

    private lateinit var binding: ActivityCustomerRepairDetailBinding

    private var repairRequestId = -1
    private var isLoading = false
    private var isOpeningChat = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityCustomerRepairDetailBinding.inflate(layoutInflater)
        setContentView(binding.root)

        repairRequestId = intent.getIntExtra(
            "REPAIR_REQUEST_ID",
            -1
        )

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.btnRefresh.setOnClickListener {
            loadDetail()
        }

        binding.btnQuotes.setOnClickListener {
            if (repairRequestId <= 0) return@setOnClickListener

            startActivity(
                Intent(
                    this,
                    CustomerQuotesActivity::class.java
                ).apply {
                    putExtra(
                        "REPAIR_REQUEST_ID",
                        repairRequestId
                    )
                }
            )
        }

        binding.btnOpenChat.setOnClickListener {
            openConversation()
        }

        binding.btnBilling.setOnClickListener {
            if (repairRequestId <= 0) return@setOnClickListener

            startActivity(
                Intent(
                    this,
                    CustomerBillingActivity::class.java
                ).apply {
                    putExtra(
                        "REPAIR_REQUEST_ID",
                        repairRequestId
                    )
                }
            )
        }

        binding.btnReview.setOnClickListener {
            if (repairRequestId <= 0) return@setOnClickListener

            startActivity(
                Intent(
                    this,
                    CustomerReviewActivity::class.java
                ).apply {
                    putExtra(
                        "REPAIR_REQUEST_ID",
                        repairRequestId
                    )
                }
            )
        }

        if (repairRequestId <= 0) {
            binding.tvState.text = "Mã yêu cầu sửa chữa không hợp lệ."
            return
        }

        loadDetail()
    }

    // ============================================
    // TOKEN JWT
    // ============================================

    private fun authorization(): String? {
        val raw = SessionManager(this)
            .getToken()
            ?.trim()
            .orEmpty()

        if (raw.isBlank()) return null

        return if (
            raw.startsWith("Bearer ", ignoreCase = true)
        ) {
            raw
        } else {
            "Bearer $raw"
        }
    }

    // ============================================
    // CHAT THẬT CỦA CUSTOMER
    // GIỮ API HIỆN TẠI CỦA ĐỒNG ĐỘI
    // ============================================

    private fun openConversation() {

        if (isOpeningChat || repairRequestId <= 0) return

        val token = authorization() ?: run {
            toast("Vui lòng đăng nhập lại.")
            return
        }

        isOpeningChat = true
        binding.btnOpenChat.isEnabled = false

        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerChatApi.create(
                    token,
                    ChatConversationCreate(repairRequestId)
                )

                val conversationId =
                    response.body()?.conversationId

                if (
                    response.isSuccessful &&
                    conversationId != null &&
                    conversationId > 0
                ) {
                    startActivity(
                        Intent(
                            this@CustomerRepairDetailActivity,
                            CustomerChatActivity::class.java
                        ).apply {
                            putExtra(
                                "CONVERSATION_ID",
                                conversationId
                            )
                        }
                    )
                } else {
                    val message = when (response.code()) {
                        401 -> "Phiên đăng nhập đã hết hạn."
                        403 -> "Bạn không có quyền mở hội thoại này."
                        404 -> "Không tìm thấy yêu cầu để tạo hội thoại."
                        else ->
                            "Chưa mở được hội thoại " +
                                    "(HTTP ${response.code()})."
                    }

                    toast(message)
                }

            } catch (e: Exception) {
                toast(
                    "Không thể kết nối Chat:\n" +
                            (e.localizedMessage ?: "Lỗi không xác định")
                )

            } finally {
                isOpeningChat = false
                binding.btnOpenChat.isEnabled = true
            }
        }
    }

    // ============================================
    // GET CHI TIẾT YÊU CẦU
    // ============================================

    private fun loadDetail() {

        if (isLoading || repairRequestId <= 0) return

        val token = authorization() ?: run {
            binding.tvState.text = "Hãy đăng nhập lại."
            return
        }

        isLoading = true

        binding.progress.visibility = View.VISIBLE
        binding.btnRefresh.isEnabled = false

        binding.tvState.text = "Đang tải chi tiết yêu cầu..."
        binding.content.removeAllViews()

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerRepairQueryApi.getDetail(
                        token,
                        repairRequestId
                    )

                if (!response.isSuccessful) {
                    binding.tvState.text = when (response.code()) {
                        401 ->
                            "Phiên đăng nhập đã hết hạn. " +
                                    "Vui lòng đăng nhập lại."

                        403 ->
                            "Bạn không có quyền xem yêu cầu này."

                        404 ->
                            "Không tìm thấy yêu cầu #$repairRequestId " +
                                    "hoặc yêu cầu không thuộc tài khoản " +
                                    "đang đăng nhập."

                        else ->
                            "Không tải được chi tiết yêu cầu. " +
                                    "HTTP ${response.code()}."
                    }

                    return@launch
                }

                val obj = response.body()

                if (obj == null) {
                    binding.tvState.text =
                        "Backend không trả về chi tiết yêu cầu."
                    return@launch
                }

                binding.tvState.text =
                    "Chi tiết yêu cầu #$repairRequestId"

                showRepairDetail(obj)

            } catch (e: Exception) {
                binding.tvState.text =
                    "Không tải được chi tiết:\n" +
                            (e.localizedMessage ?: "Lỗi không xác định")

            } finally {
                isLoading = false

                binding.progress.visibility = View.GONE
                binding.btnRefresh.isEnabled = true
            }
        }
    }

    // ============================================
    // HIỂN THỊ CHI TIẾT
    // ============================================

    private fun showRepairDetail(obj: JsonObject) {

        val status = RepairUi.number(obj, "status")
        val repairMethod = RepairUi.number(
            obj,
            "repairMethod"
        )

        val shopName = RepairUi.field(
            obj,
            "shopName"
        ).ifBlank {
            "Chưa có thông tin cửa hàng"
        }

        val description = RepairUi.field(
            obj,
            "description"
        ).ifBlank {
            "Khách hàng chưa cung cấp mô tả."
        }

        val methodText = when (repairMethod) {
            1 -> "Sửa tại nhà"
            2 -> "Mang đến cửa hàng"
            else -> "Chưa xác định"
        }

        val createdAt = cleanDateTime(
            RepairUi.field(obj, "createdAt")
        )

        val general = mutableListOf(
            "Cửa hàng: $shopName",
            "Trạng thái: ${RepairUi.status(status)}",
            "Phương thức: $methodText",
            "Mô tả: $description"
        )

        val preferredDate = cleanDate(
            RepairUi.field(obj, "preferredDate")
        )

        val fromTime = cleanTime(
            RepairUi.field(obj, "preferredTimeFrom")
        )

        val toTime = cleanTime(
            RepairUi.field(obj, "preferredTimeTo")
        )

        // Chỉ hiện ngày mong muốn khi khách có chọn.
        if (preferredDate.isNotBlank()) {
            general.add("Ngày mong muốn: $preferredDate")
        }

        // Không hiện "Khung giờ: –" khi dữ liệu null.
        if (fromTime.isNotBlank() && toTime.isNotBlank()) {
            general.add("Khung giờ: $fromTime – $toTime")
        } else if (fromTime.isNotBlank()) {
            general.add("Bắt đầu từ: $fromTime")
        } else if (toTime.isNotBlank()) {
            general.add("Trước: $toTime")
        }

        if (createdAt.isNotBlank()) {
            general.add("Ngày tạo: $createdAt")
        }

        addSection(
            "Thông tin yêu cầu",
            general
        )

        // ========================================
        // ĐỊA CHỈ
        // ========================================

        val address = RepairUi.obj(
            obj,
            "address"
        )

        if (address != null) {
            val addressParts = listOf(
                RepairUi.field(address, "addressLine"),
                RepairUi.field(address, "wardName"),
                RepairUi.field(address, "provinceName")
            ).filter {
                it.isNotBlank() && !it.equals("null", true)
            }

            val addressText = addressParts
                .distinct()
                .joinToString(", ")

            if (addressText.isNotBlank()) {
                addSection(
                    "Địa chỉ sửa chữa",
                    listOf(addressText)
                )
            }
        }

        // ========================================
        // DỊCH VỤ YÊU CẦU
        // ========================================

        val serviceLines = lines(
            jsonArray(obj, "services")
        ) { item ->

            val name = RepairUi.field(
                item,
                "serviceName"
            ).ifBlank {
                "Dịch vụ sửa chữa"
            }

            val detail = RepairUi.field(
                item,
                "description"
            )

            if (detail.isNotBlank()) {
                "• $name\n  $detail"
            } else {
                "• $name"
            }
        }

        if (serviceLines.isNotEmpty()) {
            addSection(
                "Dịch vụ yêu cầu",
                serviceLines
            )
        }

        // ========================================
        // LỊCH ĐÃ XẾP
        // ========================================

        val scheduleLines = lines(
            jsonArray(obj, "schedules")
        ) { item ->

            val start = cleanDateTime(
                RepairUi.field(item, "startTime")
            )

            val end = cleanDateTime(
                RepairUi.field(item, "endTime")
            )

            val technician = RepairUi.field(
                item,
                "technicianName"
            )

            buildString {
                append("• ")

                append(
                    start.ifBlank {
                        "Chưa có thời gian bắt đầu"
                    }
                )

                if (end.isNotBlank()) {
                    append(" – $end")
                }

                if (technician.isNotBlank()) {
                    append("\n  Kỹ thuật viên: $technician")
                }
            }
        }

        if (scheduleLines.isNotEmpty()) {
            addSection(
                "Lịch đã xếp",
                scheduleLines
            )
        }

        // ========================================
        // LỊCH SỬ TRẠNG THÁI
        // ========================================

        val historyArray =
            jsonArray(obj, "statusHistory")
                ?: jsonArray(obj, "history")

        val historyLines = lines(
            historyArray
        ) { item ->

            val date = cleanDateTime(
                RepairUi.field(item, "createdAt")
            )

            val oldStatus = statusOrEmpty(
                item,
                "oldStatus"
            )

            val newStatus = statusOrEmpty(
                item,
                "newStatus"
            )

            val reason = RepairUi.field(
                item,
                "reason"
            )

            buildString {

                append("• ")

                if (date.isNotBlank()) {
                    append("$date: ")
                }

                if (oldStatus.isNotBlank() &&
                    newStatus.isNotBlank()
                ) {
                    append("$oldStatus → $newStatus")
                } else if (newStatus.isNotBlank()) {
                    append(newStatus)
                } else {
                    append("Cập nhật trạng thái")
                }

                if (reason.isNotBlank()) {
                    append("\n  $reason")
                }
            }
        }

        if (historyLines.isNotEmpty()) {
            addSection(
                "Lịch sử trạng thái",
                historyLines
            )
        }

        // ========================================
        // HÌNH ẢNH
        // ========================================

        val images = jsonArray(obj, "images")

        if (images != null && images.size() > 0) {
            addSection(
                "Ảnh đính kèm",
                listOf(
                    "Yêu cầu có ${images.size()} hình ảnh."
                )
            )
        }

        // ========================================
        // NÚT BÁO GIÁ
        // ========================================

        binding.btnQuotes.text = "Xem báo giá của yêu cầu"
        binding.btnQuotes.isEnabled = true
    }

    // ============================================
    // HELPER
    // ============================================

    private fun cleanDate(value: String): String {
        if (
            value.isBlank() ||
            value.equals("null", true)
        ) {
            return ""
        }

        return value.take(10)
    }

    private fun cleanTime(value: String): String {
        if (
            value.isBlank() ||
            value.equals("null", true)
        ) {
            return ""
        }

        return value.take(5)
    }

    private fun cleanDateTime(value: String): String {
        if (
            value.isBlank() ||
            value.equals("null", true)
        ) {
            return ""
        }

        val formatted = RepairUi.date(value)

        return if (
            formatted.isBlank() ||
            formatted.equals("null", true)
        ) {
            ""
        } else {
            formatted
        }
    }

    private fun jsonArray(
        obj: JsonObject,
        key: String
    ): JsonArray? {
        val element: JsonElement = obj.get(key)
            ?: return null

        return if (element.isJsonArray) {
            element.asJsonArray
        } else {
            null
        }
    }

    private fun statusOrEmpty(
        obj: JsonObject,
        key: String
    ): String {
        val value = obj.get(key)

        if (value == null || value.isJsonNull) {
            return ""
        }

        val code = try {
            value.asInt
        } catch (_: Exception) {
            null
        }

        return if (code != null) {
            RepairUi.status(code)
        } else {
            ""
        }
    }

    private fun lines(
        arr: JsonArray?,
        mapping: (JsonObject) -> String
    ): List<String> {

        if (arr == null) return emptyList()

        return arr.mapNotNull { element ->
            if (element.isJsonObject) {
                mapping(element.asJsonObject)
            } else {
                null
            }
        }.filter {
            it.isNotBlank()
        }
    }

    private fun addSection(
        title: String,
        lines: List<String>
    ) {
        if (lines.isEmpty()) return

        val body = RepairUi.column(this)

        RepairUi.addLine(
            this,
            body,
            title,
            17f,
            RepairUi.GOLD,
            true
        )

        lines.forEach { line ->
            RepairUi.addLine(
                this,
                body,
                line,
                14f
            )
        }

        binding.content.addView(
            RepairUi.card(this, body)
        )
    }

    private fun toast(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
