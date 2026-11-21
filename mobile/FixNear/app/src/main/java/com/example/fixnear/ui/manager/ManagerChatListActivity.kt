package com.example.fixnear.ui.manager

import android.content.Intent
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.os.Bundle
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.customer.ChatConversationCreate
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.ui.home.CustomerChatActivity
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

private data class ShopChatRequestItem(
    val repairRequestId: Int,
    val customerName: String,
    val customerId: Int,
    val description: String
)

class ManagerChatListActivity : AppCompatActivity() {

    private val backgroundColor = Color.parseColor("#151210")
    private val caramel = Color.parseColor("#D8975B")
    private val cream = Color.parseColor("#FFF8F1")
    private val brown = Color.parseColor("#66442F")
    private val dark = Color.parseColor("#251A14")
    private val muted = Color.parseColor("#C7B9AF")

    private var shopId = -1
    private var shopName = ""

    private var loading = false
    private var openingConversation = false

    private lateinit var adapter: ChatRequestAdapter
    private lateinit var tvState: TextView
    private lateinit var rvRequests: RecyclerView
    private lateinit var btnRefresh: TextView

    private fun dp(value: Int): Int {
        return (value * resources.displayMetrics.density).toInt()
    }

    private fun rounded(color: Int, radius: Int): GradientDrawable {
        return GradientDrawable().apply {
            setColor(color)
            cornerRadius = dp(radius).toFloat()
        }
    }

    private fun token(): String? {
        val raw = SessionManager(this)
            .getToken()
            ?.trim()
            .orEmpty()

        if (raw.isBlank()) return null

        return if (raw.startsWith("Bearer ", true)) {
            raw
        } else {
            "Bearer $raw"
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        shopId = intent.getIntExtra("SHOP_ID", -1)
        shopName = intent.getStringExtra("SHOP_NAME").orEmpty()

        setupUi()
        onBackPressedDispatcher.addCallback(
            this,
            object : androidx.activity.OnBackPressedCallback(true) {
                override fun handleOnBackPressed() {
                    finish()
                }
            }
        )

        if (shopId <= 0) {
            tvState.text = "Mã cửa hàng không hợp lệ."
            return
        }

        loadCustomerRequests()
    }

    private fun setupUi() {

        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setBackgroundColor(backgroundColor)

            // Đẩy nội dung xuống dưới status bar
            fitsSystemWindows = true

            setPadding(
                dp(20),
                dp(16),
                dp(20),
                dp(14)
            )
        }

        // HEADER
        val header = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
        }


        val btnBack = TextView(this).apply {
            text = "‹"
            textSize = 32f
            gravity = Gravity.CENTER
            setTextColor(cream)

            background = rounded(
                Color.parseColor("#302925"),
                13
            )

            isClickable = true
            isFocusable = true

            contentDescription = "Quay lại"

            setOnClickListener {
                this@ManagerChatListActivity.finish()
            }
        }


        header.addView(
            btnBack,
            LinearLayout.LayoutParams(dp(44), dp(44))
        )

        header.addView(
            TextView(this).apply {
                text = "FixNear"
                textSize = 20f
                setTypeface(null, Typeface.BOLD)
                setTextColor(caramel)
                gravity = Gravity.CENTER
            },
            LinearLayout.LayoutParams(0, dp(44), 1f)
        )

        btnRefresh = TextView(this).apply {
            text = "↻"
            textSize = 27f
            gravity = Gravity.CENTER
            setTextColor(caramel)

            setOnClickListener {
                loadCustomerRequests()
            }
        }

        header.addView(
            btnRefresh,
            LinearLayout.LayoutParams(dp(44), dp(44))
        )

        root.addView(header)

        // TITLE
        root.addView(
            TextView(this).apply {
                text = "Tin nhắn khách hàng"
                textSize = 24f
                setTypeface(null, Typeface.BOLD)
                setTextColor(cream)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(25)
            }
        )

        root.addView(
            TextView(this).apply {
                text = shopName.ifBlank {
                    "Cửa hàng #$shopId"
                }
                textSize = 13f
                setTextColor(caramel)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(7)
            }
        )

        root.addView(
            TextView(this).apply {
                text = "Khách hàng đã gửi yêu cầu sửa chữa"
                textSize = 16f
                setTypeface(null, Typeface.BOLD)
                setTextColor(cream)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(28)
            }
        )

