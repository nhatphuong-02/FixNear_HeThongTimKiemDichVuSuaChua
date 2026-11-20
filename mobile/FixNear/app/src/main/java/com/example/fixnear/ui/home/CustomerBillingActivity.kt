package com.example.fixnear.ui.home

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.PaymentCreateBody
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerBillingBinding
import com.google.gson.JsonObject
import kotlinx.coroutines.launch
import java.text.NumberFormat
import java.util.Locale

class CustomerBillingActivity : AppCompatActivity() {
    private lateinit var b: ActivityCustomerBillingBinding
    private var repairId = -1
    private var invoiceId = -1
    private var paymentId: Int? = null
    private var paid = false
    private var busy = false

    private fun auth(): String? = SessionManager(this).getToken()?.let {
        if (it.startsWith("Bearer ", true)) it else "Bearer $it"
    }

    private fun JsonObject.value(key: String): String =
        get(key)?.takeUnless { it.isJsonNull }?.asString.orEmpty()

    private fun JsonObject.nested(key: String): JsonObject? =
        get(key)?.takeIf { it.isJsonObject }?.asJsonObject

    private fun money(value: String): String = try {
        NumberFormat.getNumberInstance(Locale("vi", "VN")).format(value.toDouble()) + " đ"
    } catch (_: Exception) { "$value đ" }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        b = ActivityCustomerBillingBinding.inflate(layoutInflater)
        setContentView(b.root)
        repairId = intent.getIntExtra("REPAIR_REQUEST_ID", -1)
        b.btnBack.setOnClickListener { finish() }
        b.btnRefresh.setOnClickListener { load() }
        b.btnCash.setOnClickListener { confirmPayment(1) }
        b.btnBank.setOnClickListener { confirmPayment(3) }
        load()
    }

    override fun onResume() { super.onResume(); if (repairId > 0) load() }

    private fun state(message: String) { b.tvState.text = message }

    private fun loading(value: Boolean) {
        busy = value
        b.progress.visibility = if (value) View.VISIBLE else View.GONE
        updateButtons()
    }

    private fun updateButtons() {
        val canCreate = !busy && invoiceId > 0 && !paid && paymentId == null
        b.btnCash.isEnabled = canCreate
        b.btnBank.isEnabled = canCreate
        b.btnCash.alpha = if (canCreate) 1f else .5f
        b.btnBank.alpha = if (canCreate) 1f else .5f
    }

    private fun load() {
        val token = auth() ?: run { state("Hãy đăng nhập lại"); return }
        if (repairId <= 0) { state("Mã yêu cầu không hợp lệ"); return }
        if (busy) return
        loading(true)
        lifecycleScope.launch {
            try {
                val lookup = RetrofitClient.customerBillingReviewApi.findInvoice(token, repairId)
                if (lookup.code() == 404) {
                    invoiceId = -1; paymentId = null; paid = false
                    b.tvInvoice.text = "Shop chưa lập hóa đơn cho yêu cầu này."
                    b.tvPayment.text = "Bạn có thể quay lại sau khi Shop hoàn tất sửa chữa và lập hóa đơn."
                    state("Chưa có hóa đơn")
                    return@launch
                }
                if (!lookup.isSuccessful) { state("Lỗi tìm hóa đơn: HTTP ${lookup.code()}"); return@launch }
                val reference = lookup.body()?.nested("data") ?: run {
                    state("Phản hồi hóa đơn không hợp lệ"); return@launch
                }
                invoiceId = reference.get("invoiceId")?.asInt ?: -1
                paymentId = reference.get("paymentId")?.takeUnless { it.isJsonNull }?.asInt
                paid = !reference.value("paidAt").isBlank()
                val detail = RetrofitClient.customerBillingReviewApi.invoice(token, invoiceId)
                if (!detail.isSuccessful) { state("Lỗi chi tiết hóa đơn: HTTP ${detail.code()}"); return@launch }
                val invoice = detail.body()?.nested("data") ?: run {
                    state("Không đọc được chi tiết hóa đơn"); return@launch
                }
                val rows = mutableListOf<String>()
                invoice.get("details")?.takeIf { it.isJsonArray }?.asJsonArray?.forEach { element ->
                    if (element.isJsonObject) {
                        val row = element.asJsonObject
                        rows.add("• ${row.value("description")} | SL ${row.value("quantity")} × ${money(row.value("unitPrice"))} = ${money(row.value("amount"))}")
                    }
                }
                b.tvInvoice.text = buildString {
                    append("Số hóa đơn: ${invoice.value("invoiceNumber")}\n")
                    append("Ngày phát hành: ${invoice.value("issuedAt").take(10)}\n")
                    append("Tổng tiền: ${money(invoice.value("totalAmount"))}\n")
                    append(if (paid) "Đã xác nhận thanh toán" else "Chưa xác nhận thanh toán")
                    if (rows.isNotEmpty()) append("\n\nCHI TIẾT\n${rows.joinToString("\n")}")
                }
                if (paymentId != null) {
                    val pr = RetrofitClient.customerBillingReviewApi.payment(token, paymentId!!)
                    val payment = pr.body()?.nested("data")
                    b.tvPayment.text = if (pr.isSuccessful && payment != null) {
                        "Mã thanh toán: #${payment.value("paymentId")}\n" +
                            "Phương thức: ${if (payment.value("paymentMethod") == "1") "Tiền mặt" else "Chuyển khoản"}\n" +
                            "Trạng thái: ${if (paid) "Đã xác nhận" else "Đang chờ xác nhận từ hệ thống/Shop"}"
                    } else "Đã có yêu cầu thanh toán #$paymentId. Không tạo yêu cầu trùng."
                } else b.tvPayment.text = "Chưa tạo yêu cầu thanh toán."
                state(if (paid) "Hóa đơn đã thanh toán" else "Kiểm tra số tiền trước khi chọn hình thức thanh toán")
            } catch (e: Exception) { state("Lỗi kết nối: ${e.localizedMessage}") }
            finally { loading(false) }
        }
    }

    private fun confirmPayment(method: Int) {
        if (invoiceId <= 0 || busy || paid || paymentId != null) return
        val methodName = if (method == 1) "tiền mặt" else "chuyển khoản"
        AlertDialog.Builder(this)
            .setTitle("Tạo yêu cầu thanh toán")
            .setMessage("Tạo yêu cầu thanh toán bằng $methodName? Đây chưa phải xác nhận đã thanh toán.")
            .setNegativeButton("Hủy", null)
            .setPositiveButton("Xác nhận") { _, _ -> createPayment(method) }
            .show()
    }

    private fun createPayment(method: Int) {
        val token = auth() ?: run { state("Hãy đăng nhập lại"); return }
        loading(true)
        lifecycleScope.launch {
            try {
                val result = RetrofitClient.customerBillingReviewApi.createPayment(token, PaymentCreateBody(invoiceId, method))
                if (result.isSuccessful) {
                    val data = result.body()?.nested("data")
                    paymentId = data?.get("paymentId")?.asInt
                    state("Đã tạo yêu cầu thanh toán. Chờ xác nhận từ Shop/hệ thống.")
                    Toast.makeText(this@CustomerBillingActivity, "Yêu cầu thanh toán đã được tạo", Toast.LENGTH_LONG).show()
                } else {
                    state("Không tạo được thanh toán (HTTP ${result.code()}). Nếu đã có giao dịch, hãy tải lại.")
                }
            } catch (e: Exception) { state("Lỗi kết nối: ${e.localizedMessage}") }
            finally { loading(false); load() }
        }
    }
}
