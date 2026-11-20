package com.example.fixnear.ui.manager
import android.content.Intent
import android.content.res.ColorStateList
import android.graphics.Color
import android.os.Bundle
import android.view.View
import android.widget.TextView
import android.widget.Toast
import androidx.activity.OnBackPressedCallback
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.ShopListResponse
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityManagerHomeBinding
import com.example.fixnear.ui.auth.LoginActivity
import kotlinx.coroutines.launch

class ManagerHomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityManagerHomeBinding
    private lateinit var sessionManager: SessionManager

    private var shops: List<ShopListResponse> = emptyList()
    private var currentTab = TAB_HOME

    companion object {
        private const val TAB_HOME = 0
        private const val TAB_SCHEDULE = 1
        private const val TAB_SEARCH = 2
        private const val TAB_PROFILE = 3

        private val ACTIVE = Color.parseColor("#D8975B")
        private val INACTIVE = Color.parseColor("#C7B9AF")
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        sessionManager = SessionManager(this)

        if (!sessionManager.isLoggedIn() ||
            sessionManager.getRoleType() != 2) {
            goToLogin()
            return
        }

        binding = ActivityManagerHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setupUser()
        setupNavigation()
        setupActions()

        showTab(TAB_HOME)
        loadShops()

        onBackPressedDispatcher.addCallback(
            this,
            object : OnBackPressedCallback(true) {
                override fun handleOnBackPressed() {
                    if (currentTab != TAB_HOME) {
                        showTab(TAB_HOME)
                    } else {
                        // Ở Trang chủ, trở về Home của thiết bị
                        moveTaskToBack(true)
                    }
                }
            }
        )
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
        }

        binding.navSchedule.setOnClickListener {
            showTab(TAB_SCHEDULE)
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

        binding.pageSchedule.visibility =
            if (tab == TAB_SCHEDULE) View.VISIBLE else View.GONE

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
            tab == TAB_SCHEDULE
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
            if (selected)
                android.graphics.Typeface.BOLD
            else
                android.graphics.Typeface.NORMAL
        )
    }

    private fun setupActions() {

        // Nhấn thẻ Shop để xem hồ sơ từ backend
        binding.cardShop.setOnClickListener {
            openShopProfile()
        }

        binding.btnOpenShopProfile.setOnClickListener {
            openShopProfile()
        }

        // Các thẻ thống kê chờ API tổng hợp
        binding.cardNewRequests.setOnClickListener {
            showMessage(
                "Sẽ mở danh sách yêu cầu mới"
            )
        }

        binding.cardProcessing.setOnClickListener {
            showMessage(
                "Sẽ mở các yêu cầu đang xử lý"
            )
        }

        binding.cardToday.setOnClickListener {
            showTab(TAB_SCHEDULE)
        }

        binding.btnSeeAll.setOnClickListener {
            showMessage(
                "Sẽ mở toàn bộ yêu cầu sửa chữa"
            )
        }

        binding.btnLogout.setOnClickListener {
            sessionManager.clearSession()
            goToLogin()
        }
        binding.btnManageMembers.setOnClickListener {
            openShopMembers()
        }
        binding.btnManageServices.setOnClickListener {
            openShopServices()
        }
    }
    private fun openShopMembers() {
        if (shops.isEmpty()) {
            showMessage("Chưa tải được cửa hàng")
            return
        }

        val shop = shops.first()

        val intent = Intent(
            this,
            ShopMembersActivity::class.java
        ).apply {
            putExtra("SHOP_ID", shop.shopId)
            putExtra("SHOP_ROLE", shop.currentUserRole)
        }

        startActivity(intent)
    }
    private fun openShopServices() {
        val shop = shops.firstOrNull()

        val intent = Intent(
            this,
            ShopServicesActivity::class.java
        ).apply {
            putExtra("SHOP_ID", shop?.shopId ?: -1)
            putExtra("SHOP_ROLE", shop?.currentUserRole ?: 0)
            putExtra("MANAGER_ID", -1)
        }

        startActivity(intent)
    }

    private fun loadShops() {
        val token = sessionManager.getToken()

        if (token.isNullOrBlank()) {
            binding.tvShopName.text =
                "Vui lòng đăng nhập lại"
            binding.tvShopStatus.text =
                "Không có phiên đăng nhập"
            return
        }

        binding.tvShopName.text = "Đang tải cửa hàng..."
        binding.tvShopStatus.text = "Đang kết nối backend"

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.shopApi.getMyShops(
                        "Bearer $token"
                    )

                if (response.isSuccessful) {
                    shops = response.body()?.data.orEmpty()

                    when {
                        shops.isEmpty() -> {
                            binding.tvShopName.text =
                                "Chưa có cửa hàng"

                            binding.tvShopStatus.text =
                                "Tài khoản chưa có Shop khả dụng"

                            binding.tvProfileShop.text =
                                "Cửa hàng: Chưa có"
                        }

                        else -> {
                            val shop = shops.first()

                            binding.tvShopName.text =
                                shop.shopName

                            // isVerified là xác minh,
                            // không phải trạng thái hoạt động.
                            binding.tvShopStatus.text =
                                if (shop.isVerified)
                                    "Đã xác minh"
                                else
                                    "Chưa xác minh"

                            binding.tvProfileShop.text =
                                "Cửa hàng: ${shop.shopName}"

                            if (shops.size > 1) {
                                binding.tvShopStatus.append(
                                    " • ${shops.size} cửa hàng"
                                )
                            }
                        }
                    }

                } else {
                    shops = emptyList()

                    binding.tvShopName.text =
                        "Không tải được cửa hàng"

                    binding.tvShopStatus.text =
                        "Lỗi HTTP ${response.code()}"

                    binding.tvProfileShop.text =
                        "Cửa hàng: Chưa tải được"
                }

            } catch (e: Exception) {
                shops = emptyList()

                binding.tvShopName.text =
                    "Không thể kết nối backend"

                binding.tvShopStatus.text =
                    e.localizedMessage
                        ?: "Lỗi không xác định"

                binding.tvProfileShop.text =
                    "Cửa hàng: Chưa tải được"
            }
        }
    }

    private fun openShopProfile() {
        if (shops.isEmpty()) {
            showMessage("Chưa có Shop để xem hồ sơ")
            return
        }

        // Tạm mở Shop đầu tiên từ API.
        // Sau sẽ bổ sung chọn Shop nếu tài khoản có nhiều Shop.
        val shopId = shops.first().shopId

        val intent = Intent(
            this,
            ShopProfileActivity::class.java
        )

        intent.putExtra("SHOP_ID", shopId)
        startActivity(intent)
    }

    private fun goToLogin() {
        val intent = Intent(
            this,
            LoginActivity::class.java
        )

        intent.flags =
            Intent.FLAG_ACTIVITY_NEW_TASK or
                    Intent.FLAG_ACTIVITY_CLEAR_TASK

        startActivity(intent)
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
