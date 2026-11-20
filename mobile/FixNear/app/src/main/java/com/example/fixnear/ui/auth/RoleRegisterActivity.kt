
package com.example.fixnear.ui.auth

import android.content.Intent
import android.os.Bundle
import android.text.method.HideReturnsTransformationMethod
import android.text.method.PasswordTransformationMethod
import android.util.Log
import android.util.Patterns
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.EditText
import android.widget.Spinner
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.R
import com.example.fixnear.data.model.ProvinceOption
import com.example.fixnear.data.model.WardOption
import com.example.fixnear.data.model.RegisterShopRequest
import com.example.fixnear.data.model.RegisterTechnicianRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityRoleRegisterBinding
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import org.json.JSONObject
import retrofit2.Response

class RoleRegisterActivity : AppCompatActivity() {

    private lateinit var binding: ActivityRoleRegisterBinding
    private var roleType = 0

    private var provinces: List<ProvinceOption> = emptyList()
    private var wards: List<WardOption> = emptyList()

    private var selectedProvinceId = 0
    private var selectedWardId = 0
    private var wardJob: Job? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityRoleRegisterBinding.inflate(layoutInflater)
        setContentView(binding.root)

        roleType = intent.getIntExtra("ROLE_TYPE", -1)

        if (roleType != 2 && roleType != 3) {
            finish()
            return
        }

        setupRole()
        setupEvents()

