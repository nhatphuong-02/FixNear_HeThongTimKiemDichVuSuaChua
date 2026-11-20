package com.example.fixnear.ui.home

import android.app.DatePickerDialog
import android.app.TimePickerDialog
import android.os.Bundle
import android.view.View
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.customer.CreateRepairRequest
import com.example.fixnear.data.model.customer.CustomerAddress
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCreateRepairRequestBinding
import kotlinx.coroutines.launch
import java.time.LocalDate
import java.time.LocalTime
import java.time.format.DateTimeFormatter
import java.util.Calendar

class CreateRepairRequestActivity : AppCompatActivity() {

    private lateinit var binding: ActivityCreateRepairRequestBinding
    private lateinit var sessionManager: SessionManager

    private var shopId = -1
    private var serviceId = -1
    private var selectedAddressId: Int? = null
    private var addresses = emptyList<CustomerAddress>()
    private var preferredDate: LocalDate? = null
    private var preferredTimeFrom: LocalTime? = null
    private var preferredTimeTo: LocalTime? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityCreateRepairRequestBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        shopId = intent.getIntExtra("SHOP_ID", -1)
        serviceId = intent.getIntExtra("SERVICE_ID", -1)
        binding.tvShopName.text = intent.getStringExtra("SHOP_NAME").orEmpty()
        binding.tvServiceName.text = intent.getStringExtra("SERVICE_NAME").orEmpty()

