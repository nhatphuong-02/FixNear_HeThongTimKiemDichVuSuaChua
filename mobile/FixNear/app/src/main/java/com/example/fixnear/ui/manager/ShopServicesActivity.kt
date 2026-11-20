
package com.example.fixnear.ui.manager

import android.app.Dialog
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.os.Bundle
import android.text.InputType
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.view.WindowManager
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.AppCompatButton
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.R
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.AddServicePriceRequest
import com.example.fixnear.data.model.AddShopServiceRequest
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityShopServicesBinding
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import kotlinx.coroutines.launch
import java.text.NumberFormat
import java.util.Locale

class ShopServicesActivity : AppCompatActivity() {

    private lateinit var binding: ActivityShopServicesBinding

    private var shopId = -1
    private var shopRole = 0
    private var managerId = -1

    private var token = ""
    private var loading = false

    private lateinit var serviceList: LinearLayout
    private lateinit var priceList: LinearLayout

    private var serviceEmpty: View? = null
    private var priceEmpty: View? = null

    private val caramel = 0xFFD8975B.toInt()
    private val cream = 0xFFFFF8F1.toInt()
    private val brown = 0xFF66442F.toInt()
    private val dark = 0xFF251A14.toInt()
    private val muted = 0xFF8B7160.toInt()
    private val inactive = 0xFFC7B9AF.toInt()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityShopServicesBinding.inflate(layoutInflater)
        setContentView(binding.root)

        shopId = intent.getIntExtra("SHOP_ID", -1)
        shopRole = intent.getIntExtra("SHOP_ROLE", 0)

        token = SessionManager(this).getToken().orEmpty()

        binding.btnBack.setOnClickListener {
            finish()
        }

        binding.tabServices.setOnClickListener {
            showServices()
        }

        binding.tabPrices.setOnClickListener {
            showPrices()
        }

        binding.btnAddService.setOnClickListener {
            showAddServiceDialog()
        }

        binding.btnAddPrice.setOnClickListener {
            showAddPriceDialog()
        }

