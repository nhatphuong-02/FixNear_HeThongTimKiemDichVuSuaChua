
package com.example.fixnear.ui.manager

import android.content.Intent
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.activity.OnBackPressedCallback
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.ShopListResponse
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityManagerHomeBinding
import com.example.fixnear.ui.auth.LoginActivity
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import kotlinx.coroutines.launch
import android.text.Editable
import android.text.TextWatcher
import android.widget.EditText
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.model.ManagerRepairItem

class ManagerHomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityManagerHomeBinding
    private lateinit var sessionManager: SessionManager

    private var shops: List<ShopListResponse> = emptyList()
    private var currentTab = TAB_HOME

    private var isLoadingShops = false
    private var isLoadingSummary = false
    private var loadedShopId = -1

    private var hasResumedOnce = false

    private var repairRequests = emptyList<JsonObject>()
    private var latestRequestId = -1

    private var newCountView: TextView? = null
    private var processingCountView: TextView? = null

    private lateinit var searchAdapter: ManagerRepairAdapter

    private lateinit var searchInput: EditText

    private lateinit var searchState: TextView

    companion object {
        private const val TAB_HOME = 0
        private const val TAB_SEARCH = 2
        private const val TAB_PROFILE = 3

        private val ACTIVE = Color.parseColor("#D8975B")
        private val INACTIVE = Color.parseColor("#C7B9AF")
        private val DARK = Color.parseColor("#251A14")
        private val BROWN = Color.parseColor("#66442F")
        private val CREAM = Color.parseColor("#FFF8F1")
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        sessionManager = SessionManager(this)

        if (
            !sessionManager.isLoggedIn() ||
            sessionManager.getRoleType() != 2
        ) {
            goToLogin()
            return
        }

        binding = ActivityManagerHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupUser()
        setupNavigation()
        setupActions()
        setupHeaderActions()
        setupSearchPage()

        // Lưu đúng hai TextView đếm số ngay từ đầu.
        newCountView = findCountView(binding.cardNewRequests)
        processingCountView = findCountView(binding.cardProcessing)

        showTab(TAB_HOME)

        // Chưa có dữ liệu thật thì không hiện số giả.
        updateNotificationBadge(0)

        loadShops()

        onBackPressedDispatcher.addCallback(
            this,
            object : OnBackPressedCallback(true) {
                override fun handleOnBackPressed() {
                    if (currentTab != TAB_HOME) {
                        showTab(TAB_HOME)
                    } else {
                        moveTaskToBack(true)
                    }
                }
            }
        )
    }

    override fun onResume() {
        super.onResume()

        if (!::binding.isInitialized) return

        if (!hasResumedOnce) {
            hasResumedOnce = true
            return
        }

        // Tải lại khi quay về từ màn duyệt/phân công.
        if (loadedShopId > 0) {
            loadRepairSummary(loadedShopId)
        }
    }

    private fun setupSearchPage() {

        val page = binding.pageSearch as ViewGroup
        page.removeAllViews()

        fun dp(value: Int): Int =
            (value * resources.displayMetrics.density).toInt()

        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(20), dp(24), dp(20), dp(12))
            setBackgroundColor(Color.parseColor("#151210"))
        }

        root.addView(
            TextView(this).apply {
                text = "Tìm kiếm"
                textSize = 25f
                setTextColor(Color.parseColor("#FFF4EA"))
                setTypeface(null, Typeface.BOLD)
            }
        )

        root.addView(
            TextView(this).apply {
                text = "Tìm yêu cầu sửa chữa của cửa hàng"
                textSize = 13f
                setTextColor(Color.parseColor("#C7B9AF"))
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(8)
            }
        )

        searchInput = EditText(this).apply {
            hint = "Mã yêu cầu, tên khách hoặc mô tả..."
            textSize = 14f
            isSingleLine = true

            setTextColor(Color.parseColor("#FFF4EA"))
            setHintTextColor(Color.parseColor("#B9A79B"))

            setPadding(dp(14), 0, dp(14), 0)

            background = GradientDrawable().apply {
                setColor(Color.parseColor("#302925"))
                cornerRadius = dp(13).toFloat()
                setStroke(
                    dp(1),
                    Color.parseColor("#D8975B")
                )
            }
        }

        root.addView(
            searchInput,
            LinearLayout.LayoutParams(-1, dp(54)).apply {
                topMargin = dp(22)
            }
        )

        val toolbar = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = android.view.Gravity.CENTER_VERTICAL
        }

        toolbar.addView(
            TextView(this).apply {
                text = "Kết quả tìm kiếm"
                textSize = 17f
                setTypeface(null, Typeface.BOLD)
                setTextColor(Color.parseColor("#FFF4EA"))
            },
            LinearLayout.LayoutParams(0, -2, 1f)
        )

        val refresh = TextView(this).apply {
            text = "Làm mới"
            textSize = 13f
            setTextColor(Color.parseColor("#D8975B"))
            setPadding(dp(8), dp(12), dp(8), dp(12))
            isClickable = true
            isFocusable = true

            setOnClickListener {
                if (loadedShopId > 0) {
                    loadRepairSummary(loadedShopId)
                }
            }
        }

        toolbar.addView(refresh)

        root.addView(
            toolbar,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(24)
            }
        )

        searchState = TextView(this).apply {
            text = "Đang chờ dữ liệu từ cửa hàng..."
            textSize = 13f
            setTextColor(Color.parseColor("#C7B9AF"))
            setPadding(0, dp(12), 0, dp(12))
        }

        root.addView(searchState)

        searchAdapter = ManagerRepairAdapter { item ->
            openRepairDetail(item.repairRequestId)
        }

        val recyclerView = androidx.recyclerview.widget.RecyclerView(this).apply {
            layoutManager = LinearLayoutManager(
                this@ManagerHomeActivity
            )

            adapter = searchAdapter
            clipToPadding = false
            setPadding(0, dp(8), 0, dp(12))
        }

        root.addView(
            recyclerView,
            LinearLayout.LayoutParams(-1, 0, 1f)
        )

        page.addView(
            root,
            ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
        )

        searchInput.addTextChangedListener(
            object : TextWatcher {
                override fun beforeTextChanged(
                    s: CharSequence?,
                    start: Int,
                    count: Int,
                    after: Int
                ) = Unit

                override fun onTextChanged(
                    s: CharSequence?,
                    start: Int,
                    before: Int,
                    count: Int
                ) {
                    applySearchFilter()
                }

                override fun afterTextChanged(
                    s: Editable?
                ) = Unit
            }
        )
    }

    private fun applySearchFilter() {

        if (!::searchAdapter.isInitialized) return

        val keyword = searchInput.text
            .toString()
            .trim()
            .lowercase()

        val results = repairRequests.filter { item ->

            val id = item.intValue(
                "repairRequestId",
                "id"
            )

            val description = item.textValue(
                "description",
                "issueDescription"
            )

            val customerName = item.textValue(
                "customerName",
                "customerFullName"
            )

            val customerId = item.textValue("customerId")

            val status = item.intValue(
                "status",
                "repairStatus"
            )

            val searchableText = listOf(
                id.toString(),
                "#$id",
                description,
                customerName,
                customerId,
                statusLabel(status)
            ).joinToString(" ").lowercase()

            keyword.isBlank() ||
                    searchableText.contains(keyword)
        }

        val items = results.mapNotNull { data ->

            val id = data.intValue(
                "repairRequestId",
                "id"
            )

            if (id <= 0) return@mapNotNull null

            val status = data.intValue(
                "status",
                "repairStatus"
            )

            val customerName = data.textValue(
                "customerName",
                "customerFullName"
            ).ifBlank {
                val customerId = data.textValue("customerId")

                if (customerId.isNotBlank()) {
                    "Khách hàng #$customerId"
                } else {
                    "Chưa có tên khách hàng"
                }
            }

            ManagerRepairItem(
                repairRequestId = id,
                customerName = customerName,
                description = data.textValue(
                    "description",
                    "issueDescription"
                ).ifBlank {
                    "Chưa có mô tả"
                },
                statusCode = status.toString(),
                statusLabel = statusLabel(status),
                createdAt = data.textValue(
                    "createdAt",
                    "requestedAt"
                ).replace("T", " ")
                    .substringBefore(".")
                    .take(19)
            )
        }

        searchAdapter.submitItems(items)

        searchState.text = when {
            repairRequests.isEmpty() ->
                "Cửa hàng chưa có yêu cầu sửa chữa."

            items.isEmpty() ->
                "Không tìm thấy yêu cầu phù hợp."

            else ->
                "Tìm thấy ${items.size} yêu cầu"
        }
    }

    private fun dp(value: Int): Int {
        return (value * resources.displayMetrics.density).toInt()
    }

    private fun setupUser() {
        val name = sessionManager.getFullName()
            ?.takeIf { it.isNotBlank() }
            ?: "Quản lý"

        binding.tvGreeting.text = "Xin chào, $name!"
        binding.tvProfileName.text = name
    }

    private fun setupNavigation() {
        binding.navHome.setOnClickListener {
            showTab(TAB_HOME)
            refreshHome()
        }

        binding.navSchedule.setOnClickListener {
            openManagerSchedules()
        }

        binding.navSearch.setOnClickListener {
            showTab(TAB_SEARCH)
        }

        binding.navProfile.setOnClickListener {
            showTab(TAB_PROFILE)
        }
    }

    private fun showTab(tab: Int) {
        currentTab = tab

        binding.pageHome.visibility =
            if (tab == TAB_HOME) View.VISIBLE else View.GONE

        // Lịch phân công mở bằng Activity riêng.
        binding.pageSchedule.visibility = View.GONE

        binding.pageSearch.visibility =
            if (tab == TAB_SEARCH) View.VISIBLE else View.GONE

        binding.pageProfile.visibility =
            if (tab == TAB_PROFILE) View.VISIBLE else View.GONE

        setTabColor(
            binding.iconHome,
            binding.labelHome,
            tab == TAB_HOME
        )

        setTabColor(
            binding.iconSchedule,
            binding.labelSchedule,
            false
        )

        setTabColor(
            binding.iconSearch,
            binding.labelSearch,
            tab == TAB_SEARCH
        )

        setTabColor(
            binding.iconProfile,
            binding.labelProfile,
            tab == TAB_PROFILE
        )
    }

    private fun setTabColor(
        icon: TextView,
        label: TextView,
        selected: Boolean
    ) {
        val color = if (selected) ACTIVE else INACTIVE

        icon.setTextColor(color)
        label.setTextColor(color)

        label.setTypeface(
            null,
            if (selected) Typeface.BOLD else Typeface.NORMAL
        )
    }

    private fun setupActions() {

        binding.cardShop.setOnClickListener {
            openShopProfile()
        }

        binding.btnOpenShopProfile.setOnClickListener {
            openShopProfile()
        }

        binding.cardNewRequests.setOnClickListener {
            openRepairRequests("pending")
        }

        binding.cardProcessing.setOnClickListener {
            openRepairRequests("processing")
        }

        binding.cardToday.setOnClickListener {
            openManagerSchedules()
        }

        binding.btnSeeAll.setOnClickListener {
            openRepairRequests("all")
        }

        binding.btnManageMembers.setOnClickListener {
            openShopMembers()
        }

        binding.btnManageServices.setOnClickListener {
            openShopServices()
        }

        binding.btnLogout.setOnClickListener {
            sessionManager.clearSession()
            goToLogin()
        }
    }

    // ============================================
    // CHUÔNG THÔNG BÁO
    // ============================================

    private fun setupHeaderActions() {
        binding.layoutNotification.setOnClickListener {
            if (loadedShopId <= 0) {
                showMessage("Chưa tải được cửa hàng")
                return@setOnClickListener
            }

            showNotificationDialog()
        }
    }

    private fun updateNotificationBadge(count: Int) {
        binding.tvNotificationBadge.visibility =
            if (count > 0) View.VISIBLE else View.GONE

        binding.tvNotificationBadge.text =
            if (count > 99) "99+" else count.toString()
    }

    // Dùng dữ liệu yêu cầu Pending từ API thật.
    // Chưa phải hệ thống thông báo riêng của backend.
    private fun showNotificationDialog() {

        val pendingItems = repairRequests
            .filter {
                it.intValue("status", "repairStatus") == 1
            }
            .sortedWith(
                compareByDescending<JsonObject> {
                    it.textValue("createdAt", "requestedAt")
                }.thenByDescending {
                    it.intValue("repairRequestId", "id")
                }
            )

        val container = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(20), dp(22), dp(20), dp(18))

            background = GradientDrawable().apply {
                setColor(CREAM)
                cornerRadius = dp(20).toFloat()
            }
        }

        container.addView(
            TextView(this).apply {
                text = "Thông báo yêu cầu mới"
                textSize = 20f
                setTextColor(DARK)
                setTypeface(null, Typeface.BOLD)
            }
        )

        container.addView(
            TextView(this).apply {
                text = "Shop #$loadedShopId • " +
                        "${pendingItems.size} yêu cầu chờ tiếp nhận"

                textSize = 13f
                setTextColor(BROWN)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(6)
            }
        )

        val dialog = AlertDialog.Builder(this)
            .setView(container)
            .setNegativeButton("Đóng", null)
            .setNeutralButton("Xem tất cả", null)
            .create()

        if (pendingItems.isEmpty()) {
            container.addView(
                TextView(this).apply {
                    text = "Không có yêu cầu mới cần tiếp nhận."
                    textSize = 14f
                    setTextColor(BROWN)
                },
                LinearLayout.LayoutParams(-1, -2).apply {
                    topMargin = dp(24)
                    bottomMargin = dp(12)
                }
            )
        } else {
            // Hiển thị tối đa 5 yêu cầu gần nhất.
            pendingItems.take(5).forEach { item ->

                val id = item.intValue(
                    "repairRequestId",
                    "id"
                )

                val description = item.textValue(
                    "description",
                    "issueDescription"
                )

                val card = LinearLayout(this).apply {
                    orientation = LinearLayout.VERTICAL
                    setPadding(
                        dp(14),
                        dp(12),
                        dp(14),
                        dp(12)
                    )

                    background = GradientDrawable().apply {
                        setColor(Color.WHITE)
                        cornerRadius = dp(12).toFloat()
                    }

                    isClickable = id > 0
                    isFocusable = id > 0
                }

                card.addView(
                    TextView(this).apply {
                        text = "Yêu cầu #$id"
                        textSize = 15f
                        setTypeface(null, Typeface.BOLD)
                        setTextColor(DARK)
                    }
                )

                card.addView(
                    TextView(this).apply {
                        text = description.ifBlank {
                            "Yêu cầu đang chờ tiếp nhận"
                        }.take(130)

                        textSize = 13f
                        setTextColor(BROWN)
                    },
                    LinearLayout.LayoutParams(-1, -2).apply {
                        topMargin = dp(6)
                    }
                )

                card.setOnClickListener {
                    dialog.dismiss()
                    openRepairDetail(id)
                }

                container.addView(
                    card,
                    LinearLayout.LayoutParams(-1, -2).apply {
                        topMargin = dp(12)
                    }
                )
            }
        }

        dialog.setOnShowListener {
            dialog.window?.apply {
                setBackgroundDrawableResource(
                    android.R.color.transparent
                )

                setLayout(
                    (resources.displayMetrics.widthPixels * 0.9f).toInt(),
                    android.view.WindowManager.LayoutParams.WRAP_CONTENT
                )
            }

            dialog.getButton(
                AlertDialog.BUTTON_NEGATIVE
            ).setTextColor(BROWN)

            dialog.getButton(
                AlertDialog.BUTTON_NEUTRAL
            ).apply {
                setTextColor(ACTIVE)

                setOnClickListener {
                    dialog.dismiss()
                    openRepairRequests("pending")
                }
            }
        }

        dialog.show()
    }

    // ============================================
    // TOKEN
    // ============================================

    private fun authorization(): String? {
        val raw = sessionManager.getToken()
            ?.trim()
            .orEmpty()

        if (raw.isBlank()) return null

        return if (raw.startsWith("Bearer ", true)) {
            raw
        } else {
            "Bearer $raw"
        }
    }

    // ============================================
    // GET /api/shops/me
    // ============================================

    private fun loadShops() {
        if (isLoadingShops) return

        val authorization = authorization() ?: run {
            binding.tvShopName.text = "Vui lòng đăng nhập lại"
            binding.tvShopStatus.text = "Không có phiên đăng nhập"
            return
        }

        isLoadingShops = true

        binding.tvShopName.text = "Đang tải cửa hàng..."
        binding.tvShopStatus.text = "Đang kết nối backend"

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.shopApi.getMyShops(
                        authorization
                    )

                if (!response.isSuccessful) {
                    shops = emptyList()
                    loadedShopId = -1

                    binding.tvShopName.text =
                        "Không tải được cửa hàng"

                    binding.tvShopStatus.text =
                        "HTTP ${response.code()}"

                    return@launch
                }

                shops = response.body()?.data.orEmpty()

                val shop = shops.firstOrNull()

                if (shop == null) {
                    loadedShopId = -1

                    binding.tvShopName.text =
                        "Chưa có cửa hàng"

                    binding.tvShopStatus.text =
                        "Tài khoản chưa có Shop khả dụng"

                    binding.tvProfileShop.text =
                        "Cửa hàng: Chưa có"

                    return@launch
                }

                binding.tvShopName.text = shop.shopName

                binding.tvShopStatus.text =
                    if (shop.isVerified) {
                        "Đã xác minh"
                    } else {
                        "Chưa xác minh"
                    }

                binding.tvProfileShop.text =
                    "Cửa hàng: ${shop.shopName}"

                loadedShopId = shop.shopId

                loadRepairSummary(shop.shopId)

            } catch (e: Exception) {
                shops = emptyList()
                loadedShopId = -1

                binding.tvShopName.text =
                    "Không thể kết nối backend"

                binding.tvShopStatus.text =
                    e.localizedMessage ?: "Lỗi không xác định"
            } finally {
                isLoadingShops = false
            }
        }
    }

    // ============================================
    // GET YÊU CẦU THẬT
    // ============================================

    private fun loadRepairSummary(shopId: Int) {
        if (shopId <= 0 || isLoadingSummary) return

        val authorization = authorization() ?: return

        isLoadingSummary = true

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerRepairApi
                        .getRepairRequests(
                            token = authorization,
                            shopId = shopId
                        )

                if (!response.isSuccessful) {
                    showSummaryUnavailable()

                    showMessage(
                        "Không tải được yêu cầu: HTTP ${response.code()}"
                    )

                    return@launch
                }

                val items = extractRequests(response.body())

                if (items == null) {
                    showSummaryUnavailable()
                    showMessage("Dữ liệu yêu cầu không hợp lệ")
                    return@launch
                }

                repairRequests = items

                applySearchFilter()

                val pendingCount = items.count {
                    it.intValue("status", "repairStatus") == 1
                }

                val processingCount = items.count {
                    it.intValue("status", "repairStatus") in
                            setOf(2, 4, 5, 6, 7, 8)
                }

                updateCountView(
                    newCountView,
                    pendingCount
                )

                updateCountView(
                    processingCountView,
                    processingCount
                )

                // Badge là số yêu cầu Pending thật.
                updateNotificationBadge(pendingCount)

                updateRecentActivity(items)

            } catch (e: Exception) {
                showSummaryUnavailable()

                showMessage(
                    "Lỗi tải thống kê: " +
                            (e.localizedMessage ?: "Không xác định")
                )
            } finally {
                isLoadingSummary = false
            }
        }
    }

    private fun extractRequests(
        body: JsonElement?
    ): List<JsonObject>? {
        if (body == null || body.isJsonNull) return null

        val array = when {
            body.isJsonArray -> body.asJsonArray

            body.isJsonObject -> {
                val data = body.asJsonObject.get("data")

                when {
                    data?.isJsonArray == true -> {
                        data.asJsonArray
                    }

                    data?.isJsonObject == true -> {
                        val nested = data.asJsonObject

                        val list = nested.get("items")
                            ?: nested.get("results")
                            ?: nested.get("repairRequests")

                        if (list?.isJsonArray == true) {
                            list.asJsonArray
                        } else {
                            return null
                        }
                    }

                    else -> return null
                }
            }

            else -> return null
        }

        return array.mapNotNull { element ->
            if (element.isJsonObject) {
                element.asJsonObject
            } else {
                null
            }
        }
    }

    private fun JsonObject.textValue(
        vararg keys: String
    ): String {
        for (key in keys) {
            val value = entrySet()
                .firstOrNull { entry ->
                    entry.key.equals(
                        key,
                        ignoreCase = true
                    )
                }
                ?.value

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

    private fun JsonObject.intValue(
        vararg keys: String
    ): Int {
        return textValue(*keys).toIntOrNull() ?: -1
    }

    private fun statusLabel(status: Int): String {
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
            else -> "Chưa rõ trạng thái"
        }
    }

    // ============================================
    // TÌM TEXTVIEW HIỂN THỊ SỐ ĐẾM
    // ============================================

    private fun findCountView(card: View): TextView? {
        return collectTextViews(card).firstOrNull { tv ->
            val text = tv.text.toString().trim()

            text == "—" ||
                    text == "–" ||
                    text == "-"
        }
    }

    private fun updateCountView(
        textView: TextView?,
        value: Int
    ) {
        textView ?: return

        textView.text = value.toString()
        textView.setTextColor(ACTIVE)
        textView.setTypeface(null, Typeface.BOLD)
    }

    private fun collectTextViews(
        parent: View
    ): List<TextView> {
        val result = mutableListOf<TextView>()

        fun visit(view: View) {
            if (view is TextView) {
                result.add(view)
            }

            if (view is ViewGroup) {
                for (i in 0 until view.childCount) {
                    visit(view.getChildAt(i))
                }
            }
        }

        visit(parent)
        return result
    }

    // ============================================
    // HOẠT ĐỘNG GẦN ĐÂY
    // ============================================

    private fun updateRecentActivity(
        items: List<JsonObject>
    ) {
        val allViews = collectTextViews(binding.pageHome)

        val title = allViews.firstOrNull {
            it.tag == "fixnear_recent_title" ||
                    it.text.toString().contains(
                        "Chưa có dữ liệu hoạt động",
                        ignoreCase = true
                    ) ||
                    it.text.toString().contains(
                        "Chưa có yêu cầu sửa chữa",
                        ignoreCase = true
                    )
        }

        val subtitle = allViews.firstOrNull {
            it.tag == "fixnear_recent_subtitle" ||
                    it.text.toString().contains(
                        "Danh sách yêu cầu sẽ",
                        ignoreCase = true
                    ) ||
                    it.text.toString().contains(
                        "Các yêu cầu mới sẽ",
                        ignoreCase = true
                    )
        }

        if (items.isEmpty()) {
            latestRequestId = -1

            title?.apply {
                text = "Chưa có yêu cầu sửa chữa"
                tag = "fixnear_recent_title"
            }

            subtitle?.apply {
                text = "Các yêu cầu mới sẽ xuất hiện ở đây."
                tag = "fixnear_recent_subtitle"
            }

            return
        }

        val latest = items.maxWithOrNull(
            compareBy<JsonObject> {
                it.textValue("createdAt", "requestedAt")
            }.thenBy {
                it.intValue("repairRequestId", "id")
            }
        ) ?: return

        val id = latest.intValue("repairRequestId", "id")
        latestRequestId = id

        val status = latest.intValue(
            "status",
            "repairStatus"
        )

        val description = latest.textValue(
            "description",
            "issueDescription"
        )

        title?.apply {
            text = "Yêu cầu #$id • ${statusLabel(status)}"
            tag = "fixnear_recent_title"
        }

        subtitle?.apply {
            text = description.ifBlank {
                "Nhấn để xem chi tiết yêu cầu."
            }.take(110)

            tag = "fixnear_recent_subtitle"
        }

        val recentCard = title?.parent as? ViewGroup

        recentCard?.setOnClickListener {
            openRepairDetail(id)
        }
    }

    private fun showSummaryUnavailable() {
        repairRequests = emptyList()
        latestRequestId = -1

        newCountView?.text = "—"
        processingCountView?.text = "—"

        updateNotificationBadge(0)
    }

    private fun refreshHome() {
        val shop = shops.firstOrNull()

        if (shop == null) {
            loadShops()
        } else {
            loadRepairSummary(shop.shopId)
        }
    }

    // ============================================
    // ĐIỀU HƯỚNG
    // ============================================

    private fun openRepairRequests(initialFilter: String) {
        val shop = shops.firstOrNull()

        if (shop == null) {
            showMessage("Chưa tải được cửa hàng")
            return
        }

        startActivity(
            Intent(
                this,
                ManagerRepairRequestsActivity::class.java
            ).apply {
                putExtra("SHOP_ID", shop.shopId)
                putExtra("SHOP_ROLE", shop.currentUserRole)
                putExtra("INITIAL_FILTER", initialFilter)
            }
        )
    }

    private fun openRepairDetail(repairRequestId: Int) {
        val shop = shops.firstOrNull()

        if (shop == null || repairRequestId <= 0) return

        startActivity(
            Intent(
                this,
                ManagerRepairDetailActivity::class.java
            ).apply {
                putExtra("SHOP_ID", shop.shopId)
                putExtra("REPAIR_REQUEST_ID", repairRequestId)
            }
        )
    }

    private fun openManagerSchedules() {
        val shop = shops.firstOrNull()

        if (shop == null) {
            showMessage("Chưa xác định được cửa hàng")
            return
        }

        startActivity(
            Intent(
                this,
                ManagerSchedulesActivity::class.java
            ).apply {
                putExtra("SHOP_ID", shop.shopId)
                putExtra("SHOP_ROLE", shop.currentUserRole)
            }
        )
    }

    private fun openShopProfile() {
        val shop = shops.firstOrNull()

        if (shop == null) {
            showMessage("Chưa có Shop để xem hồ sơ")
            return
        }

        startActivity(
            Intent(
                this,
                ShopProfileActivity::class.java
            ).apply {
                putExtra("SHOP_ID", shop.shopId)
            }
        )
    }

    private fun openShopMembers() {
        val shop = shops.firstOrNull()

        if (shop == null) {
            showMessage("Chưa tải được cửa hàng")
            return
        }

        startActivity(
            Intent(
                this,
                ShopMembersActivity::class.java
            ).apply {
                putExtra("SHOP_ID", shop.shopId)
                putExtra("SHOP_ROLE", shop.currentUserRole)
            }
        )
    }

    private fun openShopServices() {
        val shop = shops.firstOrNull()

        if (shop == null) {
            showMessage("Chưa tải được cửa hàng")
            return
        }

        startActivity(
            Intent(
                this,
                ShopServicesActivity::class.java
            ).apply {
                putExtra("SHOP_ID", shop.shopId)
                putExtra("SHOP_ROLE", shop.currentUserRole)
                putExtra("MANAGER_ID", -1)
            }
        )
    }

    private fun goToLogin() {
        startActivity(
            Intent(
                this,
                LoginActivity::class.java
            ).apply {
                flags =
                    Intent.FLAG_ACTIVITY_NEW_TASK or
                            Intent.FLAG_ACTIVITY_CLEAR_TASK
            }
        )

        finish()
    }

    private fun showMessage(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_SHORT
        ).show()
    }
}
