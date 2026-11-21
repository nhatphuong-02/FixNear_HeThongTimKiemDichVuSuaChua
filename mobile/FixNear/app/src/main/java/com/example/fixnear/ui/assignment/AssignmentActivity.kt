
package com.example.fixnear.ui.assignment

import android.app.AlertDialog
import android.content.Intent
import android.content.res.ColorStateList
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.os.Bundle
import android.text.InputFilter
import android.view.Gravity
import android.view.View
import android.view.WindowManager
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.R
import com.example.fixnear.data.model.technician.AssignStatus
import com.example.fixnear.data.model.technician.RejectRequest
import com.example.fixnear.data.model.technician.RepairStatus
import com.example.fixnear.data.model.technician.TechnicianAssignment
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityAssignmentBinding
import com.example.fixnear.ui.review.TechnicianReviewsActivity
import com.example.fixnear.ui.review.TechnicianFeedbackActivity
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import org.json.JSONObject
import retrofit2.Response
import java.io.IOException

class AssignmentActivity : AppCompatActivity() {

    private lateinit var binding: ActivityAssignmentBinding
    private lateinit var adapter: AssignmentAdapter

    private var allAssignments = listOf<TechnicianAssignment>()
    private var currentFilter = "all"

    private var isLoading = false
    private var isError = false
    private var loadRequestId = 0

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityAssignmentBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.btnRetryAssignments.setOnClickListener {
            loadAssignments()
        }

// UI08 - Mo man hinh danh gia Technician
        binding.btnOpenTechnicianReviews.setOnClickListener {

            val intent = Intent(
                this@AssignmentActivity,
                TechnicianReviewsActivity::class.java
            )

            startActivity(intent)
        }

// UI09 - Mo man hinh phan hoi khach hang
        binding.btnOpenTechnicianFeedback.setOnClickListener {

            val intent = Intent(
                this@AssignmentActivity,
                TechnicianFeedbackActivity::class.java
            )

            startActivity(intent)
        }


