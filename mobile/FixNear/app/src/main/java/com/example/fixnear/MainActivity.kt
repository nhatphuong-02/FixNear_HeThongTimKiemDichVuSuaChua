package com.example.fixnear

import android.content.Intent
import android.os.Bundle
import android.os.PersistableBundle
import android.util.Log
import android.widget.Button
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.model.LoginRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityMainBinding
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {
    private lateinit var binding: ActivityMainBinding

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

    private fun callLoginApI(request: LoginRequest){
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.authApi.login(request)

                if(response.isSuccessful){
                    val body = response.body()
                    val user = body?.data

                    if(user != null){
                        Toast.makeText(
                            this@MainActivity,
                            body.message,
                            Toast.LENGTH_SHORT
                        ).show()

                        val intent = Intent(this@MainActivity, HomeActivity::class.java)

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
                            this@MainActivity,
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
                        this@MainActivity,
                        message,
                        Toast.LENGTH_SHORT
                    ).show()
                }
            }
            catch (e : Exception){
                Toast.makeText(
                    this@MainActivity,
                    e.message,
                    Toast.LENGTH_SHORT
                ).show()
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.btnLogin.setOnClickListener {
            login()
        }
    }
}