        setupLists()
        showServices()
        loadShopProfile()
    }

    // =====================================================
    // GIAO DIỆN DANH SÁCH
    // =====================================================

    private fun setupLists() {

        serviceList = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            visibility = View.GONE
        }

        priceList = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            visibility = View.GONE
        }

        // Tìm hai khung thông báo trống của XML cũ.
        serviceEmpty = findEmptyContainer(
            binding.sectionServices,
            "Chưa tải danh sách dịch vụ"
        )

        priceEmpty = findEmptyContainer(
            binding.sectionPrices,
            "Chưa tải bảng giá"
        )

        // Chèn danh sách ngay trước nút Thêm.
        val serviceIndex = binding.sectionServices
            .indexOfChild(binding.btnAddService)

        binding.sectionServices.addView(
            serviceList,
            serviceIndex.coerceAtLeast(0),
            LinearLayout.LayoutParams(
                -1,
                -2
            ).apply {
                topMargin = dp(14)
            }
        )

        val priceIndex = binding.sectionPrices
            .indexOfChild(binding.btnAddPrice)

        binding.sectionPrices.addView(
            priceList,
            priceIndex.coerceAtLeast(0),
            LinearLayout.LayoutParams(
                -1,
                -2
            ).apply {
                topMargin = dp(14)
            }
        )
    }

    private fun findEmptyContainer(
        section: LinearLayout,
        message: String
    ): View? {
        for (i in 0 until section.childCount) {
            val child = section.getChildAt(i)

            if (child is ViewGroup &&
                findText(child, message) != null
            ) {
                return child
            }
        }

        return null
    }

    private fun findText(
        view: View,
        target: String
    ): TextView? {

        if (view is TextView &&
            view.text.toString().contains(target)
        ) {
            return view
        }

        if (view is ViewGroup) {
            for (i in 0 until view.childCount) {
                val result = findText(
                    view.getChildAt(i),
                    target
                )

                if (result != null) {
                    return result
                }
            }
        }

        return null
    }

    private fun updateEmptyMessage(
        container: View?,
        message: String
    ) {
        if (container == null) return

        fun findLastText(view: View): TextView? {
            if (view is TextView) return view

            if (view is ViewGroup) {
                for (i in view.childCount - 1 downTo 0) {
                    val result = findLastText(
                        view.getChildAt(i)
                    )

                    if (result != null) return result
                }
            }

            return null
        }

        findLastText(container)?.text = message
    }

    private fun showServices() {
        binding.sectionServices.visibility = View.VISIBLE
        binding.sectionPrices.visibility = View.GONE

        updateTab(binding.tabServices, true)
        updateTab(binding.tabPrices, false)
    }

    private fun showPrices() {
        binding.sectionServices.visibility = View.GONE
        binding.sectionPrices.visibility = View.VISIBLE

        updateTab(binding.tabServices, false)
        updateTab(binding.tabPrices, true)
    }

    private fun updateTab(
        view: TextView,
        selected: Boolean
    ) {
        view.setTextColor(
            if (selected) caramel else inactive
        )

        view.setTypeface(
            null,
            if (selected) Typeface.BOLD
            else Typeface.NORMAL
        )
    }

    private fun showServiceMessage(message: String) {
        serviceList.removeAllViews()
        serviceList.visibility = View.GONE

        serviceEmpty?.visibility = View.VISIBLE
        updateEmptyMessage(serviceEmpty, message)
    }

    private fun showPriceMessage(message: String) {
        priceList.removeAllViews()
        priceList.visibility = View.GONE

        priceEmpty?.visibility = View.VISIBLE
        updateEmptyMessage(priceEmpty, message)
    }

    // =====================================================
    // LẤY SHOP PROFILE VÀ MANAGER ID
    // =====================================================

    private fun loadShopProfile() {

        if (shopId <= 0) {
            binding.tvShopInfo.text = "Chưa có Shop ID"
            showServiceMessage("Chưa xác định cửa hàng.")
            showPriceMessage("Chưa xác định cửa hàng.")
            return
        }

        if (token.isBlank()) {
            showServiceMessage("Phiên đăng nhập không hợp lệ.")
            showPriceMessage("Vui lòng đăng nhập lại.")
            return
        }

        binding.tvShopInfo.text = "Đang tải cửa hàng..."

        showServiceMessage("Đang tải dịch vụ...")
        showPriceMessage("Đang tải bảng giá...")

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerServiceApi.getShopProfile(
                        token = bearerToken(),
                        shopId = shopId
                    )

                if (!response.isSuccessful) {
                    showServiceMessage(
                        "Không tải được hồ sơ Shop: HTTP ${response.code()}"
                    )
                    showPriceMessage(
                        "Không tải được hồ sơ Shop: HTTP ${response.code()}"
                    )
                    return@launch
                }

                val profile = response.body()
                    ?.takeIf { it.isJsonObject }
                    ?.asJsonObject
                    ?.get("data")
                    ?.takeIf { it.isJsonObject }
                    ?.asJsonObject

                if (profile == null) {
                    showServiceMessage("Hồ sơ Shop không hợp lệ.")
                    showPriceMessage("Hồ sơ Shop không hợp lệ.")
                    return@launch
                }

                managerId = getInt(profile, "managerId")

                val shopName = getString(
                    profile,
                    "shopName"
                )

                binding.tvShopInfo.text =
                    if (shopName.isNotBlank()) {
                        shopName
                    } else {
                        "Cửa hàng #$shopId"
                    }

                if (managerId <= 0) {
                    showServiceMessage(
                        "Backend không trả ManagerId hợp lệ."
                    )
                    showPriceMessage(
                        "Backend không trả ManagerId hợp lệ."
                    )
                    return@launch
                }

                loadShopServices()
                loadServicePrices()

            } catch (e: Exception) {
                showServiceMessage(
                    "Lỗi kết nối: ${e.localizedMessage}"
                )
                showPriceMessage(
                    "Lỗi kết nối: ${e.localizedMessage}"
                )
            }
        }
    }

    // =====================================================
    // GET DANH SÁCH DỊCH VỤ
    // =====================================================

    private fun loadShopServices() {

        if (shopId <= 0 || managerId <= 0) {
            showServiceMessage("Thiếu ShopId hoặc ManagerId.")
            return
        }

        showServiceMessage("Đang tải danh sách dịch vụ...")

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerServiceApi.getShopServices(
                        token = bearerToken(),
                        shopId = shopId,
                        managerId = managerId
                    )

                if (!response.isSuccessful) {
                    showServiceMessage(
                        "Không tải được dịch vụ: HTTP ${response.code()}"
                    )
                    return@launch
                }

                val items = extractItems(response.body())

                if (items.isEmpty()) {
                    showServiceMessage(
                        "Cửa hàng chưa có dịch vụ."
                    )
                    return@launch
                }

                serviceEmpty?.visibility = View.GONE
                serviceList.visibility = View.VISIBLE
                serviceList.removeAllViews()

                items.forEach { item ->

                    val name = getString(
                        item,
                        "serviceName"
                    ).ifBlank {
                        "Dịch vụ #${getInt(item, "serviceId")}"
                    }

                    val category = getString(
                        item,
                        "categoryName"
                    )

                    val description = getString(
                        item,
                        "description"
                    )

                    val shopServiceId = getInt(
                        item,
                        "shopServiceId"
                    )

                    val isActive = getBoolean(
                        item,
                        "isActive"
                    )

                    val detail = buildList {
                        if (category.isNotBlank()) {
                            add("Danh mục: $category")
                        }

                        add("Mã dịch vụ Shop: $shopServiceId")

                        if (description.isNotBlank()) {
                            add(description)
                        }

                        add(
                            if (isActive) {
                                "Trạng thái: Đang hoạt động"
                            } else {
                                "Trạng thái: Tạm ngừng"
                            }
                        )
                    }.joinToString("\n")

                    serviceList.addView(
                        createDataCard(name, detail)
                    )
                }

            } catch (e: Exception) {
                showServiceMessage(
                    "Lỗi tải dịch vụ: ${e.localizedMessage}"
                )
            }
        }
    }

    // =====================================================
    // GET BẢNG GIÁ
    // =====================================================

    private fun loadServicePrices() {

        if (shopId <= 0 || managerId <= 0) {
            showPriceMessage("Thiếu ShopId hoặc ManagerId.")
            return
        }

        showPriceMessage("Đang tải bảng giá...")

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerServiceApi.getServicePrices(
                        token = bearerToken(),
                        shopId = shopId,
                        managerId = managerId,
                        includeHistory = false
                    )

                if (!response.isSuccessful) {
                    showPriceMessage(
                        "Không tải được bảng giá: HTTP ${response.code()}"
                    )
                    return@launch
                }

                val items = extractItems(response.body())

                if (items.isEmpty()) {
                    showPriceMessage(
                        "Cửa hàng chưa có bảng giá."
                    )
                    return@launch
                }

                priceEmpty?.visibility = View.GONE
                priceList.visibility = View.VISIBLE
                priceList.removeAllViews()

                items.forEach { item ->

                    val name = getString(
                        item,
                        "serviceName"
                    ).ifBlank {
                        "Dịch vụ #${getInt(item, "shopServiceId")}"
                    }

                    val price = getString(item, "price")
                    val min = getString(item, "minPrice")
                    val max = getString(item, "maxPrice")
                    val unit = getString(item, "priceUnit")

                    val detail = buildList {
                        add("Giá: ${formatMoney(price)} đ")

                        if (min.isNotBlank()) {
                            add("Tối thiểu: ${formatMoney(min)} đ")
                        }

                        if (max.isNotBlank()) {
                            add("Tối đa: ${formatMoney(max)} đ")
                        }

                        if (unit.isNotBlank()) {
                            add("Đơn vị: $unit")
                        }

                        add(
                            "Mã dịch vụ Shop: " +
                                    getInt(item, "shopServiceId")
                        )
                    }.joinToString("\n")

                    priceList.addView(
                        createDataCard(name, detail)
                    )
                }

            } catch (e: Exception) {
                showPriceMessage(
                    "Lỗi tải bảng giá: ${e.localizedMessage}"
                )
            }
        }
    }

    // =====================================================
    // JSON HELPER
    // =====================================================

    private fun extractItems(
        body: JsonElement?
    ): List<JsonObject> {

        if (body == null || !body.isJsonObject) {
            return emptyList()
        }

        val data = body.asJsonObject.get("data")
            ?: return emptyList()

        if (!data.isJsonArray) {
            return emptyList()
        }

        return data.asJsonArray
            .filter { it.isJsonObject }
            .map { it.asJsonObject }
    }

    private fun getString(
        obj: JsonObject,
        name: String
    ): String {
        val value = obj.get(name) ?: return ""

        if (value.isJsonNull || !value.isJsonPrimitive) {
            return ""
        }

        return value.asString
    }

    private fun getInt(
        obj: JsonObject,
        name: String
    ): Int {
        return getString(obj, name).toIntOrNull() ?: -1
    }

    private fun getBoolean(
        obj: JsonObject,
        name: String
    ): Boolean {
        return getString(obj, name).equals(
            "true",
            ignoreCase = true
        )
    }

    private fun formatMoney(value: String): String {
        val number = value.toDoubleOrNull()
            ?: return value

        val formatter = NumberFormat.getNumberInstance(
            Locale("vi", "VN")
        )

        formatter.maximumFractionDigits = 0

        return formatter.format(number)
    }

    private fun bearerToken(): String {
        return if (token.startsWith("Bearer ")) {
            token
        } else {
            "Bearer $token"
        }
    }

    private fun dp(value: Int): Int {
        return (value * resources.displayMetrics.density).toInt()
    }

    private fun createDataCard(
        title: String,
        details: String
    ): LinearLayout {

        val card = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(
                dp(18),
                dp(16),
                dp(18),
                dp(16)
            )

            background = GradientDrawable().apply {
                setColor(cream)
                cornerRadius = dp(16).toFloat()
            }
        }

        val titleView = TextView(this).apply {
            text = title
            textSize = 16f
            setTextColor(dark)
            setTypeface(null, Typeface.BOLD)
        }

        val detailsView = TextView(this).apply {
            text = details
            textSize = 13f
            setTextColor(muted)
            setLineSpacing(dp(4).toFloat(), 1f)
        }

        card.addView(titleView)

        card.addView(
            detailsView,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(9)
            }
        )

        card.layoutParams = LinearLayout.LayoutParams(
            -1,
            -2
        ).apply {
            bottomMargin = dp(12)
        }

        return card
    }

    // =====================================================
    // POPUP DÙNG CHUNG
    // =====================================================

    private fun createDialogContent(): LinearLayout {
        return LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(dp(22), dp(22), dp(22), dp(22))
            setBackgroundResource(R.drawable.fixnear_dialog_bg)
        }
    }

    private fun addTitle(
        container: LinearLayout,
        title: String,
        subtitle: String
    ) {
        container.addView(
            TextView(this).apply {
                text = title
                textSize = 21f
                setTextColor(dark)
                setTypeface(null, Typeface.BOLD)
            }
        )

        container.addView(
            TextView(this).apply {
                text = subtitle
                textSize = 13f
                setTextColor(muted)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(7)
                bottomMargin = dp(8)
            }
        )
    }

    private fun addInput(
        container: LinearLayout,
        label: String,
        hintText: String,
        numeric: Boolean = false,
        decimal: Boolean = false
    ): EditText {

        container.addView(
            TextView(this).apply {
                text = label
                textSize = 13f
                setTextColor(brown)
                setTypeface(null, Typeface.BOLD)
            },
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(15)
            }
        )

        val field = EditText(this).apply {
            hint = hintText
            textSize = 14f
            setSingleLine(true)

            inputType = when {
                decimal ->
                    InputType.TYPE_CLASS_NUMBER or
                            InputType.TYPE_NUMBER_FLAG_DECIMAL

                numeric ->
                    InputType.TYPE_CLASS_NUMBER

                else ->
                    InputType.TYPE_CLASS_TEXT or
                            InputType.TYPE_TEXT_FLAG_CAP_SENTENCES
            }

            setTextColor(cream)
            setHintTextColor(inactive)
            setBackgroundResource(R.drawable.fixnear_auth_input)
            setPadding(dp(14), 0, dp(14), 0)
        }

        container.addView(
            field,
            LinearLayout.LayoutParams(-1, dp(50)).apply {
                topMargin = dp(7)
            }
        )

        return field
    }

    private fun addButtons(
        container: LinearLayout,
        dialog: Dialog,
        saveText: String,
        onSave: () -> Unit
    ) {
        val row = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
        }

        val cancel = AppCompatButton(this).apply {
            text = "Hủy"
            isAllCaps = false
            setTextColor(brown)
            setBackgroundResource(
                R.drawable.fixnear_manager_cream_card
            )
            backgroundTintList = null
        }

        val save = AppCompatButton(this).apply {
            text = saveText
            isAllCaps = false
            setTextColor(0xFFFFFFFF.toInt())
            setBackgroundResource(
                R.drawable.fixnear_register_button
            )
            backgroundTintList = null
        }

        row.addView(
            cancel,
            LinearLayout.LayoutParams(0, dp(52), 1f)
        )

        row.addView(
            save,
            LinearLayout.LayoutParams(0, dp(52), 1f).apply {
                marginStart = dp(10)
            }
        )

        container.addView(
            row,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(18)
            }
        )

        cancel.setOnClickListener {
            dialog.dismiss()
        }

        save.setOnClickListener {
            if (loading) return@setOnClickListener
            onSave()
        }
    }

    private fun showDialog(
        container: LinearLayout,
        dialog: Dialog,
        tall: Boolean = false
    ) {
        val scrollView = ScrollView(this).apply {
            addView(container)
        }

        dialog.setContentView(scrollView)
        dialog.show()

        dialog.window?.apply {
            setBackgroundDrawableResource(
                android.R.color.transparent
            )

            setLayout(
                (resources.displayMetrics.widthPixels * 0.90f).toInt(),
                if (tall) {
                    (resources.displayMetrics.heightPixels * 0.82f).toInt()
                } else {
                    WindowManager.LayoutParams.WRAP_CONTENT
                }
            )

            setSoftInputMode(
                WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE
            )
        }
    }

    // =====================================================
    // THÊM DỊCH VỤ
    // =====================================================

    private fun showAddServiceDialog() {

        val container = createDialogContent()
        val dialog = Dialog(this)

        addTitle(
            container,
            "Thêm dịch vụ",
            "Chọn mã dịch vụ từ danh mục hệ thống"
        )

        val serviceId = addInput(
            container,
            "Mã dịch vụ",
            "Nhập ServiceId",
            numeric = true
        )

        val description = addInput(
            container,
            "Mô tả",
            "Không bắt buộc"
        )

        addButtons(
            container,
            dialog,
            "Lưu dịch vụ"
        ) {
            val id = serviceId.text.toString()
                .trim().toIntOrNull()

            if (id == null || id <= 0) {
                serviceId.error = "Mã dịch vụ không hợp lệ"
                serviceId.requestFocus()
                return@addButtons
            }

            saveShopService(
                serviceId = id,
                description = description.text.toString()
                    .trim().ifBlank { null },
                dialog = dialog
            )
        }

        showDialog(container, dialog)
    }

    // =====================================================
    // THÊM GIÁ
    // =====================================================

    private fun showAddPriceDialog() {

        val container = createDialogContent()
        val dialog = Dialog(this)

        addTitle(
            container,
            "Thêm giá dịch vụ",
            "Thiết lập bảng giá cho cửa hàng"
        )

        val shopServiceId = addInput(
            container,
            "Mã dịch vụ cửa hàng",
            "Nhập ShopServiceId",
            numeric = true
        )

        val price = addInput(
            container,
            "Giá dịch vụ",
            "VNĐ",
            decimal = true
        )

        val minPrice = addInput(
            container,
            "Giá tối thiểu",
            "Không bắt buộc",
            decimal = true
        )

        val maxPrice = addInput(
            container,
            "Giá tối đa",
            "Không bắt buộc",
            decimal = true
        )

        val priceUnit = addInput(
            container,
            "Đơn vị tính",
            "Ví dụ: lần, thiết bị"
        )

        addButtons(
            container,
            dialog,
            "Lưu giá"
        ) {
            val id = shopServiceId.text.toString()
                .trim().toIntOrNull()

            val amount = price.text.toString()
                .trim().toDoubleOrNull()

            val minText = minPrice.text.toString().trim()
            val maxText = maxPrice.text.toString().trim()

            val min = minText.toDoubleOrNull()
            val max = maxText.toDoubleOrNull()

            if (id == null || id <= 0) {
                shopServiceId.error = "Chọn dịch vụ hợp lệ"
                return@addButtons
            }

            if (amount == null || amount <= 0) {
                price.error = "Giá phải lớn hơn 0"
                return@addButtons
            }

            if (minText.isNotEmpty() &&
                (min == null || min < 0)
            ) {
                minPrice.error = "Giá tối thiểu không hợp lệ"
                return@addButtons
            }

            if (maxText.isNotEmpty() &&
                (max == null || max < 0)
            ) {
                maxPrice.error = "Giá tối đa không hợp lệ"
                return@addButtons
            }

            if (min != null && max != null && min > max) {
                maxPrice.error = "Giá tối đa nhỏ hơn giá tối thiểu"
                return@addButtons
            }

            if (min != null && amount < min) {
                price.error = "Giá nhỏ hơn giá tối thiểu"
                return@addButtons
            }

            if (max != null && amount > max) {
                price.error = "Giá lớn hơn giá tối đa"
                return@addButtons
            }

            saveServicePrice(
                shopServiceId = id,
                price = amount,
                minPrice = min,
                maxPrice = max,
                priceUnit = priceUnit.text.toString()
                    .trim().ifBlank { null },
                dialog = dialog
            )
        }

        showDialog(container, dialog, tall = true)
    }

    // =====================================================
    // POST THÊM DỊCH VỤ
    // =====================================================

    private fun saveShopService(
        serviceId: Int,
        description: String?,
        dialog: Dialog
    ) {
        if (!canSave()) return

        loading = true

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerServiceApi.addShopService(
                        token = bearerToken(),
                        shopId = shopId,
                        managerId = managerId,
                        request = AddShopServiceRequest(
                            serviceId = serviceId,
                            description = description
                        )
                    )

                if (response.isSuccessful) {
                    dialog.dismiss()
                    toast("Thêm dịch vụ thành công")
                    loadShopServices()
                } else {
                    toast(
                        "Lỗi thêm dịch vụ: HTTP ${response.code()}"
                    )
                }

            } catch (e: Exception) {
                toast("Lỗi kết nối: ${e.localizedMessage}")
            } finally {
                loading = false
            }
        }
    }

    // =====================================================
    // POST THÊM GIÁ
    // =====================================================

    private fun saveServicePrice(
        shopServiceId: Int,
        price: Double,
        minPrice: Double?,
        maxPrice: Double?,
        priceUnit: String?,
        dialog: Dialog
    ) {
        if (!canSave()) return

        loading = true

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerServiceApi.addServicePrice(
                        token = bearerToken(),
                        shopId = shopId,
                        managerId = managerId,
                        request = AddServicePriceRequest(
                            shopServiceId = shopServiceId,
                            price = price,
                            minPrice = minPrice,
                            maxPrice = maxPrice,
                            priceUnit = priceUnit
                        )
                    )

                if (response.isSuccessful) {
                    dialog.dismiss()
                    toast("Thêm giá thành công")
                    loadServicePrices()
                } else {
                    toast(
                        "Lỗi thêm giá: HTTP ${response.code()}"
                    )
                }

            } catch (e: Exception) {
                toast("Lỗi kết nối: ${e.localizedMessage}")
            } finally {
                loading = false
            }
        }
    }

    private fun canSave(): Boolean {
        if (shopId <= 0 || managerId <= 0) {
            toast("Chưa có ShopId hoặc ManagerId hợp lệ.")
            return false
        }

        if (token.isBlank()) {
            toast("Vui lòng đăng nhập lại.")
            return false
        }

        return true
    }

    private fun toast(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