        setupRecyclerView()
        setupFilters()
        applyFilter(currentFilter)
    }

    override fun onResume() {
        super.onResume()
        loadAssignments()
    }

    // =====================================
    // RECYCLERVIEW
    // =====================================

    private fun setupRecyclerView() {
        adapter = AssignmentAdapter(
            onAccept = { assignment ->
                acceptAssignment(assignment)
            },
            onReject = { assignment ->
                showRejectDialog(assignment)
            },
            onItemClick = { assignment ->
                val intent = Intent(
                    this,
                    AssignmentDetailActivity::class.java
                )

                intent.putExtra(
                    "assignmentId",
                    assignment.repairAssignmentId
                )

                startActivity(intent)
            }
        )

        binding.rvAssignments.layoutManager =
            LinearLayoutManager(this)

        binding.rvAssignments.adapter = adapter
    }

    // =====================================
    // FILTER EVENTS
    // =====================================

    private fun setupFilters() {
        binding.tvFilterAll.setOnClickListener {
            applyFilter("all")
        }

        binding.tvFilterPending.setOnClickListener {
            applyFilter("pending")
        }

        binding.tvFilterActive.setOnClickListener {
            applyFilter("active")
        }

        binding.tvFilterDone.setOnClickListener {
            applyFilter("done")
        }
    }

    // =====================================
    // BACKEND STATUS CLASSIFICATION
    // =====================================

    private fun isPending(
        item: TechnicianAssignment
    ): Boolean {
        return item.assignmentStatus == AssignStatus.OFFERED &&
                item.repairStatus == 2
    }

    private fun isActive(
        item: TechnicianAssignment
    ): Boolean {
        return item.assignmentStatus == AssignStatus.ACCEPTED &&
                item.repairStatus in
                RepairStatus.ASSIGNED..RepairStatus.REPAIRING
    }

    private fun isCompleted(
        item: TechnicianAssignment
    ): Boolean {
        return item.assignmentStatus == AssignStatus.ACCEPTED &&
                (
                        item.repairStatus == RepairStatus.COMPLETED ||
                                item.repairStatus == 10
                        )
    }

    // =====================================
    // LOAD DATA FROM BACKEND
    // =====================================

    private fun loadAssignments() {
        val requestId = ++loadRequestId

        isLoading = true
        isError = false

        showLoadingState()

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.technicianApi.getAssignments()

                // Bo qua ket qua cua request cu
                if (requestId != loadRequestId) {
                    return@launch
                }

                if (response.isSuccessful) {
                    val responseBody = response.body()

                    if (responseBody == null) {
                        showErrorState(
                            "Backend không trả về dữ liệu công việc."
                        )
                        return@launch
                    }

                    allAssignments =
                        responseBody.data ?: emptyList()

                    isLoading = false
                    isError = false

                    updateStatistics()
                    applyFilter(currentFilter)

                } else {
                    val errorMessage = getHttpErrorMessage(
                        response.code(),
                        response.errorBody()?.string()
                    )

                    showErrorState(errorMessage)
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: IOException) {
                if (requestId == loadRequestId) {
                    showErrorState(
                        "Không thể kết nối Backend. " +
                                "Vui lòng kiểm tra mạng và thử lại."
                    )
                }

            } catch (e: Exception) {
                if (requestId == loadRequestId) {
                    showErrorState(
                        e.message
                            ?: "Không thể tải danh sách công việc."
                    )
                }
            }
        }
    }

    // =====================================
    // UI STATES
    // =====================================

    private fun showLoadingState() {
        binding.layoutLoadingAssignments.visibility =
            View.VISIBLE

        binding.layoutErrorAssignments.visibility =
            View.GONE

        binding.tvEmptyAssignments.visibility =
            View.GONE

        binding.rvAssignments.visibility =
            View.GONE
    }

    private fun showErrorState(message: String) {
        isLoading = false
        isError = true

        binding.layoutLoadingAssignments.visibility =
            View.GONE

        binding.layoutErrorAssignments.visibility =
            View.VISIBLE

        binding.tvErrorAssignments.text = message

        binding.tvEmptyAssignments.visibility =
            View.GONE

        binding.rvAssignments.visibility =
            View.GONE
    }

    private fun showContentState(
        hasItems: Boolean
    ) {
        binding.layoutLoadingAssignments.visibility =
            View.GONE

        binding.layoutErrorAssignments.visibility =
            View.GONE

        binding.tvEmptyAssignments.visibility =
            if (hasItems) View.GONE else View.VISIBLE

        binding.rvAssignments.visibility =
            if (hasItems) View.VISIBLE else View.GONE
    }

    // =====================================
    // STATISTICS
    // =====================================

    private fun updateStatistics() {
        val pending = allAssignments.count {
            isPending(it)
        }

        val active = allAssignments.count {
            isActive(it)
        }

        val completed = allAssignments.count {
            isCompleted(it)
        }

        binding.tvCountPending.text = pending.toString()
        binding.tvCountActive.text = active.toString()
        binding.tvCountDone.text = completed.toString()
    }

    // =====================================
    // FILTER LIST
    // =====================================

    private fun applyFilter(filter: String) {
        currentFilter = filter

        val filtered = when (filter) {
            "pending" -> allAssignments.filter {
                isPending(it)
            }

            "active" -> allAssignments.filter {
                isActive(it)
            }

            "done" -> allAssignments.filter {
                isCompleted(it)
            }

            else -> allAssignments
        }

        adapter.submitList(filtered)

        styleFilter(
            binding.tvFilterAll,
            filter == "all"
        )

        styleFilter(
            binding.tvFilterPending,
            filter == "pending"
        )

        styleFilter(
            binding.tvFilterActive,
            filter == "active"
        )

        styleFilter(
            binding.tvFilterDone,
            filter == "done"
        )

        // Khong hien Empty trong khi dang tai
        // hoac khi API dang bi loi.
        if (isLoading || isError) {
            return
        }

        showContentState(filtered.isNotEmpty())
    }

    // =====================================
    // FIXNEAR FILTER DESIGN
    // =====================================

    private fun styleFilter(
        view: TextView,
        selected: Boolean
    ) {
        val backgroundColor = ContextCompat.getColor(
            this,
            if (selected) {
                R.color.accent_gold
            } else {
                R.color.input_background
            }
        )

        val textColor = ContextCompat.getColor(
            this,
            if (selected) {
                R.color.button_primary_text
            } else {
                R.color.tab_unselected
            }
        )

        view.backgroundTintList =
            ColorStateList.valueOf(backgroundColor)

        view.setTextColor(textColor)
    }

    // =====================================
    // ACCEPT ASSIGNMENT
    // =====================================

    private fun acceptAssignment(
        assignment: TechnicianAssignment
    ) {
        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.technicianApi.accept(
                        assignment.repairAssignmentId
                    )

                if (response.isSuccessful) {
                    showMessage(
                        response.body()?.message
                            ?: "Đã nhận công việc thành công"
                    )

                    loadAssignments()
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
                    "Không thể kết nối Backend."
                )

            } catch (e: Exception) {
                showMessage(
                    e.message ?: "Không thể nhận công việc"
                )
            }
        }
    }

    // =====================================
    // FIXNEAR REJECT DIALOG
    // =====================================

    private fun showRejectDialog(
        assignment: TechnicianAssignment
    ) {
        val creamColor = ContextCompat.getColor(
            this,
            R.color.card_cream
        )

        val darkTextColor = ContextCompat.getColor(
            this,
            R.color.text_primary_card
        )

        val brownTextColor = ContextCompat.getColor(
            this,
            R.color.accent_brown
        )

        val inputBackgroundColor = ContextCompat.getColor(
            this,
            R.color.input_background
        )

        val inputTextColor = ContextCompat.getColor(
            this,
            R.color.input_text
        )

        val hintColor = ContextCompat.getColor(
            this,
            R.color.input_hint
        )

        val primaryColor = ContextCompat.getColor(
            this,
            R.color.accent_gold
        )

        val secondaryColor = ContextCompat.getColor(
            this,
            R.color.button_secondary
        )

        val whiteColor = ContextCompat.getColor(
            this,
            R.color.button_primary_text
        )

        // Popup container
        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL

            setPadding(
                dp(20),
                dp(22),
                dp(20),
                dp(12)
            )

            background = roundedBackground(
                creamColor,
                22f
            )
        }

        // Title
        val title = TextView(this).apply {
            text = "Từ chối công việc"
            textSize = 20f
            setTextColor(darkTextColor)
            setTypeface(null, Typeface.BOLD)
        }

        container.addView(title)

        // Description
        val description = TextView(this).apply {
            text = "Vui lòng nhập lý do từ chối công việc được giao."
            textSize = 14f
            setTextColor(brownTextColor)
        }

        val descriptionParams = LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT
        ).apply {
            topMargin = dp(10)
        }

        container.addView(
            description,
            descriptionParams
        )

        // Reason input
        val input = EditText(this).apply {
            hint = "Nhập lý do từ chối..."
            textSize = 14f

            setTextColor(inputTextColor)
            setHintTextColor(hintColor)

            minLines = 3
            maxLines = 5

            gravity = Gravity.TOP or Gravity.START

            filters = arrayOf(
                InputFilter.LengthFilter(500)
            )

            background = roundedBackground(
                inputBackgroundColor,
                12f
            )

            setPadding(
                dp(14),
                dp(12),
                dp(14),
                dp(12)
            )
        }

        val inputParams = LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT
        ).apply {
            topMargin = dp(16)
        }

        container.addView(input, inputParams)

        // Character limit
        val note = TextView(this).apply {
            text = "Tối đa 500 ký tự"
            textSize = 12f
            setTextColor(brownTextColor)
            gravity = Gravity.END
        }

        val noteParams = LinearLayout.LayoutParams(
            LinearLayout.LayoutParams.MATCH_PARENT,
            LinearLayout.LayoutParams.WRAP_CONTENT
        ).apply {
            topMargin = dp(8)
        }

        container.addView(note, noteParams)

        // Create dialog
        val dialog = AlertDialog.Builder(this)
            .setView(container)
            .setPositiveButton("Xác nhận", null)
            .setNegativeButton("Hủy", null)
            .create()

        dialog.setOnShowListener {
            dialog.window?.apply {
                setBackgroundDrawable(
                    roundedBackground(
                        creamColor,
                        22f
                    )
                )

                setLayout(
                    (
                            resources.displayMetrics.widthPixels *
                                    0.90f
                            ).toInt(),
                    WindowManager.LayoutParams.WRAP_CONTENT
                )
            }

            // Confirm button
            val confirmButton = dialog.getButton(
                AlertDialog.BUTTON_POSITIVE
            )

            confirmButton.setTextColor(whiteColor)

            confirmButton.backgroundTintList =
                ColorStateList.valueOf(primaryColor)

            confirmButton.isAllCaps = false

            confirmButton.setOnClickListener {
                val reason = input.text
                    .toString()
                    .trim()

                if (reason.isBlank()) {
                    input.error = "Vui lòng nhập lý do"
                    input.requestFocus()
                    return@setOnClickListener
                }

                dialog.dismiss()

                rejectAssignment(
                    assignment,
                    reason
                )
            }

            // Cancel button
            val cancelButton = dialog.getButton(
                AlertDialog.BUTTON_NEGATIVE
            )

            cancelButton.setTextColor(brownTextColor)

            cancelButton.backgroundTintList =
                ColorStateList.valueOf(secondaryColor)

            cancelButton.isAllCaps = false
        }

        dialog.show()
    }

    // =====================================
    // REJECT ASSIGNMENT
    // =====================================

    private fun rejectAssignment(
        assignment: TechnicianAssignment,
        reason: String
    ) {
        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.technicianApi.reject(
                        assignment.repairAssignmentId,
                        RejectRequest(reason)
                    )

                if (response.isSuccessful) {
                    showMessage(
                        response.body()?.message
                            ?: "Đã từ chối công việc"
                    )

                    loadAssignments()
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
                    "Không thể kết nối Backend."
                )

            } catch (e: Exception) {
                showMessage(
                    e.message ?: "Không thể từ chối công việc"
                )
            }
        }
    }

    // =====================================
    // UI HELPERS
    // =====================================

    private fun dp(value: Int): Int {
        return (
                value * resources.displayMetrics.density
                ).toInt()
    }

    private fun roundedBackground(
        color: Int,
        radiusDp: Float
    ): GradientDrawable {
        return GradientDrawable().apply {
            shape = GradientDrawable.RECTANGLE
            setColor(color)
            cornerRadius =
                radiusDp * resources.displayMetrics.density
        }
    }

    // =====================================
    // ERROR HANDLING
    // =====================================

    private fun getHttpErrorMessage(
        statusCode: Int,
        error: String?
    ): String {
        return when (statusCode) {
            401 ->
                "Phiên đăng nhập đã hết hạn. " +
                        "Vui lòng đăng nhập lại."

            403 ->
                "Bạn không có quyền thực hiện thao tác này."

            409 ->
                "Trạng thái công việc đã thay đổi. " +
                        "Vui lòng tải lại dữ liệu."

            else -> getErrorMessage(error)
        }
    }

    private fun getErrorMessage(
        error: String?
    ): String {
        if (error.isNullOrBlank()) {
            return "Đã xảy ra lỗi khi gọi API."
        }

        return try {
            val json = JSONObject(error)

            json.optString(
                "message",
                "Đã xảy ra lỗi."
            ).ifBlank {
                "Đã xảy ra lỗi."
            }

        } catch (e: Exception) {
            "Đã xảy ra lỗi khi gọi API."
        }
    }

    private fun showMessage(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_SHORT
        ).show()
    }
}
