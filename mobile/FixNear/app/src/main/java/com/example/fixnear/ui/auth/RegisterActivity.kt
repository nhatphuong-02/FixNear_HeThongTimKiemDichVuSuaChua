package com.example.fixnear.ui.auth

import android.app.DatePickerDialog
import android.os.Bundle
import android.util.Log
import android.util.Patterns
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.R
import com.example.fixnear.data.model.ProvinceOption
import com.example.fixnear.data.model.WardOption
import com.example.fixnear.data.model.RegisterRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityRegisterBinding
import com.example.fixnear.helper.setupPasswordToggle
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.util.Calendar
import java.util.Locale

class RegisterActivity : AppCompatActivity() {

    private lateinit var binding: ActivityRegisterBinding

    private var provinces = listOf<ProvinceOption>()
    private var wards = listOf<WardOption>()

    private var selectedProvinceId = 0
    private var selectedWardId = 0

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityRegisterBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupDatePicker()

        setupPasswordToggle(
            binding.edtPassword,
            binding.ivTogglePassword
        )

        setupPasswordToggle(
            binding.edtConfirmPassword,
            binding.ivToggleConfirmPassword
        )

        setupLocations()

        binding.btnRegister.setOnClickListener {
            register()
        }

        binding.tvGoToLogin.setOnClickListener {
            finish()
        }
    }

    private fun setSpinnerItems(
        spinner: android.widget.Spinner,
        names: List<String>
    ) {
        val adapter = ArrayAdapter(
            this,
            R.layout.item_register_location,
            names
        )

        adapter.setDropDownViewResource(
            R.layout.item_register_location
        )

        spinner.adapter = adapter
    }

    private fun setupLocations() {

        setSpinnerItems(
            binding.spProvince,
            listOf("Đang tải tỉnh/thành phố...")
        )

        setSpinnerItems(
            binding.spWard,
            listOf("Chọn tỉnh/thành phố trước")
        )

        binding.spWard.isEnabled = false
        binding.btnRegister.isEnabled = false

        binding.spProvince.onItemSelectedListener =
            object : AdapterView.OnItemSelectedListener {

                override fun onNothingSelected(
                    parent: AdapterView<*>?
                ) {}

                override fun onItemSelected(
                    parent: AdapterView<*>?,
                    view: View?,
                    position: Int,
                    id: Long
                ) {
                    selectedProvinceId = 0
                    selectedWardId = 0
                    wards = emptyList()

                    setSpinnerItems(
                        binding.spWard,
                        listOf("Chọn phường/xã")
                    )
                    binding.spWard.isEnabled = false

                    if (position == 0 || provinces.isEmpty()) {
                        return
                    }

                    val province = provinces[position - 1]
                    selectedProvinceId = province.provinceId

                    loadWards(province.provinceId)
                }
            }

        binding.spWard.onItemSelectedListener =
            object : AdapterView.OnItemSelectedListener {

                override fun onNothingSelected(
                    parent: AdapterView<*>?
                ) {}

                override fun onItemSelected(
                    parent: AdapterView<*>?,
                    view: View?,
                    position: Int,
                    id: Long
                ) {
                    selectedWardId =
                        if (position > 0 &&
                            position <= wards.size
                        ) {
                            wards[position - 1].wardId
                        } else {
                            0
                        }
                }
            }

        loadProvinces()
    }

    private fun loadProvinces() {
        lifecycleScope.launch {
            try {
                provinces =
                    RetrofitClient.locationApi.getProvinces()

                setSpinnerItems(
                    binding.spProvince,
                    listOf("Chọn tỉnh/thành phố") +
                            provinces.map { it.provinceName }
                )

                binding.btnRegister.isEnabled = true

            } catch (e: Exception) {
                Log.e("LOCATION_ERROR", "Tải tỉnh thất bại", e)

                setSpinnerItems(
                    binding.spProvince,
                    listOf("Không tải được tỉnh/thành phố")
                )

                Toast.makeText(
                    this@RegisterActivity,
                    "Không tải được danh sách tỉnh/thành phố",
                    Toast.LENGTH_LONG
                ).show()
            }
        }
    }

    private fun loadWards(provinceId: Int) {
        lifecycleScope.launch {

            setSpinnerItems(
                binding.spWard,
                listOf("Đang tải phường/xã...")
            )

            try {
                val result =
                    RetrofitClient.locationApi.getWards(provinceId)

                // Tránh hiển thị danh sách của tỉnh cũ
                // nếu người dùng đổi tỉnh quá nhanh.
                if (selectedProvinceId != provinceId) {
                    return@launch
                }

                wards = result
                selectedWardId = 0

                setSpinnerItems(
                    binding.spWard,
                    listOf("Chọn phường/xã") +
                            wards.map { it.wardName }
                )

                binding.spWard.isEnabled = true

            } catch (e: Exception) {
                Log.e("LOCATION_ERROR", "Tải phường lỗi", e)

                if (selectedProvinceId == provinceId) {
                    setSpinnerItems(
                        binding.spWard,
                        listOf("Không tải được phường/xã")
                    )
                }

                Toast.makeText(
                    this@RegisterActivity,
                    "Không tải được danh sách phường/xã",
                    Toast.LENGTH_LONG
                ).show()
            }
        }
    }

    private fun setupDatePicker() {
        binding.edtDateOfBirth.setOnClickListener {

            val calendar = Calendar.getInstance()

            DatePickerDialog(
                this,
                { _, year, month, day ->

                    val date = String.format(
                        Locale.US,
                        "%04d-%02d-%02d",
                        year,
                        month + 1,
                        day
                    )

                    binding.edtDateOfBirth.setText(date)
                },
                calendar.get(Calendar.YEAR),
                calendar.get(Calendar.MONTH),
                calendar.get(Calendar.DAY_OF_MONTH)
            ).apply {
                datePicker.maxDate = System.currentTimeMillis()
            }.show()
        }
    }

    private fun register() {

        val fullName =
            binding.edtFullName.text.toString().trim()

        val email =
            binding.edtEmail.text.toString().trim()

        val phone =
            binding.edtPhone.text.toString().trim()

        val dateOfBirth =
            binding.edtDateOfBirth.text.toString().trim()

        val addressLine =
            binding.edtAddressLine.text.toString().trim()

        val password =
            binding.edtPassword.text.toString()

        val confirmPassword =
            binding.edtConfirmPassword.text.toString()

        if (fullName.length < 3) {
            binding.edtFullName.error =
                "Họ tên phải có ít nhất 3 ký tự"
            binding.edtFullName.requestFocus()
            return
        }

        if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            binding.edtEmail.error = "Email không hợp lệ"
            binding.edtEmail.requestFocus()
            return
        }

        if (!Regex("^(03|05|07|08|09)\\d{8}$")
                .matches(phone)) {
            binding.edtPhone.error =
                "Số điện thoại phải có 10 chữ số hợp lệ"
            binding.edtPhone.requestFocus()
            return
        }

        if (dateOfBirth.isBlank()) {
            binding.edtDateOfBirth.error = "Chọn ngày sinh"
            binding.edtDateOfBirth.requestFocus()
            return
        }

        val gender = when (
            binding.radioGroupGender.checkedRadioButtonId
        ) {
            binding.rbMale.id -> 1
            binding.rbFemale.id -> 0
            else -> {
                Toast.makeText(
                    this,
                    "Vui lòng chọn giới tính",
                    Toast.LENGTH_SHORT
                ).show()
                return
            }
        }

        if (selectedProvinceId <= 0) {
            Toast.makeText(
                this,
                "Vui lòng chọn tỉnh/thành phố",
                Toast.LENGTH_SHORT
            ).show()
            return
        }

        if (selectedWardId <= 0) {
            Toast.makeText(
                this,
                "Vui lòng chọn phường/xã",
                Toast.LENGTH_SHORT
            ).show()
            return
        }

        if (addressLine.length < 5) {
            binding.edtAddressLine.error =
                "Vui lòng nhập địa chỉ chi tiết"
            binding.edtAddressLine.requestFocus()
            return
        }

        if (password.length < 8) {
            binding.edtPassword.error =
                "Mật khẩu phải có ít nhất 8 ký tự"
            binding.edtPassword.requestFocus()
            return
        }

        if (password != confirmPassword) {
            binding.edtConfirmPassword.error =
                "Mật khẩu xác nhận không khớp"
            binding.edtConfirmPassword.requestFocus()
            return
        }

        val request = RegisterRequest(
            fullName = fullName,
            email = email,
            phone = phone,
            dateOfBirth = dateOfBirth,
            gender = gender,
            password = password,
            provinceId = selectedProvinceId,
            wardId = selectedWardId,
            addressLine = addressLine,
            recipientName = fullName,
            recipientPhone = phone
        )

        callRegisterApi(request)
    }

    private fun callRegisterApi(request: RegisterRequest) {
        lifecycleScope.launch {

            binding.btnRegister.isEnabled = false

            try {
                val response =
                    RetrofitClient.authApi.register(request)

                if (response.isSuccessful) {
                    Toast.makeText(
                        this@RegisterActivity,
                        response.body()?.message
                            ?: "Vui lòng kiểm tra email để nhập OTP",
                        Toast.LENGTH_LONG
                    ).show()

                    RegistrationOtpHelper.show(
                        this@RegisterActivity,
                        request.email
                    )
                } else {
                    val errorBody = response.errorBody()?.string()

                    Log.e(
                        "REGISTER_ERROR",
                        "HTTP ${response.code()}: $errorBody"
                    )

                    val message = try {
                        val json = JSONObject(errorBody ?: "{}")
                        val errors = json.optJSONObject("errors")

                        if (errors != null) {
                            val details = mutableListOf<String>()
                            val keys = errors.keys()

                            while (keys.hasNext()) {
                                val key = keys.next()
                                val values = errors.optJSONArray(key)

                                if (values != null) {
                                    for (i in 0 until values.length()) {
                                        details.add(values.optString(i))
                                    }
                                }
                            }

                            details.joinToString("\n").ifBlank {
                                json.optString(
                                    "title",
                                    "Dữ liệu đăng ký không hợp lệ"
                                )
                            }
                        } else {
                            json.optString(
                                "message",
                                json.optString(
                                    "title",
                                    "Đăng ký thất bại"
                                )
                            )
                        }
                    } catch (e: Exception) {
                        "Đăng ký thất bại (${response.code()})"
                    }

                    Toast.makeText(
                        this@RegisterActivity,
                        message,
                        Toast.LENGTH_LONG
                    ).show()
                }

            } catch (e: Exception) {
                Log.e("REGISTER_ERROR", "Lỗi gọi API", e)

                Toast.makeText(
                    this@RegisterActivity,
                    "Lỗi kết nối: ${e.localizedMessage}",
                    Toast.LENGTH_LONG
                ).show()

            } finally {
                binding.btnRegister.isEnabled = true
            }
        }
    }
}
