
package com.example.fixnear.ui.manager

import android.content.Intent
import android.content.res.ColorStateList
import android.graphics.Typeface
import android.os.Bundle
import android.text.InputFilter
import android.text.InputType
import android.view.Gravity
import android.view.View
import android.view.WindowManager
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.widget.AppCompatButton
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.AcceptRepairRequest
import com.example.fixnear.data.model.RejectRepairRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityManagerRepairDetailBinding
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

class ManagerRepairDetailActivity : AppCompatActivity() {

    private lateinit var binding: ActivityManagerRepairDetailBinding

    private var shopId = -1
    private var requestId = -1

    private var currentStatus: Int? = null
    private var isSaving = false
    private var isLoading = false
    private var openedOnce = false

    private val caramel = 0xFFD8975B.toInt()
    private val brown = 0xFF66442F.toInt()
    private val dark = 0xFF251A14.toInt()
    private val cream = 0xFFFFF8F1.toInt()


    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityManagerRepairDetailBinding.inflate(
            layoutInflater
        )
        setContentView(binding.root)

        shopId = intent.getIntExtra("SHOP_ID", -1)
        requestId = intent.getIntExtra("REPAIR_REQUEST_ID", -1)

        binding.tvTitle.text = "Yêu cầu #$requestId"

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.btnAccept.setOnClickListener {
            if (canTakeAction()) {
                showActionDialog(true)
            }
        }

        binding.btnReject.setOnClickListener {
            if (canTakeAction()) {
                showActionDialog(false)
            }
        }

        binding.btnAssignTechnician.setOnClickListener {
            if (
                currentStatus == 2 &&
                !isSaving &&
                !isLoading &&
                shopId > 0 &&
                requestId > 0
            ) {
                startActivity(
                    Intent(
                        this,
                        ManagerAssignActivity::class.java
                    ).apply {
                        putExtra("SHOP_ID", shopId)
                        putExtra("REPAIR_REQUEST_ID", requestId)
                    }
                )
            }
        }

