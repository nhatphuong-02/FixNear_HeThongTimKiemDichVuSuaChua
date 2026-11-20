
package com.example.fixnear.ui.quote

import android.app.DatePickerDialog
import android.os.Bundle
import android.view.View
import android.view.ViewGroup
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.R
import com.example.fixnear.data.model.technician.ShopRepairItem
import com.example.fixnear.data.model.technician.ShopRepairPart
import com.example.fixnear.data.model.technician.ShopServiceItem
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCreateQuoteBinding
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import org.json.JSONObject
import java.io.IOException
import java.math.BigDecimal
import java.math.RoundingMode
import com.example.fixnear.data.model.technician.CreateQuoteRequest
import com.example.fixnear.data.model.technician.CreateQuoteDetailRequest
import java.text.NumberFormat
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Locale

class CreateQuoteActivity : AppCompatActivity() {

    private lateinit var binding: ActivityCreateQuoteBinding

    private var repairRequestId = -1
    private var shopId = -1
    private var validUntil: String? = null

    private var catalogJob: Job? = null
    private var catalogLoading = false
    private var selectedType = 1

    private val catalogItems = mutableListOf<CatalogOption>()
    private val draftItems = mutableListOf<QuoteDraftItem>()
    private var isSavingQuote = false
    private lateinit var quoteAdapter: QuoteDetailAdapter
    private val quoteItemTypes = listOf(
        QuoteItemTypeOption("Dịch vụ", 1),
        QuoteItemTypeOption("Hạng mục sửa chữa", 2),
        QuoteItemTypeOption("Linh kiện / Vật tư", 3)
    )

    private data class QuoteItemTypeOption(
        val name: String,
        val value: Int
    ) {
        override fun toString(): String = name
    }

    private data class CatalogOption(
        val id: Int,
        val name: String,
        val price: Double?
    ) {
        override fun toString(): String = name
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityCreateQuoteBinding.inflate(
            layoutInflater
        )
        setContentView(binding.root)

        repairRequestId = intent.getIntExtra(
            "repairRequestId", -1
        )

        shopId = intent.getIntExtra(
            "shopId", -1
        )

        setupToolbar()

        if (repairRequestId <= 0 || shopId <= 0) {
            showMessage(
                "Thiếu thông tin yêu cầu sửa chữa hoặc cửa hàng"
            )
            finish()
            return
        }

        setupRequestInformation()
        setupCatalogPlaceholder()
        setupQuoteDetails()
        setupValidUntil()
        setupActions()
        setupQuoteItemTypes()
    }

    // =====================================
    // HEADER
    // =====================================

    private fun setupToolbar() {
        binding.btnBack.setOnClickListener {
            finish()
        }
    }

    private fun setupRequestInformation() {
        binding.tvQuoteRepairRequestId.text =
            "Mã yêu cầu: #$repairRequestId"

        binding.tvQuoteShopId.text =
            "Mã cửa hàng: #$shopId"
    }

    // =====================================
    // SPINNER LOAI HANG MUC
    // =====================================

    private fun setupQuoteItemTypes() {
        val adapter = createSpinnerAdapter(
            quoteItemTypes
        )

        binding.spinnerQuoteItemType.adapter = adapter

        binding.spinnerQuoteItemType.onItemSelectedListener =
            object : AdapterView.OnItemSelectedListener {

                override fun onItemSelected(
                    parent: AdapterView<*>?,
                    view: View?,
                    position: Int,
                    id: Long
                ) {
                    selectedType = quoteItemTypes[position].value
                    loadCatalog(selectedType)
                }

                override fun onNothingSelected(
                    parent: AdapterView<*>?
                ) {
                    // Khong tai danh muc khi chua co lua chon
                }
            }

        binding.spinnerQuoteItemType.setSelection(0)
    }

    // =====================================
    // SPINNER DESIGN FIXNEAR
    // =====================================

