package com.example.fixnear.ui.auth

import android.content.Intent
import android.os.Bundle
import android.text.method.HideReturnsTransformationMethod
import android.text.method.PasswordTransformationMethod
import android.util.Patterns
import android.view.View
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.example.fixnear.databinding.ActivityRoleRegisterBinding

class RoleRegisterActivity : AppCompatActivity() {

    private lateinit var binding: ActivityRoleRegisterBinding
    private var roleType = 0

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
                "Đăng ký để quản lý cửa hàng và cung cấp dịch vụ sửa chữa trên FixNear."
            else
                "Tham gia FixNear để nhận công việc sửa chữa phù hợp với chuyên môn."

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
            val intent = Intent(
                this,
                LoginActivity::class.java
            )
            intent.flags =
                Intent.FLAG_ACTIVITY_CLEAR_TOP or
                        Intent.FLAG_ACTIVITY_SINGLE_TOP

            startActivity(intent)
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
            validateForm()
        }
    }

    private fun togglePassword(
        editText: EditText,
        button: TextView
    ) {
        val isHidden =
            editText.transformationMethod is PasswordTransformationMethod

        editText.transformationMethod =
            if (isHidden)
                HideReturnsTransformationMethod.getInstance()
            else
                PasswordTransformationMethod.getInstance()

        button.text = if (isHidden) "Ẩn" else "Hiện"

        editText.setSelection(editText.text.length)
    }

    private fun required(
        field: EditText,
        errorMessage: String
    ): String? {
        val value = field.text.toString().trim()

        if (value.isBlank()) {
            field.error = errorMessage
            field.requestFocus()
            return null
        }

        return value
    }

    private fun validateForm() {

        // 1. Họ tên
        val fullName = required(
            binding.edtFullName,
            "Vui lòng nhập họ tên"
        ) ?: return

        if (fullName.length < 3) {
            binding.edtFullName.error =
                "Họ tên tối thiểu 3 ký tự"
            binding.edtFullName.requestFocus()
            return
        }

        // 2. Email
        val email = required(
            binding.edtEmail,
            "Vui lòng nhập email"
        ) ?: return

        if (!Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            binding.edtEmail.error = "Email không hợp lệ"
            binding.edtEmail.requestFocus()
            return
        }

        // 3. Số điện thoại
        val phone = required(
            binding.edtPhone,
            "Vui lòng nhập số điện thoại"
        ) ?: return

        val phoneRegex = Regex("^(03|05|07|08|09)\\d{8}$")

        if (!phoneRegex.matches(phone)) {
            binding.edtPhone.error =
                "Số điện thoại phải có 10 chữ số hợp lệ"
            binding.edtPhone.requestFocus()
            return
        }

        // 4. Thông tin Shop
        if (roleType == 2) {
            val shopName = required(
                binding.edtShopName,
                "Vui lòng nhập tên cửa hàng"
            ) ?: return

            if (shopName.length < 3) {
                binding.edtShopName.error =
                    "Tên cửa hàng tối thiểu 3 ký tự"
                binding.edtShopName.requestFocus()
                return
            }

            val address = required(
                binding.edtShopAddress,
                "Vui lòng nhập địa chỉ cửa hàng"
            ) ?: return

            if (address.length < 5) {
                binding.edtShopAddress.error =
                    "Vui lòng nhập địa chỉ chi tiết hơn"
                binding.edtShopAddress.requestFocus()
                return
            }
        }

        // 5. Thông tin Technician
        if (roleType == 3) {
            val experience = required(
                binding.edtExperience,
                "Nhập số năm kinh nghiệm"
            ) ?: return

            val years = experience.toIntOrNull()

            if (years == null || years !in 0..70) {
                binding.edtExperience.error =
                    "Kinh nghiệm phải từ 0 đến 70 năm"
                binding.edtExperience.requestFocus()
                return
            }

            required(
                binding.edtSpecialization,
                "Vui lòng nhập chuyên môn"
            ) ?: return
        }

        // 6. Mật khẩu
        val password = binding.edtPassword.text.toString()

        if (password.length < 8) {
            binding.edtPassword.error =
                "Mật khẩu phải có ít nhất 8 ký tự"
            binding.edtPassword.requestFocus()
            return
        }

        // 7. Xác nhận mật khẩu
        val confirmPassword =
            binding.edtConfirmPassword.text.toString()

        if (confirmPassword != password) {
            binding.edtConfirmPassword.error =
                "Mật khẩu xác nhận không khớp"
            binding.edtConfirmPassword.requestFocus()
            return
        }

        // 8. Đồng ý điều khoản
        if (!binding.cbAgreeTerms.isChecked) {
            Toast.makeText(
                this,
                "Bạn cần đồng ý với điều khoản dịch vụ",
                Toast.LENGTH_SHORT
            ).show()

            binding.cbAgreeTerms.requestFocus()
            return
        }

        // 9. Chưa gọi API tạo tài khoản
        val roleName =
            if (roleType == 2) "cửa hàng" else "kỹ thuật viên"

        Toast.makeText(
            this,
            "Thông tin đăng ký $roleName hợp lệ. Chưa tích hợp API đăng ký.",
            Toast.LENGTH_LONG
        ).show()
    }
}
