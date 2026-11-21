
package com.example.fixnear.ui.manager

import android.content.res.ColorStateList
import android.graphics.Color
import android.os.Bundle
import android.text.InputType
import android.util.Patterns
import android.view.View
import android.widget.EditText
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.InviteTechnicianRequest
import com.example.fixnear.data.remote.ShopTechnicianNetwork
import com.example.fixnear.databinding.ActivityShopMembersBinding
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import kotlinx.coroutines.launch
import retrofit2.Response

class ShopMembersActivity : AppCompatActivity() {

    private lateinit var binding: ActivityShopMembersBinding
    private lateinit var sessionManager: SessionManager

    private val technicianAdapter = ShopTechnicianAdapter()
    private val invitationAdapter = ShopTechnicianAdapter()

    private val api by lazy {
        ShopTechnicianNetwork.api
    }

    private var shopId = -1
    private var shopRole = 0
    private var loading = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityShopMembersBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        shopId = intent.getIntExtra("SHOP_ID", -1)
        shopRole = intent.getIntExtra("SHOP_ROLE", 0)

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.rvTechnicians.apply {
            layoutManager = LinearLayoutManager(
                this@ShopMembersActivity
            )
            adapter = technicianAdapter
            isNestedScrollingEnabled = false
        }

        binding.rvInvitations.apply {
            layoutManager = LinearLayoutManager(
                this@ShopMembersActivity
            )
            adapter = invitationAdapter
            isNestedScrollingEnabled = false
        }

        binding.tvRole.text = when (shopRole) {
            1 -> "Quyền: Chủ cửa hàng"
            2 -> "Quyền: Quản lý"
            3 -> "Quyền: Nhân viên"
            else -> "Chưa xác định quyền"
        }

        binding.btnAddMember.visibility =
            if (shopRole == 1 && shopId > 0) {
                View.VISIBLE
            } else {
                View.GONE
            }

        binding.btnAddMember.setOnClickListener {
            showInviteDialog()
        }

        binding.btnRefresh.setOnClickListener {
            loadData()
        }

        if (shopId <= 0) {
            binding.tvEmpty.text = "Chưa chọn cửa hàng"
            binding.tvInvitationEmpty.text =
                "Không thể tải lời mời"
            return
        }

