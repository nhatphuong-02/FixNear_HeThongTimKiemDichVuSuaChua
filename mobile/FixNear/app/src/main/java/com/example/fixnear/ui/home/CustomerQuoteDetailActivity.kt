package com.example.fixnear.ui.home

import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerQuoteDetailBinding
import com.google.gson.JsonObject
import kotlinx.coroutines.launch
import java.text.NumberFormat
import java.time.OffsetDateTime
import java.time.Instant
import java.time.LocalDateTime
import java.time.ZoneOffset
import java.util.Locale

class CustomerQuoteDetailActivity : AppCompatActivity() {
    private lateinit var binding: ActivityCustomerQuoteDetailBinding
    private var quoteId = -1
    private var busy = false
    private val currency = NumberFormat.getNumberInstance(Locale("vi", "VN"))

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityCustomerQuoteDetailBinding.inflate(layoutInflater)
        setContentView(binding.root)
        quoteId = intent.getIntExtra("QUOTE_ID", -1)
        binding.btnBack.setOnClickListener { finish() }
        binding.btnRefresh.setOnClickListener { load() }
        binding.btnAccept.setOnClickListener { confirm(true) }
        binding.btnReject.setOnClickListener { confirm(false) }
        load()
    }

    private fun auth(): String? = SessionManager(this).getToken()?.takeIf { it.isNotBlank() }
        ?.let { if (it.startsWith("Bearer ", true)) it else "Bearer $it" }

    private fun load() {
        if (busy) return
        if (quoteId <= 0) { binding.tvState.text = "Báo giá không hợp lệ"; return }
        val token = auth() ?: run { binding.tvState.text = "Hãy đăng nhập lại"; return }
        busy = true
        binding.progress.visibility = View.VISIBLE
        binding.btnAccept.visibility = View.GONE
        binding.btnReject.visibility = View.GONE
        binding.content.removeAllViews()
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerQuotesApi.detail(token, quoteId)
                if (!response.isSuccessful) {
                    binding.tvState.text = "Không xem được báo giá (HTTP ${response.code()})."
                    return@launch
                }
                val quote = RepairUi.obj(response.body(), "data") ?: run {
                    binding.tvState.text = "Dữ liệu báo giá không hợp lệ"
                    return@launch
                }
                render(quote)
            } catch (e: Exception) {
                binding.tvState.text = "Lỗi tải báo giá: ${e.localizedMessage}"
            } finally {
                busy = false
                binding.progress.visibility = View.GONE
            }
        }
    }

    private fun render(quote: JsonObject) {
        val status = RepairUi.number(quote, "status")
        val response = quote.get("customerResponse")?.takeUnless { it.isJsonNull }
        val validUntil = RepairUi.field(quote, "validUntil")
        val isExpired = try {
            if (validUntil.isBlank()) false
            else {
                val time = try { OffsetDateTime.parse(validUntil).toInstant() }
                    catch (_: Exception) { LocalDateTime.parse(validUntil).toInstant(ZoneOffset.UTC) }
                !time.isAfter(Instant.now())
            }
        } catch (_: Exception) { true } // Nếu không đọc được hạn, không cho phản hồi nhầm.
        val canRespond = status == 2 && response == null && !isExpired

        binding.tvState.text = "Báo giá #$quoteId • ${QuoteUi.status(status)}" +
            if (isExpired && status == 2) " • Đã hết hạn" else ""
        section("Thông tin báo giá", listOf(
            "Số báo giá: ${RepairUi.field(quote, "quoteNumber")}",
            "Yêu cầu sửa chữa: #${RepairUi.number(quote, "repairRequestId")}",
            "Loại: ${if (quote.get("isSupplemental")?.asBoolean == true) "Phát sinh" else "Ban đầu"}",
            "Tổng tiền: ${money(quote, "totalAmount")}",
            "Hạn phản hồi: ${RepairUi.date(validUntil)}",
            "Trạng thái: ${QuoteUi.status(status)}"
        ))
        val items = quote.getAsJsonArray("details")
        section("Chi tiết hạng mục (${items?.size() ?: 0})", items?.mapNotNull { el ->
            if (!el.isJsonObject) null else {
                val item = el.asJsonObject
                "${RepairUi.field(item, "description").ifBlank { "Hạng mục #${RepairUi.number(item, "quoteDetailId")}" }}\n" +
                    "Số lượng ${RepairUi.field(item, "quantity")} × ${money(item, "unitPrice")} = ${money(item, "amount")}" 
            }
        }.orEmpty().ifEmpty { listOf("Chưa có hạng mục") })
        binding.btnAccept.visibility = if (canRespond) View.VISIBLE else View.GONE
        binding.btnReject.visibility = if (canRespond) View.VISIBLE else View.GONE
    }

    private fun money(obj: JsonObject, key: String): String =
        "${currency.format(obj.get(key)?.takeUnless { it.isJsonNull }?.asDouble ?: 0.0)} đ"

    private fun section(title: String, lines: List<String>) {
        val content = RepairUi.column(this)
        RepairUi.addLine(this, content, title, 17f, RepairUi.GOLD, true)
        lines.forEach { RepairUi.addLine(this, content, it, 14f) }
        binding.content.addView(RepairUi.card(this, content))
    }

    private fun confirm(accept: Boolean) {
        AlertDialog.Builder(this)
            .setTitle(if (accept) "Chấp nhận báo giá?" else "Từ chối báo giá?")
            .setMessage("Bạn có chắc muốn ${if (accept) "chấp nhận" else "từ chối"} báo giá #$quoteId?")
            .setNegativeButton("Quay lại", null)
            .setPositiveButton("Xác nhận") { _, _ -> respond(accept) }
            .show()
    }

    private fun respond(accept: Boolean) {
        if (busy) return
        val token = auth() ?: run { binding.tvState.text = "Hãy đăng nhập lại"; return }
        busy = true
        binding.progress.visibility = View.VISIBLE
        binding.btnAccept.isEnabled = false
        binding.btnReject.isEnabled = false
        lifecycleScope.launch {
            try {
                val result = if (accept) RetrofitClient.customerQuotesApi.accept(token, quoteId)
                    else RetrofitClient.customerQuotesApi.reject(token, quoteId)
                binding.tvState.text = if (result.isSuccessful) {
                    if (accept) "Đã chấp nhận báo giá." else "Đã từ chối báo giá."
                } else {
                    val error = result.errorBody()?.string()?.take(300).orEmpty()
                    "Không thực hiện được (HTTP ${result.code()}). $error"
                }
            } catch (e: Exception) {
                binding.tvState.text = "Lỗi kết nối: ${e.localizedMessage}"
            } finally {
                busy = false
                binding.btnAccept.isEnabled = true
                binding.btnReject.isEnabled = true
                binding.progress.visibility = View.GONE
                // Luôn đọc lại quyền phản hồi và trạng thái thực tế từ server.
                load()
            }
        }
    }
}