    private fun <T> createSpinnerAdapter(
        data: List<T>
    ): ArrayAdapter<T> {

        return object : ArrayAdapter<T>(
            this,
            android.R.layout.simple_spinner_item,
            data
        ) {
            override fun getView(
                position: Int,
                convertView: View?,
                parent: ViewGroup
            ): View {
                val textView =
                    (convertView as? TextView)
                        ?: TextView(this@CreateQuoteActivity)

                textView.text = getItem(position).toString()
                textView.textSize = 14f

                textView.setTextColor(
                    ContextCompat.getColor(
                        this@CreateQuoteActivity,
                        R.color.input_text
                    )
                )

                textView.setPadding(
                    dp(12), dp(12), dp(12), dp(12)
                )

                textView.setSingleLine(true)
                textView.ellipsize =
                    android.text.TextUtils.TruncateAt.END

                return textView
            }

            override fun getDropDownView(
                position: Int,
                convertView: View?,
                parent: ViewGroup
            ): View {
                val textView =
                    (convertView as? TextView)
                        ?: TextView(this@CreateQuoteActivity)

                textView.text = getItem(position).toString()
                textView.textSize = 14f

                textView.setTextColor(
                    ContextCompat.getColor(
                        this@CreateQuoteActivity,
                        R.color.text_primary_card
                    )
                )

                textView.setPadding(
                    dp(16), dp(14), dp(16), dp(14)
                )

                return textView
            }
        }
    }

    // =====================================
    // CATALOG INITIAL STATE
    // =====================================

    private fun setupCatalogPlaceholder() {
        setCatalogPlaceholder(
            "Đang chuẩn bị danh mục..."
        )

        binding.spinnerQuoteCatalog.isEnabled = false

        binding.layoutQuoteCatalogLoading.visibility =
            View.GONE

        binding.layoutQuoteCatalogError.visibility =
            View.GONE
    }

    private fun setCatalogPlaceholder(message: String) {
        binding.spinnerQuoteCatalog.adapter =
            createSpinnerAdapter(listOf(message))

        binding.spinnerQuoteCatalog.isEnabled = false
    }

    // =====================================
    // LOAD CATALOG FROM BACKEND
    // =====================================

    private fun loadCatalog(type: Int) {
        catalogJob?.cancel()

        catalogItems.clear()
        catalogLoading = true

        showCatalogLoading()

        catalogJob = lifecycleScope.launch {
            try {
                val result = when (type) {
                    1 -> loadServices()
                    2 -> loadRepairItems()
                    3 -> loadRepairParts()
                    else -> emptyList()
                }

                catalogItems.clear()
                catalogItems.addAll(result)

                if (result.isEmpty()) {
                    showCatalogEmpty()
                } else {
                    showCatalogSuccess()
                }

            } catch (e: CancellationException) {
                throw e

            } catch (e: ApiError) {
                showCatalogError(e.message ?: "Lỗi API")

            } catch (e: IOException) {
                showCatalogError(
                    "Không thể kết nối Backend. " +
                            "Vui lòng kiểm tra mạng."
                )

            } catch (e: Exception) {
                showCatalogError(
                    e.message ?: "Không thể tải danh mục."
                )

            } finally {
                catalogLoading = false
            }
        }
    }

    // =====================================
    // LOAD SERVICES
    // =====================================

    private suspend fun loadServices(): List<CatalogOption> {
        val result = mutableListOf<CatalogOption>()

        var page = 1
        var totalPages: Int

        do {
            val response =
                RetrofitClient.technicianApi.getShopServices(
                    shopId = shopId,
                    page = page,
                    pageSize = 50
                )

            if (!response.isSuccessful) {
                throw createApiError(
                    response.code(),
                    response.errorBody()?.string()
                )
            }

            val body = response.body()
                ?: throw ApiError("Backend không trả dữ liệu dịch vụ.")

            result.addAll(
                body.data.map { service: ShopServiceItem ->
                    CatalogOption(
                        id = service.serviceId,
                        name = service.serviceName,
                        price = service.currentPrice?.price
                    )
                }
            )

            totalPages = body.totalPages
            if (body.data.isEmpty()) break
            page++

        } while (page <= totalPages)

        return result
    }

