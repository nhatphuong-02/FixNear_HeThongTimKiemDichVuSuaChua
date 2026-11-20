
package com.example.fixnear.ui.home

import android.content.Intent
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.model.customer.CustomerServiceItem
import com.example.fixnear.data.model.customer.ServiceCategoryItem
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerCatalogBinding
import android.widget.Toast
import com.example.fixnear.ui.profile.ProfileActivity
import kotlinx.coroutines.async
import kotlinx.coroutines.launch
import java.util.Locale

class CustomerCatalogActivity : AppCompatActivity() {

    private lateinit var binding: ActivityCustomerCatalogBinding
    private lateinit var serviceAdapter: CustomerServiceAdapter

    private var categories = emptyList<ServiceCategoryItem>()
    private var services = emptyList<CustomerServiceItem>()

    private var selectedCategoryId: Int? = null
    private var servicesLoaded = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityCustomerCatalogBinding.inflate(layoutInflater)
        setContentView(binding.root)
        setupBottomNavigation()
        selectedCategoryId = intent
            .getIntExtra("CATEGORY_ID", -1)
            .takeIf { it > 0 }


        setupRecyclerView()
        setupSpinner()
        setupSearch()

        binding.btnShowAllServices.setOnClickListener {
            binding.spCategory.setSelection(0)
        }

