package com.example.fixnear.ui.home

import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityShopReviewsBinding
import kotlinx.coroutines.launch

class ShopReviewsActivity : AppCompatActivity() {
    private lateinit var binding: ActivityShopReviewsBinding
    private lateinit var adapter: ShopReviewAdapter
    private var shopId = -1
    private var page = 0
    private var totalCount = 0
    private var loadedCount = 0
    private var loading = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityShopReviewsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        shopId = intent.getIntExtra("SHOP_ID", -1)
        binding.tvShopName.text = intent.getStringExtra("SHOP_NAME") ?: "Đánh giá cửa hàng"
        binding.btnBack.setOnClickListener { finish() }
        binding.btnLoadMore.setOnClickListener { loadPage(page + 1) }
        binding.btnRefresh.setOnClickListener {
            if (!loading) {
                page = 0
                loadedCount = 0
                totalCount = 0
                adapter.submit(emptyList(), false)
                loadPage(1)
            }
        }
        adapter = ShopReviewAdapter()
        binding.rvReviews.layoutManager = LinearLayoutManager(this)
        binding.rvReviews.adapter = adapter
        if (shopId > 0) loadPage(1)
        else binding.tvState.text = "Không tìm thấy cửa hàng."
    }

    private fun loadPage(nextPage: Int) {
        if (loading || (page > 0 && loadedCount >= totalCount)) return
        loading = true
        binding.progress.visibility = View.VISIBLE
        binding.btnLoadMore.visibility = View.GONE
        binding.tvState.text = "Đang tải đánh giá..."
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.shopPublicApi.getShopReviews(shopId, nextPage, 10)
                if (!response.isSuccessful) {
                    binding.tvState.text = "Không thể tải đánh giá (HTTP ${response.code()})."
                    return@launch
                }
                val result = response.body()?.data
                if (result == null) {
                    binding.tvState.text = "Dữ liệu đánh giá không hợp lệ."
                    return@launch
                }
                page = result.page
                totalCount = result.totalCount
                loadedCount += result.reviews.size
                adapter.submit(result.reviews, nextPage > 1)
                binding.tvState.text = if (totalCount == 0) {
                    "Cửa hàng chưa có đánh giá nào."
                } else {
                    "Đã hiển thị $loadedCount / $totalCount đánh giá"
                }
            } catch (e: Exception) {
                binding.tvState.text = "Lỗi kết nối: ${e.localizedMessage}"
            } finally {
                loading = false
                binding.progress.visibility = View.GONE
                binding.btnLoadMore.visibility =
                    if (page > 0 && loadedCount < totalCount) View.VISIBLE else View.GONE
            }
        }
    }
}
