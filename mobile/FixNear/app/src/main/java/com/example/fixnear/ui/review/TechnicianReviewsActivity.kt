
package com.example.fixnear.ui.review

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityTechnicianReviewsBinding
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.launch
import java.util.Locale
import kotlin.math.roundToInt

class TechnicianReviewsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityTechnicianReviewsBinding
    private lateinit var reviewAdapter: TechnicianReviewAdapter

    private var technicianId = -1
    private var currentPage = 0
    private var totalCount = 0
    private var displayedCount = 0
    private var ratingAverage: Double? = null
    private var isLoading = false

    companion object {
        private const val PAGE_SIZE = 10
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityTechnicianReviewsBinding.inflate(
            layoutInflater
        )
        setContentView(binding.root)

        setupRecyclerView()
        setupEvents()

        loadReviews(1)
    }

    private fun setupRecyclerView() {
        reviewAdapter = TechnicianReviewAdapter()

        binding.rvTechnicianReviews.layoutManager =
            LinearLayoutManager(this)

        binding.rvTechnicianReviews.adapter = reviewAdapter
    }

    private fun setupEvents() {

        binding.btnBackReviews.setOnClickListener {
            finish()
        }

        binding.btnRetryReviews.setOnClickListener {
            loadReviews(1)
        }

        binding.btnLoadMoreReviews.setOnClickListener {
            loadReviews(currentPage + 1)
        }
    }

    private fun loadReviews(nextPage: Int) {

        if (isLoading) return

        if (nextPage > 1 &&
            (currentPage == 0 || displayedCount >= totalCount)
        ) {
            return
        }

        isLoading = true
        showLoading(nextPage)

        lifecycleScope.launch {
            try {

                // Trang dau: lay TechnicianId tu JWT
                if (nextPage == 1) {

                    val profileResponse =
                        RetrofitClient.technicianApi
                            .getMyReviewProfile()

                    if (!profileResponse.isSuccessful) {
                        showLoadError(
                            nextPage,
                            "Không thể tải hồ sơ Technician " +
                                    "(HTTP ${profileResponse.code()})."
                        )
                        return@launch
                    }

                    val profile = profileResponse.body()?.data

                    if (profile == null ||
                        profile.technicianId <= 0
                    ) {
                        showLoadError(
                            nextPage,
                            "Không tìm thấy hồ sơ kỹ thuật viên."
                        )
                        return@launch
                    }

                    technicianId = profile.technicianId
                    ratingAverage = profile.ratingAverage
                }

                if (technicianId <= 0) {
                    showLoadError(
                        nextPage,
                        "Mã kỹ thuật viên không hợp lệ."
                    )
                    return@launch
                }

                // Lay danh sach danh gia tu Backend
                val response =
                    RetrofitClient.technicianApi
                        .getTechnicianReviews(
                            technicianId = technicianId,
                            page = nextPage,
                            pageSize = PAGE_SIZE
                        )

                if (!response.isSuccessful) {
                    showLoadError(
                        nextPage,
                        "Không thể tải đánh giá " +
                                "(HTTP ${response.code()})."
                    )
                    return@launch
                }

                val result = response.body()?.data

                if (result == null) {
                    showLoadError(
                        nextPage,
                        "Dữ liệu đánh giá không hợp lệ."
                    )
                    return@launch
                }

                // Cap nhat phan trang
                currentPage = result.page
                totalCount = result.totalCount

                if (nextPage == 1) {

                    reviewAdapter.submitReviews(
                        result.reviews,
                        append = false
                    )

                    displayedCount = result.reviews.size

                } else {

                    reviewAdapter.submitReviews(
                        result.reviews,
                        append = true
                    )

                    displayedCount += result.reviews.size
                }

                updateReviewSummary()

                if (displayedCount == 0) {
                    showEmptyState()
                } else {
                    binding.rvTechnicianReviews.visibility =
                        View.VISIBLE

                    binding.layoutReviewsState.visibility =
                        View.GONE
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: Exception) {

                showLoadError(
                    nextPage,
                    "Không thể kết nối máy chủ. " +
                            "Vui lòng kiểm tra kết nối và thử lại."
                )

            } finally {

                isLoading = false

                binding.progressReviews.visibility =
                    View.GONE

                binding.progressMoreReviews.visibility =
                    View.GONE

                binding.btnLoadMoreReviews.visibility =
                    if (currentPage > 0 &&
                        displayedCount < totalCount
                    ) {
                        View.VISIBLE
                    } else {
                        View.GONE
                    }
            }
        }
    }

    private fun updateReviewSummary() {

        binding.tvReviewTotalCount.text =
            "$totalCount đánh giá"

        val average = ratingAverage

        if (totalCount == 0) {

            binding.tvReviewAverage.text = "--/5"
            binding.tvReviewStars.text =
                "Chưa có điểm đánh giá"

        } else if (average != null && average > 0.0) {

            binding.tvReviewAverage.text =
                String.format(
                    Locale("vi", "VN"),
                    "%.1f/5",
                    average
                )

            val roundedStars =
                average.roundToInt().coerceIn(0, 5)

            binding.tvReviewStars.text =
                "★".repeat(roundedStars) +
                        "☆".repeat(5 - roundedStars)

        } else {

            binding.tvReviewAverage.text = "--/5"
            binding.tvReviewStars.text =
                "Điểm trung bình chưa cập nhật"
        }
    }

    private fun showLoading(page: Int) {

        binding.btnRetryReviews.visibility = View.GONE
        binding.btnLoadMoreReviews.visibility = View.GONE

        if (page == 1) {

            binding.rvTechnicianReviews.visibility =
                View.GONE

            binding.layoutReviewsState.visibility =
                View.GONE

            binding.progressReviews.visibility =
                View.VISIBLE

        } else {

            binding.progressMoreReviews.visibility =
                View.VISIBLE
        }
    }

    private fun showEmptyState() {

        binding.rvTechnicianReviews.visibility =
            View.GONE

        binding.layoutReviewsState.visibility =
            View.VISIBLE

        binding.tvReviewsStateTitle.text =
            "Chưa có đánh giá"

        binding.tvReviewsStateMessage.text =
            "Khi khách hàng đánh giá công việc của bạn, " +
                    "nhận xét sẽ xuất hiện tại đây."

        binding.btnRetryReviews.visibility =
            View.GONE
    }

    private fun showLoadError(
        page: Int,
        message: String
    ) {

        if (page == 1) {

            binding.rvTechnicianReviews.visibility =
                View.GONE

            binding.layoutReviewsState.visibility =
                View.VISIBLE

            binding.tvReviewsStateTitle.text =
                "Không thể tải đánh giá"

            binding.tvReviewsStateMessage.text = message

            binding.btnRetryReviews.visibility =
                View.VISIBLE

        } else {

            // Loi trang tiep theo: giu danh sach da tai
            Toast.makeText(
                this,
                message,
                Toast.LENGTH_LONG
            ).show()
        }
    }
}
