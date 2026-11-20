
package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.customer.CustomerServiceItem
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityHomeBinding
import com.example.fixnear.ui.profile.ProfileActivity
import kotlinx.coroutines.launch

class HomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityHomeBinding
    private lateinit var sessionManager: SessionManager
    private lateinit var categoryBannerAdapter: HomeCategoryBannerAdapter

    private var allServices = emptyList<CustomerServiceItem>()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        categoryBannerAdapter = HomeCategoryBannerAdapter { categoryId ->
            openCatalog(categoryId)
        }

        binding.rvCategories.apply {
            layoutManager = LinearLayoutManager(
                this@HomeActivity,
                LinearLayoutManager.HORIZONTAL,
                false
            )
            adapter = categoryBannerAdapter
            isNestedScrollingEnabled = false
        }

        setupNavigation()
        setupGreeting()
        loadCategories()
        loadServicesForPicker()
        loadCustomerProfile()
    }

    private fun setupNavigation() {
        binding.navHome.setOnClickListener {
            binding.homeScroll.smoothScrollTo(0, 0)
        }

        binding.navSearch.setOnClickListener {
            openCatalog(
                searchQuery = binding.edtSearchService.text.toString()
            )
        }

        binding.edtSearchService.setOnClickListener {
            openCatalog()
        }

        binding.edtSearchService.setOnEditorActionListener {
                _, _, _ ->
            openCatalog(
                searchQuery = binding.edtSearchService.text.toString()
            )
            true
        }

        binding.btnExploreServices.setOnClickListener {
            openCatalog()
        }

        binding.btnAllCategories.setOnClickListener {
            openCatalog()
        }

        binding.btnFindNearby.setOnClickListener {
            showServicePicker()
        }

        binding.navAccount.setOnClickListener {
            openProfile()
        }

        binding.tvProfileAvatar.setOnClickListener {
            openProfile()
        }

        binding.navMessages.setOnClickListener {
            Toast.makeText(
                this,
                "Chức năng tin nhắn đang phát triển",
                Toast.LENGTH_SHORT
            ).show()
        }
    }

    private fun openCatalog(
        categoryId: Int? = null,
        searchQuery: String = ""
    ) {
        startActivity(
            Intent(this, CustomerCatalogActivity::class.java).apply {
                putExtra("CATEGORY_ID", categoryId ?: -1)
                putExtra("SEARCH_QUERY", searchQuery)
            }
        )
    }

    private fun openProfile() {
        startActivity(
            Intent(this, ProfileActivity::class.java)
        )
    }

    private fun setupGreeting() {
        val name = sessionManager.getFullName()

        binding.tvGreeting.text =
            if (name.isNullOrBlank()) {
                "Xin chào!"
            } else {
                "Xin chào, $name!"
            }
    }

    private fun loadCategories() {
        binding.tvCategoryState.visibility = View.VISIBLE
        binding.tvCategoryState.text = "Đang tải danh mục..."

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerCatalogApi.getCategories()

                if (response.isSuccessful) {
                    val categories = response.body().orEmpty()

                    categoryBannerAdapter.submitCategories(categories)

                    binding.tvCategoryState.visibility =
                        if (categories.isEmpty())
                            View.VISIBLE else View.GONE

                    binding.tvCategoryState.text =
                        "Chưa có danh mục trên hệ thống"
                } else {
                    binding.tvCategoryState.text =
                        "Lỗi HTTP ${response.code()}"
                }
            } catch (e: Exception) {
                binding.tvCategoryState.text =
                    "Không tải được danh mục: ${e.localizedMessage}"
            }
        }
    }

    private fun loadServicesForPicker() {
        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerCatalogApi.getServices()

                if (response.isSuccessful) {
                    allServices = response.body().orEmpty()
                }
            } catch (_: Exception) {
                allServices = emptyList()
            }
        }
    }

    private fun showServicePicker() {
        if (allServices.isEmpty()) {
            Toast.makeText(
                this,
                "Chưa tải được danh sách dịch vụ",
                Toast.LENGTH_SHORT
            ).show()
            return
        }

        val names = allServices.map {
            "${it.serviceName} • ${it.categoryName}"
        }.toTypedArray()

        val dialog = AlertDialog.Builder(this)
            .setTitle("Chọn dịch vụ cần sửa")
            .setItems(names) { _, index ->
                val service = allServices[index]

                startActivity(
                    Intent(
                        this,
                        NearbyShopsActivity::class.java
                    ).apply {
                        putExtra("SERVICE_ID", service.serviceId)
                        putExtra("SERVICE_NAME", service.serviceName)
                    }
                )
            }
            .setNegativeButton("Hủy", null)
            .create()

        dialog.show()
        dialog.window?.setBackgroundDrawableResource(
            com.example.fixnear.R.drawable.fixnear_dialog_bg
        )
        dialog.getButton(AlertDialog.BUTTON_NEGATIVE)
            ?.setTextColor(0xFFA5652B.toInt())
    }

    private fun loadCustomerProfile() {
        val rawToken = sessionManager.getToken()

        if (rawToken.isNullOrBlank()) {
            binding.tvAddress.text = "Chưa có thông tin địa chỉ"
            return
        }

        val token =
            if (rawToken.startsWith("Bearer ", ignoreCase = true))
                rawToken
            else
                "Bearer $rawToken"

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.customerApi.getProfile(token)

                if (response.isSuccessful) {
                    response.body()?.let { profile ->
                        binding.tvGreeting.text =
                            "Xin chào, ${profile.fullName}!"

                        binding.tvAddress.text =
                            profile.addressLine
                                ?.takeIf { it.isNotBlank() }
                                ?: "Chưa thiết lập địa chỉ"
                    }
                } else {
                    binding.tvAddress.text =
                        "Chưa tải được địa chỉ"
                }
            } catch (_: Exception) {
                binding.tvAddress.text =
                    "Không kết nối được hồ sơ khách hàng"
            }
        }
    }

    override fun onResume() {
        super.onResume()

        if (::sessionManager.isInitialized) {
            setupGreeting()
            loadCustomerProfile()
        }
    }
}