    // =====================================
    // LOAD REPAIR ITEMS
    // =====================================

    private suspend fun loadRepairItems(): List<CatalogOption> {
        val result = mutableListOf<CatalogOption>()

        var page = 1
        var totalPages: Int

        do {
            val response =
                RetrofitClient.technicianApi.getRepairItems(
                    shopId = shopId,
                    page = page,
                    pageSize = 50
                )

            if (!response.isSuccessful) {
                throw createApiError(
                    response.code(),
                    response.errorBody()?.string()
                )
            }

            val body = response.body()
                ?: throw ApiError("Backend không trả dữ liệu hạng mục.")

            result.addAll(
                body.data.filter { it.isActive }
                    .map { item: ShopRepairItem ->
                        CatalogOption(
                            id = item.repairItemId,
                            name = item.itemName,
                            price = item.price
                        )
                    }
            )

            totalPages = body.totalPages
            if (body.data.isEmpty()) break
            page++

        } while (page <= totalPages)

        return result
    }

    // =====================================
    // LOAD REPAIR PARTS
    // =====================================

    private suspend fun loadRepairParts(): List<CatalogOption> {
        val result = mutableListOf<CatalogOption>()

        var page = 1
        var totalPages: Int

        do {
            val response =
                RetrofitClient.technicianApi.getRepairParts(
                    shopId = shopId,
                    page = page,
                    pageSize = 50
                )

            if (!response.isSuccessful) {
                throw createApiError(
                    response.code(),
                    response.errorBody()?.string()
                )
            }

            val body = response.body()
                ?: throw ApiError("Backend không trả dữ liệu linh kiện.")

            result.addAll(
                body.data.filter { it.isActive }
                    .map { part: ShopRepairPart ->
                        CatalogOption(
                            id = part.repairPartId,
                            name = part.partName,
                            price = part.price
                        )
                    }
            )

            totalPages = body.totalPages
            if (body.data.isEmpty()) break
            page++

        } while (page <= totalPages)

        return result
    }

    // =====================================
    // LOADING / EMPTY / ERROR / SUCCESS
    // =====================================

    private fun showCatalogLoading() {
        binding.layoutQuoteCatalogLoading.visibility =
            View.VISIBLE

        binding.layoutQuoteCatalogError.visibility =
            View.GONE

        setCatalogPlaceholder("Đang tải dữ liệu...")
        binding.btnAddQuoteItem.isEnabled = false
    }

    private fun showCatalogEmpty() {
        binding.layoutQuoteCatalogLoading.visibility =
            View.GONE

        binding.layoutQuoteCatalogError.visibility =
            View.GONE

        setCatalogPlaceholder("Không có danh mục khả dụng")
        binding.btnAddQuoteItem.isEnabled = false
    }

    private fun showCatalogError(message: String) {
        binding.layoutQuoteCatalogLoading.visibility =
            View.GONE

        binding.layoutQuoteCatalogError.visibility =
            View.VISIBLE

        binding.tvQuoteCatalogError.text = message

        setCatalogPlaceholder("Không thể tải danh mục")
        binding.btnAddQuoteItem.isEnabled = false
    }

    private fun showCatalogSuccess() {
        binding.layoutQuoteCatalogLoading.visibility =
            View.GONE

        binding.layoutQuoteCatalogError.visibility =
            View.GONE

        val displayItems = catalogItems.map { item ->
            val priceText = item.price?.let {
                "${formatMoney(it)} đ"
            } ?: "Giá theo báo giá"

            "${item.name} • $priceText"
        }

        binding.spinnerQuoteCatalog.adapter =
            createSpinnerAdapter(displayItems)

        binding.spinnerQuoteCatalog.isEnabled = true

        binding.btnAddQuoteItem.isEnabled = true
    }

    // =====================================
    // QUOTE DETAILS
    // =====================================


