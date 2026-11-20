
package com.example.fixnear.ui.assignment

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.R
import com.example.fixnear.data.model.technician.AssignStatus
import com.example.fixnear.data.model.technician.RepairStatus
import com.example.fixnear.data.model.technician.SurveyRequest
import com.example.fixnear.data.model.technician.TechnicianAssignment
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.ui.repair.RepairProgressActivity
import com.example.fixnear.databinding.ActivityAssignmentDetailBinding
import com.example.fixnear.ui.quote.CreateQuoteActivity
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.io.IOException

class AssignmentDetailActivity : AppCompatActivity() {

    private lateinit var binding: ActivityAssignmentDetailBinding

    private var assignmentId = -1
    private var currentAssignment: TechnicianAssignment? = null
    private var currentRepairStatus = -1

    private var isLoading = false
    private var isSubmitting = false
    private var surveySavedThisSession = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityAssignmentDetailBinding.inflate(
            layoutInflater
        )
        setContentView(binding.root)

        assignmentId = intent.getIntExtra(
            "assignmentId",
            -1
        )

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.btnStartSurvey.setOnClickListener {
            startSurvey()
        }

        binding.btnSaveSurvey.setOnClickListener {
            submitSurvey()
        }

        binding.btnCreateQuote.setOnClickListener {
            openCreateQuote()
        }


        binding.btnRepairProgress.setOnClickListener {

            val assignment = currentAssignment

            if (assignment == null ||
                assignment.repairRequestId <= 0
            ) {
                Toast.makeText(
                    this,
                    "Chưa có thông tin yêu cầu sửa chữa",
                    Toast.LENGTH_SHORT
                ).show()

            } else {
                val intent = android.content.Intent(
                    this@AssignmentDetailActivity,
                    RepairProgressActivity::class.java
                )

                intent.putExtra(
                    "repairRequestId",
                    assignment.repairRequestId
                )

                startActivity(intent)
            }
        }


        if (assignmentId <= 0) {
            showMessage("Không tìm thấy mã công việc")
            finish()
            return
        }

