
package com.example.fixnear.ui.profile

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.bumptech.glide.Glide
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityProfileBinding
import com.example.fixnear.ui.auth.LoginActivity
import kotlinx.coroutines.launch

class ProfileActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProfileBinding
    private lateinit var sessionManager: SessionManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityProfileBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        setupNavigation()
    }

    override fun onResume() {
        super.onResume()
        loadProfile()
    }

    // Thiết lập điều hướng
    private fun setupNavigation() {

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.navHome.setOnClickListener {
            finish()
        }

        binding.btnLogout.setOnClickListener {
            confirmLogout()
        }
    }

    // Tải hồ sơ khách hàng từ Backend

    private fun loadProfile() {
        lifecycleScope.launch {
            try {
                val token = sessionManager.getToken()

                if (token.isNullOrBlank()) {
                    Log.e("PROFILE_DEBUG", "Token đang null hoặc rỗng")
                    Toast.makeText(
                        this@ProfileActivity,
                        "Không tìm thấy phiên đăng nhập",
                        Toast.LENGTH_LONG
                    ).show()
                    return@launch
                }

                val response = RetrofitClient.customerApi.getProfile(
                    "Bearer $token"
                )

                Log.d(
                    "PROFILE_DEBUG",
                    "HTTP CODE: ${response.code()}"
                )

                if (response.isSuccessful) {
                    val body = response.body()

                    Log.d(
                        "PROFILE_DEBUG",
                        "RESPONSE BODY: $body"
                    )

                    val profile = body

                    if (profile == null) {
                        Log.e(
                            "PROFILE_DEBUG",
                            "Data trong response đang null"
                        )
                        return@launch
                    }

                    Log.d(
                        "PROFILE_DEBUG",
                        "FULL NAME: ${profile.fullName}"
                    )

                    // Bind dữ liệu vào giao diện
                    binding.tvProfileName.text = profile.fullName
                    binding.tvProfileEmail.text =
                        profile.email ?: "Chưa cập nhật email"
                    binding.tvProfilePhone.text =
                        profile.phone ?: "Chưa cập nhật số điện thoại"

                } else {
                    val error = response.errorBody()?.string()

                    Log.e(
                        "PROFILE_DEBUG",
                        "ERROR BODY: $error"
                    )
                }

            } catch (e: Exception) {
                Log.e(
                    "PROFILE_DEBUG",
                    "Lỗi tải hồ sơ",
                    e
                )
            }
        }
    }

    // Hiển thị ảnh đại diện bằng URL
    private fun loadAvatar(avatarUrl: String?) {

        if (!avatarUrl.isNullOrBlank()) {

            Glide.with(this)
                .load(avatarUrl)
                .placeholder(android.R.drawable.ic_menu_myplaces)
                .error(android.R.drawable.ic_menu_myplaces)
                .circleCrop()
                .into(binding.tvProfileAvatar)

        } else {

            binding.tvProfileAvatar.setImageResource(
                android.R.drawable.ic_menu_myplaces
            )
        }
    }

    // Xác nhận trước khi đăng xuất
    private fun confirmLogout() {

        AlertDialog.Builder(this)
            .setTitle("Đăng xuất")
            .setMessage("Bạn có chắc chắn muốn đăng xuất khỏi FixNear?")
            .setNegativeButton("Hủy", null)
            .setPositiveButton("Đăng xuất") { _, _ ->
                logout()
            }
            .show()
    }

    // Xóa session và quay về Login
    private fun logout() {

        sessionManager.clearSession()

        val intent = Intent(this, LoginActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or
                    Intent.FLAG_ACTIVITY_CLEAR_TASK
        }

        startActivity(intent)
        finish()
    }
}
