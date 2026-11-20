package com.example.fixnear.ui.manager

import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.AddShopServiceRequest
import com.example.fixnear.data.model.AddServicePriceRequest
import com.example.fixnear.data.remote.RetrofitClient
import kotlinx.coroutines.launch
import org.json.JSONObject
import android.app.Dialog
import android.graphics.Typeface
import android.os.Bundle
import android.text.InputType
import android.view.Gravity
import android.view.View
import android.view.WindowManager
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.AppCompatButton
import com.example.fixnear.R
import com.example.fixnear.databinding.ActivityShopServicesBinding

class ShopServicesActivity : AppCompatActivity() {

    private lateinit var binding: ActivityShopServicesBinding

    private var shopId = -1
    private var shopRole = 0

    private var managerId = -1
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
        managerId = intent.getIntExtra("MANAGER_ID", -1)
        shopRole = intent.getIntExtra("SHOP_ROLE", 0)

        binding.tvShopInfo.text =
            if (shopId > 0) {
                "Cửa hàng #$shopId"
            } else {
                "Chế độ xem trước • Chưa tải được cửa hàng"
            }

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
            showAddServicePreview()
        }

        binding.btnAddPrice.setOnClickListener {
            showAddPricePreview()
        }

        showServices()
    }

    // =====================================================
    // CHUYỂN TAB
    // =====================================================

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
            if (selected) Typeface.BOLD else Typeface.NORMAL
        )
    }

    // =====================================================
    // CÁC HÀM DÙNG CHUNG CHO POPUP
    // =====================================================

    private fun dp(value: Int): Int {
        return (value * resources.displayMetrics.density).toInt()
    }

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
        val titleView = TextView(this).apply {
            text = title
            textSize = 21f
            setTextColor(dark)
            setTypeface(null, Typeface.BOLD)
        }

        container.addView(titleView)

        val subtitleView = TextView(this).apply {
            text = subtitle
            textSize = 13f
            setTextColor(muted)
        }

        container.addView(
            subtitleView,
            LinearLayout.LayoutParams(
                -1,
                -2
            ).apply {
                topMargin = dp(7)
                bottomMargin = dp(8)
            }
        )
    }

    private fun addInput(
        container: LinearLayout,
        label: String,
        hint: String,
        numeric: Boolean = false,
        decimal: Boolean = false
    ): EditText {

        val labelView = TextView(this).apply {
            text = label
            textSize = 13f
            setTextColor(brown)
            setTypeface(null, Typeface.BOLD)
        }

        container.addView(
            labelView,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(15)
            }
        )

        val field = EditText(this).apply {
            this.hint = hint
            textSize = 14f
            setSingleLine(true)

            inputType = when {
                decimal ->
                    InputType.TYPE_CLASS_NUMBER or
                            InputType.TYPE_NUMBER_FLAG_DECIMAL

                numeric -> InputType.TYPE_CLASS_NUMBER

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
            LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                dp(50)
            ).apply {
                topMargin = dp(7)
            }
        )

        return field
    }

    private fun addNote(container: LinearLayout) {
        val note = TextView(this).apply {
            text = "Bản xem trước • Chưa lưu lên hệ thống"
            textSize = 12f
            setTextColor(muted)
        }

        container.addView(
            note,
            LinearLayout.LayoutParams(-1, -2).apply {
                topMargin = dp(18)
                bottomMargin = dp(8)
            }
        )
    }

    private fun addActionButtons(
        container: LinearLayout,
        dialog: Dialog,
        saveLabel: String,
        onSave: () -> Unit
    ) {
        val row = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
        }

        val btnCancel = AppCompatButton(this).apply {
            text = "Hủy"
            isAllCaps = false
            textSize = 14f
            setTextColor(brown)
            setBackgroundResource(R.drawable.fixnear_manager_cream_card)
            backgroundTintList = null
            elevation = 0f
        }

        val btnSave = AppCompatButton(this).apply {
            text = saveLabel
            isAllCaps = false
            textSize = 14f
            setTextColor(0xFFFFFFFF.toInt())
            setBackgroundResource(R.drawable.fixnear_register_button)
            backgroundTintList = null
            elevation = 0f
        }

        row.addView(
            btnCancel,
            LinearLayout.LayoutParams(
                0,
                dp(52),
                1f
            )
        )

        row.addView(
            btnSave,
            LinearLayout.LayoutParams(
                0,
                dp(52),
                1f
            ).apply {
                marginStart = dp(10)
            }
        )

        container.addView(row)

        btnCancel.setOnClickListener {
            dialog.dismiss()
        }

        btnSave.setOnClickListener {
            onSave()
        }
    }

    private fun showStyledDialog(
        container: LinearLayout,
        maxHeightFraction: Float = 0.82f
    ): Dialog {

        val scrollView = ScrollView(this).apply {
            isFillViewport = false
            clipToPadding = false
            addView(container)
        }

        val dialog = Dialog(this)
        dialog.setContentView(scrollView)

        dialog.window?.setBackgroundDrawableResource(
            android.R.color.transparent
        )

        dialog.show()

        val width =
            (resources.displayMetrics.widthPixels * 0.90f).toInt()

        val maxHeight =
            (resources.displayMetrics.heightPixels *
                    maxHeightFraction).toInt()

        // Đo chiều cao thực tế để popup ngắn không bị kéo dài.
        scrollView.measure(
            View.MeasureSpec.makeMeasureSpec(
                width,
                View.MeasureSpec.EXACTLY
            ),
            View.MeasureSpec.makeMeasureSpec(
                maxHeight,
                View.MeasureSpec.AT_MOST
            )
        )

        val height = scrollView.measuredHeight
            .coerceAtMost(maxHeight)

        dialog.window?.setLayout(width, height)

        dialog.window?.setSoftInputMode(
            WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE
        )

        return dialog
    }

    private fun showNotConnected() {
        Toast.makeText(
            this,
            "Dữ liệu hợp lệ. Chức năng lưu đang chờ kết nối API.",
            Toast.LENGTH_LONG
        ).show()
    }

    // =====================================================
    // POPUP THÊM DỊCH VỤ
    // =====================================================

    private fun showAddServicePreview() {

        val container = createDialogContent()
        val dialog = Dialog(this)

        addTitle(
            container,
            "Thêm dịch vụ",
            "Thêm dịch vụ có sẵn trong danh mục hệ thống"
        )

        val serviceId = addInput(
            container,
            "Mã dịch vụ",
            "Nhập ServiceId",
            numeric = true
        )

        val description = addInput(
            container,
            "Mô tả (không bắt buộc)",
            "Nhập mô tả dịch vụ"
        )

        addNote(container)

        addActionButtons(
            container = container,
            dialog = dialog,
            saveLabel = "Lưu dịch vụ"
        ) {

            val id = serviceId.text
                .toString()
                .trim()
                .toIntOrNull()

            if (id == null || id <= 0) {
                serviceId.error = "Mã dịch vụ phải lớn hơn 0"
                serviceId.requestFocus()
                return@addActionButtons
            }

            // Description để sẵn cho request POST T04.
            val descriptionValue =
                description.text.toString().trim()

            // Chưa gọi POST vì đang làm giao diện.
            saveShopService(
                serviceId = id,
                description = descriptionValue.ifBlank { null }
            )
        }

        val scrollView = ScrollView(this).apply {
            addView(container)
        }

        dialog.setContentView(scrollView)
        dialog.show()

        val width =
            (resources.displayMetrics.widthPixels * 0.90f).toInt()

        dialog.window?.apply {
            setBackgroundDrawableResource(
                android.R.color.transparent
            )

            setLayout(
                width,
                WindowManager.LayoutParams.WRAP_CONTENT
            )

            setSoftInputMode(
                WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE
            )
        }
    }

    // =====================================================
    // POPUP THÊM GIÁ DỊCH VỤ
    // =====================================================

    private fun showAddPricePreview() {

        val container = createDialogContent()
        val dialog = Dialog(this)

        addTitle(
            container,
            "Thêm giá dịch vụ",
            "Thiết lập giá cho dịch vụ của cửa hàng"
        )

        val shopServiceId = addInput(
            container,
            "Dịch vụ cửa hàng",
            "Nhập ShopServiceId",
            numeric = true
        )

        val price = addInput(
            container,
            "Giá dịch vụ (VNĐ)",
            "Nhập giá",
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

        addNote(container)

        addActionButtons(
            container = container,
            dialog = dialog,
            saveLabel = "Lưu giá"
        ) {

            val selectedShopServiceId =
                shopServiceId.text.toString()
                    .trim().toIntOrNull()

            val enteredPrice =
                price.text.toString()
                    .trim().toDoubleOrNull()

            val minText =
                minPrice.text.toString().trim()

            val maxText =
                maxPrice.text.toString().trim()

            val min =
                minText.toDoubleOrNull()

            val max =
                maxText.toDoubleOrNull()

            val unit =
                priceUnit.text.toString().trim()

            if (
                selectedShopServiceId == null ||
                selectedShopServiceId <= 0
            ) {
                shopServiceId.error =
                    "Vui lòng nhập mã dịch vụ hợp lệ"

                shopServiceId.requestFocus()
                return@addActionButtons
            }

            if (
                enteredPrice == null ||
                enteredPrice <= 0
            ) {
                price.error = "Giá phải lớn hơn 0"
                price.requestFocus()
                return@addActionButtons
            }

            if (minText.isNotEmpty() &&
                (min == null || min < 0)
            ) {
                minPrice.error = "Giá tối thiểu không hợp lệ"
                minPrice.requestFocus()
                return@addActionButtons
            }

            if (maxText.isNotEmpty() &&
                (max == null || max < 0)
            ) {
                maxPrice.error = "Giá tối đa không hợp lệ"
                maxPrice.requestFocus()
                return@addActionButtons
            }

            if (min != null &&
                max != null &&
                min > max
            ) {
                maxPrice.error =
                    "Giá tối đa phải lớn hơn hoặc bằng giá tối thiểu"

                maxPrice.requestFocus()
                return@addActionButtons
            }

            if (min != null && enteredPrice < min) {
                price.error = "Giá thấp hơn mức tối thiểu"
                price.requestFocus()
                return@addActionButtons
            }

            if (max != null && enteredPrice > max) {
                price.error = "Giá vượt mức tối đa"
                price.requestFocus()
                return@addActionButtons
            }

            if (unit.isBlank()) {
                priceUnit.error = "Nhập đơn vị tính"
                priceUnit.requestFocus()
                return@addActionButtons
            }

            // Các giá trị đã sẵn sàng để gửi API T05.
            // Chưa gọi POST nên không thông báo lưu thành công.
            saveServicePrice(
                shopServiceId = selectedShopServiceId,
                price = enteredPrice,
                minPrice = min,
                maxPrice = max,
                priceUnit = unit
            )
        }

        val scrollView = ScrollView(this).apply {
            isFillViewport = false
            addView(container)
        }

        dialog.setContentView(scrollView)
        dialog.show()

        val screenWidth =
            resources.displayMetrics.widthPixels

        val screenHeight =
            resources.displayMetrics.heightPixels

        dialog.window?.apply {
            setBackgroundDrawableResource(
                android.R.color.transparent
            )

            setLayout(
                (screenWidth * 0.90f).toInt(),
                (screenHeight * 0.82f).toInt()
            )

            setSoftInputMode(
                WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE
            )
        }
    }
    private fun saveShopService(
        serviceId: Int,
        description: String?
    ) {
        val token = SessionManager(this).getToken()

        if (shopId <= 0 || managerId <= 0 || token.isNullOrBlank()) {
            showSaveError(
                "Chưa đủ thông tin Shop/Manager hoặc phiên đăng nhập."
            )
            return
        }

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerServiceApi.addShopService(
                        token = "Bearer $token",
                        shopId = shopId,
                        managerId = managerId,
                        request = AddShopServiceRequest(
                            serviceId = serviceId,
                            description = description
                        )
                    )

                if (response.isSuccessful) {
                    Toast.makeText(
                        this@ShopServicesActivity,
                        "Thêm dịch vụ thành công",
                        Toast.LENGTH_LONG
                    ).show()

                    // Sau này gọi lại API danh sách T04 ở đây.
                } else {
                    showSaveError(
                        readApiError(
                            response.errorBody()?.string(),
                            response.code()
                        )
                    )
                }

            } catch (e: Exception) {
                showSaveError(
                    "Lỗi kết nối: ${e.localizedMessage}"
                )
            }
        }
    }

    private fun saveServicePrice(
        shopServiceId: Int,
        price: Double,
        minPrice: Double?,
        maxPrice: Double?,
        priceUnit: String?
    ) {
        val token = SessionManager(this).getToken()

        if (shopId <= 0 || managerId <= 0 || token.isNullOrBlank()) {
            showSaveError(
                "Chưa đủ thông tin Shop/Manager hoặc phiên đăng nhập."
            )
            return
        }

        lifecycleScope.launch {
            try {
                val response =
                    RetrofitClient.managerServiceApi.addServicePrice(
                        token = "Bearer $token",
                        managerId = managerId,
                        shopId = shopId,
                        request = AddServicePriceRequest(
                            shopServiceId = shopServiceId,
                            price = price,
                            minPrice = minPrice,
                            maxPrice = maxPrice,
                            priceUnit = priceUnit
                        )
                    )

                if (response.isSuccessful) {
                    Toast.makeText(
                        this@ShopServicesActivity,
                        "Thêm giá dịch vụ thành công",
                        Toast.LENGTH_LONG
                    ).show()
                } else {
                    showSaveError(
                        readApiError(
                            response.errorBody()?.string(),
                            response.code()
                        )
                    )
                }

            } catch (e: Exception) {
                showSaveError(
                    "Lỗi kết nối: ${e.localizedMessage}"
                )
            }
        }
    }

    private fun readApiError(
        body: String?,
        code: Int
    ): String {
        return try {
            JSONObject(body ?: "{}")
                .optString("message")
                .ifBlank { "Lỗi HTTP $code" }
        } catch (_: Exception) {
            "Lỗi HTTP $code"
        }
    }

    private fun showSaveError(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_LONG
        ).show()
    }
}