        hideAllActions()
        loadAssignmentDetail()
    }

    override fun onResume() {
        super.onResume()

        if (
            assignmentId > 0 &&
            currentAssignment != null &&
            !isLoading &&
            !isSubmitting
        ) {
            loadAssignmentDetail()
        }
    }

    // ====================================
    // LOAD ASSIGNMENT DETAIL
    // ====================================

    override fun onRestart() {
        super.onRestart()

        if (assignmentId > 0 && !isFinishing) {
            loadAssignmentDetail()
        }
    }

    private fun loadAssignmentDetail() {
        if (isLoading || isSubmitting) return

        isLoading = true
        updateActionButtons()
        hideAllActions()

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.technicianApi
                        .getAssignment(assignmentId)

                if (response.isSuccessful) {
                    val assignment = response.body()?.data

                    if (assignment == null) {
                        currentAssignment = null
                        currentRepairStatus = -1

                        showMessage(
                            "Không tìm thấy dữ liệu công việc"
                        )
                    } else {
                        currentAssignment = assignment
                        currentRepairStatus =
                            assignment.repairStatus

                        bindAssignmentData(assignment)

                        bindRepairStatus(
                            assignment.repairStatus
                        )
                    }
                } else {
                    showMessage(
                        getHttpErrorMessage(
                            response.code(),
                            response.errorBody()?.string()
                        )
                    )
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: IOException) {
                showMessage(
                    "Không thể kết nối Backend. " +
                            "Vui lòng kiểm tra mạng."
                )

            } catch (e: Exception) {
                showMessage(
                    e.message
                        ?: "Không thể tải chi tiết công việc"
                )

            } finally {
                isLoading = false

                bindActionByStatus()
                updateActionButtons()
            }
        }
    }

    // ====================================
    // START SURVEY
    // ====================================

    private fun startSurvey() {
        val assignment = currentAssignment

        if (assignment == null) {
            showMessage(
                "Chưa tải thông tin công việc"
            )
            return
        }

        if (isLoading || isSubmitting) return

        if (
            assignment.assignmentStatus !=
            AssignStatus.ACCEPTED ||
            currentRepairStatus != RepairStatus.ASSIGNED
        ) {
            showMessage(
                "Công việc chưa đủ điều kiện khảo sát"
            )
            return
        }

        isSubmitting = true
        updateActionButtons()

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.technicianApi.startSurvey(
                        assignment.repairRequestId
                    )

                if (response.isSuccessful) {
                    showMessage(
                        response.body()?.message
                            ?: "Bắt đầu khảo sát thành công"
                    )

                    // Cap nhat trang thai tam thoi
                    currentRepairStatus =
                        RepairStatus.INSPECTING

                    bindRepairStatus(currentRepairStatus)
                } else {
                    showMessage(
                        getHttpErrorMessage(
                            response.code(),
                            response.errorBody()?.string()
                        )
                    )
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: IOException) {
                showMessage(
                    "Không thể kết nối Backend"
                )

            } catch (e: Exception) {
                showMessage(
                    e.message
                        ?: "Không thể bắt đầu khảo sát"
                )

            } finally {
                isSubmitting = false
                bindActionByStatus()
                updateActionButtons()

                // Lay lai trang thai thuc te
                loadAssignmentDetail()
            }
        }
    }

    // ====================================
    // SUBMIT SURVEY
    // ====================================

    private fun submitSurvey() {
        val assignment = currentAssignment

        if (assignment == null) {
            showMessage(
                "Chưa tải thông tin công việc"
            )
            return
        }

        if (isLoading || isSubmitting) return

        if (
            assignment.assignmentStatus !=
            AssignStatus.ACCEPTED ||
            currentRepairStatus != RepairStatus.INSPECTING
        ) {
            showMessage(
                "Công việc không ở trạng thái khảo sát"
            )
            return
        }

        val diagnosis =
            binding.edtDiagnosis.text.toString().trim()

        val recommendation =
            binding.edtRecommendation.text.toString().trim()

        // Validate diagnosis
        if (diagnosis.length !in 5..2000) {
            binding.edtDiagnosis.error =
                "Chẩn đoán phải từ 5 đến 2000 ký tự"

            binding.edtDiagnosis.requestFocus()
            return
        }

        // Validate recommendation
        if (recommendation.length > 1000) {
            binding.edtRecommendation.error =
                "Đề xuất tối đa 1000 ký tự"

            binding.edtRecommendation.requestFocus()
            return
        }

        val request = SurveyRequest(
            diagnosis = diagnosis,
            recommendation =
                recommendation.ifBlank { null }
        )

        isSubmitting = true
        updateActionButtons()

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.technicianApi.submitSurvey(
                        assignment.repairRequestId,
                        request
                    )

                if (response.isSuccessful) {
                    surveySavedThisSession = true

                    showMessage(
                        response.body()?.message
                            ?: "Lưu khảo sát thành công"
                    )

                    // Backend van giu RepairStatus = 5
                    // sau khi luu bao cao khao sat.
                } else {
                    showMessage(
                        getHttpErrorMessage(
                            response.code(),
                            response.errorBody()?.string()
                        )
                    )
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: IOException) {
                showMessage(
                    "Không thể kết nối Backend"
                )

            } catch (e: Exception) {
                showMessage(
                    e.message ?: "Không thể lưu khảo sát"
                )

            } finally {
                isSubmitting = false

                bindActionByStatus()
                updateActionButtons()

                loadAssignmentDetail()
            }
        }
    }

    // ====================================
    // OPEN UI03 - CREATE QUOTE
    // ====================================

    private fun openCreateQuote() {
        val assignment = currentAssignment

        if (assignment == null) {
            showMessage(
                "Chưa tải thông tin công việc"
            )
            return
        }

        if (isLoading || isSubmitting) return

        if (
            assignment.assignmentStatus !=
            AssignStatus.ACCEPTED
        ) {
            showMessage(
                "Bạn chưa được phép tạo báo giá"
            )
            return
        }

        if (
            currentRepairStatus != RepairStatus.INSPECTING
        ) {
            showMessage(
                "Trạng thái hiện tại không cho phép " +
                        "tạo báo giá ban đầu"
            )
            return
        }

        if (
            assignment.repairRequestId <= 0 ||
            assignment.shopId <= 0
        ) {
            showMessage(
                "Thiếu thông tin yêu cầu sửa chữa hoặc cửa hàng"
            )
            return
        }

        // Bao cao khao sat phai ton tai tren Backend.
        // UI02 chua co API kiem tra bao cao rieng.
        // Backend POST /api/quotes se xac minh dieu kien nay.

        val intent = Intent(
            this,
            CreateQuoteActivity::class.java
        ).apply {
            putExtra(
                "repairRequestId",
                assignment.repairRequestId
            )
            putExtra(
                "shopId",
                assignment.shopId
            )
        }

        // Tranh crash neu UI03 chua duoc khai bao
        // trong AndroidManifest.
        if (
            intent.resolveActivity(packageManager) == null
        ) {
            showMessage(
                "UI03 chưa được đăng ký trong AndroidManifest"
            )
            return
        }

        startActivity(intent)
    }

    // ====================================
    // BIND ASSIGNMENT DATA
    // ====================================

    private fun bindAssignmentData(
        assignment: TechnicianAssignment
    ) {
        binding.tvRepairRequestId.text =
            "#${assignment.repairRequestId}"

        binding.tvDetailCustomerName.text =
            assignment.customerName.ifBlank {
                "Khách hàng"
            }

        binding.tvDetailShopName.text =
            assignment.shopName.ifBlank {
                "Chưa có thông tin cửa hàng"
            }

        binding.tvDetailServices.text =
            assignment.services.joinToString(", ") {
                it.serviceName
            }.ifBlank {
                "Chưa có thông tin dịch vụ"
            }

        binding.tvDetailRepairType.text =
            when (assignment.repairMethod) {
                1 -> "Sửa chữa tại nhà"
                2 -> "Sửa chữa tại cửa hàng"
                else -> "Chưa xác định"
            }

        binding.tvDetailDescription.text =
            assignment.description
                ?.takeIf { it.isNotBlank() }
                ?: "Không có mô tả"

        binding.tvDetailNote.text =
            assignment.note
                ?.takeIf { it.isNotBlank() }
                ?: "Không có ghi chú"

        val date = assignment.preferredDate
            ?.takeIf { it.isNotBlank() }
            ?.substringBefore("T")
            ?.split("-")
            ?.let { parts ->
                if (parts.size == 3) {
                    "${parts[2]}/${parts[1]}/${parts[0]}"
                } else {
                    assignment.preferredDate
                }
            } ?: "Chưa xác định ngày"

        val from = assignment.preferredTimeFrom
            ?.takeIf { it.isNotBlank() }
            ?.take(5)

        val to = assignment.preferredTimeTo
            ?.takeIf { it.isNotBlank() }
            ?.take(5)

        binding.tvDetailAppointment.text =
            buildString {
                append(date)

                if (from != null) {
                    append(" • ")
                    append(from)

                    if (to != null) {
                        append(" - ")
                        append(to)
                    }
                }
            }
    }

    // ====================================
    // REPAIR STATUS
    // ====================================

    private fun getRepairStatusText(
        status: Int
    ): String {
        return when (status) {
            1 -> "Chờ cửa hàng tiếp nhận"
            2 -> "Đã tiếp nhận"
            3 -> "Yêu cầu bị từ chối"
            RepairStatus.ASSIGNED -> "Đã phân công"
            RepairStatus.INSPECTING -> "Đang khảo sát"
            RepairStatus.QUOTED -> "Đã gửi báo giá"
            RepairStatus.QUOTE_APPROVED -> "Đã duyệt báo giá"
            RepairStatus.REPAIRING -> "Đang sửa chữa"
            RepairStatus.COMPLETED -> "Hoàn thành"
            10 -> "Đã thanh toán"
            11 -> "Đã hủy"
            else -> "Trạng thái $status"
        }
    }

    private fun bindRepairStatus(status: Int) {
        binding.tvAssignmentDetailStatus.text =
            getRepairStatusText(status)

        val colorRes = when (status) {
            1, 2 -> R.color.status_pending

            3, 11 -> R.color.status_cancelled

            RepairStatus.ASSIGNED,
            RepairStatus.INSPECTING,
            RepairStatus.QUOTED,
            RepairStatus.REPAIRING ->
                R.color.status_processing

            RepairStatus.QUOTE_APPROVED,
            RepairStatus.COMPLETED,
            10 -> R.color.status_completed

            else -> R.color.accent_brown
        }

        binding.tvAssignmentDetailStatus.setTextColor(
            ContextCompat.getColor(this, colorRes)
        )
    }

    // ====================================
    // ACTION VISIBILITY BY BACKEND STATUS
    // ====================================

    private fun hideAllActions() {
        binding.btnStartSurvey.visibility = View.GONE
        binding.layoutSurveyForm.visibility = View.GONE
        binding.tvSurveyDone.visibility = View.GONE
        binding.btnCreateQuote.visibility = View.GONE
        binding.btnRepairProgress.visibility = View.GONE
    }

    private fun bindActionByStatus() {
        hideAllActions()

        val assignment = currentAssignment ?: return

        if (
            assignment.assignmentStatus !=
            AssignStatus.ACCEPTED
        ) {
            return
        }

        when (currentRepairStatus) {
            RepairStatus.ASSIGNED -> {
                binding.btnStartSurvey.visibility =
                    View.VISIBLE
            }

            RepairStatus.INSPECTING -> {
                if (surveySavedThisSession) {
                    binding.tvSurveyDone.visibility =
                        View.VISIBLE
                } else {
                    binding.layoutSurveyForm.visibility =
                        View.VISIBLE
                }

                // Backend yeu cau bao cao khao sat ton tai
                // truoc khi chap nhan tao bao gia.
                binding.btnCreateQuote.visibility =
                    View.VISIBLE
            }

            RepairStatus.QUOTED -> {
                binding.tvSurveyDone.visibility =
                    View.VISIBLE
            }

            RepairStatus.QUOTE_APPROVED,
            RepairStatus.REPAIRING -> {
                binding.tvSurveyDone.visibility =
                    View.VISIBLE

                binding.btnRepairProgress.visibility =
                    View.VISIBLE
            }

            RepairStatus.COMPLETED, 10 -> {
                binding.tvSurveyDone.visibility =
                    View.VISIBLE
            }
        }
    }

    private fun updateActionButtons() {
        val enabled = !isLoading && !isSubmitting

        binding.btnStartSurvey.isEnabled = enabled
        binding.btnSaveSurvey.isEnabled = enabled
        binding.btnCreateQuote.isEnabled = enabled
        binding.btnRepairProgress.isEnabled = enabled
    }

    // ====================================
    // ERROR HANDLING
    // ====================================

    private fun getHttpErrorMessage(
        statusCode: Int,
        errorBody: String?
    ): String {
        val backendMessage = try {
            JSONObject(errorBody ?: "{}")
                .optString("message")
        } catch (_: Exception) {
            ""
        }

        return when (statusCode) {
            400 -> backendMessage.ifBlank {
                "Dữ liệu không hợp lệ."
            }

            401 -> "Phiên đăng nhập đã hết hạn. " +
                    "Vui lòng đăng nhập lại."

            403 -> "Bạn không có quyền thực hiện thao tác này."

            404 -> "Không tìm thấy công việc hoặc yêu cầu sửa chữa."

            409 -> backendMessage.ifBlank {
                "Trạng thái công việc đã thay đổi. " +
                        "Vui lòng kiểm tra lại."
            }

            else -> backendMessage.ifBlank {
                "Lỗi Backend (HTTP $statusCode)"
            }
        }
    }

    private fun showMessage(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
