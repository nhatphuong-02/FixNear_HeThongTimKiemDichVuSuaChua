package com.example.fixnear

import android.app.DatePickerDialog
import java.util.Calendar
import android.os.Bundle
import android.util.Log
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.model.RegisterRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityRegisterBinding
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.util.Locale

class RegisterActivity : AppCompatActivity() {

    private lateinit var binding: ActivityRegisterBinding

    private fun setupDatePicker() {
        binding.edtDateOfBirth.setOnClickListener {
            val calendar = Calendar.getInstance()

            val year = calendar.get(Calendar.YEAR)
            val month = calendar.get(Calendar.MONTH)
            val day = calendar.get(Calendar.DAY_OF_MONTH)

            DatePickerDialog(
                this,{ _, selectedYear, selectedMonth, selectedDay ->
                    val date = String.format(
                        Locale.US,
                        "%04d-%02d-%02d",
                        selectedYear,
                        selectedMonth + 1,
                        selectedDay
                    )

                    binding.edtDateOfBirth.setText(date)
                },
                year,
                month,
                day
            ).apply {
                datePicker.maxDate = System.currentTimeMillis()
            }.show()
        }
    }

    private fun register(){
        val fullName = binding.edtFullName.text.toString().trim()
        val email = binding.edtEmail.text.toString().trim()
        val phone = binding.edtPhone.text.toString().trim()
        val dateOfBirth = binding.edtDateOfBirth.text.toString().trim()
        val password = binding.edtPassword.text.toString().trim()
        val confirmPassword = binding.edtConfirmPassword.text.toString().trim()

        if(fullName.isEmpty()){
            binding.edtFullName.error = "Vui lòng nhập họ tên"
            return
        }

        if (email.isEmpty()) {
            binding.edtEmail.error = "Vui lòng nhập email"
            return
        }

        if (phone.isEmpty()) {
            binding.edtPhone.error = "Vui lòng nhập số điện thoại"
            return
        }

        if (dateOfBirth.isEmpty()) {
            binding.edtDateOfBirth.error = "Vui lòng chọn ngày sinh"
            return
        }

        val gender = when (binding.radioGroupGender.checkedRadioButtonId) {
            binding.rbMale.id -> 1
            binding.rbFemale.id -> 2
            else -> {
                Toast.makeText(
                    this,
                    "Vui lòng chọn giới tính",
                    Toast.LENGTH_SHORT
                ).show()
                return
            }
        }

        if (password.isEmpty()) {
            binding.edtPassword.error = "Vui lòng nhập mật khẩu"
            return
        }

        if (password != confirmPassword) {
            binding.edtConfirmPassword.error =
                "Mật khẩu xác nhận không khớp"
            return
        }
        val request = RegisterRequest(fullName,email,phone,dateOfBirth,gender,password)

        callRegisterApi(request)
    }

    private fun callRegisterApi(request : RegisterRequest){
        lifecycleScope.launch {
            binding.btnRegister.isEnabled = false
            try {
                val respone = RetrofitClient.authApi.register(request)

                if(respone.isSuccessful){
                    val message = respone.body()?.message?:"Đăng ký thành công"

                    Toast.makeText(
                        this@RegisterActivity,
                        message,
                        Toast.LENGTH_LONG
                    ).show()

                    finish()
                }
                else{
                    val errorMessage = respone.errorBody()?.string()

                    val message = try{
                        JSONObject(errorMessage?:"{}")
                            .optString("message")
                            .takeIf { it.isNotBlank() }
                    }
                    catch (e: Exception){null} ?: "Đăng ký thất bại (${respone.code()})"

                    Toast.makeText(
                        this@RegisterActivity,
                        message,
                        Toast.LENGTH_LONG
                    ).show()
                }
            }
            catch (e: Exception){
                Log.e("FixNear", "Lỗi gọi API đăng ký", e)

                Toast.makeText(
                    this@RegisterActivity,
                    "Không thể kết nối máy chủ. Vui lòng thử lại.",
                    Toast.LENGTH_LONG
                ).show()

            } finally {
                binding.btnRegister.isEnabled = true
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityRegisterBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupDatePicker()
        binding.btnRegister.setOnClickListener {
            register()
        }
        binding.tvGoToLogin.setOnClickListener {
            finish()
        }
    }
}