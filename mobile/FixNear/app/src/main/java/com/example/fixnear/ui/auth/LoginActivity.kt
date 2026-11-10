package com.example.fixnear.ui.auth

import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.LoginRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityLoginBinding
import com.example.fixnear.helper.setupPasswordToggle
import com.example.fixnear.ui.home.HomeActivity
import kotlinx.coroutines.launch
import org.json.JSONObject

class LoginActivity : AppCompatActivity() {

    private lateinit var binding: ActivityLoginBinding
    private lateinit var sessionManager: SessionManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        setupPasswordToggle(
            binding.edtPassword,
            binding.btnTogglePassword
        )

        setupEvents()
    }

    private fun setupEvents() {
        binding.btnLogin.setOnClickListener {
            login()
        }

        binding.btnGoRegister.setOnClickListener {
            startActivity(
                Intent(this, RegisterActivity::class.java)
            )
        }
    }

    private fun login() {
        val account = binding.edtEmail.text.toString().trim()
        val password = binding.edtPassword.text.toString()

        if (account.isBlank()) {
            binding.edtEmail.error = "Vui lòng nhập email hoặc số điện thoại"
            binding.edtEmail.requestFocus()
            return
        }

        if (password.isBlank()) {
            binding.edtPassword.error = "Vui lòng nhập mật khẩu"
            binding.edtPassword.requestFocus()
            return
        }

        val request =
            if(account.contains("@"))
                LoginRequest(
                    null,
                    account,
                    password
                )
            else
                LoginRequest(
                    account,
                    null,
                    password
                )

        callLoginApi(request)
    }

    private fun callLoginApi(request: LoginRequest) {

        lifecycleScope.launch {

            binding.btnLogin.isEnabled = false

            try {
                val response = RetrofitClient.authApi.login(request)

                if (response.isSuccessful) {

                    val loginData = response.body()?.data

                    if (loginData == null) {
                        Toast.makeText(
                            this@LoginActivity,
                            "Không nhận được dữ liệu đăng nhập",
                            Toast.LENGTH_LONG
                        ).show()
                        return@launch
                    }

                    val token = loginData.token

                    if (token.isNullOrBlank()) {
                        Log.e(
                            "LOGIN_ERROR",
                            "Đăng nhập thành công nhưng Token bị null hoặc rỗng"
                        )

                        Toast.makeText(
                            this@LoginActivity,
                            "Không nhận được Token từ máy chủ",
                            Toast.LENGTH_LONG
                        ).show()

                        return@launch
                    }

                    // Lưu thông tin tài khoản và Token
                    sessionManager.saveLogin(
                        userId = loginData.userId,
                        fullName = loginData.fullName,
                        roleType = loginData.roleType,
                        token = token
                    )

                    // Kiểm tra trạng thái lưu Token
                    val savedToken = sessionManager.getToken()

                    if (savedToken.isNullOrBlank()) {
                        Toast.makeText(
                            this@LoginActivity,
                            "Không thể lưu phiên đăng nhập",
                            Toast.LENGTH_LONG
                        ).show()
                        return@launch
                    }

                    Log.d(
                        "LOGIN_SUCCESS",
                        "Đăng nhập thành công. Token đã được lưu."
                    )

                    Toast.makeText(
                        this@LoginActivity,
                        response.body()?.message ?: "Đăng nhập thành công",
                        Toast.LENGTH_SHORT
                    ).show()

                    // Chuyển sang màn hình Home
                    val intent = Intent(
                        this@LoginActivity,
                        HomeActivity::class.java
                    )

                    intent.flags =
                        Intent.FLAG_ACTIVITY_NEW_TASK or
                                Intent.FLAG_ACTIVITY_CLEAR_TASK

                    startActivity(intent)

                } else {

                    val errorBody = response.errorBody()?.string()

                    Log.e(
                        "LOGIN_ERROR",
                        "HTTP ${response.code()} - $errorBody"
                    )

                    val message = try {
                        JSONObject(errorBody ?: "{}")
                            .optString(
                                "message",
                                "Đăng nhập thất bại (${response.code()})"
                            )
                    } catch (e: Exception) {
                        "Đăng nhập thất bại (${response.code()})"
                    }

                    Toast.makeText(
                        this@LoginActivity,
                        message,
                        Toast.LENGTH_LONG
                    ).show()
                }

            } catch (e: Exception) {

                Log.e(
                    "LOGIN_ERROR",
                    "Lỗi gọi API đăng nhập",
                    e
                )

                Toast.makeText(
                    this@LoginActivity,
                    "Không thể kết nối máy chủ. Vui lòng thử lại.",
                    Toast.LENGTH_LONG
                ).show()

            } finally {
                binding.btnLogin.isEnabled = true
            }
        }
    }
}
