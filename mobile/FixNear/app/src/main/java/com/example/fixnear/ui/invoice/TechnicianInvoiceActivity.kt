
package com.example.fixnear.ui.invoice

import android.os.Bundle
import android.view.View
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import com.example.fixnear.R

class TechnicianInvoiceActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(R.layout.activity_technician_invoice)

        setupBackButton()
        setupInvoiceScreen()
    }

    private fun setupBackButton() {
        findViewById<TextView>(
            R.id.btnBackInvoice
        ).setOnClickListener {
            finish()
        }
    }

    private fun setupInvoiceScreen() {

        val repairRequestId = intent.getIntExtra(
            "repairRequestId",
            -1
        )

        val tvRepairId = findViewById<TextView>(
            R.id.tvInvoiceRepairId
        )

        val tvMessage = findViewById<TextView>(
            R.id.tvInvoiceMessage
        )

        // Chưa có API hóa đơn dành cho Technician.
        // Không hiển thị số tiền hoặc trạng thái giả.
        findViewById<TextView>(
            R.id.tvInvoiceNumber
        ).text = "Mã hóa đơn: --"

        findViewById<TextView>(
            R.id.tvInvoiceDate
        ).text = "Ngày lập: --"

        findViewById<TextView>(
            R.id.tvInvoiceStatus
        ).text = "Trạng thái: Chưa có dữ liệu"

        findViewById<TextView>(
            R.id.tvInvoiceServices
        ).text = "Chi phí dịch vụ: --"

        findViewById<TextView>(
            R.id.tvInvoiceParts
        ).text = "Chi phí linh kiện: --"

        findViewById<TextView>(
            R.id.tvInvoiceTotal
        ).text = "--"

        findViewById<View>(
            R.id.layoutInvoiceDetails
        ).visibility = View.GONE

        findViewById<View>(
            R.id.tvInvoiceDetailEmpty
        ).visibility = View.VISIBLE

        findViewById<View>(
            R.id.progressInvoice
        ).visibility = View.GONE

        if (repairRequestId > 0) {
            tvRepairId.text =
                "Mã yêu cầu sửa chữa: #$repairRequestId"

            tvMessage.text =
                "Hóa đơn của công việc này chưa có dữ liệu " +
                        "được cấp quyền truy cập cho Technician."
        } else {
            tvRepairId.text =
                "Mã yêu cầu sửa chữa: --"

            tvMessage.text =
                "Chưa có mã yêu cầu sửa chữa hợp lệ. " +
                        "Vui lòng mở hóa đơn từ công việc tương ứng."
        }
    }
}
