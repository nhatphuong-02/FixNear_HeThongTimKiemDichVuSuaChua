package com.example.fixnear.ui.auth

import android.app.DatePickerDialog
import android.content.Intent
import android.os.Bundle
import android.util.Log
import android.util.Patterns
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.Spinner
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.R
import com.example.fixnear.data.model.ProvinceOption
import com.example.fixnear.data.model.RegisterRequest
import com.example.fixnear.data.model.WardOption
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityRegisterBinding
import com.example.fixnear.helper.setupPasswordToggle
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.util.Calendar
import java.util.Locale

class RegisterActivity : AppCompatActivity() {

    private lateinit var binding: ActivityRegisterBinding

    private var provinces: List<ProvinceOption> = emptyList()
    private var wards: List<WardOption> = emptyList()

    private var selectedProvinceId = 0
    private var selectedWardId = 0

    private var wardJob: Job? = null
    private var isRegistering = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityRegisterBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupNavigation()
        setupDatePicker()
        setupPasswords()
        setupLocations()

        binding.btnRegister.setOnClickListener {
            register()
        }
    }

    // =====================================================
    // ĐIỀU HƯỚNG
    // =====================================================

    private fun setupNavigation() {

        // Quay lại màn hình chọn vai trò đăng ký
        binding.btnBack.setOnClickListener {
            finish()
        }

        // Về thẳng màn hình đăng nhập
        binding.tvGoToLogin.setOnClickListener {
            goToLogin()
        }
    }

    private fun goToLogin() {
        val intent = Intent(
            this@RegisterActivity,
            LoginActivity::class.java
        ).apply {
            flags = Intent.FLAG_ACTIVITY_CLEAR_TOP or
                    Intent.FLAG_ACTIVITY_SINGLE_TOP
        }

        startActivity(intent)
        finish()
    }

    // =====================================================
    // HIỆN / ẨN MẬT KHẨU
    // =====================================================

    private fun setupPasswords() {

        setupPasswordToggle(
            binding.edtPassword,
            binding.ivTogglePassword
        )

        setupPasswordToggle(
            binding.edtConfirmPassword,
            binding.ivToggleConfirmPassword
        )
    }

    // =====================================================
    // DATE PICKER
    // =====================================================

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
                datePicker.maxDate =
                    System.currentTimeMillis()
            }.show()
        }
    }

    // =====================================================
    // SPINNER TỈNH / PHƯỜNG
    // =====================================================

    private fun setSpinnerItems(
        spinner: Spinner,
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

        binding.spProvince.isEnabled = false
        binding.spWard.isEnabled = false
        binding.btnRegister.isEnabled = false

        // Khi chọn Tỉnh / Thành phố
        binding.spProvince.onItemSelectedListener =
            object : AdapterView.OnItemSelectedListener {

                override fun onNothingSelected(
                    parent: AdapterView<*>?
                ) = Unit

                override fun onItemSelected(
                    parent: AdapterView<*>?,
                    view: View?,
                    position: Int,
                    id: Long
                ) {
                    // Hủy request phường của tỉnh cũ
                    wardJob?.cancel()

                    selectedProvinceId = 0
                    selectedWardId = 0
                    wards = emptyList()

                    setSpinnerItems(
                        binding.spWard,
                        listOf("Chọn phường/xã")
                    )

                    binding.spWard.isEnabled = false

                    if (
                        position <= 0 ||
                        position > provinces.size
                    ) {
                        return
                    }

                    val province = provinces[position - 1]

                    selectedProvinceId =
                        province.provinceId

                    loadWards(province.provinceId)
                }
            }

        // Khi chọn Phường / Xã
        binding.spWard.onItemSelectedListener =
            object : AdapterView.OnItemSelectedListener {

                override fun onNothingSelected(
                    parent: AdapterView<*>?
                ) = Unit

                override fun onItemSelected(
                    parent: AdapterView<*>?,
                    view: View?,
                    position: Int,
                    id: Long
                ) {
                    selectedWardId =
                        if (
                            position > 0 &&
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

    // =====================================================
    // TẢI TỈNH / THÀNH PHỐ
    // =====================================================

    private fun loadProvinces() {

        lifecycleScope.launch {

            try {
                val result =
                    RetrofitClient.locationApi.getProvinces()

                provinces = result

                selectedProvinceId = 0
                selectedWardId = 0

                setSpinnerItems(
                    binding.spProvince,
                    listOf("Chọn tỉnh/thành phố") +
                            provinces.map {
                                it.provinceName
                            }
                )

                binding.spProvince.isEnabled =
                    provinces.isNotEmpty()

                binding.btnRegister.isEnabled =
                    provinces.isNotEmpty()

                Log.d(
                    "LOCATION_ERROR",
                    "Đã tải ${provinces.size} tỉnh/thành phố"
                )

                if (provinces.isEmpty()) {
                    showToast(
                        "Chưa có dữ liệu tỉnh/thành phố"
                    )
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: Exception) {

                Log.e(
                    "LOCATION_ERROR",
                    "Không tải được tỉnh/thành phố",
                    e
                )

                provinces = emptyList()
                selectedProvinceId = 0

                setSpinnerItems(
                    binding.spProvince,
                    listOf("Không tải được tỉnh/thành phố")
                )

                binding.spProvince.isEnabled = false
                binding.btnRegister.isEnabled = false

                showToast(
                    "Lỗi tải tỉnh: ${e.localizedMessage}"
                )
            }
        }
    }

    // =====================================================
    // TẢI PHƯỜNG / XÃ THEO TỈNH
    // =====================================================

    private fun loadWards(provinceId: Int) {

        wardJob = lifecycleScope.launch {

            setSpinnerItems(
                binding.spWard,
                listOf("Đang tải phường/xã...")
            )

            binding.spWard.isEnabled = false

            try {
                val result =
                    RetrofitClient.locationApi.getWards(
                        provinceId
                    )

                // Tránh nhận dữ liệu từ tỉnh đã bỏ chọn
                if (selectedProvinceId != provinceId) {
                    return@launch
                }

                wards = result
                selectedWardId = 0

                setSpinnerItems(
                    binding.spWard,
                    listOf("Chọn phường/xã") +
                            wards.map {
                                it.wardName
                            }
                )

                binding.spWard.isEnabled =
                    wards.isNotEmpty()

                Log.d(
                    "LOCATION_ERROR",
                    "Tỉnh $provinceId có ${wards.size} phường/xã"
                )

                if (wards.isEmpty()) {
                    showToast(
                        "Tỉnh này chưa có danh sách phường/xã"
                    )
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: Exception) {

                Log.e(
                    "LOCATION_ERROR",
                    "Lỗi tải phường/xã",
                    e
                )

                if (selectedProvinceId == provinceId) {

                    wards = emptyList()
                    selectedWardId = 0

                    setSpinnerItems(
                        binding.spWard,
                        listOf("Không tải được phường/xã")
                    )

                    binding.spWard.isEnabled = false

                    showToast(
                        "Lỗi tải phường/xã: ${e.localizedMessage}"
                    )
                }
            }
        }
    }

    // =====================================================
    // KIỂM TRA THÔNG TIN ĐĂNG KÝ CUSTOMER
    // =====================================================

    private fun register() {

        if (isRegistering) return

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

        // Họ tên
        if (fullName.length < 3) {
            binding.edtFullName.error =
                "Họ tên phải có ít nhất 3 ký tự"

            binding.edtFullName.requestFocus()
            return
        }

        // Email
        if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            binding.edtEmail.error = "Email không hợp lệ"
            binding.edtEmail.requestFocus()
            return
        }

        // Số điện thoại Việt Nam
        if (!Regex("^(03|05|07|08|09)\\d{8}$")
                .matches(phone)
        ) {
            binding.edtPhone.error =
                "Số điện thoại phải có 10 chữ số hợp lệ"

            binding.edtPhone.requestFocus()
            return
        }

        // Ngày sinh
        if (dateOfBirth.isBlank()) {
            binding.edtDateOfBirth.error =
                "Vui lòng chọn ngày sinh"

            binding.edtDateOfBirth.requestFocus()
            return
        }

        // Giới tính
        val gender = when (
            binding.radioGroupGender.checkedRadioButtonId
        ) {
            binding.rbMale.id -> 1
            binding.rbFemale.id -> 0

            else -> {
                showToast("Vui lòng chọn giới tính")
                return
            }
        }

        // Tỉnh / Thành phố
        if (selectedProvinceId <= 0) {
            showToast(
                "Vui lòng chọn tỉnh/thành phố"
            )
            return
        }

        // Phường / Xã
        if (selectedWardId <= 0) {
            showToast(
                "Vui lòng chọn phường/xã"
            )
            return
        }

        // Địa chỉ chi tiết
        if (addressLine.length < 5) {
            binding.edtAddressLine.error =
                "Vui lòng nhập địa chỉ chi tiết"

            binding.edtAddressLine.requestFocus()
            return
        }

        // Mật khẩu
        if (password.length < 8) {
            binding.edtPassword.error =
                "Mật khẩu phải có ít nhất 8 ký tự"

            binding.edtPassword.requestFocus()
            return
        }

        // Xác nhận mật khẩu
        if (password != confirmPassword) {
            binding.edtConfirmPassword.error =
                "Mật khẩu xác nhận không khớp"

            binding.edtConfirmPassword.requestFocus()
            return
        }

        // Tạo request đúng DTO backend
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

    // =====================================================
    // GỬI API ĐĂNG KÝ
    // =====================================================

    private fun callRegisterApi(request: RegisterRequest) {

        isRegistering = true
        binding.btnRegister.isEnabled = false

        lifecycleScope.launch {

            try {
                val response =
                    RetrofitClient.authApi.register(request)

                if (response.isSuccessful) {

                    showToast(
                        response.body()?.message
                            ?: "Vui lòng kiểm tra email để lấy OTP"
                    )

                    // Chưa tạo tài khoản hoàn chỉnh.
                    // Phải nhập OTP theo quy trình backend.
                    RegistrationOtpHelper.show(
                        this@RegisterActivity,
                        request.email
                    )

                } else {

                    val errorBody =
                        response.errorBody()?.string()

                    Log.e(
                        "REGISTER_ERROR",
                        "HTTP ${response.code()}: $errorBody"
                    )

                    val message = parseRegisterError(
                        errorBody,
                        response.code()
                    )

                    showToast(message)
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: Exception) {

                Log.e(
                    "REGISTER_ERROR",
                    "Lỗi gọi API đăng ký",
                    e
                )

                showToast(
                    "Lỗi kết nối: ${e.localizedMessage}"
                )

            } finally {
                isRegistering = false
                binding.btnRegister.isEnabled =
                    provinces.isNotEmpty()
            }
        }
    }

    // =====================================================
    // ĐỌC LỖI VALIDATION TỪ BACKEND
    // =====================================================

    private fun parseRegisterError(
        body: String?,
        statusCode: Int
    ): String {

        return try {

            val json = JSONObject(body ?: "{}")
            val errors = json.optJSONObject("errors")

            if (errors != null) {

                val messages = mutableListOf<String>()
                val keys = errors.keys()

                while (keys.hasNext()) {

                    val key = keys.next()
                    val values = errors.optJSONArray(key)

                    if (values != null) {
                        for (i in 0 until values.length()) {
                            messages.add(
                                values.optString(i)
                            )
                        }
                    }
                }

                messages.joinToString("\n").ifBlank {
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

        } catch (_: Exception) {
            "Đăng ký thất bại (HTTP $statusCode)"
        }
    }

    // =====================================================
    // THÔNG BÁO
    // =====================================================

    private fun showToast(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
