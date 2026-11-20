package com.example.fixnear.ui.home

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.ReviewCreateBody
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerReviewBinding
import kotlinx.coroutines.launch

class CustomerReviewActivity : AppCompatActivity() {
    private lateinit var b: ActivityCustomerReviewBinding
    private var repairId = -1
    private var submitting = false
    private var technicianId: Int? = null

    private fun token(): String? = SessionManager(this).getToken()?.let {
        if (it.startsWith("Bearer ", true)) it else "Bearer $it"
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        b = ActivityCustomerReviewBinding.inflate(layoutInflater)
        setContentView(b.root)
        repairId = intent.getIntExtra("REPAIR_REQUEST_ID", -1)
        b.btnBack.setOnClickListener { finish() }
        b.btnSubmit.setOnClickListener { confirm() }
        b.rgType.setOnCheckedChangeListener { _, checked ->
            b.tvTechnician.visibility = if (checked == b.rbTechnician.id) View.VISIBLE else View.GONE
        }
        fetchRepair()
    }

    private fun fetchRepair() {
        val auth = token() ?: return
        lifecycleScope.launch {
            try {
                val r = RetrofitClient.customerRepairQueryApi.getDetail(auth, repairId)
                if (!r.isSuccessful) { b.tvState.text = "Không tải được yêu cầu: HTTP ${r.code()}"; return@launch }
                val root = r.body() ?: return@launch
                val completed = root.get("completedAt")?.takeUnless { it.isJsonNull } != null
                technicianId = root.get("schedules")?.takeIf { it.isJsonArray }?.asJsonArray
                    ?.mapNotNull { if (it.isJsonObject) it.asJsonObject.get("technicianId")?.takeUnless { id -> id.isJsonNull }?.asInt else null }
                    ?.firstOrNull()
                b.tvTechnician.text = if (technicianId != null) "Kỹ thuật viên được phân công: #$technicianId" else "Chưa có kỹ thuật viên được phân công trong lịch hiện hành."
                b.rbTechnician.isEnabled = technicianId != null
                if (!completed) {
                    b.tvState.text = "Yêu cầu chưa hoàn tất. Chỉ được đánh giá sau khi sửa xong và xác nhận thanh toán."
                    b.btnSubmit.isEnabled = false
                } else {
                    b.tvState.text = "Hãy đánh giá sau khi hóa đơn đã được xác nhận thanh toán."
                }
            } catch (e: Exception) { b.tvState.text = "Lỗi tải dữ liệu: ${e.localizedMessage}" }
        }
    }

    private fun confirm() {
        if (submitting || repairId <= 0) return
        val rating = b.ratingBar.rating.toInt()
        if (rating !in 1..5) { b.tvState.text = "Vui lòng chọn 1–5 sao"; return }
        val reviewType = if (b.rbTechnician.isChecked) 2 else 1
        if (reviewType == 2 && technicianId == null) {
            b.tvState.text = "Chưa xác định được kỹ thuật viên để đánh giá"; return
        }
        val comment = b.edtComment.text?.toString()?.trim().orEmpty()
        if (comment.length > 2000) { b.tvState.text = "Bình luận tối đa 2000 ký tự"; return }
        AlertDialog.Builder(this)
            .setTitle("Xác nhận gửi đánh giá")
            .setMessage("Gửi đánh giá $rating sao? Mỗi đối tượng chỉ được đánh giá một lần cho mỗi yêu cầu.")
            .setNegativeButton("Quay lại", null)
            .setPositiveButton("Gửi đánh giá") { _, _ -> sendReview(reviewType, rating, comment) }
            .show()
    }

    private fun sendReview(type: Int, rating: Int, comment: String) {
        val auth = token() ?: run { b.tvState.text = "Hãy đăng nhập lại"; return }
        submitting = true; b.btnSubmit.isEnabled = false
        b.progress.visibility = View.VISIBLE
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerBillingReviewApi.createReview(auth,
                    ReviewCreateBody(repairId, type, rating, comment.ifBlank { null }, if (type == 2) technicianId else null))
                if (response.isSuccessful) {
                    Toast.makeText(this@CustomerReviewActivity, "Đã gửi đánh giá thành công", Toast.LENGTH_LONG).show()
                    finish()
                } else {
                    val message = when (response.code()) {
                        409 -> "Chưa đủ điều kiện (chưa hoàn thành/chưa thanh toán) hoặc đã đánh giá."
                        403 -> "Tài khoản không có quyền đánh giá yêu cầu này."
                        else -> "Không gửi được đánh giá: HTTP ${response.code()}"
                    }
                    b.tvState.text = message
                }
            } catch (e: Exception) { b.tvState.text = "Lỗi kết nối: ${e.localizedMessage}" }
            finally { submitting = false; b.btnSubmit.isEnabled = true; b.progress.visibility = View.GONE }
        }
    }
}