        tvState = TextView(this).apply {
            text = "Đang tải danh sách khách hàng..."
            textSize = 13f
            setTextColor(muted)
            setPadding(0, dp(12), 0, dp(12))
        }

        root.addView(tvState)

        adapter = ChatRequestAdapter { item ->
            openRealConversation(item)
        }

        rvRequests = RecyclerView(this).apply {
            layoutManager = LinearLayoutManager(
                this@ManagerChatListActivity
            )
            adapter = this@ManagerChatListActivity.adapter
            clipToPadding = false
            setPadding(0, dp(6), 0, dp(14))
        }

        root.addView(
            rvRequests,
            LinearLayout.LayoutParams(-1, 0, 1f)
        )

        setContentView(root)
    }

    // ==========================================
    // API THẬT:
    // GET /api/manager/repair-requests?shopId=X
    // ==========================================

    private fun loadCustomerRequests() {

        if (loading || shopId <= 0) return

        val authorization = token()

        if (authorization == null) {
            tvState.text = "Vui lòng đăng nhập lại."
            return
        }

        loading = true
        btnRefresh.isEnabled = false

        tvState.text = "Đang tải khách hàng từ Shop..."
        adapter.submitItems(emptyList())

        lifecycleScope.launch {

            try {

                val response = RetrofitClient.managerRepairApi
                    .getRepairRequests(
                        token = authorization,
                        shopId = shopId
                    )

                if (!response.isSuccessful) {
                    tvState.text =
                        "Không tải được yêu cầu sửa chữa: " +
                                "HTTP ${response.code()}."
                    return@launch
                }

                val body = response.body()

                val array = when {

                    body?.isJsonArray == true ->
                        body.asJsonArray

                    body?.isJsonObject == true -> {
                        val data = body.asJsonObject.get("data")

                        if (data?.isJsonArray == true) {
                            data.asJsonArray
                        } else {
                            null
                        }
                    }

                    else -> null
                }

                if (array == null) {
                    tvState.text =
                        "Backend trả danh sách không hợp lệ."
                    return@launch
                }

                val items = array.mapNotNull { element ->

                    if (!element.isJsonObject) {
                        return@mapNotNull null
                    }

                    val obj = element.asJsonObject

                    val requestId = obj.intField(
                        "repairRequestId"
                    )

                    if (requestId <= 0) {
                        return@mapNotNull null
                    }

                    val customerId = obj.intField(
                        "customerId"
                    )

                    val customerName = obj.stringField(
                        "customerName",
                        "customerFullName"
                    ).ifBlank {
                        if (customerId > 0) {
                            "Khách hàng #$customerId"
                        } else {
                            "Khách hàng"
                        }
                    }

                    ShopChatRequestItem(
                        repairRequestId = requestId,
                        customerName = customerName,
                        customerId = customerId,
                        description = obj.stringField(
                            "description",
                            "issueDescription"
                        )
                    )
                }.sortedByDescending {
                    it.repairRequestId
                }

                adapter.submitItems(items)

                tvState.text = if (items.isEmpty()) {
                    "Cửa hàng chưa có yêu cầu sửa chữa."
                } else {
                    "Có ${items.size} yêu cầu • " +
                            "Chọn khách để mở hội thoại"
                }

            } catch (e: Exception) {
                tvState.text =
                    "Lỗi kết nối: ${e.localizedMessage}"

            } finally {
                loading = false
                btnRefresh.isEnabled = true
            }
        }
    }

    // ==========================================
    // TẠO HOẶC MỞ HỘI THOẠI THẬT
    //
    // POST /api/conversations
    // Body: { "repairRequestId": X }
    // ==========================================

    private fun openRealConversation(
        item: ShopChatRequestItem
    ) {

        if (openingConversation) return

        val authorization = token()

        if (authorization == null) {
            toast("Phiên đăng nhập không hợp lệ.")
            return
        }

        openingConversation = true

        tvState.text =
            "Đang mở hội thoại yêu cầu " +
                    "#${item.repairRequestId}..."

        lifecycleScope.launch {

            try {

                val response = RetrofitClient.customerChatApi
                    .create(
                        authorization,
                        ChatConversationCreate(
                            item.repairRequestId
                        )
                    )

                val conversationId =
                    response.body()?.conversationId ?: -1

                if (
                    response.isSuccessful &&
                    conversationId > 0
                ) {

                    // Dùng lại màn chat THẬT của đồng đội.
                    startActivity(
                        Intent(
                            this@ManagerChatListActivity,
                            CustomerChatActivity::class.java
                        ).apply {
                            putExtra(
                                "CONVERSATION_ID",
                                conversationId
                            )
                        }
                    )

                } else {

                    val errorText = response.errorBody()
                        ?.string()
                        .orEmpty()

                    val backendMessage = try {
                        org.json.JSONObject(errorText)
                            .optString("message")
                    } catch (_: Exception) {
                        ""
                    }

                    toast(
                        backendMessage.ifBlank {
                            when (response.code()) {
                                401 -> "Vui lòng đăng nhập lại."
                                403 -> "Bạn không có quyền mở hội thoại này."
                                404 -> "Không tìm thấy yêu cầu sửa chữa."
                                else ->
                                    "Không mở được hội thoại: " +
                                            "HTTP ${response.code()}"
                            }
                        }
                    )
                }

            } catch (e: Exception) {

                toast(
                    "Không kết nối được Chat: " +
                            (e.localizedMessage ?: "Lỗi không xác định")
                )

            } finally {

                openingConversation = false

                tvState.text =
                    "Chọn khách hàng để mở hội thoại"
            }
        }
    }

    private fun JsonObject.stringField(
        vararg keys: String
    ): String {

        for (key in keys) {
            val value = entrySet()
                .firstOrNull {
                    it.key.equals(key, ignoreCase = true)
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

    private fun JsonObject.intField(
        key: String
    ): Int {
        return stringField(key).toIntOrNull() ?: -1
    }

    private fun toast(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }

    // ==========================================
    // RECYCLERVIEW - DANH SÁCH KHÁCH HÀNG
    // ==========================================

    private inner class ChatRequestAdapter(
        private val onClick: (ShopChatRequestItem) -> Unit
    ) : RecyclerView.Adapter<ChatRequestAdapter.Holder>() {

        private val items =
            mutableListOf<ShopChatRequestItem>()

        inner class Holder(
            val card: LinearLayout,
            val tvName: TextView,
            val tvRequest: TextView,
            val tvDescription: TextView
        ) : RecyclerView.ViewHolder(card)

        override fun onCreateViewHolder(
            parent: ViewGroup,
            viewType: Int
        ): Holder {

            val card = LinearLayout(parent.context).apply {
                orientation = LinearLayout.VERTICAL
                setPadding(
                    dp(17),
                    dp(15),
                    dp(17),
                    dp(15)
                )
                background = rounded(cream, 17)
            }

            val name = TextView(parent.context).apply {
                textSize = 16f
                setTypeface(null, Typeface.BOLD)
                setTextColor(dark)
            }

            val request = TextView(parent.context).apply {
                textSize = 12f
                setTextColor(Color.parseColor("#A5652B"))
            }

            val description = TextView(parent.context).apply {
                textSize = 13f
                setTextColor(brown)
                maxLines = 2
            }

            card.addView(name)

            card.addView(
                request,
                LinearLayout.LayoutParams(-1, -2).apply {
                    topMargin = dp(6)
                }
            )

            card.addView(
                description,
                LinearLayout.LayoutParams(-1, -2).apply {
                    topMargin = dp(8)
                }
            )

            return Holder(
                card,
                name,
                request,
                description
            )
        }

        override fun onBindViewHolder(
            holder: Holder,
            position: Int
        ) {

            val item = items[position]

            holder.tvName.text = item.customerName

            holder.tvRequest.text =
                "Yêu cầu #${item.repairRequestId} • Nhấn để chat"

            holder.tvDescription.text =
                item.description.ifBlank {
                    "Xem và hỗ trợ khách hàng"
                }

            holder.card.setOnClickListener {
                onClick(item)
            }

            val params =
                RecyclerView.LayoutParams(
                    ViewGroup.LayoutParams.MATCH_PARENT,
                    ViewGroup.LayoutParams.WRAP_CONTENT
                )

            params.bottomMargin = dp(12)

            holder.card.layoutParams = params
        }

        override fun getItemCount(): Int = items.size

        fun submitItems(data: List<ShopChatRequestItem>) {
            items.clear()
            items.addAll(data)
            notifyDataSetChanged()
        }
    }
}
