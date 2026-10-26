package com.example.fixnear

import android.content.Intent
import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.model.LoginRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityLoginBinding
import kotlinx.coroutines.launch

class LoginActivity : AppCompatActivity() {

    //Kiểm tra dữ liệu đăng nhập
    private fun login(){
        val account = binding.edtEmail.text.toString().trim()
        val password = binding.edtPassword.text.toString().trim()

        if(account.isEmpty()){
            binding.edtEmail.error = "Vui lòng nhập tên đăng nhập"
            return
        }
        if(password.isEmpty()){
            binding.edtPassword.error = "Vui lòng nhập mật khẩu"
            return
        }

        val request =
            if(account.contains("@"))
                LoginRequest(
                    email = account,
                    phone = null,
                    password = password
                )
            else
                LoginRequest(
                    phone = account,
                    email = null,
                    password = password
                )

        callLoginApI(request)
    }

    //Gọi api đăng nhập
    private fun callLoginApI(request: LoginRequest){
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.authApi.login(request)

                if(response.isSuccessful){
                    val body = response.body()
                    val user = body?.data

                    if(user != null){
                        Toast.makeText(
                            this@LoginActivity,
                            body.message,
                            Toast.LENGTH_SHORT
                        ).show()

                        val intent = Intent(this@LoginActivity, HomeActivity::class.java)

                        with(intent) {
                            putExtra("USER_ID",user.userId)
                            putExtra("FULL_NAME",user.fullName)
                            putExtra("ROLE_TYPE",user.roleType)
                            putExtra("TOKEN",user.token)
                        }

                        startActivity(intent)
                        finish()
                    }
                    else {
                        Toast.makeText(
                            this@LoginActivity,
                            "Không nhận được dữ liệu người dùng",
                            Toast.LENGTH_SHORT
                        ).show()
                    }
                }
                else{
                    val errorJson = response.errorBody()?.string()

                    val message = try {
                        org.json.JSONObject(errorJson ?: "")
                            .optString("message", "Đăng nhập thất bại")
                    } catch (e: Exception) {
                        "Đăng nhập thất bại"
                    }
                    Toast.makeText(
                        this@LoginActivity,
                        message,
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
            catch (e : Exception){
                Toast.makeText(
                    this@LoginActivity,
                    e.message,
                    Toast.LENGTH_SHORT
                ).show()
            }
        }
    }

    private lateinit var binding: ActivityLoginBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.btnLogin.setOnClickListener {
            login()
        }
        binding.btnGoRegister.setOnClickListener {
            val intent = Intent(this@LoginActivity, RegisterActivity::class.java)
            startActivity(intent)
        }
    }
}