    private fun setupQuoteDetails() {

        quoteAdapter = QuoteDetailAdapter { item ->
            if (!isSavingQuote) {
                draftItems.remove(item)
                renderDraftItems()
            }
        }

        binding.rvQuoteDetails.layoutManager =
            LinearLayoutManager(this)

        binding.rvQuoteDetails.adapter = quoteAdapter

        renderDraftItems()
    }


    // =====================================
    // VALID UNTIL DATE PICKER
    // =====================================

    private fun renderDraftItems() {

        quoteAdapter.submitList(draftItems.toList())
        binding.btnSaveQuoteDraft.isEnabled =
            draftItems.isNotEmpty() && !isSavingQuote
        val isEmpty = draftItems.isEmpty()

        binding.tvQuoteItemsEmpty.visibility =
            if (isEmpty) View.VISIBLE else View.GONE

        binding.rvQuoteDetails.visibility =
            if (isEmpty) View.GONE else View.VISIBLE

        // Tinh tong tien du kien
        val total = calculateEstimatedTotal()

        binding.tvQuoteTotal.text = when {
            total == null -> "Chưa xác định"

            total >= BigDecimal("10000000000000000") ->
                "Vượt giới hạn"

            else -> "${formatMoney(total.toDouble())} đ"
        }
    }


    private fun saveDraftQuote() {

        if (isSavingQuote) return

        // Tao request tu danh sach da chon
        val request = buildDraftQuoteRequest() ?: return

        isSavingQuote = true
        binding.btnSaveQuoteDraft.isEnabled = false

        lifecycleScope.launch {

            try {
                val response =
                    RetrofitClient.technicianApi.createQuote(request)

                if (response.isSuccessful) {

                    val result = response.body()
                    val quoteId = result?.quoteId


                    if (quoteId != null && quoteId > 0) {

                        val totalText = result.totalAmount
                            ?.takeIf { it.isFinite() && it >= 0.0 }
                            ?.let { "${formatMoney(it)} đ" }
                            ?: "Chưa có tổng tiền"

                        showMessage(
                            "Đã lưu nháp #$quoteId - $totalText"
                        )

                        // Tra ma bao gia ve man hinh truoc
                        val resultIntent = android.content.Intent().apply {
                            putExtra("quoteId", quoteId)
                            putExtra("repairRequestId", repairRequestId)
                        }


                        setResult(RESULT_OK, resultIntent)

// Mở UI04 để xem báo giá vừa lưu
                        val detailIntent = android.content.Intent(
                            this@CreateQuoteActivity,
                            QuoteDetailActivity::class.java
                        ).apply {
                            putExtra("quoteId", quoteId)
                        }

                        startActivity(detailIntent)
                        finish()


                    } else {

                        // Khong tu dong dong man hinh
                        // neu Backend chua tra ma bao gia
                        showMessage(
                            "Backend báo thành công nhưng chưa trả mã báo giá. " +
                                    "Hãy kiểm tra trước khi thử lưu lại."
                        )
                    }


                    // Quay ve man hinh chi tiet cong viec
                    setResult(RESULT_OK)
                    finish()

                } else {

                    val message = when (response.code()) {

                        400 ->
                            "Dữ liệu không hợp lệ hoặc báo giá đã tồn tại"

                        401 ->
                            "Phiên đăng nhập đã hết hạn"

                        403 ->
                            "Tài khoản không có quyền tạo báo giá"

                        404 ->
                            "Không tìm thấy công việc được giao"

                        409 ->
                            "Công việc chưa đủ điều kiện tạo báo giá"

                        else ->
                            "Lưu báo giá thất bại (${response.code()})"
                    }

                    showMessage(message)
                }

            } catch (e: Exception) {

                showMessage(
                    "Không xác nhận được kết quả lưu. " +
                            "Hãy kiểm tra báo giá trước khi thử lại."
                )

            } finally {

                isSavingQuote = false

                if (!isFinishing && !isDestroyed) {
                    binding.btnSaveQuoteDraft.isEnabled =
                        draftItems.isNotEmpty()
                }
            }
        }
    }

