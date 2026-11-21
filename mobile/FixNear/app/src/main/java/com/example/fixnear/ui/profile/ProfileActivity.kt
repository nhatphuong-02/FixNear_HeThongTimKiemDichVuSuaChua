package com.example.fixnear.ui.profile

import android.app.DatePickerDialog
import android.app.Dialog
import android.content.Intent
import android.content.res.ColorStateList
import android.graphics.Typeface
import android.os.Bundle
import android.text.InputType
import android.view.View
import android.view.ViewGroup
import android.view.WindowManager
import android.widget.ArrayAdapter
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.Spinner
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.AppCompatButton
import androidx.appcompat.widget.AppCompatRadioButton
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.bumptech.glide.Glide
import com.example.fixnear.R
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.customer.CustomerAddress
import com.example.fixnear.data.model.customer.CustomerAddressRequest
import com.example.fixnear.data.model.customer.CustomerProfileResponse
import com.example.fixnear.data.model.customer.UpdateCustomerProfileRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityProfileBinding
import com.example.fixnear.ui.auth.LoginActivity
import com.example.fixnear.ui.home.HomeActivity
import com.example.fixnear.ui.home.CustomerCatalogActivity
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.util.Calendar
import java.util.Locale


class ProfileActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProfileBinding
    private lateinit var session: SessionManager
    private lateinit var addressAdapter: CustomerAddressAdapter

    private var profile: CustomerProfileResponse? = null
    private var saving = false

    private val caramel = 0xFFD8975B.toInt()
    private val dark = 0xFF251A14.toInt()
    private val brown = 0xFF66442F.toInt()
    private val cream = 0xFFFFF8F1.toInt()

    private fun dp(v: Int) =
        (v * resources.displayMetrics.density).toInt()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityProfileBinding.inflate(layoutInflater)
        setContentView(binding.root)

        session = SessionManager(this)

        addressAdapter = CustomerAddressAdapter(
            onEdit = { showAddressDialog(it) },
            onSetDefault = { setDefaultAddress(it) }
        )

        binding.rvAddresses.apply {
            layoutManager = LinearLayoutManager(
                this@ProfileActivity
            )
            adapter = addressAdapter
            isNestedScrollingEnabled = false
        }


        binding.navHome.setOnClickListener {
            val intent = Intent(this, HomeActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_CLEAR_TOP or
                        Intent.FLAG_ACTIVITY_SINGLE_TOP
            }
            startActivity(intent)
            finish()
        }

        binding.navSearch.setOnClickListener {
            startActivity(
                Intent(this, CustomerCatalogActivity::class.java)
            )
        }

        binding.navMessages.setOnClickListener {
            Toast.makeText(
                this,
                "Chức năng tin nhắn đang được phát triển",
                Toast.LENGTH_SHORT
            ).show()
        }

        binding.navAccount.setOnClickListener {
            // Đang ở tab Tài khoản, không mở Activity trùng.
        }

        binding.btnEditProfile.setOnClickListener {
            profile?.let { showEditProfileDialog(it) }
                ?: toast("Chưa tải được hồ sơ")
        }

        binding.btnAddAddress.setOnClickListener {
            showAddressDialog(null)
        }

        binding.btnLogout.setOnClickListener {
            showLogoutDialog()
        }

        binding.btnEditProfile.isEnabled = false
    }

    override fun onResume() {
        super.onResume()
        loadProfile()
        loadAddresses()
    }

    private fun token(): String? {
        val value = session.getToken()
            ?.trim()
            ?.takeIf { it.isNotBlank() }
            ?: return null

        return if (value.startsWith("Bearer ", true)) {
            value
        } else {
            "Bearer $value"
        }
    }

    private fun errorMessage(raw: String?, code: Int): String {
        return try {
            JSONObject(raw.orEmpty())
                .optString("message")
                .ifBlank { "Lỗi HTTP $code" }
        } catch (_: Exception) {
            "Lỗi HTTP $code"
        }
    }

    private fun toast(message: String) {
        Toast.makeText(this, message, Toast.LENGTH_LONG).show()
    }

    // ==================== GET PROFILE ====================

    private fun loadProfile() {
        val auth = token() ?: return

        binding.tvProfileState.text = "Đang tải hồ sơ..."
        binding.btnEditProfile.isEnabled = false

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerApi.getProfile(auth)

                if (!response.isSuccessful) {
                    binding.tvProfileState.text =
                        "Lỗi hồ sơ HTTP ${response.code()}"
                    return@launch
                }

                val result = response.body() ?: return@launch
                profile = result

                binding.tvProfileName.text = result.fullName
                binding.tvProfileEmail.text = result.email ?: "—"
                binding.tvProfilePhone.text = result.phone ?: "—"
                binding.tvDateOfBirth.text =
                    result.dateOfBirth?.take(10) ?: "Chưa cập nhật"

                binding.tvGender.text = when (result.gender) {
                    0 -> "Nữ"
                    1 -> "Nam"
                    else -> "Chưa cập nhật"
                }

                binding.tvProfileAvatar.clearColorFilter()

                if (!result.avatarUrl.isNullOrBlank()) {
                    Glide.with(this@ProfileActivity)
                        .load(result.avatarUrl)
                        .circleCrop()
                        .placeholder(
                            android.R.drawable.ic_menu_myplaces
                        )
                        .into(binding.tvProfileAvatar)
                } else {
                    binding.tvProfileAvatar.setImageResource(
                        android.R.drawable.ic_menu_myplaces
                    )
                    binding.tvProfileAvatar.setColorFilter(caramel)
                }

                binding.tvProfileState.text = ""
                binding.btnEditProfile.isEnabled = true

            } catch (e: Exception) {
                binding.tvProfileState.text =
                    "Lỗi kết nối hồ sơ: ${e.localizedMessage}"
            }
        }
    }

    // ==================== GET ADDRESSES ====================

    private fun loadAddresses() {
        val auth = token() ?: return

        binding.tvAddressState.visibility = View.VISIBLE
        binding.tvAddressState.text = "Đang tải địa chỉ..."

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerAddressApi.getAddresses(auth)

                if (!response.isSuccessful) {
                    binding.tvAddressState.text =
                        "Không tải được địa chỉ: HTTP ${response.code()}"
                    return@launch
                }

                val items = response.body().orEmpty()
                addressAdapter.submitItems(items)

                binding.tvAddressState.visibility =
                    if (items.isEmpty()) View.VISIBLE else View.GONE

                binding.tvAddressState.text =
                    "Bạn chưa có địa chỉ nào"

            } catch (e: Exception) {
                binding.tvAddressState.text =
                    "Lỗi tải địa chỉ: ${e.localizedMessage}"
            }
        }
    }

    // ==================== FORM HELPERS ====================

    private fun formContainer(): LinearLayout {
        return LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(20), dp(20), dp(20), dp(20))
            setBackgroundResource(R.drawable.fixnear_dialog_bg)
        }
    }

    private fun label(
        container: LinearLayout,
        textValue: String
    ) {
        container.addView(
            TextView(this).apply {
                text = textValue
                textSize = 13f
                setTextColor(brown)
                setTypeface(null, Typeface.BOLD)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(13)
                bottomMargin = dp(6)
            }
        )
    }

    private fun field(
        container: LinearLayout,
        labelText: String,
        value: String = "",
        hintText: String = ""
    ): EditText {
        label(container, labelText)

        return EditText(this).also { input ->
            input.setText(value)
            input.hint = hintText
            input.textSize = 14f
            input.setTextColor(cream)
            input.setHintTextColor(0xFFB9A79B.toInt())
            input.setBackgroundResource(R.drawable.fixnear_auth_input)
            input.setPadding(dp(14), 0, dp(14), 0)

            container.addView(
                input,
                LinearLayout.LayoutParams(-1, dp(50))
            )
        }
    }

    private fun formTitle(
        container: LinearLayout,
        title: String
    ) {
        container.addView(
            TextView(this).apply {
                text = title
                textSize = 21f
                setTextColor(dark)
                setTypeface(null, Typeface.BOLD)
            }
        )
    }

    private fun dialogButtons(
        container: LinearLayout,
        dialog: Dialog,
        onSave: () -> Unit
    ) {
        val row = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
        }

        fun button(textValue: String, primary: Boolean) =
            AppCompatButton(this).apply {
                text = textValue
                isAllCaps = false
                setTextColor(if (primary) 0xFFFFFFFF.toInt() else brown)
                setBackgroundResource(
                    if (primary) R.drawable.fixnear_register_button
                    else R.drawable.fixnear_customer_cream
                )
                backgroundTintList = null
            }

        val cancel = button("Hủy", false)
        val save = button("Lưu", true)

        row.addView(
            cancel,
            LinearLayout.LayoutParams(0, dp(50), 1f)
        )
        row.addView(
            save,
            LinearLayout.LayoutParams(0, dp(50), 1f).apply {
                marginStart = dp(8)
            }
        )

        container.addView(
            row,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(20)
            }
        )

        cancel.setOnClickListener {
            if (!saving) dialog.dismiss()
        }
        save.setOnClickListener {
            if (!saving) onSave()
        }
    }

    private fun openDialog(container: LinearLayout): Dialog {
        val dialog = Dialog(this)

        dialog.setContentView(
            ScrollView(this).apply {
                addView(container)
            }
        )
        dialog.show()
        dialog.window?.apply {
            setBackgroundDrawableResource(
                android.R.color.transparent
            )
            setLayout(
                (resources.displayMetrics.widthPixels * .91f).toInt(),
                (resources.displayMetrics.heightPixels * .82f).toInt()
            )
            setSoftInputMode(
                WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE
            )
        }
        return dialog
    }

    // ==================== EDIT PROFILE ====================

    private fun showEditProfileDialog(p: CustomerProfileResponse) {
        val content = formContainer()
        val dialog = Dialog(this)

        formTitle(content, "Chỉnh sửa hồ sơ")

        val name = field(content, "Họ tên", p.fullName)

        val birth = field(
            content,
            "Ngày sinh",
            p.dateOfBirth?.take(10).orEmpty(),
            "yyyy-MM-dd"
        )
        birth.inputType = InputType.TYPE_NULL
        birth.isFocusable = false
        birth.setOnClickListener {
            val parts = birth.text.toString().split("-")
            val today = Calendar.getInstance()

            val year = parts.getOrNull(0)?.toIntOrNull()
                ?: today.get(Calendar.YEAR)
            val month = (parts.getOrNull(1)?.toIntOrNull()
                ?: today.get(Calendar.MONTH) + 1) - 1
            val day = parts.getOrNull(2)?.toIntOrNull()
                ?: today.get(Calendar.DAY_OF_MONTH)

            DatePickerDialog(this, { _, y, m, d ->
                birth.setText(
                    String.format(Locale.US, "%04d-%02d-%02d", y, m + 1, d)
                )
            }, year, month, day).apply {
                datePicker.maxDate = System.currentTimeMillis()
            }.show()
        }

        label(content, "Giới tính")

        val group = android.widget.RadioGroup(this).apply {
            orientation = android.widget.RadioGroup.HORIZONTAL
        }

        val male = AppCompatRadioButton(this).apply {
            text = "Nam"
            setTextColor(dark)
            buttonTintList = ColorStateList.valueOf(caramel)
        }
        val female = AppCompatRadioButton(this).apply {
            text = "Nữ"
            setTextColor(dark)
            buttonTintList = ColorStateList.valueOf(caramel)
        }

        group.addView(male)
        group.addView(female)

        if (p.gender == 1) male.isChecked = true
        if (p.gender == 0) female.isChecked = true

        content.addView(group)

        val avatar = field(
            content,
            "URL ảnh đại diện (không bắt buộc)",
            p.avatarUrl.orEmpty(),
            "https://..."
        )

        dialogButtons(content, dialog) {
            val fullName = name.text.toString().trim()

            if (fullName.length !in 3..200) {
                name.error = "Họ tên từ 3 đến 200 ký tự"
                return@dialogButtons
            }

            val avatarUrl = avatar.text.toString().trim()

            if (avatarUrl.isNotBlank() &&
                (!android.util.Patterns.WEB_URL.matcher(avatarUrl).matches()
                        || avatarUrl.length > 500)
            ) {
                avatar.error = "URL ảnh không hợp lệ"
                return@dialogButtons
            }

            val request = UpdateCustomerProfileRequest(
                fullName = fullName,
                dateOfBirth = birth.text.toString()
                    .trim().ifBlank { null },
                gender = when {
                    male.isChecked -> 1
                    female.isChecked -> 0
                    else -> null
                },
                avatarUrl = avatarUrl.ifBlank { null }
            )

            saveProfile(request, dialog)
        }

        dialog.setContentView(
            ScrollView(this).apply { addView(content) }
        )
        dialog.show()
        dialog.window?.apply {
            setBackgroundDrawableResource(
                android.R.color.transparent
            )
            setLayout(
                (resources.displayMetrics.widthPixels * .91f).toInt(),
                (resources.displayMetrics.heightPixels * .80f).toInt()
            )
        }
    }

    private fun saveProfile(
        request: UpdateCustomerProfileRequest,
        dialog: Dialog
    ) {
        val auth = token() ?: return
        saving = true

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerApi.updateProfile(auth, request)

                if (response.isSuccessful) {
                    dialog.dismiss()
                    toast("Cập nhật hồ sơ thành công")
                    loadProfile()
                } else {
                    toast(errorMessage(
                        response.errorBody()?.string(),
                        response.code()
                    ))
                }
            } catch (e: Exception) {
                toast("Lỗi cập nhật: ${e.localizedMessage}")
            } finally {
                saving = false
            }
        }
    }
    private fun showLogoutDialog() {
        val dialog = Dialog(this)

        val density = resources.displayMetrics.density
        fun dp(value: Int) = (value * density).toInt()

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(22), dp(24), dp(22), dp(22))
            setBackgroundResource(R.drawable.fixnear_dialog_bg)
        }

        val title = TextView(this).apply {
            text = "Đăng xuất"
            textSize = 21f
            setTextColor(0xFF251A14.toInt())
            setTypeface(null, Typeface.BOLD)
        }
        container.addView(title)

        val message = TextView(this).apply {
            text = "Bạn có chắc muốn đăng xuất khỏi FixNear?"
            textSize = 14f
            setTextColor(0xFF66442F.toInt())
        }
        container.addView(
            message,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(10)
            }
        )

        val buttonRow = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
        }

        val btnCancel = AppCompatButton(this).apply {
            text = "Hủy"
            isAllCaps = false
            textSize = 14f
            setTextColor(0xFF66442F.toInt())
            setBackgroundResource(R.drawable.fixnear_customer_cream)
            backgroundTintList = null
        }

        val btnLogout = AppCompatButton(this).apply {
            text = "Đăng xuất"
            isAllCaps = false
            textSize = 14f
            setTextColor(0xFFFFFFFF.toInt())
            setBackgroundResource(R.drawable.fixnear_register_button)
            backgroundTintList = null
        }

        buttonRow.addView(
            btnCancel,
            LinearLayout.LayoutParams(0, dp(50), 1f)
        )

        buttonRow.addView(
            btnLogout,
            LinearLayout.LayoutParams(0, dp(50), 1f).apply {
                marginStart = dp(10)
            }
        )

        container.addView(
            buttonRow,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(22)
            }
        )

        dialog.setContentView(container)
        dialog.show()

        dialog.window?.apply {
            setBackgroundDrawableResource(
                android.R.color.transparent
            )
            setLayout(
                (resources.displayMetrics.widthPixels * 0.90f).toInt(),
                WindowManager.LayoutParams.WRAP_CONTENT
            )
        }

        btnCancel.setOnClickListener {
            dialog.dismiss()
        }

        btnLogout.setOnClickListener {
            session.clearSession()

            val intent = Intent(
                this,
                LoginActivity::class.java
            ).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or
                        Intent.FLAG_ACTIVITY_CLEAR_TASK
            }

            dialog.dismiss()
            startActivity(intent)
            finish()
        }
    }

    // ==================== ADD / EDIT ADDRESS ====================

    private fun showAddressDialog(existing: CustomerAddress?) {
        if (existing?.isLocked == true) {
            toast("Địa chỉ đã khóa, không thể chỉnh sửa")
            return
        }

        val content = formContainer()
        val dialog = Dialog(this)

        formTitle(
            content,
            if (existing == null) "Thêm địa chỉ"
            else "Chỉnh sửa địa chỉ"
        )

        label(content, "Tỉnh / Thành phố")
        val provinceSpinner = Spinner(this)
        content.addView(
            provinceSpinner,
            LinearLayout.LayoutParams(-1, dp(52))
        )

        label(content, "Phường / Xã")
        val wardSpinner = Spinner(this)
        content.addView(
            wardSpinner,
            LinearLayout.LayoutParams(-1, dp(52))
        )

        val addressLine = field(
            content,
            "Địa chỉ chi tiết",
            existing?.addressLine.orEmpty(),
            "Số nhà, đường..."
        )

        val recipientName = field(
            content,
            "Người liên hệ",
            existing?.recipientName.orEmpty(),
            "Họ tên người nhận"
        )

        val recipientPhone = field(
            content,
            "Số điện thoại liên hệ",
            existing?.recipientPhone.orEmpty(),
            "Số điện thoại"
        )
        recipientPhone.inputType = InputType.TYPE_CLASS_PHONE

        val status = TextView(this).apply {
            text = "Đang tải tỉnh/thành..."
            textSize = 12f
            setTextColor(brown)
        }
        content.addView(status)

        // Tỉnh/phường lấy từ API Location hiện có.
        var provinceIds = emptyList<Int>()
        var wardIds = emptyList<Int>()
        var wardJob: Job? = null
        var changingProvince = false
        var locationsReady = false

        fun setSpinner(
            spinner: Spinner,
            labels: List<String>,
            selectedIndex: Int = 0
        ) {
            val adapter = object : ArrayAdapter<String>(
                this,
                android.R.layout.simple_spinner_dropdown_item,
                labels
            ) {
                override fun getView(
                    position: Int,
                    convertView: View?,
                    parent: ViewGroup
                ): View {
                    return TextView(context).apply {
                        text = labels[position]
                        setTextColor(dark)
                        textSize = 14f
                        setPadding(dp(10), dp(12), dp(10), dp(12))
                    }
                }
            }
            spinner.adapter = adapter
            spinner.setSelection(selectedIndex.coerceIn(
                0, (labels.size - 1).coerceAtLeast(0)
            ))
        }

        // Spinner được đặt trên nền kem, chữ nâu đậm.
        setSpinner(provinceSpinner, listOf("Đang tải..."))
        setSpinner(wardSpinner, listOf("Chọn phường/xã"))

        fun loadWards(provinceId: Int) {
            wardJob?.cancel()
            wardIds = emptyList()
            locationsReady = false
            setSpinner(wardSpinner, listOf("Đang tải phường/xã..."))

            if (provinceId <= 0) return

            wardJob = lifecycleScope.launch {
                try {
                    val wards = RetrofitClient.locationApi
                        .getWards(provinceId)

                    wardIds = wards.map { it.wardId }

                    setSpinner(
                        wardSpinner,
                        wards.map { it.wardName },
                        wards.indexOfFirst {
                            it.wardId == existing?.wardId
                        }.coerceAtLeast(0)
                    )

                    locationsReady = wards.isNotEmpty()
                    status.text =
                        if (wards.isEmpty()) "Không có phường/xã"
                        else ""
                } catch (e: Exception) {
                    status.text =
                        "Lỗi tải phường/xã: ${e.localizedMessage}"
                }
            }
        }

        provinceSpinner.onItemSelectedListener =
            object : android.widget.AdapterView.OnItemSelectedListener {
                override fun onItemSelected(
                    parent: android.widget.AdapterView<*>?,
                    view: View?,
                    position: Int,
                    id: Long
                ) {
                    val provinceId =
                        provinceIds.getOrNull(position) ?: return

                    if (changingProvince) return
                    loadWards(provinceId)
                }

                override fun onNothingSelected(
                    parent: android.widget.AdapterView<*>?
                ) = Unit
            }

        lifecycleScope.launch {
            try {
                val provinces =
                    RetrofitClient.locationApi.getProvinces()

                provinceIds = provinces.map { it.provinceId }

                val position = provinces.indexOfFirst {
                    it.provinceId == existing?.provinceId
                }.coerceAtLeast(0)

                changingProvince = true
                setSpinner(
                    provinceSpinner,
                    provinces.map { it.provinceName },
                    position
                )
                changingProvince = false

                provinces.getOrNull(position)?.let {
                    loadWards(it.provinceId)
                }

                if (provinces.isEmpty()) {
                    status.text = "Không có dữ liệu tỉnh/thành"
                }

            } catch (e: Exception) {
                status.text =
                    "Lỗi tải tỉnh/thành: ${e.localizedMessage}"
            }
        }

        dialogButtons(content, dialog) {
            val provinceId =
                provinceIds.getOrNull(
                    provinceSpinner.selectedItemPosition
                )

            val wardId =
                wardIds.getOrNull(
                    wardSpinner.selectedItemPosition
                )

            val line = addressLine.text.toString().trim()
            val name = recipientName.text.toString().trim()
            val phone = recipientPhone.text.toString().trim()

            if (!locationsReady || provinceId == null || wardId == null) {
                toast("Vui lòng chọn tỉnh và phường hợp lệ")
                return@dialogButtons
            }
            if (line.isBlank()) {
                addressLine.error = "Nhập địa chỉ chi tiết"
                return@dialogButtons
            }
            if (name.isBlank()) {
                recipientName.error = "Nhập tên liên hệ"
                return@dialogButtons
            }
            if (!phone.matches(Regex("^0[0-9]{9,10}$"))) {
                recipientPhone.error = "Số điện thoại không hợp lệ"
                return@dialogButtons
            }

            val request = CustomerAddressRequest(
                provinceId = provinceId,
                wardId = wardId,
                addressLine = line,
                recipientName = name,
                recipientPhone = phone
            )

            saveAddress(existing?.addressId, request, dialog)
        }

        dialog.setContentView(
            ScrollView(this).apply { addView(content) }
        )
        dialog.show()
        dialog.window?.apply {
            setBackgroundDrawableResource(
                android.R.color.transparent
            )
            setLayout(
                (resources.displayMetrics.widthPixels * .91f).toInt(),
                (resources.displayMetrics.heightPixels * .82f).toInt()
            )
        }
    }

    private fun saveAddress(
        addressId: Int?,
        request: CustomerAddressRequest,
        dialog: Dialog
    ) {
        val auth = token() ?: return
        saving = true

        lifecycleScope.launch {
            try {
                val response =
                    if (addressId == null) {
                        RetrofitClient.customerAddressApi
                            .addAddress(auth, request)
                    } else {
                        RetrofitClient.customerAddressApi
                            .updateAddress(auth, addressId, request)
                    }

                if (response.isSuccessful) {
                    dialog.dismiss()
                    toast(
                        if (addressId == null) "Đã thêm địa chỉ"
                        else "Đã cập nhật địa chỉ"
                    )
                    loadAddresses()
                    loadProfile()
                } else {
                    toast(errorMessage(
                        response.errorBody()?.string(),
                        response.code()
                    ))
                }
            } catch (e: Exception) {
                toast("Không lưu được địa chỉ: ${e.localizedMessage}")
            } finally {
                saving = false
            }
        }
    }

    // ==================== DEFAULT ADDRESS ====================

    private fun setDefaultAddress(item: CustomerAddress) {
        if (item.isLocked || item.isDefault) return

        val auth = token() ?: return

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerAddressApi
                        .setDefaultAddress(auth, item.addressId)

                if (response.isSuccessful) {
                    toast("Đã đặt địa chỉ mặc định")
                    loadAddresses()
                    loadProfile()
                } else {
                    toast(errorMessage(
                        response.errorBody()?.string(),
                        response.code()
                    ))
                }
            } catch (e: Exception) {
                toast("Lỗi đặt địa chỉ: ${e.localizedMessage}")
            }
        }
    }
}
