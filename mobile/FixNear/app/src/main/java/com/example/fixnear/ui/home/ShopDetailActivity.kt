package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.bumptech.glide.Glide
import com.example.fixnear.R
import com.example.fixnear.data.model.customer.ShopPublicServiceItem
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityShopDetailBinding
import kotlinx.coroutines.launch
import java.util.Locale

class ShopDetailActivity : AppCompatActivity() {

    private lateinit var binding: ActivityShopDetailBinding
    private lateinit var serviceAdapter: ShopServiceCardAdapter

    private var shopId = -1
    private var selectedServiceId: Int? = null
    private var selectedServiceName: String? = null
    private var page = 0
    private var totalPages = 1
    private var loading = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityShopDetailBinding.inflate(layoutInflater)
        setContentView(binding.root)

        shopId = intent.getIntExtra("SHOP_ID", -1)
        selectedServiceId = intent.getIntExtra("SERVICE_ID", -1).takeIf { it > 0 }
        selectedServiceName = intent.getStringExtra("SERVICE_NAME")

        setupViews()

        if (shopId <= 0) {
            binding.tvStatus.text = "Cửa hàng không hợp lệ."
            binding.progress.visibility = View.GONE
            return
        }

        loadProfile()
        loadServices(1)
    }

    private fun setupViews() {
        binding.btnBack.setOnClickListener { finish() }
        binding.btnMoreServices.setOnClickListener { loadServices(page + 1) }
        binding.btnShopReviews.setOnClickListener {
            startActivity(Intent(this, ShopReviewsActivity::class.java).apply {
                putExtra("SHOP_ID", shopId)
                putExtra("SHOP_NAME", binding.tvName.text.toString())
            })
        }

        serviceAdapter = ShopServiceCardAdapter { service ->
            onServiceSelected(service)
        }

        binding.rvServices.apply {
            layoutManager = LinearLayoutManager(this@ShopDetailActivity)
            adapter = serviceAdapter
            isNestedScrollingEnabled = false
        }

        binding.btnBooking.setOnClickListener {
            val service = serviceAdapter.getSelectedItem()
            if (service == null) {
                binding.tvStatus.text = "Hãy chọn một dịch vụ trước khi tạo yêu cầu sửa chữa."
                return@setOnClickListener
            }
            startActivity(
                Intent(this, CreateRepairRequestActivity::class.java).apply {
                    putExtra("SHOP_ID", shopId)
                    putExtra("SHOP_NAME", binding.tvName.text.toString())
                    putExtra("SERVICE_ID", service.serviceId)
                    putExtra("SERVICE_NAME", service.serviceName)
                }
            )
        }
        updateBookingState()
    }

    private fun onServiceSelected(service: ShopPublicServiceItem) {
        selectedServiceId = service.serviceId
        selectedServiceName = service.serviceName
        binding.tvSelectedService.text = "Dịch vụ đã chọn: ${service.serviceName}"
        updateBookingState()
    }

    private fun updateBookingState() {
        val ready = selectedServiceId != null
        binding.btnBooking.isEnabled = ready
        binding.btnBooking.alpha = if (ready) 1f else 0.55f
        binding.tvSelectedService.visibility = if (ready) View.VISIBLE else View.GONE
        binding.tvSelectedService.text = if (ready) {
            "Dịch vụ đã chọn: ${selectedServiceName.orEmpty()}"
        } else {
            ""
        }
    }

    private fun loadProfile() = lifecycleScope.launch {
        try {
            val response = RetrofitClient.shopPublicApi.getProfile(shopId)
            if (!response.isSuccessful) {
                binding.tvStatus.text = "Không tải được hồ sơ cửa hàng (HTTP ${response.code()})."
                return@launch
            }
            val shop = response.body()?.data ?: return@launch
            binding.tvName.text = shop.shopName
            binding.tvRating.text = buildString {
                append(String.format(Locale("vi", "VN"), "★ %.1f", shop.ratingAverage))
                if (shop.isVerified) append("  • Đã xác minh")
            }
            binding.tvAddress.text = listOfNotNull(
                shop.address?.addressLine,
                shop.address?.wardName,
                shop.address?.provinceName
            ).filter { it.isNotBlank() }.joinToString(", ")
            binding.tvContact.text = listOfNotNull(shop.phone, shop.email)
                .filter { it.isNotBlank() }
                .joinToString(" • ")
            binding.tvDescription.text = shop.description?.takeIf { it.isNotBlank() }
                ?: "Cửa hàng chưa cập nhật mô tả."

            val primaryImage = shop.images.firstOrNull { it.isPrimary }?.imageUrl
                ?: shop.images.firstOrNull()?.imageUrl
            val displayImage = primaryImage ?: shop.logoUrl

            if (!displayImage.isNullOrBlank()) {
                Glide.with(this@ShopDetailActivity)
                    .load(displayImage)
                    .centerCrop()
                    .placeholder(R.drawable.logo_fixnear)
                    .error(R.drawable.logo_fixnear)
                    .into(binding.imgCover)

                Glide.with(this@ShopDetailActivity)
                    .load(shop.logoUrl ?: displayImage)
                    .centerCrop()
                    .placeholder(R.drawable.logo_fixnear)
                    .error(R.drawable.logo_fixnear)
                    .into(binding.imgAvatar)
            } else {
                binding.imgCover.setImageResource(R.drawable.logo_fixnear)
                binding.imgAvatar.setImageResource(R.drawable.logo_fixnear)
            }
        } catch (e: Exception) {
            binding.tvStatus.text = "Lỗi tải cửa hàng: ${e.localizedMessage}"
        }
    }

    private fun loadServices(nextPage: Int) {
        if (loading || nextPage > totalPages) return
        loading = true
        binding.progress.visibility = View.VISIBLE
        binding.btnMoreServices.visibility = View.GONE

        lifecycleScope.launch {
            try {
                val response = RetrofitClient.shopPublicApi.getServices(shopId, nextPage, 10)
                if (!response.isSuccessful) {
                    binding.tvStatus.text = "Không tải được dịch vụ (HTTP ${response.code()})."
                    return@launch
                }
                val body = response.body() ?: return@launch
                page = body.page
                totalPages = body.totalPages
                serviceAdapter.submitItems(body.data, append = nextPage > 1)
                if (selectedServiceId != null) {
                    serviceAdapter.setSelectedService(selectedServiceId)
                    if (body.data.any { it.serviceId == selectedServiceId }) {
                        selectedServiceName = body.data.first { it.serviceId == selectedServiceId }.serviceName
                        updateBookingState()
                    }
                }
                binding.tvStatus.text = if (body.total == 0) {
                    "Cửa hàng chưa có dịch vụ đang hoạt động."
                } else {
                    "Chạm vào một dịch vụ để tạo yêu cầu sửa chữa."
                }
            } catch (e: Exception) {
                binding.tvStatus.text = "Lỗi kết nối: ${e.localizedMessage}"
            } finally {
                loading = false
                binding.progress.visibility = View.GONE
                binding.btnMoreServices.visibility = if (page < totalPages) View.VISIBLE else View.GONE
            }
        }
    }
}
