
package com.example.fixnear.ui.manager

import android.app.Activity
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.ManagerTechnicianItem
import com.example.fixnear.data.remote.ManagerAssignTechnicianRequest
import com.example.fixnear.data.remote.ShopTechnicianNetwork
import com.example.fixnear.databinding.ActivityManagerAssignBinding
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

class ManagerAssignActivity : AppCompatActivity() {

    private lateinit var binding: ActivityManagerAssignBinding
    private lateinit var technicianAdapter: ManagerTechnicianAdapter
    private lateinit var sessionManager: SessionManager

    private val api by lazy {
        ShopTechnicianNetwork.api
    }

    private var shopId = -1
    private var repairRequestId = -1

    private var selectedTechnician: ManagerTechnicianItem? = null

    private var isLoading = false
    private var isAssigning = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityManagerAssignBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        shopId = intent.getIntExtra("SHOP_ID", -1)
        repairRequestId = intent.getIntExtra(
            "REPAIR_REQUEST_ID",
            -1
        )

        binding.tvRequestInfo.text =
            "Yêu cầu #$repairRequestId • Shop #$shopId"

        binding.btnBack.setOnClickListener {
            finish()
        }

        setupRecyclerView()

        binding.btnAssign.setOnClickListener {
            val technician = selectedTechnician

            if (technician == null) {
                toast("Vui lòng chọn kỹ thuật viên.")
                return@setOnClickListener
            }

            if (
                shopId <= 0 ||
                repairRequestId <= 0 ||
                technician.technicianId <= 0
            ) {
                toast("Thông tin phân công không hợp lệ.")
                return@setOnClickListener
            }

            assignTechnician(technician)
        }

        if (shopId <= 0 || repairRequestId <= 0) {
            showState("Thiếu thông tin cửa hàng hoặc yêu cầu sửa chữa.")
            updateButton()
            return
        }

        // GET kỹ thuật viên thật từ backend.
        loadTechnicians()

