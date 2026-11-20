package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerRepairRequestsBinding
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

class CustomerRepairRequestsActivity : AppCompatActivity() {
    private lateinit var binding: ActivityCustomerRepairRequestsBinding
    private var page = 0
    private var totalPages = 1
    private var loading = false
    private var selectedStatus: Int? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityCustomerRepairRequestsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.btnBack.setOnClickListener { finish() }
        binding.btnUpcoming.setOnClickListener {
            startActivity(Intent(this, CustomerUpcomingSchedulesActivity::class.java))
        }
        binding.btnRefresh.setOnClickListener { refresh() }
        binding.btnLoadMore.setOnClickListener { load(page + 1) }
        binding.spStatus.adapter = object : android.widget.ArrayAdapter<String>(this, android.R.layout.simple_spinner_item,
            listOf("Tất cả trạng thái", "Chờ phản hồi", "Đã tiếp nhận", "Đã phân công", "Đang khảo sát", "Đã báo giá", "Đang sửa", "Đã hoàn tất", "Đã thanh toán", "Đã hủy", "Shop từ chối")) {
            override fun getView(position: Int, convertView: View?, parent: android.view.ViewGroup): View =
                label(position, false)
            override fun getDropDownView(position: Int, convertView: View?, parent: android.view.ViewGroup): View =
                label(position, true)
            private fun label(pos: Int, dropdown: Boolean) = RepairUi.text(this@CustomerRepairRequestsActivity, getItem(pos).orEmpty(), 14f,
                if (dropdown) android.graphics.Color.parseColor("#34251C") else RepairUi.WHITE).apply {
                setPadding(RepairUi.dp(context, 12), RepairUi.dp(context, 15), RepairUi.dp(context, 12), RepairUi.dp(context, 15))
                if (dropdown) setBackgroundColor(android.graphics.Color.parseColor("#FFF8F1"))
            }
        }
        val values = arrayOf<Int?>(null, 1, 2, 4, 5, 6, 8, 9, 10, 11, 3)
        binding.spStatus.onItemSelectedListener = object : android.widget.AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: android.widget.AdapterView<*>?, view: View?, position: Int, id: Long) {
                val chosen = values[position]
                if (chosen != selectedStatus) { selectedStatus = chosen; refresh() }
            }
            override fun onNothingSelected(parent: android.widget.AdapterView<*>?) = Unit
        }
        refresh()
    }

    private fun auth(): String? {
        val token = SessionManager(this).getToken()?.takeIf { it.isNotBlank() } ?: return null
        return if (token.startsWith("Bearer ", true)) token else "Bearer $token"
    }
    private fun refresh() {
        if (loading) return
        page = 0
        totalPages = 1
        binding.requestsContainer.removeAllViews()
        load(1)
    }
    private fun load(next: Int) {
        if (loading || next > totalPages) return
        val token = auth() ?: run { binding.tvState.text = "Hãy đăng nhập để xem yêu cầu."; return }
        loading = true
        binding.progress.visibility = View.VISIBLE
        binding.btnLoadMore.visibility = View.GONE
        binding.tvState.text = "Đang tải yêu cầu..."
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerRepairQueryApi.getRequests(token, next, 10, selectedStatus)
                if (!response.isSuccessful) {
                    binding.tvState.text = "Lỗi HTTP ${response.code()}. Kiểm tra phiên đăng nhập và kết nối Backend."
                    return@launch
                }
                val root = response.body() ?: return@launch
                val items = root.getAsJsonArray("data")
                page = RepairUi.number(root, "page", next)
                totalPages = RepairUi.number(root, "totalPages", 1)
                val total = RepairUi.number(root, "total", 0)
                items?.forEach { element ->
                    if (element.isJsonObject) addCard(element.asJsonObject)
                }
                binding.tvState.text = if (total == 0) "Chưa có yêu cầu phù hợp." else "Có $total yêu cầu • trang $page/$totalPages"
            } catch (e: Exception) {
                binding.tvState.text = "Không tải được yêu cầu: ${e.localizedMessage}"
            } finally {
                loading = false
                binding.progress.visibility = View.GONE
                binding.btnLoadMore.visibility = if (page < totalPages) View.VISIBLE else View.GONE
            }
        }
    }
    private fun addCard(obj: JsonObject) {
        val id = RepairUi.number(obj, "repairRequestId")
        val content = RepairUi.column(this)
        RepairUi.addLine(this, content, "#${id}  •  ${RepairUi.field(obj, "shopName")}", 16f, RepairUi.WHITE, true)
        RepairUi.addLine(this, content, RepairUi.status(RepairUi.number(obj, "status")), 13f, RepairUi.GOLD, true)
        val services = obj.getAsJsonArray("services")?.mapNotNull { if (it.isJsonObject) RepairUi.field(it.asJsonObject, "serviceName") else null }.orEmpty()
        RepairUi.addLine(this, content, services.joinToString(" • ").ifBlank { "Dịch vụ sửa chữa" }, 13f, RepairUi.MUTED)
        RepairUi.addLine(this, content, RepairUi.field(obj, "description").take(150), 13f)
        RepairUi.addLine(this, content, "Ngày hẹn: ${RepairUi.field(obj, "preferredDate").take(10)} • ${RepairUi.field(obj, "preferredTimeFrom").take(5)}–${RepairUi.field(obj, "preferredTimeTo").take(5)}", 12f, RepairUi.MUTED)
        val card = RepairUi.card(this, content)
        card.setOnClickListener {
            startActivity(Intent(this, CustomerRepairDetailActivity::class.java).putExtra("REPAIR_REQUEST_ID", id))
        }
        binding.requestsContainer.addView(card)
    }
}
