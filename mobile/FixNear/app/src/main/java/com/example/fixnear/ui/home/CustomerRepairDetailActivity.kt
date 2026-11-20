package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerRepairDetailBinding
import com.google.gson.JsonArray
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

class CustomerRepairDetailActivity : AppCompatActivity() {
    private lateinit var binding: ActivityCustomerRepairDetailBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityCustomerRepairDetailBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.btnBack.setOnClickListener { finish() }
        val id = intent.getIntExtra("REPAIR_REQUEST_ID", -1)
        if (id <= 0) { binding.tvState.text = "Mã yêu cầu không hợp lệ"; return }
        binding.btnRefresh.setOnClickListener { load(id) }
        binding.btnQuotes.setOnClickListener {
            startActivity(Intent(this, CustomerQuotesActivity::class.java)
                .putExtra("REPAIR_REQUEST_ID", id))
        }
        binding.btnOpenChat.setOnClickListener { openConversation(id) }
        binding.btnBilling.setOnClickListener {
            startActivity(Intent(this, CustomerBillingActivity::class.java)
                .putExtra("REPAIR_REQUEST_ID", id))
        }
        binding.btnReview.setOnClickListener {
            startActivity(Intent(this, CustomerReviewActivity::class.java)
                .putExtra("REPAIR_REQUEST_ID", id))
        }

        load(id)
    }
    private fun openConversation(repairRequestId: Int) {
        val token = ChatUi.token(this) ?: run {
            Toast.makeText(this, "Vui lòng đăng nhập lại", Toast.LENGTH_SHORT).show()
            return
        }
        binding.btnOpenChat.isEnabled = false
        lifecycleScope.launch {
            try {
                val result = RetrofitClient.customerChatApi.create(
                    token, com.example.fixnear.data.model.customer.ChatConversationCreate(repairRequestId)
                )
                val conversationId = result.body()?.conversationId
                if (result.isSuccessful && conversationId != null) {
                    startActivity(Intent(this@CustomerRepairDetailActivity, CustomerChatActivity::class.java)
                        .putExtra("CONVERSATION_ID", conversationId))
                } else {
                    Toast.makeText(this@CustomerRepairDetailActivity,
                        "Chưa mở được hội thoại (HTTP ${result.code()})", Toast.LENGTH_LONG).show()
                }
            } catch (e: Exception) {
                Toast.makeText(this@CustomerRepairDetailActivity,
                    "Lỗi kết nối: ${e.localizedMessage}", Toast.LENGTH_LONG).show()
            } finally { binding.btnOpenChat.isEnabled = true }
        }
    }
    private fun load(id: Int) {
        val raw = SessionManager(this).getToken()
        if (raw.isNullOrBlank()) { binding.tvState.text = "Hãy đăng nhập lại."; return }
        val token = if (raw.startsWith("Bearer ", true)) raw else "Bearer $raw"
        binding.progress.visibility = View.VISIBLE
        binding.tvState.text = "Đang tải chi tiết..."
        binding.content.removeAllViews()
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerRepairQueryApi.getDetail(token, id)
                if (!response.isSuccessful) { binding.tvState.text = "Lỗi HTTP ${response.code()}"; return@launch }
                val obj = response.body() ?: return@launch
                binding.tvState.text = "Chi tiết yêu cầu #$id"
                addSection("Thông tin yêu cầu", listOf(
                    "Cửa hàng: ${RepairUi.field(obj, "shopName")}",
                    "Trạng thái: ${RepairUi.status(RepairUi.number(obj, "status"))}",
                    "Phương thức: ${if (RepairUi.number(obj, "repairMethod") == 1) "Sửa tại nhà" else "Mang đến cửa hàng"}",
                    "Mô tả: ${RepairUi.field(obj, "description")}",
                    "Ngày mong muốn: ${RepairUi.field(obj, "preferredDate").take(10)}",
                    "Khung giờ: ${RepairUi.field(obj, "preferredTimeFrom").take(5)} – ${RepairUi.field(obj, "preferredTimeTo").take(5)}",
                    "Ngày tạo: ${RepairUi.date(RepairUi.field(obj, "createdAt"))}"
                ))
                val address = RepairUi.obj(obj, "address")
                if (address != null) addSection("Địa chỉ sửa chữa", listOf(
                    listOf("addressLine", "wardName", "provinceName").map { RepairUi.field(address, it) }.filter { it.isNotBlank() }.joinToString(", ")
                ))
                addSection("Dịch vụ yêu cầu", lines(obj.getAsJsonArray("services")) { item ->
                    "• ${RepairUi.field(item, "serviceName")}" + (RepairUi.field(item, "description").takeIf { it.isNotBlank() }?.let { " — $it" } ?: "")
                })
                addSection("Lịch đã xếp", lines(obj.getAsJsonArray("schedules")) { item ->
                    "• ${RepairUi.date(RepairUi.field(item, "startTime"))} — ${RepairUi.date(RepairUi.field(item, "endTime"))}\n  KTV: ${RepairUi.field(item, "technicianName")}"
                })
                addSection("Lịch sử trạng thái", lines(obj.getAsJsonArray("statusHistory")) { item ->
                    "• ${RepairUi.date(RepairUi.field(item, "createdAt"))}: ${RepairUi.status(RepairUi.number(item, "oldStatus"))} → ${RepairUi.status(RepairUi.number(item, "newStatus"))}\n  ${RepairUi.field(item, "reason")}"
                })
                val images = obj.getAsJsonArray("images")
                if (images != null && images.size() > 0) addSection("Ảnh đính kèm", listOf("Có ${images.size()} ảnh trong yêu cầu. Chức năng xem ảnh sẽ được triển khai tiếp."))
            } catch (e: Exception) {
                binding.tvState.text = "Không tải được chi tiết: ${e.localizedMessage}"
            } finally { binding.progress.visibility = View.GONE }
        }
    }
    private fun lines(arr: JsonArray?, mapping: (JsonObject) -> String): List<String> =
        arr?.mapNotNull { if (it.isJsonObject) mapping(it.asJsonObject) else null }.orEmpty().ifEmpty { listOf("Chưa có dữ liệu") }
    private fun addSection(title: String, lines: List<String>) {
        val body = RepairUi.column(this)
        RepairUi.addLine(this, body, title, 17f, RepairUi.GOLD, true)
        lines.forEach { RepairUi.addLine(this, body, it, 14f) }
        binding.content.addView(RepairUi.card(this, body))
    }
}