        updateButton()
    }

    private fun setupRecyclerView() {

        technicianAdapter = ManagerTechnicianAdapter { technician ->

            if (isLoading || isAssigning) {
                return@ManagerTechnicianAdapter
            }

            selectedTechnician = technician

            binding.tvSelected.text =
                "Đã chọn: ${technician.fullName}"

            updateButton()
        }

        binding.rvTechnicians.apply {
            layoutManager = LinearLayoutManager(
                this@ManagerAssignActivity
            )

            adapter = technicianAdapter
        }
    }

    private fun getAuthorization(): String? {

        val token = sessionManager.getToken()
            ?.trim()
            .orEmpty()

        if (token.isBlank()) {
            toast("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.")
            return null
        }

        return if (
            token.startsWith("Bearer ", ignoreCase = true)
        ) {
            token
        } else {
            "Bearer $token"
        }
    }

    // ============================================
    // GET /api/shops/{shopId}/technicians
    // ============================================

    private fun loadTechnicians() {

        if (isLoading || isAssigning || shopId <= 0) {
            return
        }

        val token = getAuthorization() ?: run {
            showState("Không tìm thấy token đăng nhập.")
            return
        }

        lifecycleScope.launch {

            isLoading = true
            selectedTechnician = null

            technicianAdapter.submitItems(emptyList())

            binding.tvSelected.text =
                "Chưa chọn kỹ thuật viên"

            showState("Đang tải kỹ thuật viên...")

            updateButton()

            try {

                val response = api.getTechnicians(
                    token = token,
                    shopId = shopId
                )

                if (!response.isSuccessful) {

                    showState(
                        "Không tải được kỹ thuật viên.\n" +
                                readError(
                                    response.code(),
                                    response.errorBody()?.string()
                                )
                    )

                    return@launch
                }

                val json = response.body()

                val technicians = parseTechnicians(json)

                technicianAdapter.submitItems(technicians)

                if (technicians.isEmpty()) {

                    showState(
                        "Shop #$shopId chưa có kỹ thuật viên hợp lệ.\n" +
                                "Hãy kiểm tra danh sách kỹ thuật viên " +
                                "trong cửa hàng."
                    )

                } else {

                    binding.tvState.visibility = View.GONE

                    toast(
                        "Đã tải ${technicians.size} kỹ thuật viên."
                    )
                }

            } catch (e: Exception) {

                showState(
                    "Lỗi kết nối backend:\n" +
                            (e.localizedMessage ?: "Không xác định")
                )

            } finally {

                isLoading = false
                updateButton()
            }
        }
    }

    // ============================================
    // Đọc response:
    // {
    //   "message": "...",
    //   "data": [...]
    // }
    // ============================================

    private fun parseTechnicians(
        response: JsonElement?
    ): List<ManagerTechnicianItem> {

        if (response == null || response.isJsonNull) {
            return emptyList()
        }

        val array = when {

            response.isJsonArray -> {
                response.asJsonArray
            }

            response.isJsonObject -> {

                val data = response.asJsonObject.get("data")

                if (data != null && data.isJsonArray) {
                    data.asJsonArray
                } else {
                    return emptyList()
                }
            }

            else -> return emptyList()
        }

        return buildList {

            for (element in array) {

                if (!element.isJsonObject) {
                    continue
                }

                val obj = element.asJsonObject

                // Quan trọng:
                // Dùng TechnicianId chứ không phải
                // TechnicianShopId hay UserId.
                val technicianId = getInt(
                    obj,
                    "technicianId",
                    "TechnicianId"
                )

                if (technicianId <= 0) {
                    continue
                }

                val fullName = getString(
                    obj,
                    "technicianName",
                    "TechnicianName",
                    "fullName",
                    "FullName",
                    "name",
                    "Name"
                ).ifBlank {
                    "Kỹ thuật viên #$technicianId"
                }

                val email = getString(
                    obj,
                    "email",
                    "Email"
                )

                val phone = getString(
                    obj,
                    "phone",
                    "Phone",
                    "phoneNumber",
                    "PhoneNumber"
                )

                val experience = getString(
                    obj,
                    "experienceYears",
                    "ExperienceYears"
                )

                val description = buildString {

                    if (email.isNotBlank()) {
                        append(email)
                    }

                    if (phone.isNotBlank()) {
                        if (isNotEmpty()) append(" • ")
                        append(phone)
                    }

                    if (experience.isNotBlank()) {
                        if (isNotEmpty()) append(" • ")
                        append("$experience năm kinh nghiệm")
                    }

                    if (isEmpty()) {
                        append("Kỹ thuật viên của cửa hàng")
                    }
                }

                add(
                    ManagerTechnicianItem(
                        technicianId = technicianId,
                        fullName = fullName,
                        description = description
                    )
                )
            }
        }.distinctBy {
            it.technicianId
        }
    }

    private fun getString(
        obj: JsonObject,
        vararg keys: String
    ): String {

        for (key in keys) {

            val element = obj.get(key)

            if (
                element != null &&
                !element.isJsonNull &&
                element.isJsonPrimitive
            ) {
                return element.asString
            }
        }

        return ""
    }

    private fun getInt(
        obj: JsonObject,
        vararg keys: String
    ): Int {

        for (key in keys) {

            val element = obj.get(key)

            if (
                element != null &&
                !element.isJsonNull &&
                element.isJsonPrimitive
            ) {
                val value = element.asString.toIntOrNull()

                if (value != null) {
                    return value
                }
            }
        }

        return -1
    }

    // ============================================
    // POST /api/manager/repair-assignments
    // ?shopId=...
    // ============================================

    private fun assignTechnician(
        technician: ManagerTechnicianItem
    ) {

        if (isAssigning || isLoading) {
            return
        }

        val token = getAuthorization() ?: return

        lifecycleScope.launch {

            isAssigning = true

            binding.tvSelected.text =
                "Đang phân công cho ${technician.fullName}..."

            updateButton()

            try {

                val body = ManagerAssignTechnicianRequest(
                    repairRequestId = repairRequestId,
                    technicianId = technician.technicianId,
                    note = "Phân công kỹ thuật viên kiểm tra và sửa chữa."
                )

                val response = api.assignTechnician(
                    token = token,
                    shopId = shopId,
                    body = body
                )

                if (response.isSuccessful) {

                    toast(
                        "Phân công ${technician.fullName} thành công!"
                    )

                    setResult(Activity.RESULT_OK)

                    finish()

                }
                else {
                    binding.tvSelected.text =
                        "Đã chọn: ${technician.fullName}"

                    if (response.code() == 409) {
                        showAlreadyAssignedDialog()
                    } else {
                        val message = readError(
                            response.code(),
                            response.errorBody()?.string()
                        )
                        toast("Phân công thất bại:\n$message")
                    }
                }


            } catch (e: Exception) {

                binding.tvSelected.text =
                    "Đã chọn: ${technician.fullName}"

                toast(
                    "Không thể phân công:\n" +
                            (e.localizedMessage ?: "Lỗi kết nối")
                )

            } finally {

                isAssigning = false
                updateButton()
            }
        }
    }

    private fun showAlreadyAssignedDialog() {

        val density = resources.displayMetrics.density
        fun dp(value: Int) = (value * density).toInt()

        val cream = android.graphics.Color.parseColor("#FFF8F1")
        val dark = android.graphics.Color.parseColor("#251A14")
        val brown = android.graphics.Color.parseColor("#66442F")
        val caramel = android.graphics.Color.parseColor("#D8975B")

        val container = android.widget.LinearLayout(this).apply {
            orientation = android.widget.LinearLayout.VERTICAL
            setPadding(dp(22), dp(24), dp(22), dp(22))

            background = android.graphics.drawable.GradientDrawable().apply {
                setColor(cream)
                cornerRadius = dp(20).toFloat()
            }
        }

        val title = android.widget.TextView(this).apply {
            text = "Yêu cầu đã được phân công"
            textSize = 19f
            setTextColor(dark)
            setTypeface(null, android.graphics.Typeface.BOLD)
        }

        container.addView(title)

        val message = android.widget.TextView(this).apply {
            text = "Yêu cầu #$repairRequestId đã có kỹ thuật viên " +
                    "được phân công.\n\n" +
                    "Mỗi yêu cầu chỉ có một phân công hợp lệ " +
                    "tại một thời điểm. Hãy quay lại chi tiết " +
                    "để kiểm tra hoặc sử dụng chức năng đổi kỹ thuật viên."
            textSize = 14f
            setTextColor(brown)
            setLineSpacing(dp(3).toFloat(), 1f)
        }

        container.addView(
            message,
            android.widget.LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(12)
            }
        )

        val buttons = android.widget.LinearLayout(this).apply {
            orientation = android.widget.LinearLayout.HORIZONTAL
            gravity = android.view.Gravity.END
        }

        val btnStay = android.widget.TextView(this).apply {
            text = "Ở lại"
            textSize = 14f
            setTextColor(brown)
            gravity = android.view.Gravity.CENTER
            setPadding(dp(12), dp(12), dp(12), dp(12))
            isClickable = true
            isFocusable = true
        }

        val btnBack = android.widget.TextView(this).apply {
            text = "Về chi tiết"
            textSize = 14f
            setTypeface(null, android.graphics.Typeface.BOLD)
            setTextColor(caramel)
            gravity = android.view.Gravity.CENTER
            setPadding(dp(12), dp(12), dp(12), dp(12))
            isClickable = true
            isFocusable = true
        }

        buttons.addView(btnStay)
        buttons.addView(btnBack)

        container.addView(
            buttons,
            android.widget.LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(16)
            }
        )

        val dialog = androidx.appcompat.app.AlertDialog.Builder(this)
            .setView(container)
            .create()

        dialog.show()

        dialog.window?.apply {
            setBackgroundDrawableResource(android.R.color.transparent)
            setLayout(
                (resources.displayMetrics.widthPixels * 0.88f).toInt(),
                android.view.WindowManager.LayoutParams.WRAP_CONTENT
            )
        }

        btnStay.setOnClickListener {
            dialog.dismiss()
        }

        btnBack.setOnClickListener {
            dialog.dismiss()
            finish()
        }
    }

    private fun readError(
        statusCode: Int,
        errorBody: String?
    ): String {

        val raw = errorBody.orEmpty()

        if (raw.isNotBlank()) {

            try {

                val json = com.google.gson.JsonParser
                    .parseString(raw)

                if (json.isJsonObject) {

                    val message = getString(
                        json.asJsonObject,
                        "message",
                        "Message",
                        "title",
                        "Title"
                    )

                    if (message.isNotBlank()) {
                        return "HTTP $statusCode: $message"
                    }
                }

            } catch (_: Exception) {
                // Trả lỗi thô nếu backend không dùng JSON.
            }

            return "HTTP $statusCode: $raw"
        }

        return "HTTP $statusCode"
    }

    private fun showState(message: String) {

        binding.tvState.visibility = View.VISIBLE
        binding.tvState.text = message
    }

    private fun updateButton() {

        val technician = selectedTechnician

        val enabled =
            technician != null &&
                    !isLoading &&
                    !isAssigning &&
                    shopId > 0 &&
                    repairRequestId > 0

        binding.btnAssign.isEnabled = enabled
        binding.btnAssign.alpha =
            if (enabled) 1f else 0.5f

        binding.btnAssign.text = when {

            isAssigning -> {
                "Đang phân công..."
            }

            isLoading -> {
                "Đang tải kỹ thuật viên..."
            }

            technician == null -> {
                "Chọn kỹ thuật viên"
            }

            else -> {
                "Phân công kỹ thuật viên"
            }
        }
    }

    private fun toast(message: String) {

        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
