package com.example.fixnear.ui.manager

import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityShopProfileBinding
import kotlinx.coroutines.launch

class ShopProfileActivity : AppCompatActivity() {

    private lateinit var binding: ActivityShopProfileBinding
    private lateinit var sessionManager: SessionManager
    private var shopId = -1

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityShopProfileBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)
        shopId = intent.getIntExtra("SHOP_ID", -1)

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.btnReload.setOnClickListener {
            loadProfile()
        }

        if (shopId <= 0) {
            showError("Không tìm thấy cửa hàng")
            return
        }

        loadProfile()
    }

    private fun loadProfile() {
        val token = sessionManager.getToken()

        if (token.isNullOrBlank()) {
            showError("Vui lòng đăng nhập lại")
            return
        }

        binding.tvError.visibility = View.GONE
        binding.btnReload.isEnabled = false
        binding.btnReload.text = "Đang tải..."

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.shopApi.getShopProfile(
                        "Bearer $token",
                        shopId
                    )

                if (response.isSuccessful) {
                    val shop = response.body()?.data

                    if (shop == null) {
                        showError("Không có dữ liệu cửa hàng")
                        return@launch
                    }

                    binding.tvShopName.text = shop.shopName

                    binding.tvVerification.text =
                        if (shop.isVerified) {
                            "✓ Đã xác minh"
                        } else {
                            "Chưa xác minh"
                        }

                    binding.tvDescription.text =
                        shop.description
                            ?.takeIf { it.isNotBlank() }
                            ?: "Chưa có mô tả"

                    binding.tvPhone.text =
                        shop.phone
                            ?.takeIf { it.isNotBlank() }
                            ?: "Chưa cập nhật"

                    binding.tvEmail.text =
                        shop.email
                            ?.takeIf { it.isNotBlank() }
                            ?: "Chưa cập nhật"

                    binding.tvTaxCode.text =
                        shop.taxCode
                            ?.takeIf { it.isNotBlank() }
                            ?: "Chưa cập nhật"

                } else {
                    showError(
                        "Không tải được cửa hàng. HTTP ${response.code()}"
                    )
                }

            } catch (e: Exception) {
                showError(
                    "Lỗi kết nối: ${e.localizedMessage}"
                )
            } finally {
                binding.btnReload.isEnabled = true
                binding.btnReload.text = "Tải lại thông tin"
            }
        }
    }

    private fun showError(message: String) {
        binding.tvError.text = message
        binding.tvError.visibility = View.VISIBLE
    }
}
