
package com.example.fixnear.ui.payment

import android.os.Bundle
import android.view.View
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.example.fixnear.R

class TechnicianPaymentActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(R.layout.activity_technician_payment)

        setupBackButton()
        setupPaymentScreen()
    }

    private fun setupBackButton() {
        findViewById<TextView>(
            R.id.btnBackPayment
        ).setOnClickListener {
            finish()
        }
    }

    private fun setupPaymentScreen() {

        val repairRequestId = intent.getIntExtra(
            "repairRequestId",
            -1
        )

        val invoiceId = intent.getIntExtra(
            "invoiceId",
            -1
        )

        val tvRepairId = findViewById<TextView>(
            R.id.tvPaymentRepairId
        )

        val tvInvoiceId = findViewById<TextView>(
            R.id.tvPaymentInvoiceId
        )

        val tvMessage = findViewById<TextView>(
            R.id.tvPaymentMessage
        )

        // Chua co API doc thanh toan cho Technician.
        // Khong hien thi so tien hoac trang thai gia.

        findViewById<TextView>(
            R.id.tvPaymentStatus
        ).text = "Chưa có dữ liệu"

        findViewById<TextView>(
            R.id.tvPaymentAmount
        ).text = "--"

        findViewById<TextView>(
            R.id.tvPaymentAmountDescription
        ).text = "Chưa có thông tin số tiền"

        findViewById<TextView>(
            R.id.tvPaymentMethod
        ).text = "Phương thức: --"

        findViewById<TextView>(
            R.id.tvPaymentTime
        ).text = "Thời gian thanh toán: --"

        findViewById<TextView>(
            R.id.tvPaymentTransactionId
        ).text = "Mã giao dịch: --"

        findViewById<View>(
            R.id.progressPayment
        ).visibility = View.GONE

        // Hien thi ma cong viec neu duoc truyen vao.
        if (repairRequestId > 0) {
            tvRepairId.text =
                "Mã yêu cầu sửa chữa: #$repairRequestId"
        } else {
            tvRepairId.text =
                "Mã yêu cầu sửa chữa: --"
        }

        // Hien thi ma hoa don neu duoc truyen vao.
        if (invoiceId > 0) {
            tvInvoiceId.text =
                "Mã hóa đơn: #$invoiceId"
        } else {
            tvInvoiceId.text =
                "Mã hóa đơn: --"
        }

        tvMessage.text = if (repairRequestId > 0) {
            "Thông tin thanh toán của công việc này " +
                    "chưa được hệ thống cấp quyền truy cập cho Technician."
        } else {
            "Chưa có mã yêu cầu sửa chữa hợp lệ. " +
                    "Vui lòng mở màn hình từ công việc tương ứng."
        }
    }
}