        loadCatalog()
    }
    private fun setupBottomNavigation() {

        binding.navHome.setOnClickListener {
            val intent = Intent(
                this,
                HomeActivity::class.java
            ).apply {
                flags = Intent.FLAG_ACTIVITY_CLEAR_TOP or
                        Intent.FLAG_ACTIVITY_SINGLE_TOP
            }

            startActivity(intent)
            finish()
        }

        binding.navSearch.setOnClickListener {
            // Đang ở tab Tìm kiếm.
            // Không tạo thêm CustomerCatalogActivity.
            binding.edtSearchService.requestFocus()
        }

        binding.navMessages.setOnClickListener {
            Toast.makeText(
                this,
                "Chức năng tin nhắn đang được phát triển",
                Toast.LENGTH_SHORT
            ).show()
        }

        binding.navAccount.setOnClickListener {
            startActivity(
                Intent(
                    this,
                    ProfileActivity::class.java
                ).apply {
                    flags = Intent.FLAG_ACTIVITY_CLEAR_TOP or
                            Intent.FLAG_ACTIVITY_SINGLE_TOP
                }
            )
            finish()
        }
    }

    // ==================================================
    // RECYCLERVIEW DỊCH VỤ
    // ==================================================

    private fun setupRecyclerView() {

        serviceAdapter = CustomerServiceAdapter { service ->
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

        binding.rvServices.apply {
            layoutManager = LinearLayoutManager(
                this@CustomerCatalogActivity
            )

            adapter = serviceAdapter
            setHasFixedSize(false)
        }
    }

    // ==================================================
    // COMBOBOX DANH MỤC
    // ==================================================

    private fun setupSpinner() {
        updateSpinner(emptyList())

        binding.spCategory.onItemSelectedListener =
            object : AdapterView.OnItemSelectedListener {

                override fun onItemSelected(
                    parent: AdapterView<*>?,
                    view: View?,
                    position: Int,
                    id: Long
                ) {
                    selectedCategoryId =
                        if (position == 0) {
                            null
                        } else {
                            categories.getOrNull(position - 1)
                                ?.serviceCategoryId
                        }

                    updateServiceList()
                }

                override fun onNothingSelected(
                    parent: AdapterView<*>?
                ) = Unit
            }
    }

    private fun updateSpinner(
        data: List<ServiceCategoryItem>
    ) {

        val labels = mutableListOf("Tất cả danh mục")
        labels.addAll(data.map { it.categoryName })

        val adapter = object : ArrayAdapter<String>(
            this,
            android.R.layout.simple_spinner_item,
            labels
        ) {
            private fun dp(value: Int): Int =
                (value * resources.displayMetrics.density).toInt()

            override fun getView(
                position: Int,
                convertView: View?,
                parent: ViewGroup
            ): View {
                return TextView(context).apply {
                    text = labels[position]
                    textSize = 15f
                    setTextColor(Color.parseColor("#FFF4EA"))
                    gravity = Gravity.CENTER_VERTICAL
                    setPadding(dp(12), 0, dp(36), 0)
                    setTypeface(null, Typeface.BOLD)
                    minHeight = dp(52)
                }
            }

            override fun getDropDownView(
                position: Int,
                convertView: View?,
                parent: ViewGroup
            ): View {
                return TextView(context).apply {
                    text = labels[position]
                    textSize = 15f
                    setTextColor(Color.parseColor("#66442F"))
                    gravity = Gravity.CENTER_VERTICAL
                    setPadding(
                        dp(18), dp(10),
                        dp(18), dp(10)
                    )
                    minHeight = dp(48)

                    background = GradientDrawable().apply {
                        setColor(
                            Color.parseColor("#FFF8F1")
                        )
                    }
                }
            }
        }

        binding.spCategory.adapter = adapter

        val selectedPosition =
            categories.indexOfFirst {
                it.serviceCategoryId == selectedCategoryId
            }.let { index ->
                if (index < 0) 0 else index + 1
            }

        binding.spCategory.setSelection(selectedPosition)
    }

    // ==================================================
    // TÌM KIẾM
    // ==================================================

    private fun setupSearch() {

        binding.edtSearchService.setText(
            intent.getStringExtra("SEARCH_QUERY").orEmpty()
        )

        binding.edtSearchService.addTextChangedListener(
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
                    updateServiceList()
                }

                override fun afterTextChanged(
                    s: Editable?
                ) = Unit
            }
        )
    }

    // ==================================================
    // GET DANH MỤC VÀ DỊCH VỤ
    // ==================================================

    private fun loadCatalog() {

        binding.tvCategoryState.visibility = View.VISIBLE
        binding.tvCategoryState.text = "Đang tải danh mục..."

        binding.tvServiceState.visibility = View.VISIBLE
        binding.tvServiceState.text = "Đang tải dịch vụ..."

        lifecycleScope.launch {

            val categoryRequest = async {
                runCatching {
                    RetrofitClient.customerCatalogApi
                        .getCategories()
                }
            }

            val serviceRequest = async {
                runCatching {
                    RetrofitClient.customerCatalogApi
                        .getServices()
                }
            }

            categoryRequest.await()
                .onSuccess { response ->

                    if (response.isSuccessful) {
                        categories = response.body().orEmpty()

                        updateSpinner(categories)

                        binding.tvCategoryState.visibility =
                            if (categories.isEmpty()) {
                                View.VISIBLE
                            } else {
                                View.GONE
                            }

                        binding.tvCategoryState.text =
                            "Chưa có danh mục dịch vụ"
                    } else {
                        binding.tvCategoryState.text =
                            "Lỗi tải danh mục: HTTP ${response.code()}"
                    }
                }
                .onFailure { error ->
                    binding.tvCategoryState.text =
                        "Không tải được danh mục: ${error.localizedMessage}"
                }

            serviceRequest.await()
                .onSuccess { response ->

                    if (response.isSuccessful) {
                        services = response.body().orEmpty()
                        servicesLoaded = true
                        updateServiceList()
                    } else {
                        binding.tvServiceState.text =
                            "Lỗi tải dịch vụ: HTTP ${response.code()}"
                    }
                }
                .onFailure { error ->
                    binding.tvServiceState.text =
                        "Không tải được dịch vụ: ${error.localizedMessage}"
                }
        }
    }

    // ==================================================
    // LỌC DỊCH VỤ THEO DANH MỤC / TÊN
    // ==================================================

    private fun updateServiceList() {

        if (!servicesLoaded ||
            !::serviceAdapter.isInitialized
        ) {
            return
        }

        val selectedName = categories.firstOrNull {
            it.serviceCategoryId == selectedCategoryId
        }?.categoryName

        binding.tvServiceTitle.text =
            if (selectedName == null) {
                "Tất cả dịch vụ"
            } else {
                "Dịch vụ: $selectedName"
            }

        val keyword = binding.edtSearchService.text
            .toString()
            .trim()
            .lowercase(Locale.ROOT)

        val filtered = services.filter { service ->

            val categoryMatches =
                selectedCategoryId == null ||
                        service.categoryId == selectedCategoryId

            val keywordMatches =
                keyword.isBlank() ||
                        service.serviceName.lowercase(
                            Locale.ROOT
                        ).contains(keyword) ||
                        service.categoryName.lowercase(
                            Locale.ROOT
                        ).contains(keyword) ||
                        service.description.orEmpty().lowercase(
                            Locale.ROOT
                        ).contains(keyword)

            categoryMatches && keywordMatches
        }

        serviceAdapter.submitItems(filtered)

        if (filtered.isEmpty()) {
            binding.tvServiceState.visibility = View.VISIBLE

            binding.tvServiceState.text =
                if (services.isEmpty()) {
                    "Hệ thống chưa có dịch vụ"
                } else {
                    "Không tìm thấy dịch vụ phù hợp"
                }
        } else {
            binding.tvServiceState.visibility = View.GONE
        }
    }
}