    private fun setupValidUntil() {
        binding.edtQuoteValidUntil.setOnClickListener {
            showValidUntilDatePicker()
        }
    }

    private fun calculateEstimatedTotal(): BigDecimal? {

        var total = BigDecimal.ZERO

        for (item in draftItems) {

            val price = item.unitPrice ?: return null

            if (
                !price.isFinite() ||
                price < 0 ||
                !item.quantity.isFinite() ||
                item.quantity <= 0
            ) {
                return null
            }

            val amount = BigDecimal.valueOf(price)
                .multiply(BigDecimal.valueOf(item.quantity))
                .setScale(2, RoundingMode.HALF_UP)

            total = total.add(amount)
        }

        return total
    }

    private fun addSelectedQuoteItem() {
        if (isSavingQuote) return

        if (catalogLoading) return

        // Lay hang muc dang chon tu danh muc Backend
        val selectedItem = catalogItems.getOrNull(
            binding.spinnerQuoteCatalog.selectedItemPosition
        )

        if (selectedItem == null) {
            showMessage("Vui lòng chọn hạng mục")
            return
        }

        // Kiem tra so luong
        val quantityText = binding.edtQuoteQuantity.text
            .toString()
            .trim()
            .replace(',', '.')

        val quantity = quantityText.toDoubleOrNull()

        if (quantity == null ||
            !quantity.isFinite() ||
            quantity <= 0.0
        ) {
            binding.edtQuoteQuantity.error =
                "Số lượng phải lớn hơn 0"

            binding.edtQuoteQuantity.requestFocus()
            return
        }

        // Don gia lay tu Backend, khong tu dat gia
        val price = selectedItem.price

        if (price == null ||
            !price.isFinite() ||
            price < 0.0
        ) {
            showMessage(
                "Hạng mục chưa có đơn giá hợp lệ"
            )
            return
        }

        // Kiem tra hang muc da duoc them chua
        val existingIndex = draftItems.indexOfFirst {
            it.itemType == selectedType &&
                    it.referenceId == selectedItem.id
        }

        val updatedQuantity = if (existingIndex >= 0) {
            draftItems[existingIndex].quantity + quantity
        } else {
            quantity
        }

        if (!updatedQuantity.isFinite() ||
            !(updatedQuantity * price).isFinite()
        ) {
            showMessage("Số lượng hoặc thành tiền quá lớn")
            return
        }

        val draftItem = QuoteDraftItem(
            itemType = selectedType,
            referenceId = selectedItem.id,
            name = selectedItem.name,
            quantity = updatedQuantity,
            unitPrice = price
        )

        if (existingIndex >= 0) {
            // Neu trung hang muc, cong don so luong
            draftItems[existingIndex] = draftItem
        } else {
            // Them hang muc moi
            draftItems.add(draftItem)
        }

        // Cap nhat RecyclerView va tong tien
        renderDraftItems()

        // Xoa so luong vua nhap
        binding.edtQuoteQuantity.text?.clear()

        showMessage("Đã thêm hạng mục vào báo giá")
    }

    private fun showValidUntilDatePicker() {
        val selectedDate = Calendar.getInstance().apply {
            add(Calendar.DAY_OF_MONTH, 1)
        }

        val dialog = DatePickerDialog(
            this,
            { _, year, month, day ->
                selectedDate.set(
                    year, month, day, 0, 0, 0
                )

                selectedDate.set(Calendar.MILLISECOND, 0)

                val displayFormat = SimpleDateFormat(
                    "dd/MM/yyyy",
                    Locale.getDefault()
                )

                val apiFormat = SimpleDateFormat(
                    "yyyy-MM-dd",
                    Locale.US
                )

                binding.edtQuoteValidUntil.setText(
                    displayFormat.format(selectedDate.time)
                )

                validUntil =
                    apiFormat.format(selectedDate.time)
            },
            selectedDate.get(Calendar.YEAR),
            selectedDate.get(Calendar.MONTH),
            selectedDate.get(Calendar.DAY_OF_MONTH)
        )

        val tomorrow = Calendar.getInstance().apply {
            add(Calendar.DAY_OF_MONTH, 1)
            set(Calendar.HOUR_OF_DAY, 0)
            set(Calendar.MINUTE, 0)
            set(Calendar.SECOND, 0)
            set(Calendar.MILLISECOND, 0)
        }

        dialog.datePicker.minDate =
            tomorrow.timeInMillis

        dialog.show()
    }

