package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerQuotesBinding
import com.google.gson.JsonObject
import kotlinx.coroutines.launch
import java.text.NumberFormat
import java.util.Locale

class CustomerQuotesActivity : AppCompatActivity() {
    private lateinit var binding: ActivityCustomerQuotesBinding
    private var repairId = -1
    private var page = 0
    private var totalPages = 1
    private var loading = false
    private val currency = NumberFormat.getNumberInstance(Locale("vi", "VN"))

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityCustomerQuotesBinding.inflate(layoutInflater)
        setContentView(binding.root)
        repairId = intent.getIntExtra("REPAIR_REQUEST_ID", -1)
        binding.btnBack.setOnClickListener { finish() }
        binding.btnRefresh.setOnClickListener { refresh() }
        binding.btnLoadMore.setOnClickListener { load(page + 1) }
    }

    override fun onResume() {
        super.onResume()
        refresh()
    }

    private fun auth(): String? = SessionManager(this).getToken()?.takeIf { it.isNotBlank() }
        ?.let { if (it.startsWith("Bearer ", true)) it else "Bearer $it" }

    private fun refresh() {
        if (loading) return
        page = 0
        totalPages = 1
        binding.content.removeAllViews()
        load(1)
    }

    private fun load(next: Int) {
        if (loading || next > totalPages) return
        if (repairId <= 0) { binding.tvState.text = "Mã yêu cầu không hợp lệ"; return }
        val token = auth() ?: run { binding.tvState.text = "Hãy đăng nhập lại"; return }
        loading = true
        binding.progress.visibility = View.VISIBLE
        binding.btnLoadMore.visibility = View.GONE
        binding.tvState.text = "Đang tải báo giá..."
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerQuotesApi.list(token, repairId, next, 10)
                if (!response.isSuccessful) {
                    binding.tvState.text = "Không tải được báo giá (HTTP ${response.code()})"
                    return@launch
                }
                val root = response.body() ?: return@launch
                page = RepairUi.number(root, "page", next)
                totalPages = RepairUi.number(root, "totalPages", 1)
                val total = RepairUi.number(root, "total")
                root.getAsJsonArray("data")?.forEach { item ->
                    if (item.isJsonObject) addQuote(item.asJsonObject)
                }
                binding.tvState.text = if (total == 0) "Chưa có báo giá được gửi." else "Có $total báo giá • trang $page/$totalPages"
            } catch (e: Exception) {
                binding.tvState.text = "Lỗi kết nối: ${e.localizedMessage}"
            } finally {
                loading = false
                binding.progress.visibility = View.GONE
                binding.btnLoadMore.visibility = if (page < totalPages) View.VISIBLE else View.GONE
            }
        }
    }

    private fun addQuote(obj: JsonObject) {
        val id = RepairUi.number(obj, "quoteId")
        val status = RepairUi.number(obj, "status")
        val supplemental = obj.get("isSupplemental")?.asBoolean == true
        val total = obj.get("totalAmount")?.asDouble ?: 0.0
        val content = RepairUi.column(this)
        RepairUi.addLine(this, content, "${RepairUi.field(obj, "quoteNumber")} • #$id", 16f, RepairUi.WHITE, true)
        RepairUi.addLine(this, content, if (supplemental) "Báo giá phát sinh" else "Báo giá ban đầu", 13f, RepairUi.MUTED)
        RepairUi.addLine(this, content, "${currency.format(total)} đ", 19f, RepairUi.GOLD, true)
        RepairUi.addLine(this, content, QuoteUi.status(status), 13f)
        val card = RepairUi.card(this, content)
        card.setOnClickListener {
            startActivity(Intent(this, CustomerQuoteDetailActivity::class.java).apply {
                putExtra("QUOTE_ID", id)
            })
        }
        binding.content.addView(card)
    }
}

internal object QuoteUi {
    fun status(status: Int) = when (status) {
        2 -> "Đã gửi • Chờ phản hồi"
        3 -> "Đã chấp nhận"
        4 -> "Đã từ chối"
        else -> "Trạng thái $status"
    }
}
