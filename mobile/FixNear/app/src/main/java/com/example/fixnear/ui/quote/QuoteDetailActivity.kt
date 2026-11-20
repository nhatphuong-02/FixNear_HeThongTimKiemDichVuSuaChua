
package com.example.fixnear.ui.quote

import android.os.Bundle
import android.view.View
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.remote.RetrofitClient
import kotlinx.coroutines.launch
import java.text.NumberFormat
import java.util.Locale
import com.example.fixnear.R

class QuoteDetailActivity : AppCompatActivity() {
    private lateinit var quoteAdapter: QuoteDetailAdapter

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(R.layout.activity_quote_detail)
        quoteAdapter = QuoteDetailAdapter(
            readOnly = true,
            onRemove = {}
        )

        findViewById<RecyclerView>(
            R.id.rvQuoteDetailItems
        ).apply {
            layoutManager = LinearLayoutManager(
                this@QuoteDetailActivity
            )
            adapter = quoteAdapter
        }

        findViewById<TextView>(
            R.id.btnBackQuoteDetail
        ).setOnClickListener {
            finish()
        }

        val quoteId = intent.getIntExtra("quoteId", -1)

        findViewById<View>(R.id.btnRetryQuoteDetail)
            .setOnClickListener {
                if (quoteId > 0) loadQuote(quoteId)
            }

        if (quoteId > 0) {
            loadQuote(quoteId)
        } else {
            findViewById<TextView>(
                R.id.tvQuoteDetailMessage
            ).apply {
                visibility = View.VISIBLE
                text = "Không có mã báo giá hợp lệ"
            }
        }
    }

    private fun loadQuote(quoteId: Int) {

        val progress = findViewById<View>(
            R.id.progressQuoteDetail
        )
        val retry = findViewById<View>(
            R.id.btnRetryQuoteDetail
        )
        val message = findViewById<TextView>(
            R.id.tvQuoteDetailMessage
        )

        progress.visibility = View.VISIBLE
        quoteAdapter.submitList(emptyList())
        retry.visibility = View.GONE
        message.visibility = View.GONE

        lifecycleScope.launch {
            try {
                val response = RetrofitClient.technicianApi
                    .getQuote(quoteId)

                val quote = response.body()?.data

                if (response.isSuccessful && quote != null) {

                    findViewById<TextView>(
                        R.id.tvQuoteDetailId
                    ).text = "Mã báo giá: ${quote.quoteNumber}"

                    val statusName = when (quote.status) {
                        1 -> "Bản nháp"
                        2 -> "Đã gửi"
                        3 -> "Đã chấp nhận"
                        4 -> "Đã từ chối"
                        else -> "Không xác định"
                    }

                    findViewById<TextView>(
                        R.id.tvQuoteDetailStatus
                    ).text = "Trạng thái: $statusName"

                    findViewById<TextView>(
                        R.id.tvQuoteDetailValidUntil
                    ).text = "Hiệu lực đến: " +
                            (quote.validUntil?.take(10)
                                ?: "Không đặt thời hạn")

                    val formatter = NumberFormat
                        .getNumberInstance(
                            Locale.forLanguageTag("vi-VN")
                        )
                    formatter.maximumFractionDigits = 2

                    findViewById<TextView>(
                        R.id.tvQuoteDetailTotal
                    ).text = "${formatter.format(quote.totalAmount)} đ"


                    val items = quote.details.map { detail ->

                        val referenceId = when (detail.itemType) {
                            1 -> detail.serviceRefId
                            2 -> detail.repairItemRefId
                            3 -> detail.repairPartRefId
                            else -> null
                        }

                        val fallbackName = when (detail.itemType) {
                            1 -> "Dịch vụ #${referenceId ?: "?"}"
                            2 -> "Hạng mục sửa chữa #${referenceId ?: "?"}"
                            3 -> "Linh kiện #${referenceId ?: "?"}"
                            else -> "Hạng mục #${detail.quoteDetailId}"
                        }

                        val itemName = detail.description
                            ?.trim()
                            ?.takeIf { it.isNotEmpty() }
                            ?.let { "$it ($fallbackName)" }
                            ?: fallbackName

                        QuoteDraftItem(
                            itemType = detail.itemType,
                            referenceId = referenceId ?: 0,
                            name = itemName,
                            quantity = detail.quantity,
                            unitPrice = detail.unitPrice,
                            description = detail.description,
                            serverAmount = detail.amount
                        )
                    }

                    quoteAdapter.submitList(items)

                    message.visibility =
                        if (items.isEmpty()) View.VISIBLE else View.GONE

                    message.text = "Báo giá chưa có hạng mục"


                } else {
                    message.visibility = View.VISIBLE
                    message.text = when (response.code()) {
                        401 -> "Phiên đăng nhập đã hết hạn"
                        403 -> "Không có quyền xem báo giá"
                        404 -> "Không tìm thấy báo giá hoặc không có quyền xem"
                        else -> "Không tải được báo giá (${response.code()})"
                    }
                    retry.visibility = View.VISIBLE
                }

            } catch (e: Exception) {
                message.visibility = View.VISIBLE
                message.text = "Lỗi kết nối. Vui lòng thử lại."
                retry.visibility = View.VISIBLE
            } finally {
                progress.visibility = View.GONE
            }
        }
    }

}