        updateActionButtons()
        loadDetail()
    }

    override fun onResume() {
        super.onResume()

        if (openedOnce) {
            // Cập nhật status khi quay về từ màn phân công.
            loadDetail()
        } else {
            openedOnce = true
        }
    }

    private fun dp(value: Int): Int {
        return (value * resources.displayMetrics.density).toInt()
    }

    private fun token(): String? {
        val raw = SessionManager(this)
            .getToken()
            ?.trim()
            ?.takeIf { it.isNotBlank() }
            ?: return null

        return if (raw.startsWith("Bearer ", true)) {
            raw
        } else {
            "Bearer $raw"
        }
    }

    private fun JsonObject.getText(
        vararg keys: String
    ): String {
        for (key in keys) {
            val value = entrySet().firstOrNull { entry ->
                entry.key.equals(key, ignoreCase = true)
            }?.value

            if (
                value != null &&
                !value.isJsonNull &&
                value.isJsonPrimitive
            ) {
                return value.asString
            }
        }

        return ""
    }

    private fun statusName(status: Int?): String {
        return when (status) {
            1 -> "Chờ tiếp nhận"
            2 -> "Đã tiếp nhận"
            3 -> "Đã từ chối"
            4 -> "Đã phân công"
            5 -> "Đang khảo sát"
            6 -> "Đã báo giá"
            7 -> "Khách đã duyệt giá"
            8 -> "Đang sửa chữa"
            9 -> "Đã sửa xong"
            10 -> "Đã thanh toán"
            11 -> "Đã hủy"
            else -> "Chưa xác định"
        }
    }

    // ===========================================
    // HIỂN THỊ ĐÚNG NÚT THEO TRẠNG THÁI
    // ===========================================

    private fun updateActionButtons() {

        val pending = currentStatus == 1
        val accepted = currentStatus == 2
        val busy = isSaving || isLoading

        // Chờ tiếp nhận: hiện 2 nút duyệt.
        binding.btnAccept.visibility =
            if (pending) View.VISIBLE else View.GONE

        binding.btnReject.visibility =
            if (pending) View.VISIBLE else View.GONE

        // Đã tiếp nhận: hiện nút phân công.
        binding.btnAssignTechnician.visibility =
            if (accepted) View.VISIBLE else View.GONE

        binding.btnAccept.isEnabled = pending && !busy
        binding.btnReject.isEnabled = pending && !busy

        binding.btnAssignTechnician.isEnabled =
            accepted && !busy

        binding.btnAccept.alpha =
            if (binding.btnAccept.isEnabled) 1f else 0.5f

        binding.btnReject.alpha =
            if (binding.btnReject.isEnabled) 1f else 0.5f

        binding.btnAssignTechnician.alpha =
            if (binding.btnAssignTechnician.isEnabled) 1f else 0.5f
    }

    private fun canTakeAction(): Boolean {
        if (isSaving || isLoading) return false

        if (currentStatus != 1) {
            toast("Yêu cầu không còn ở trạng thái chờ tiếp nhận.")
            return false
        }

        if (shopId <= 0 || requestId <= 0) {
            toast("Thông tin yêu cầu không hợp lệ.")
            return false
        }

        return true
    }

    // ===========================================
    // GET CHI TIẾT YÊU CẦU
    // ===========================================

    private fun loadDetail() {

        if (isLoading || isSaving) return

        val authorization = token()

        if (authorization == null) {
            showError("Phiên đăng nhập không hợp lệ.")
            return
        }

        if (shopId <= 0 || requestId <= 0) {
            showError("Mã cửa hàng hoặc yêu cầu không hợp lệ.")
            return
        }

        isLoading = true
        currentStatus = null

        updateActionButtons()

        binding.progressLoading.visibility = View.VISIBLE
        binding.tvState.visibility = View.VISIBLE
        binding.tvState.text = "Đang tải yêu cầu..."
        binding.detailCard.visibility = View.GONE

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerRepairApi.getRepairDetail(
                        token = authorization,
                        repairRequestId = requestId,
                        shopId = shopId
                    )

                if (!response.isSuccessful) {
                    val message = when (response.code()) {
                        401 -> "Phiên đăng nhập đã hết hạn."
                        403 -> "Không có quyền xem yêu cầu này."
                        404 -> "Không tìm thấy yêu cầu sửa chữa."
                        else ->
                            "Không tải được yêu cầu: HTTP ${response.code()}"
                    }

                    showError(message)
                    return@launch
                }

                val body = response.body()

                val data =
                    if (body?.isJsonObject == true) {
                        val root = body.asJsonObject
                        val nested = root.get("data")

                        if (nested?.isJsonObject == true) {
                            nested.asJsonObject
                        } else {
                            root
                        }
                    } else {
                        null
                    }

                if (data == null) {
                    showError("Backend trả dữ liệu không hợp lệ.")
                    return@launch
                }

                showDetail(data)

            } catch (e: Exception) {
                showError(
                    "Không thể kết nối backend:\n" +
                            (e.localizedMessage ?: "Lỗi không xác định")
                )
            } finally {
                isLoading = false
                binding.progressLoading.visibility = View.GONE
                updateActionButtons()
            }
        }
    }

    // ===========================================
    // HIỂN THỊ CHI TIẾT - KHÔNG CÒN JSON THÔ
    // ===========================================

    private fun showDetail(data: JsonObject) {
        val status = data.getText(
            "status",
            "repairStatus"
        ).toIntOrNull()

        currentStatus = status

        val customer = data.getText(
            "customerName",
            "customerFullName"
        ).ifBlank {
            val nested = data.get("customer")

            if (nested?.isJsonObject == true) {
                nested.asJsonObject.getText(
                    "fullName",
                    "customerName",
                    "name"
                )
            } else {
                ""
            }
        }.ifBlank {
            val id = data.getText("customerId")
            if (id.isNotBlank()) {
                "Khách hàng #$id"
            } else {
                "Chưa có thông tin"
            }
        }

        val description = data.getText(
            "description",
            "issueDescription"
        ).ifBlank {
            "Chưa có mô tả"
        }

        val method = data.getText("repairMethod")

        val createdAt = data.getText(
            "createdAt",
            "requestedAt"
        )
            .replace("T", " ")
            .substringBefore(".")
            .take(19)

        binding.tvStatus.text =
            "Trạng thái: ${statusName(status)}"

        binding.tvCustomer.text =
            "Khách hàng: $customer"

        binding.tvDescription.text =
            "Mô tả: $description"

        // Chưa xác nhận enum RepairMethod,
        // nên giữ đúng mã thay vì đoán tên.
        binding.tvRepairMethod.text =
            "Phương thức sửa chữa: " +
                    method.ifBlank { "Chưa xác định" }

        binding.tvCreatedAt.text =
            "Ngày tạo: " +
                    createdAt.ifBlank { "Chưa có thông tin" }

        // Bỏ JSON debug hoàn toàn.
        binding.tvExtra.text = ""
        binding.tvExtra.visibility = View.GONE

        binding.detailCard.visibility = View.VISIBLE

        binding.tvState.visibility =
            if (status == null) View.VISIBLE else View.GONE

        binding.tvState.text =
            if (status == null) {
                "Không đọc được trạng thái yêu cầu từ backend."
            } else {
                ""
            }

        updateActionButtons()
    }

    // ===========================================
    // POPUP TIẾP NHẬN / TỪ CHỐI
    // ===========================================

    private fun showActionDialog(isAccept: Boolean) {

        if (!canTakeAction()) return

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(22), dp(24), dp(22), dp(22))

            background =
                android.graphics.drawable.GradientDrawable().apply {
                    setColor(cream)
                    cornerRadius = dp(20).toFloat()
                }
        }

        val title = TextView(this).apply {
            text = if (isAccept) {
                "Tiếp nhận yêu cầu"
            } else {
                "Từ chối yêu cầu"
            }

            textSize = 20f
            setTextColor(dark)
            setTypeface(null, Typeface.BOLD)
        }

        container.addView(title)

        container.addView(
            TextView(this).apply {
                text = if (isAccept) {
                    "Xác nhận tiếp nhận yêu cầu #$requestId."
                } else {
                    "Nhập lý do từ chối yêu cầu #$requestId."
                }

                textSize = 14f
                setTextColor(brown)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(10)
            }
        )

        val input = EditText(this).apply {
            hint = if (isAccept) {
                "Ghi chú (không bắt buộc)"
            } else {
                "Lý do từ chối (bắt buộc)"
            }

            inputType =
                InputType.TYPE_CLASS_TEXT or
                        InputType.TYPE_TEXT_FLAG_MULTI_LINE or
                        InputType.TYPE_TEXT_FLAG_CAP_SENTENCES

            minLines = 3
            maxLines = 5

            gravity = Gravity.TOP or Gravity.START

            textSize = 14f
            setTextColor(cream)
            setHintTextColor(0xFFB9A79B.toInt())

            background =
                android.graphics.drawable.GradientDrawable().apply {
                    setColor(0xFF302925.toInt())
                    cornerRadius = dp(12).toFloat()

                    setStroke(
                        dp(1),
                        0xFF795A44.toInt()
                    )
                }

            filters = arrayOf(
                InputFilter.LengthFilter(500)
            )

            setPadding(
                dp(14), dp(12), dp(14), dp(12)
            )
        }

        container.addView(
            input,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(18)
            }
        )

        container.addView(
            TextView(this).apply {
                text = "Tối đa 500 ký tự"
                textSize = 12f
                setTextColor(brown)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(6)
            }
        )

        val dialog = AlertDialog.Builder(this)
            .setView(container)
            .setNegativeButton("Hủy", null)
            .setPositiveButton(
                if (isAccept) "Tiếp nhận" else "Từ chối",
                null
            )
            .create()

        dialog.setOnShowListener {
            dialog.window?.apply {
                setBackgroundDrawableResource(
                    android.R.color.transparent
                )

                setLayout(
                    (resources.displayMetrics.widthPixels * 0.90f).toInt(),
                    WindowManager.LayoutParams.WRAP_CONTENT
                )
            }

            val positive = dialog.getButton(
                AlertDialog.BUTTON_POSITIVE
            )

            val negative = dialog.getButton(
                AlertDialog.BUTTON_NEGATIVE
            )

            positive.setTextColor(caramel)
            negative.setTextColor(brown)

            positive.setOnClickListener {
                val text = input.text.toString().trim()

                if (!isAccept && text.isBlank()) {
                    input.error = "Vui lòng nhập lý do từ chối"
                    input.requestFocus()
                    return@setOnClickListener
                }

                if (!canTakeAction()) {
                    return@setOnClickListener
                }

                submitAction(
                    isAccept = isAccept,
                    text = text,
                    dialog = dialog
                )
            }
        }

        dialog.show()
    }

    // ===========================================
    // PATCH TIẾP NHẬN / TỪ CHỐI
    // ===========================================

    private fun submitAction(
        isAccept: Boolean,
        text: String,
        dialog: AlertDialog
    ) {

        if (!canTakeAction()) return

        val authorization = token() ?: run {
            toast("Vui lòng đăng nhập lại.")
            return
        }

        isSaving = true
        updateActionButtons()

        dialog.setCancelable(false)

        val positive = dialog.getButton(
            AlertDialog.BUTTON_POSITIVE
        )

        positive.isEnabled = false
        positive.text = "Đang xử lý..."

        lifecycleScope.launch {

            var refreshNeeded = false

            try {
                val response = if (isAccept) {

                    RetrofitClient.managerRepairApi.acceptRepair(
                        token = authorization,
                        repairRequestId = requestId,
                        shopId = shopId,
                        request = AcceptRepairRequest(
                            note = text.ifBlank { null }
                        )
                    )

                } else {

                    RetrofitClient.managerRepairApi.rejectRepair(
                        token = authorization,
                        repairRequestId = requestId,
                        shopId = shopId,
                        request = RejectRepairRequest(
                            reason = text
                        )
                    )
                }

                if (response.isSuccessful) {

                    toast(
                        if (isAccept) {
                            "Tiếp nhận yêu cầu #$requestId thành công"
                        } else {
                            "Đã từ chối yêu cầu #$requestId"
                        }
                    )

                    dialog.dismiss()
                    refreshNeeded = true

                } else {

                    val raw = response.errorBody()
                        ?.string()
                        .orEmpty()

                    val message = try {
                        org.json.JSONObject(raw)
                            .optString("message")
                            .ifBlank {
                                "HTTP ${response.code()}"
                            }
                    } catch (_: Exception) {
                        "HTTP ${response.code()}"
                    }

                    toast("Không thể xử lý: $message")

                    if (response.code() == 409) {
                        dialog.dismiss()
                        refreshNeeded = true
                    }
                }

            } catch (e: Exception) {

                toast(
                    "Lỗi kết nối:\n" +
                            (e.localizedMessage ?: "Không xác định")
                )

            } finally {

                isSaving = false

                positive.isEnabled = true
                positive.text =
                    if (isAccept) "Tiếp nhận" else "Từ chối"

                dialog.setCancelable(true)
                updateActionButtons()

                if (refreshNeeded) {
                    loadDetail()
                }
            }
        }
    }

    private fun showError(message: String) {
        currentStatus = null
        updateActionButtons()

        binding.detailCard.visibility = View.GONE
        binding.tvState.visibility = View.VISIBLE
        binding.tvState.text = message
    }

    private fun toast(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