        if (roleType == 2) {
            setupShopLocations()
        }
    }

    private fun setupRole() {
        val isShop = roleType == 2

        binding.tvTitle.text =
            if (isShop) "Đăng ký cửa hàng"
            else "Đăng ký kỹ thuật viên"

        binding.tvRoleBadge.text =
            if (isShop) "Tài khoản cửa hàng"
            else "Tài khoản kỹ thuật viên"

        binding.tvSubtitle.text =
            if (isShop)
                "Đăng ký cửa hàng và quản lý dịch vụ sửa chữa."
            else
                "Tham gia FixNear với vai trò kỹ thuật viên."

        binding.sectionShop.visibility =
            if (isShop) View.VISIBLE else View.GONE

        binding.sectionTechnician.visibility =
            if (isShop) View.GONE else View.VISIBLE
    }

    private fun setupEvents() {
        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.tvGoToLogin.setOnClickListener {
            startActivity(
                Intent(this, LoginActivity::class.java).apply {
                    flags = Intent.FLAG_ACTIVITY_CLEAR_TOP or
                            Intent.FLAG_ACTIVITY_SINGLE_TOP
                }
            )
            finish()
        }

        binding.btnShowPassword.setOnClickListener {
            togglePassword(
                binding.edtPassword,
                binding.btnShowPassword
            )
        }

        binding.btnShowConfirmPassword.setOnClickListener {
            togglePassword(
                binding.edtConfirmPassword,
                binding.btnShowConfirmPassword
            )
        }

        binding.btnRegister.setOnClickListener {
            validateAndRegister()
        }
    }

    private fun togglePassword(
        editText: EditText,
        button: TextView
    ) {
        val hidden =
            editText.transformationMethod is PasswordTransformationMethod

        editText.transformationMethod =
            if (hidden)
                HideReturnsTransformationMethod.getInstance()
            else
                PasswordTransformationMethod.getInstance()

        button.text = if (hidden) "Ẩn" else "Hiện"
        editText.setSelection(editText.text.length)
    }

    private fun setSpinnerItems(
        spinner: Spinner,
        items: List<String>
    ) {
        val adapter = ArrayAdapter(
            this,
            R.layout.item_role_location,
            items
        )

        adapter.setDropDownViewResource(
            R.layout.item_role_location
        )

        spinner.adapter = adapter
    }

    private fun setupShopLocations() {
        setSpinnerItems(
            binding.spShopProvince,
            listOf("Đang tải tỉnh/thành phố...")
        )

        setSpinnerItems(
            binding.spShopWard,
            listOf("Chọn tỉnh/thành phố trước")
        )

        binding.spShopProvince.isEnabled = false
        binding.spShopWard.isEnabled = false
        binding.btnRegister.isEnabled = false

        binding.spShopProvince.onItemSelectedListener =
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
                    wardJob?.cancel()
                    wards = emptyList()
                    selectedProvinceId = 0
                    selectedWardId = 0

                    binding.spShopWard.isEnabled = false

                    setSpinnerItems(
                        binding.spShopWard,
                        listOf("Chọn phường/xã")
                    )

                    if (position == 0 ||
                        position > provinces.size
                    ) {
                        return
                    }

                    val province = provinces[position - 1]
                    selectedProvinceId = province.provinceId

                    loadWards(province.provinceId)
                }
            }

        binding.spShopWard.onItemSelectedListener =
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
                val result =
                    RetrofitClient.locationApi.getProvinces()

                provinces = result
                selectedProvinceId = 0
                selectedWardId = 0

                setSpinnerItems(
                    binding.spShopProvince,
                    listOf("Chọn tỉnh/thành phố") +
                            result.map { it.provinceName }
                )

                binding.spShopProvince.isEnabled =
                    result.isNotEmpty()

                binding.btnRegister.isEnabled =
                    result.isNotEmpty()

                if (result.isEmpty()) {
                    showMessage(
                        "Chưa có dữ liệu tỉnh/thành phố"
                    )
                }

            } catch (e: Exception) {
                Log.e(
                    "ROLE_LOCATION",
                    "Không tải được tỉnh/thành phố",
                    e
                )

                provinces = emptyList()

                setSpinnerItems(
                    binding.spShopProvince,
                    listOf("Không tải được tỉnh/thành phố")
                )

                binding.spShopProvince.isEnabled = false
                binding.btnRegister.isEnabled = false

                showMessage(
                    "Lỗi tải tỉnh: ${e.localizedMessage}"
                )
            }
        }
    }

    private fun loadWards(provinceId: Int) {
        wardJob = lifecycleScope.launch {

            setSpinnerItems(
                binding.spShopWard,
                listOf("Đang tải phường/xã...")
            )

            try {
                val result =
                    RetrofitClient.locationApi.getWards(
                        provinceId
                    )

                if (selectedProvinceId != provinceId) {
                    return@launch
                }

                wards = result
                selectedWardId = 0

                setSpinnerItems(
                    binding.spShopWard,
                    listOf("Chọn phường/xã") +
                            result.map { it.wardName }
                )

                binding.spShopWard.isEnabled =
                    result.isNotEmpty()

                if (result.isEmpty()) {
                    showMessage(
                        "Tỉnh đã chọn chưa có phường/xã"
                    )
                }

            } catch (e: Exception) {
                if (e is kotlinx.coroutines.CancellationException) {
                    throw e
                }

                Log.e(
                    "ROLE_LOCATION",
                    "Không tải được phường/xã",
                    e
                )

                if (selectedProvinceId == provinceId) {
                    setSpinnerItems(
                        binding.spShopWard,
                        listOf("Không tải được phường/xã")
                    )

                    binding.spShopWard.isEnabled = false

                    showMessage(
                        "Lỗi tải phường/xã: ${e.localizedMessage}"
                    )
                }
            }
        }
    }

    private fun required(
        field: EditText,
        message: String
    ): String? {
        val value = field.text.toString().trim()

        if (value.isBlank()) {
            field.error = message
            field.requestFocus()
            return null
        }

        return value
    }

    private fun validateAndRegister() {
        val fullName = required(
            binding.edtFullName,
            "Vui lòng nhập họ tên"
        ) ?: return

        if (fullName.length < 3) {
            binding.edtFullName.error =
                "Họ tên phải có ít nhất 3 ký tự"
            return
        }

        val email = required(
            binding.edtEmail,
            "Vui lòng nhập email"
        ) ?: return

        if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            binding.edtEmail.error = "Email không hợp lệ"
            return
        }

        val phone = required(
            binding.edtPhone,
            "Vui lòng nhập số điện thoại"
        ) ?: return

        if (!Regex("^(03|05|07|08|09)\\d{8}$")
                .matches(phone)
        ) {
            binding.edtPhone.error =
                "Số điện thoại không hợp lệ"
            return
        }

        val password = binding.edtPassword.text.toString()
        val confirm = binding.edtConfirmPassword.text.toString()

        if (password.length < 8) {
            binding.edtPassword.error =
                "Mật khẩu phải có ít nhất 8 ký tự"
            return
        }

        if (confirm != password) {
            binding.edtConfirmPassword.error =
                "Mật khẩu xác nhận không khớp"
            return
        }

        if (!binding.cbAgreeTerms.isChecked) {
            showMessage(
                "Vui lòng đồng ý điều khoản dịch vụ"
            )
            return
        }

        if (roleType == 2) {
            val shopName = required(
                binding.edtShopName,
                "Vui lòng nhập tên cửa hàng"
            ) ?: return

            if (selectedProvinceId <= 0) {
                showMessage("Vui lòng chọn tỉnh/thành phố")
                return
            }

            if (selectedWardId <= 0) {
                showMessage("Vui lòng chọn phường/xã")
                return
            }

            val addressLine = required(
                binding.edtShopAddress,
                "Vui lòng nhập địa chỉ chi tiết"
            ) ?: return

            if (addressLine.length < 5) {
                binding.edtShopAddress.error =
                    "Địa chỉ phải có ít nhất 5 ký tự"
                return
            }

            val request = RegisterShopRequest(
                fullName = fullName,
                email = email,
                phone = phone,
                password = password,
                shopName = shopName,
                provinceId = selectedProvinceId,
                wardId = selectedWardId,
                addressLine = addressLine,
                recipientName = fullName,
                recipientPhone = phone,
                shopPhone = phone,
                shopEmail = email
            )

            submitRegistration(email) {
                RetrofitClient.authApi.registerShop(request)
            }

        } else {
            val experienceText = required(
                binding.edtExperience,
                "Vui lòng nhập số năm kinh nghiệm"
            ) ?: return

            val years = experienceText.toIntOrNull()

            if (years == null || years !in 0..60) {
                binding.edtExperience.error =
                    "Số năm kinh nghiệm phải từ 0 đến 60"
                return
            }

            // Backend nhận Introduction,
            // không nhận chuỗi Specialization.
            val introduction =
                binding.edtSpecialization.text
                    .toString().trim()
                    .takeIf { it.isNotBlank() }

            val request = RegisterTechnicianRequest(
                fullName = fullName,
                email = email,
                phone = phone,
                password = password,
                experienceYears = years,
                introduction = introduction,
                serviceIds = emptyList()
            )

            submitRegistration(email) {
                RetrofitClient.authApi
                    .registerTechnician(request)
            }
        }
    }

    private fun submitRegistration(
        email: String,
        call: suspend () -> Response<
                com.example.fixnear.data.model.ApiResponse<Any>
                >
    ) {
        binding.btnRegister.isEnabled = false

        lifecycleScope.launch {
            try {
                val response = call()

                if (response.isSuccessful) {
                    showMessage(
                        response.body()?.message
                            ?: "Hãy kiểm tra email để nhập OTP"
                    )

                    RegistrationOtpHelper.show(
                        this@RoleRegisterActivity,
                        email
                    )
                } else {
                    val errorText =
                        response.errorBody()?.string()

                    Log.e(
                        "ROLE_REGISTER",
                        "HTTP ${response.code()}: $errorText"
                    )

                    showMessage(
                        parseError(errorText)
                    )
                }

            } catch (e: Exception) {
                Log.e(
                    "ROLE_REGISTER",
                    "Lỗi đăng ký",
                    e
                )

                showMessage(
                    "Không gửi được đăng ký: ${e.localizedMessage}"
                )

            } finally {
                binding.btnRegister.isEnabled = true
            }
        }
    }

    private fun parseError(body: String?): String {
        return try {
            val json = JSONObject(body ?: "{}")

            val errors = json.optJSONObject("errors")

            if (errors != null) {
                val messages = mutableListOf<String>()
                val keys = errors.keys()

                while (keys.hasNext()) {
                    val key = keys.next()
                    val arr = errors.optJSONArray(key)

                    if (arr != null) {
                        for (i in 0 until arr.length()) {
                            messages.add(arr.optString(i))
                        }
                    }
                }

                messages.joinToString("\n")
                    .ifBlank { "Dữ liệu đăng ký không hợp lệ" }
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
            "Đăng ký thất bại"
        }
    }

    private fun showMessage(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