        setupViews()
        loadAddresses()
    }

    private fun setupViews() {
        binding.btnBack.setOnClickListener { finish() }

        binding.edtPreferredDate.setOnClickListener { showDatePicker() }
        binding.edtTimeFrom.setOnClickListener { showTimePicker(true) }
        binding.edtTimeTo.setOnClickListener { showTimePicker(false) }

        binding.rgRepairMethod.setOnCheckedChangeListener { _, checkedId ->
            val atHome = checkedId == binding.rbAtHome.id
            binding.layoutAddress.visibility = if (atHome) View.VISIBLE else View.GONE
            binding.tvAddressHint.visibility = if (atHome && addresses.isEmpty()) View.VISIBLE else View.GONE
        }

        binding.btnSubmit.setOnClickListener { submitRepairRequest() }

        binding.spAddress.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                selectedAddressId = addresses.getOrNull(position)?.addressId
            }
            override fun onNothingSelected(parent: AdapterView<*>?) = Unit
        }
    }

    private fun loadAddresses() {
        val rawToken = sessionManager.getToken()
        if (rawToken.isNullOrBlank()) {
            binding.tvAddressHint.visibility = View.VISIBLE
            binding.tvAddressHint.text = "Bạn cần đăng nhập lại để tải địa chỉ."
            return
        }
        val token = if (rawToken.startsWith("Bearer ", true)) rawToken else "Bearer $rawToken"
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerAddressApi.getAddresses(token)
                if (!response.isSuccessful) {
                    binding.tvAddressHint.visibility = View.VISIBLE
                    binding.tvAddressHint.text = "Không tải được địa chỉ (HTTP ${response.code()})."
                    return@launch
                }
                addresses = response.body().orEmpty()
                setupAddressSpinner(addresses)
            } catch (e: Exception) {
                binding.tvAddressHint.visibility = View.VISIBLE
                binding.tvAddressHint.text = "Lỗi tải địa chỉ: ${e.localizedMessage}"
            }
        }
    }

    private fun setupAddressSpinner(data: List<CustomerAddress>) {
        val labels = data.map {
            val base = listOf(it.addressLine, it.wardName, it.provinceName).joinToString(", ")
            if (it.isDefault) "$base (mặc định)" else base
        }

        val adapter = object : ArrayAdapter<String>(
            this,
            android.R.layout.simple_spinner_item,
            labels.ifEmpty { listOf("Chưa có địa chỉ") }
        ) {
            override fun getView(position: Int, convertView: View?, parent: android.view.ViewGroup): View {
                return createItemView(position)
            }
            override fun getDropDownView(position: Int, convertView: View?, parent: android.view.ViewGroup): View {
                return createItemView(position, true)
            }
            private fun createItemView(position: Int, dropDown: Boolean = false): View {
                return TextView(context).apply {
                    text = if (labels.isEmpty()) "Chưa có địa chỉ" else labels[position]
                    setTextColor(android.graphics.Color.parseColor(if (dropDown) "#2C201A" else "#FFF4EA"))
                    textSize = 14f
                    setPadding(24, 22, 24, 22)
                    if (dropDown) setBackgroundColor(android.graphics.Color.parseColor("#FFF8F1"))
                }
            }
        }

        binding.spAddress.adapter = adapter
        val defaultIndex = data.indexOfFirst { it.isDefault }.takeIf { it >= 0 } ?: 0
        if (data.isNotEmpty()) {
            binding.spAddress.setSelection(defaultIndex)
            selectedAddressId = data[defaultIndex].addressId
            binding.tvAddressHint.visibility = View.GONE
        } else {
            selectedAddressId = null
            binding.tvAddressHint.visibility = View.VISIBLE
            binding.tvAddressHint.text = "Chưa có địa chỉ nào. Vào Tài khoản → Địa chỉ để thêm địa chỉ trước khi chọn sửa tại nhà."
        }
    }

    private fun showDatePicker() {
        val now = Calendar.getInstance()
        val dialog = DatePickerDialog(
            this,
            { _, year, month, dayOfMonth ->
                preferredDate = LocalDate.of(year, month + 1, dayOfMonth)
                binding.edtPreferredDate.setText(preferredDate.toString())
            },
            now.get(Calendar.YEAR),
            now.get(Calendar.MONTH),
            now.get(Calendar.DAY_OF_MONTH)
        )
        dialog.datePicker.minDate = System.currentTimeMillis() - 1000
        dialog.show()
    }

    private fun showTimePicker(isFrom: Boolean) {
        val now = Calendar.getInstance()
        TimePickerDialog(
            this,
            { _, hourOfDay, minute ->
                val value = LocalTime.of(hourOfDay, minute)
                val formatted = value.format(DateTimeFormatter.ofPattern("HH:mm"))
                if (isFrom) {
                    preferredTimeFrom = value
                    binding.edtTimeFrom.setText(formatted)
                } else {
                    preferredTimeTo = value
                    binding.edtTimeTo.setText(formatted)
                }
            },
            now.get(Calendar.HOUR_OF_DAY),
            now.get(Calendar.MINUTE),
            true
        ).show()
    }

    private fun submitRepairRequest() {
        val description = binding.edtDescription.text?.toString()?.trim().orEmpty()
        val repairMethod = if (binding.rbAtHome.isChecked) 1 else 2

        if (shopId <= 0 || serviceId <= 0) {
            toast("Thiếu thông tin cửa hàng hoặc dịch vụ.")
            return
        }
        if (description.length < 10) {
            binding.tilDescription.error = "Mô tả sự cố ít nhất 10 ký tự"
            return
        } else {
            binding.tilDescription.error = null
        }
        val date = preferredDate
        val from = preferredTimeFrom
        val to = preferredTimeTo
        if (date == null || from == null || to == null) {
            toast("Hãy chọn ngày và khung giờ mong muốn.")
            return
        }
        if (!to.isAfter(from)) {
            toast("Giờ kết thúc phải sau giờ bắt đầu.")
            return
        }
        val addressId = if (repairMethod == 1) selectedAddressId else null
        if (repairMethod == 1 && addressId == null) {
            toast("Sửa tại nhà cần chọn một địa chỉ hợp lệ.")
            return
        }

        val rawToken = sessionManager.getToken()
        if (rawToken.isNullOrBlank()) {
            toast("Phiên đăng nhập đã hết. Hãy đăng nhập lại.")
            return
        }
        val token = if (rawToken.startsWith("Bearer ", true)) rawToken else "Bearer $rawToken"

        val request = CreateRepairRequest(
            shopId = shopId,
            serviceId = serviceId,
            repairMethod = repairMethod,
            addressId = addressId,
            description = description,
            preferredDate = date.toString(),
            preferredTimeFrom = from.format(DateTimeFormatter.ofPattern("HH:mm:ss")),
            preferredTimeTo = to.format(DateTimeFormatter.ofPattern("HH:mm:ss"))
        )

        setLoading(true)
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerRepairApi.createRepairRequest(token, request)
                if (response.isSuccessful) {
                    val body = response.body()
                    Toast.makeText(
                        this@CreateRepairRequestActivity,
                        body?.message ?: "Tạo yêu cầu sửa chữa thành công.",
                        Toast.LENGTH_LONG
                    ).show()
                    finish()
                } else {
                    toast("Không thể tạo yêu cầu (HTTP ${response.code()}).")
                }
            } catch (e: Exception) {
                toast("Lỗi kết nối: ${e.localizedMessage}")
            } finally {
                setLoading(false)
            }
        }
    }

    private fun setLoading(loading: Boolean) {
        binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        binding.btnSubmit.isEnabled = !loading
    }

    private fun toast(message: String) {
        Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
    }
}
