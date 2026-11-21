
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

    private val currency =
        NumberFormat.getNumberInstance(Locale("vi", "VN"))

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityCustomerQuotesBinding.inflate(layoutInflater)
        setContentView(binding.root)

        repairId = intent.getIntExtra("REPAIR_REQUEST_ID", -1)

        binding.btnBack.setOnClickListener { finish() }
        binding.btnRefresh.setOnClickListener { refresh() }
        binding.btnLoadMore.setOnClickListener {
            load(page + 1)
        }
    }

    override fun onResume() {
        super.onResume()
        refresh()
    }

    private fun auth(): String? {
        val raw = SessionManager(this).getToken()
            ?.trim()
            ?.takeIf { it.isNotBlank() }
            ?: return null

        return if (raw.startsWith("Bearer ", true)) {
            raw
        } else {
            "Bearer $raw"
        }
    }

    private fun refresh() {
        if (loading) return

        page = 0
        totalPages = 1
        binding.content.removeAllViews()

        load(1)
    }

    private fun load(next: Int) {
        if (loading || next > totalPages || next < 1) return

        if (repairId <= 0) {
            binding.tvState.text = "Mã yêu cầu không hợp lệ."
            return
        }

        val token = auth() ?: run {
            binding.tvState.text =
                "Vui lòng đăng nhập để xem báo giá."
            return
        }

        loading = true
        binding.progress.visibility = View.VISIBLE
        binding.btnLoadMore.visibility = View.GONE

        binding.tvState.text = "Đang tải báo giá..."

        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerQuotesApi.list(
                    token,
                    repairId,
                    next,
                    10
                )


                if (!response.isSuccessful) {

                    val errorText = response.errorBody()
                        ?.string()
                        .orEmpty()

                    val backendMessage = try {
                        com.google.gson.JsonParser.parseString(errorText)
                            .asJsonObject
                            .get("message")
                            ?.asString
                            .orEmpty()
                    } catch (_: Exception) {
                        errorText.take(300)
                    }

                    binding.tvState.text = when (response.code()) {
                        401 -> "Phiên đăng nhập hết hạn."
                        403 -> "Tài khoản không có quyền xem báo giá."
                        404 -> "API báo giá trả HTTP 404.\n" +
                                backendMessage.ifBlank {
                                    "Cần kiểm tra route và backend đang chạy."
                                }
                        else -> "HTTP ${response.code()}\n$backendMessage"
                    }

                    return@launch
                }


                val root = response.body() ?: run {
                    binding.tvState.text =
                        "Backend trả về dữ liệu trống."
                    return@launch
                }

                page = RepairUi.number(root, "page", next)
                totalPages = RepairUi.number(
                    root,
                    "totalPages",
                    1
                )

                val total = RepairUi.number(root, "total")

                val data = root.get("data")
                    ?.takeIf { it.isJsonArray }
                    ?.asJsonArray

                data?.forEach { element ->
                    if (element.isJsonObject) {
                        addQuote(element.asJsonObject)
                    }
                }

                binding.tvState.text = if (total == 0) {
                    "Chưa có báo giá được gửi.\n" +
                            "Cửa hàng sẽ gửi báo giá sau khi " +
                            "kỹ thuật viên khảo sát và lập báo giá."
                } else {
                    "Có $total báo giá • Trang $page/$totalPages"
                }

            } catch (e: Exception) {
                binding.tvState.text =
                    "Lỗi kết nối backend:\n${e.localizedMessage}"

            } finally {
                loading = false
                binding.progress.visibility = View.GONE

                binding.btnLoadMore.visibility =
                    if (page < totalPages) {
                        View.VISIBLE
                    } else {
                        View.GONE
                    }
            }
        }
    }

    private fun addQuote(obj: JsonObject) {
        val id = RepairUi.number(obj, "quoteId")
        if (id <= 0) return

        val status = RepairUi.number(obj, "status")
        val supplemental =
            obj.get("isSupplemental")?.asBoolean == true

        val total = obj.get("totalAmount")
            ?.takeUnless { it.isJsonNull }
            ?.asDouble ?: 0.0

        val content = RepairUi.column(this)

        RepairUi.addLine(
            this,
            content,
            RepairUi.field(obj, "quoteNumber")
                .ifBlank { "Báo giá #$id" },
            17f,
            RepairUi.WHITE,
            true
        )

        RepairUi.addLine(
            this,
            content,
            if (supplemental) {
                "Báo giá phát sinh"
            } else {
                "Báo giá ban đầu"
            },
            13f,
            RepairUi.MUTED
        )

        RepairUi.addLine(
            this,
            content,
            "${currency.format(total)} đ",
            21f,
            RepairUi.GOLD,
            true
        )

        RepairUi.addLine(
            this,
            content,
            QuoteUi.status(status),
            13f,
            RepairUi.GOLD
        )

        val card = RepairUi.card(this, content)

        card.setOnClickListener {
            startActivity(
                Intent(
                    this,
                    CustomerQuoteDetailActivity::class.java
                ).apply {
                    putExtra("QUOTE_ID", id)
                }
            )
        }

        binding.content.addView(card)
    }
}

internal object QuoteUi {
    fun status(status: Int): String = when (status) {
        2 -> "Đã gửi • Chờ phản hồi"
        3 -> "Đã chấp nhận"
        4 -> "Đã từ chối"
        else -> "Trạng thái $status"
    }
}