    // =====================================
    // ACTION BUTTONS
    // =====================================

    private fun buildDraftQuoteRequest(): CreateQuoteRequest? {

        if (draftItems.isEmpty()) {
            showMessage("Báo giá phải có ít nhất một hạng mục")
            return null
        }

        val total = calculateEstimatedTotal()

        if (
            total == null ||
            total >= BigDecimal("10000000000000000")
        ) {
            showMessage("Tổng tiền dự kiến không hợp lệ")
            return null
        }

        val details = mutableListOf<CreateQuoteDetailRequest>()

        for (item in draftItems) {

            if (
                item.referenceId <= 0 ||
                item.itemType !in 1..3 ||
                !item.quantity.isFinite() ||
                item.quantity <= 0 ||
                item.quantity >= 10000000000000000.0 ||
                (item.description?.length ?: 0) > 500
            ) {
                showMessage("Có hạng mục báo giá không hợp lệ")
                return null
            }

            details.add(
                CreateQuoteDetailRequest(
                    itemType = item.itemType,

                    serviceRefId =
                        if (item.itemType == 1)
                            item.referenceId else null,

                    repairItemRefId =
                        if (item.itemType == 2)
                            item.referenceId else null,

                    repairPartRefId =
                        if (item.itemType == 3)
                            item.referenceId else null,

                    description = item.description,

                    quantity = item.quantity
                )
            )
        }

        return CreateQuoteRequest(
            repairRequestId = repairRequestId,
            validUntil = validUntil,
            details = details
        )
    }

    private fun setupActions() {
        binding.btnAddQuoteItem.isEnabled = false
        binding.btnSaveQuoteDraft.isEnabled = draftItems.isNotEmpty()

        binding.btnSaveQuoteDraft.setOnClickListener {
            saveDraftQuote()
        }
        binding.btnSubmitQuote.isEnabled = false
        binding.btnAddQuoteItem.setOnClickListener {
            addSelectedQuoteItem()
        }
        binding.btnRetryQuoteCatalog.setOnClickListener {
            if (!catalogLoading) {
                loadCatalog(selectedType)
            }
        }
    }

    // =====================================
    // ERROR HANDLING
    // =====================================

    private class ApiError(message: String) : Exception(message)

    private fun createApiError(
        code: Int,
        body: String?
    ): ApiError {
        val backendMessage = try {
            JSONObject(body ?: "{}").optString("message")
        } catch (_: Exception) {
            ""
        }

        val message = when (code) {
            401 -> "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại."
            403 -> "Bạn không có quyền xem danh mục cửa hàng này."
            404 -> "Không tìm thấy cửa hàng hoặc danh mục."
            409 -> "Dữ liệu đã thay đổi. Vui lòng tải lại."
            else -> backendMessage.ifBlank {
                "Lỗi Backend (HTTP $code)"
            }
        }

        return ApiError(message)
    }

    // =====================================
    // HELPERS
    // =====================================

    private fun formatMoney(value: Double): String {
        return NumberFormat.getNumberInstance(
            Locale("vi", "VN")
        ).format(value)
    }

    private fun dp(value: Int): Int {
        return (value * resources.displayMetrics.density).toInt()
    }

    private fun showMessage(message: String) {
        Toast.makeText(
            this,
            message,
            Toast.LENGTH_SHORT
        ).show()
    }

    override fun onDestroy() {
        catalogJob?.cancel()
        super.onDestroy()
    }
}
