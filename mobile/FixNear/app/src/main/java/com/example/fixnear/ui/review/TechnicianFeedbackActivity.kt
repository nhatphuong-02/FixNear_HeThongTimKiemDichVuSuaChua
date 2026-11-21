
package com.example.fixnear.ui.review

import android.os.Bundle
import android.view.View
import android.widget.ProgressBar
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.R
import com.example.fixnear.data.remote.RetrofitClient
import com.google.android.material.button.MaterialButton
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch

class TechnicianFeedbackActivity : AppCompatActivity() {

    private lateinit var adapter: TechnicianReviewAdapter

    private lateinit var rvFeedback: RecyclerView
    private lateinit var layoutState: View
    private lateinit var tvStateTitle: TextView
    private lateinit var tvStateMessage: TextView
    private lateinit var tvFeedbackCount: TextView
    private lateinit var btnRetry: MaterialButton
    private lateinit var btnLoadMore: MaterialButton
    private lateinit var progressFeedback: ProgressBar
    private lateinit var progressMore: ProgressBar

    private var technicianId = -1
    private var currentPage = 0
    private var totalReviews = 0
    private var loadedReviews = 0
    private var displayedComments = 0
    private var hasMore = false
    private var isLoading = false

    companion object {
        private const val PAGE_SIZE = 10
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(R.layout.activity_technician_feedback)

        setupViews()
        setupRecyclerView()
        setupEvents()

        loadFeedback(1)
    }

    private fun setupViews() {
        rvFeedback = findViewById(R.id.rvTechnicianFeedback)
        layoutState = findViewById(R.id.layoutFeedbackState)

        tvStateTitle =
            findViewById(R.id.tvFeedbackStateTitle)

        tvStateMessage =
            findViewById(R.id.tvFeedbackStateMessage)

        tvFeedbackCount =
            findViewById(R.id.tvFeedbackCount)

        btnRetry = findViewById(R.id.btnRetryFeedback)
        btnLoadMore = findViewById(R.id.btnLoadMoreFeedback)

        progressFeedback =
            findViewById(R.id.progressFeedback)

        progressMore =
            findViewById(R.id.progressMoreFeedback)
    }

    private fun setupRecyclerView() {
        adapter = TechnicianReviewAdapter()

        rvFeedback.layoutManager =
            LinearLayoutManager(this)

        rvFeedback.adapter = adapter
    }

    private fun setupEvents() {
        findViewById<TextView>(
            R.id.btnBackFeedback
        ).setOnClickListener {
            finish()
        }

        btnRetry.setOnClickListener {
            loadFeedback(1)
        }

        btnLoadMore.setOnClickListener {
            loadFeedback(currentPage + 1)
        }
    }

    private fun loadFeedback(page: Int) {

        if (isLoading) return

        if (page > 1 && (!hasMore || currentPage == 0)) {
            return
        }

        if (page == 1) {
            currentPage = 0
            totalReviews = 0
            loadedReviews = 0
            displayedComments = 0
            hasMore = false

            adapter.submitReviews(emptyList())

            tvFeedbackCount.text = "Số nhận xét: --"
        }

        isLoading = true
        showLoading(page)

        lifecycleScope.launch {

            try {

                // Lay technicianId tu ho so tai khoan
                if (page == 1) {

                    val profileResponse =
                        RetrofitClient.technicianApi
                            .getMyReviewProfile()

                    if (!profileResponse.isSuccessful) {
                        throw IllegalStateException(
                            "Không thể tải hồ sơ kỹ thuật viên " +
                                    "(HTTP ${profileResponse.code()})."
                        )
                    }

                    val profile = profileResponse.body()?.data

                    if (profile == null ||
                        profile.technicianId <= 0
                    ) {
                        throw IllegalStateException(
                            "Không tìm thấy hồ sơ kỹ thuật viên."
                        )
                    }

                    technicianId = profile.technicianId
                }

                if (technicianId <= 0) {
                    throw IllegalStateException(
                        "Mã kỹ thuật viên không hợp lệ."
                    )
                }

                // Lay trang danh gia tu Backend
                val response =
                    RetrofitClient.technicianApi
                        .getTechnicianReviews(
                            technicianId = technicianId,
                            page = page,
                            pageSize = PAGE_SIZE
                        )

                if (!response.isSuccessful) {
                    throw IllegalStateException(
                        "Không thể tải nhận xét " +
                                "(HTTP ${response.code()})."
                    )
                }

                val result = response.body()?.data
                    ?: throw IllegalStateException(
                        "Dữ liệu nhận xét không hợp lệ."
                    )

                // UI09 chi hien thi danh gia co binh luan
                val comments = result.reviews.filter {
                    !it.comment.isNullOrBlank()
                }

                adapter.submitReviews(
                    reviews = comments,
                    append = page > 1
                )

                currentPage = result.page
                totalReviews = result.totalCount

                // Dem tong review da doc, ke ca review chi co sao
                loadedReviews += result.reviews.size

                // Dem so binh luan da hien thi
                displayedComments += comments.size

                hasMore =
                    result.reviews.isNotEmpty() &&
                            loadedReviews < totalReviews

                tvFeedbackCount.text =
                    "Số nhận xét đã tải: $displayedComments"

                updateContentState()

            } catch (e: CancellationException) {
                throw e

            } catch (e: Exception) {

                if (page == 1) {

                    hasMore = false

                    showState(
                        title = "Không thể tải phản hồi",
                        message = e.message
                            ?: "Vui lòng kiểm tra kết nối và thử lại.",
                        showRetry = true
                    )

                } else {

                    // Giu nguyen du lieu trang truoc
                    Toast.makeText(
                        this@TechnicianFeedbackActivity,
                        e.message
                            ?: "Không thể tải thêm nhận xét.",
                        Toast.LENGTH_LONG
                    ).show()
                }

            } finally {

                isLoading = false

                progressFeedback.visibility = View.GONE
                progressMore.visibility = View.GONE

                btnLoadMore.visibility =
                    if (hasMore) View.VISIBLE else View.GONE
            }
        }
    }

    private fun updateContentState() {

        if (displayedComments > 0) {

            rvFeedback.visibility = View.VISIBLE
            layoutState.visibility = View.GONE

        } else {

            val message = if (hasMore) {
                "Chưa tìm thấy nhận xét trong các đánh giá " +
                        "đã tải. Nhấn Xem thêm để tiếp tục."
            } else {
                "Khách hàng chưa để lại nhận xét bằng chữ."
            }

            showState(
                title = "Chưa có nhận xét",
                message = message,
                showRetry = false
            )
        }
    }

    private fun showLoading(page: Int) {

        btnRetry.visibility = View.GONE
        btnLoadMore.visibility = View.GONE

        if (page == 1) {

            rvFeedback.visibility = View.GONE
            layoutState.visibility = View.GONE

            progressFeedback.visibility = View.VISIBLE

        } else {

            progressMore.visibility = View.VISIBLE
        }
    }

    private fun showState(
        title: String,
        message: String,
        showRetry: Boolean
    ) {

        rvFeedback.visibility = View.GONE
        layoutState.visibility = View.VISIBLE

        tvStateTitle.text = title
        tvStateMessage.text = message

        btnRetry.visibility =
            if (showRetry) View.VISIBLE else View.GONE
    }
}