        loadData()
    }

    private fun authorization(): String? {
        val token = sessionManager.getToken()
            ?.trim()
            .orEmpty()

        if (token.isBlank()) {
            Toast.makeText(
                this,
                "Phiên đăng nhập đã hết hạn",
                Toast.LENGTH_LONG
            ).show()
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

    private fun setLoading(value: Boolean) {
        loading = value

        binding.progressBar.visibility =
            if (value) View.VISIBLE else View.GONE

        binding.btnRefresh.isEnabled = !value
        binding.btnAddMember.isEnabled = !value

        binding.btnAddMember.alpha =
            if (value) 0.6f else 1f
    }

    private fun loadData() {
        if (shopId <= 0 || loading) return

        val token = authorization() ?: return

        lifecycleScope.launch {
            setLoading(true)

            try {
                // Danh sách kỹ thuật viên đang thuộc Shop
                val techResponse = api.getTechnicians(
                    token = token,
                    shopId = shopId
                )

                if (techResponse.isSuccessful) {
                    val items = extractData(
                        techResponse.body()
                    ).map { parseTechnician(it) }

                    technicianAdapter.submitItems(items)

                    binding.tvTechnicianTitle.text =
                        "Danh sách kỹ thuật viên (${items.size})"

                    binding.layoutEmpty.visibility =
                        if (items.isEmpty()) {
                            View.VISIBLE
                        } else {
                            View.GONE
                        }

                    binding.tvEmpty.text =
                        "Chưa có kỹ thuật viên trong cửa hàng"
                } else {
                    technicianAdapter.submitItems(emptyList())
                    binding.layoutEmpty.visibility = View.VISIBLE

                    binding.tvEmpty.text =
                        "Không tải được kỹ thuật viên\n" +
                                "HTTP ${techResponse.code()}"
                }

                // Lời mời đã gửi
                val inviteResponse = api.getInvitations(
                    token = token,
                    shopId = shopId
                )

                if (inviteResponse.isSuccessful) {
                    val items = extractData(
                        inviteResponse.body()
                    ).map { parseInvitation(it) }

                    invitationAdapter.submitItems(items)

                    binding.tvInvitationTitle.text =
                        "Lời mời kỹ thuật viên (${items.size})"

                    binding.tvInvitationEmpty.visibility =
                        if (items.isEmpty()) {
                            View.VISIBLE
                        } else {
                            View.GONE
                        }

                    binding.tvInvitationEmpty.text =
                        "Chưa có lời mời kỹ thuật viên"
                } else {
                    invitationAdapter.submitItems(emptyList())

                    binding.tvInvitationEmpty.visibility =
                        View.VISIBLE

                    binding.tvInvitationEmpty.text =
                        "Không tải được lời mời\n" +
                                "HTTP ${inviteResponse.code()}"
                }

            } catch (e: Exception) {
                binding.layoutEmpty.visibility = View.VISIBLE
                binding.tvEmpty.text =
                    "Không thể kết nối backend:\n" +
                            (e.message ?: "Lỗi không xác định")

                binding.tvInvitationEmpty.visibility =
                    View.VISIBLE

                binding.tvInvitationEmpty.text =
                    "Không thể tải danh sách lời mời"

                Toast.makeText(
                    this@ShopMembersActivity,
                    "Lỗi kết nối: ${e.message}",
                    Toast.LENGTH_LONG
                ).show()

            } finally {
                setLoading(false)
            }
        }
    }


    private fun showInviteDialog() {
        if (shopRole != 1 || shopId <= 0 || loading) return

        val density = resources.displayMetrics.density
        fun dp(value: Int) = (value * density).toInt()

        val container = android.widget.LinearLayout(this).apply {
            orientation = android.widget.LinearLayout.VERTICAL
            setPadding(dp(22), dp(24), dp(22), dp(18))

            background = android.graphics.drawable.GradientDrawable().apply {
                setColor(android.graphics.Color.parseColor("#FFF8F1"))
                cornerRadius = dp(20).toFloat()
            }
        }

        val title = android.widget.TextView(this).apply {
            text = "Thêm kỹ thuật viên"
            textSize = 21f
            setTextColor(android.graphics.Color.parseColor("#251A14"))
            setTypeface(null, android.graphics.Typeface.BOLD)
        }

        container.addView(title)

        val description = android.widget.TextView(this).apply {
            text = "Nhập email tài khoản kỹ thuật viên đã đăng ký " +
                    "để mời vào Shop #$shopId."
            textSize = 14f
            setTextColor(android.graphics.Color.parseColor("#66442F"))
        }

        container.addView(
            description,
            android.widget.LinearLayout.LayoutParams(
                -1, -2
            ).apply {
                topMargin = dp(12)
            }
        )

        val input = android.widget.EditText(this).apply {
            hint = "Email kỹ thuật viên"
            inputType = android.text.InputType.TYPE_CLASS_TEXT or
                    android.text.InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS
            isSingleLine = true
            textSize = 14f
            setTextColor(android.graphics.Color.parseColor("#FFF4EA"))
            setHintTextColor(android.graphics.Color.parseColor("#C7B9AF"))
            setPadding(dp(14), 0, dp(14), 0)

            background = android.graphics.drawable.GradientDrawable().apply {
                setColor(android.graphics.Color.parseColor("#302925"))
                cornerRadius = dp(12).toFloat()
                setStroke(
                    dp(1),
                    android.graphics.Color.parseColor("#795A44")
                )
            }
        }

        container.addView(
            input,
            android.widget.LinearLayout.LayoutParams(
                -1, dp(52)
            ).apply {
                topMargin = dp(18)
            }
        )

        val dialog = androidx.appcompat.app.AlertDialog.Builder(this)
            .setView(container)
            .setNegativeButton("Hủy", null)
            .setPositiveButton("Gửi lời mời", null)
            .create()

        dialog.setOnShowListener {
            dialog.window?.setBackgroundDrawableResource(
                android.R.color.transparent
            )

            dialog.window?.setLayout(
                (resources.displayMetrics.widthPixels * 0.90f).toInt(),
                android.view.WindowManager.LayoutParams.WRAP_CONTENT
            )

            dialog.getButton(
                androidx.appcompat.app.AlertDialog.BUTTON_POSITIVE
            ).apply {
                setTextColor(
                    android.graphics.Color.parseColor("#A5652B")
                )

                setOnClickListener {
                    val email = input.text.toString().trim()

                    if (
                        email.isBlank() ||
                        !android.util.Patterns.EMAIL_ADDRESS
                            .matcher(email).matches()
                    ) {
                        input.error = "Nhập email hợp lệ"
                        return@setOnClickListener
                    }

                    sendInvitation(email, dialog)
                }
            }

            dialog.getButton(
                androidx.appcompat.app.AlertDialog.BUTTON_NEGATIVE
            ).setTextColor(
                android.graphics.Color.parseColor("#66442F")
            )
        }

        dialog.show()
    }


    private fun sendInvitation(
        email: String,
        dialog: AlertDialog
    ) {
        if (loading) return

        val token = authorization() ?: return

        lifecycleScope.launch {
            setLoading(true)

            try {
                val response = api.inviteTechnician(
                    token = token,
                    shopId = shopId,
                    body = InviteTechnicianRequest(
                        email = email
                    )
                )

                if (response.isSuccessful) {
                    Toast.makeText(
                        this@ShopMembersActivity,
                        "Đã gửi lời mời tới $email",
                        Toast.LENGTH_LONG
                    ).show()

                    dialog.dismiss()

                } else {
                    val error = response.errorBody()
                        ?.string()
                        .orEmpty()

                    Toast.makeText(
                        this@ShopMembersActivity,
                        "Gửi lời mời thất bại " +
                                "(HTTP ${response.code()})\n$error",
                        Toast.LENGTH_LONG
                    ).show()
                }

                // Để finally chạy xong rồi mới tải lại
                if (response.isSuccessful) {
                    setLoading(false)
                    loadData()
                }

            } catch (e: Exception) {
                Toast.makeText(
                    this@ShopMembersActivity,
                    "Không thể gửi lời mời: ${e.message}",
                    Toast.LENGTH_LONG
                ).show()

            } finally {
                setLoading(false)
            }
        }
    }

    // API trả { message, data: [...] }
    private fun extractData(
        body: JsonElement?
    ): List<JsonObject> {
        if (body == null || body.isJsonNull) {
            return emptyList()
        }

        val array = when {
            body.isJsonArray -> body.asJsonArray

            body.isJsonObject -> {
                val data = body.asJsonObject.get("data")
                if (data != null && data.isJsonArray) {
                    data.asJsonArray
                } else {
                    return emptyList()
                }
            }

            else -> return emptyList()
        }

        return array.mapNotNull { item ->
            if (item.isJsonObject) {
                item.asJsonObject
            } else {
                null
            }
        }
    }

    private fun stringValue(
        obj: JsonObject,
        vararg keys: String
    ): String {
        for (key in keys) {
            val value = obj.get(key)

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

    private fun parseTechnician(
        obj: JsonObject
    ): ShopTechnicianDisplay {

        // Chưa có DTO GetShopTechniciansAsync,
        // nên hỗ trợ một số tên field phổ biến.
        // Nếu backend đặt khác, ID vẫn giúp đối chiếu.
        val id = stringValue(
            obj,
            "technicianId",
            "TechnicianId"
        )

        val name = stringValue(
            obj,
            "technicianName",
            "TechnicianName",
            "fullName",
            "FullName",
            "name",
            "Name"
        ).ifBlank {
            if (id.isNotBlank()) {
                "Kỹ thuật viên #$id"
            } else {
                "Kỹ thuật viên"
            }
        }

        val email = stringValue(
            obj,
            "email",
            "Email"
        ).ifBlank {
            stringValue(
                obj,
                "phone",
                "phoneNumber",
                "Phone"
            )
        }

        val status = stringValue(
            obj,
            "status",
            "Status"
        )

        val technicianShopId = stringValue(
            obj,
            "technicianShopId",
            "TechnicianShopId"
        )

        return ShopTechnicianDisplay(
            name = name,
            email = email.ifBlank {
                "Chưa có thông tin liên hệ"
            },
            status = if (status.isBlank()) {
                "Trong cửa hàng"
            } else {
                "Trạng thái: $status"
            },
            extra = if (technicianShopId.isNotBlank()) {
                "Mã liên kết #$technicianShopId"
            } else {
                "Technician ID: $id"
            }
        )
    }

    private fun parseInvitation(
        obj: JsonObject
    ): ShopTechnicianDisplay {

        val id = stringValue(
            obj,
            "invitationId",
            "InvitationId"
        )

        val name = stringValue(
            obj,
            "technicianName",
            "TechnicianName"
        ).ifBlank {
            "Lời mời #$id"
        }

        val email = stringValue(
            obj,
            "email",
            "Email"
        )

        val status = stringValue(
            obj,
            "status",
            "Status"
        )

        val expires = stringValue(
            obj,
            "expiresAt",
            "ExpiresAt"
        )

        return ShopTechnicianDisplay(
            name = name,
            email = email,
            status = if (status.isBlank()) {
                "Chưa rõ trạng thái"
            } else {
                "Trạng thái: $status"
            },
            extra = if (expires.isNotBlank()) {
                "Hạn lời mời: $expires"
            } else {
                "Mã lời mời #$id"
            }
        )
    }
}
