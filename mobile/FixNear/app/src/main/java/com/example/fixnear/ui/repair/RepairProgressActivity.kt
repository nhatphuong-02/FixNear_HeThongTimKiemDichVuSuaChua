
package com.example.fixnear.ui.repair

import android.os.Bundle
import android.widget.Button
import android.widget.EditText
import com.example.fixnear.data.model.technician.PartUsageRequest
import com.example.fixnear.data.model.technician.CompleteRequest
import androidx.appcompat.app.AlertDialog
import java.math.BigDecimal
import android.widget.ArrayAdapter
import android.widget.Spinner
import com.example.fixnear.data.model.technician.ShopRepairPart
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.data.model.technician.AssignStatus
import com.example.fixnear.data.model.technician.RepairStatus
import kotlinx.coroutines.launch
import android.view.View
import com.example.fixnear.R

class RepairProgressActivity : AppCompatActivity() {
    private var isStartingRepair = false
    private var statusLoadVersion = 0
    private var partsLoadVersion = 0
    private var isRecordingPart = false
    private var isCompletingRepair = false
    private var canCompleteRepair = false
    private var canRecordPart = false
    private var repairParts: List<ShopRepairPart> = emptyList()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        setContentView(R.layout.activity_repair_progress)
        setPartInputsEnabled(false)
        setCompletionFieldsEnabled(false)

        // Nút quay lại
        findViewById<TextView>(
            R.id.btnBackRepair
        ).setOnClickListener {
            finish()
        }

        // Nhận mã yêu cầu sửa chữa
        val repairRequestId = intent.getIntExtra(
            "repairRequestId", -1
        )

        val tvRequestId = findViewById<TextView>(
            R.id.tvRepairRequestId
        )

        val tvStatus = findViewById<TextView>(
            R.id.tvRepairStatus
        )

        val tvMessage = findViewById<TextView>(
            R.id.tvRepairMessage
        )

        // Chưa bật nút khi chưa kiểm tra Backend
        findViewById<Button>(
            R.id.btnStartRepair
        ).isEnabled = false
        findViewById<Button>(
            R.id.btnRetryRepairStatus
        ).setOnClickListener {
            if (repairRequestId > 0) {
                loadRepairStatus(repairRequestId)
            }
        }
        findViewById<Button>(
            R.id.btnStartRepair
        ).setOnClickListener {
            if (repairRequestId > 0) {
                startRepair(repairRequestId)
            }
        }
        findViewById<Button>(
            R.id.btnRecordRepairPart
        ).setOnClickListener {
            if (repairRequestId > 0) {
                recordRepairPart(repairRequestId)
            }
        }
        findViewById<Button>(
            R.id.btnCompleteRepair
        ).setOnClickListener {

            if (repairRequestId > 0 &&
                canCompleteRepair &&
                !isCompletingRepair &&
                !isRecordingPart &&
                !isStartingRepair
            ) {
                AlertDialog.Builder(this)
                    .setTitle("Xác nhận hoàn tất")
                    .setMessage(
                        "Bạn đã sửa chữa xong và muốn " +
                                "xác nhận hoàn tất công việc này?"
                    )
                    .setNegativeButton("Hủy", null)
                    .setPositiveButton("Hoàn tất") { _, _ ->
                        completeRepair(repairRequestId)
                    }
                    .show()
            }
        }
        if (repairRequestId <= 0) {
            tvRequestId.text = "Mã yêu cầu: --"
            tvStatus.text = "Trạng thái: Không xác định"
            tvMessage.text = "Không có mã yêu cầu hợp lệ"
        } else {
            tvRequestId.text = "Mã yêu cầu: #$repairRequestId"
            tvStatus.text = "Trạng thái: Chưa tải"
            tvMessage.text = "Đang kiểm tra trạng thái công việc..."
            loadRepairStatus(repairRequestId)
        }
    }

    private fun startRepair(repairRequestId: Int) {

        val btnStart = findViewById<Button>(
            R.id.btnStartRepair
        )
        val tvStatus = findViewById<TextView>(
            R.id.tvRepairStatus
        )
        val tvMessage = findViewById<TextView>(
            R.id.tvRepairMessage
        )
        val progress = findViewById<View>(
            R.id.progressRepair
        )

        if (isStartingRepair ||
            repairRequestId <= 0 ||
            !btnStart.isEnabled
        ) return

        isStartingRepair = true
        statusLoadVersion++
        btnStart.isEnabled = false
        canRecordPart = false

        findViewById<Button>(
            R.id.btnRecordRepairPart
        ).isEnabled = false
        progress.visibility = View.VISIBLE
        tvMessage.text = "Đang bắt đầu sửa chữa..."

        lifecycleScope.launch {
            var shouldRefresh = false

            try {
                val response = RetrofitClient.technicianApi
                    .startRepair(repairRequestId)

                if (response.isSuccessful) {
                    tvStatus.text = "Trạng thái: Đang sửa chữa"
                    tvMessage.text = "Bắt đầu sửa chữa thành công"
                    shouldRefresh = true

                } else {
                    tvMessage.text = when (response.code()) {
                        400 -> "Yêu cầu không hợp lệ"
                        401 -> "Phiên đăng nhập đã hết hạn"
                        403 -> "Bạn không có quyền sửa chữa công việc này"
                        404 -> "Không tìm thấy yêu cầu sửa chữa"
                        409 -> "Chưa đủ điều kiện bắt đầu sửa chữa"
                        else -> "Không thể bắt đầu sửa chữa (${response.code()})"
                    }
                }

            } catch (e: Exception) {
                tvMessage.text =
                    "Không xác nhận được kết quả. " +
                            "Hãy kiểm tra trạng thái trước khi thử lại."

            } finally {
                isStartingRepair = false
                progress.visibility = View.GONE

                if (shouldRefresh && !isFinishing && !isDestroyed) {
                    loadRepairStatus(repairRequestId)
                }
            }
        }
    }

    private fun recordRepairPart(repairRequestId: Int) {

        if (isRecordingPart || !canRecordPart) return

        val spinner = findViewById<Spinner>(
            R.id.spinnerRepairPart
        )

        val edtQuantity = findViewById<EditText>(
            R.id.edtRepairPartQuantity
        )

        val edtNote = findViewById<EditText>(
            R.id.edtRepairPartNote
        )

        val btnRecord = findViewById<Button>(
            R.id.btnRecordRepairPart
        )

        val message = findViewById<TextView>(
            R.id.tvRepairMessage
        )

        val progress = findViewById<View>(
            R.id.progressRepair
        )

        val part = repairParts.getOrNull(
            spinner.selectedItemPosition
        )

        if (part == null || part.repairPartId <= 0) {
            message.text = "Vui lòng chọn linh kiện hợp lệ"
            return
        }

        val quantity = edtQuantity.text.toString()
            .trim()
            .replace(',', '.')
            .toBigDecimalOrNull()

        if (
            quantity == null ||
            quantity < BigDecimal("0.01") ||
            quantity > BigDecimal("999999") ||
            quantity.stripTrailingZeros().scale() > 2
        ) {
            edtQuantity.error =
                "Nhập số lượng từ 0.01 đến 999999, tối đa 2 số thập phân"
            edtQuantity.requestFocus()
            return
        }

        val note = edtNote.text.toString().trim()

        if (note.length > 500) {
            edtNote.error = "Ghi chú tối đa 500 ký tự"
            return
        }

        val request = PartUsageRequest(
            repairRequestId = repairRequestId,
            repairPartId = part.repairPartId,
            quantity = quantity.toDouble(),
            note = note.ifEmpty { null }
        )

        isRecordingPart = true
        statusLoadVersion++
        setPartInputsEnabled(false)
        findViewById<Button>(
            R.id.btnCompleteRepair
        ).isEnabled = false
        btnRecord.isEnabled = false
        spinner.isEnabled = false
        progress.visibility = View.VISIBLE
        message.text = "Đang ghi nhận linh kiện..."

        lifecycleScope.launch {

            var resultUncertain = false

            try {
                val response = RetrofitClient.technicianApi
                    .recordPart(repairRequestId, request)

                if (response.isSuccessful) {

                    val usageId =
                        response.body()?.repairPartUsageId

                    message.text =
                        if (usageId != null && usageId > 0) {
                            "Đã ghi nhận linh kiện thành công (#$usageId)"
                        } else {
                            "Đã ghi nhận linh kiện thành công"
                        }

                    edtQuantity.text.clear()
                    edtNote.text.clear()

                } else {

                    message.text = when (response.code()) {
                        400 -> "Số lượng hoặc ghi chú không hợp lệ"
                        401 -> "Phiên đăng nhập đã hết hạn"
                        403 -> "Bạn không có quyền ghi nhận linh kiện"
                        404 -> "Không tìm thấy công việc hoặc linh kiện"
                        409 -> "Linh kiện chưa được duyệt, vượt số lượng được duyệt hoặc công việc chưa ở trạng thái sửa chữa"
                        else -> "Ghi nhận thất bại (${response.code()})"
                    }
                }

            } catch (e: Exception) {
                resultUncertain = true

                message.text =
                    "Không xác nhận được kết quả lưu. " +
                            "Hãy kiểm tra trước khi thử ghi nhận lại."

            } finally {
                isRecordingPart = false
                progress.visibility = View.GONE

                if (!isFinishing && !isDestroyed) {
                    setPartInputsEnabled(
                        canRecordPart &&
                                repairParts.isNotEmpty() &&
                                !resultUncertain &&
                                !isCompletingRepair &&
                                !isStartingRepair
                    )
                    findViewById<Button>(
                        R.id.btnCompleteRepair
                    ).isEnabled =
                        canCompleteRepair &&
                                !isCompletingRepair &&
                                !isStartingRepair &&
                                !resultUncertain
                    spinner.isEnabled =
                        canRecordPart && repairParts.isNotEmpty()

                    btnRecord.isEnabled =
                        canRecordPart &&
                                repairParts.isNotEmpty() &&
                                !resultUncertain
                }
            }
        }
    }

    private fun completeRepair(repairRequestId: Int) {

        if (
            repairRequestId <= 0 ||
            !canCompleteRepair ||
            isCompletingRepair ||
            isRecordingPart ||
            isStartingRepair
        ) return

        val edtResult = findViewById<EditText>(
            R.id.edtRepairResult
        )

        val edtWork = findViewById<EditText>(
            R.id.edtRepairWorkDescription
        )

        val edtRecommendation = findViewById<EditText>(
            R.id.edtRepairRecommendation
        )

        val btnComplete = findViewById<Button>(
            R.id.btnCompleteRepair
        )

        val btnRecord = findViewById<Button>(
            R.id.btnRecordRepairPart
        )

        val spinner = findViewById<Spinner>(
            R.id.spinnerRepairPart
        )

        val progress = findViewById<View>(
            R.id.progressRepair
        )

        val message = findViewById<TextView>(
            R.id.tvCompleteRepairMessage
        )

        val result = edtResult.text.toString().trim()
        val work = edtWork.text.toString().trim()
        val recommendation =
            edtRecommendation.text.toString().trim()

        if (result.isBlank() || result.length > 1000) {
            edtResult.error =
                "Kết quả sửa chữa bắt buộc, tối đa 1000 ký tự"
            edtResult.requestFocus()
            return
        }

        if (work.length > 2000) {
            edtWork.error = "Tối đa 2000 ký tự"
            edtWork.requestFocus()
            return
        }

        if (recommendation.length > 1000) {
            edtRecommendation.error = "Tối đa 1000 ký tự"
            edtRecommendation.requestFocus()
            return
        }

        val request = CompleteRequest(
            result = result,
            workDescription = work.ifEmpty { null },
            recommendation = recommendation.ifEmpty { null }
        )

        isCompletingRepair = true
        statusLoadVersion++
        setPartInputsEnabled(false)
        setCompletionFieldsEnabled(false)
        btnComplete.isEnabled = false
        btnRecord.isEnabled = false
        spinner.isEnabled = false
        progress.visibility = View.VISIBLE
        message.text = "Đang xác nhận hoàn tất..."

        lifecycleScope.launch {

            var allowRetry = false

            try {
                val response = RetrofitClient.technicianApi
                    .complete(repairRequestId, request)

                if (response.isSuccessful) {

                    canCompleteRepair = false
                    canRecordPart = false

                    findViewById<TextView>(
                        R.id.tvRepairStatus
                    ).text = "Trạng thái: Hoàn thành"

                    findViewById<Button>(
                        R.id.btnStartRepair
                    ).isEnabled = false

                    message.text =
                        "Hoàn tất sửa chữa thành công"

                    findViewById<TextView>(
                        R.id.tvRepairMessage
                    ).text = "Công việc đã hoàn thành"

                    // Chỉ cập nhật trạng thái sau khi API thành công
                    setResult(RESULT_OK)

                } else {

                    message.text = when (response.code()) {
                        400 -> "Thông tin hoàn tất không hợp lệ"
                        401 -> "Phiên đăng nhập đã hết hạn"
                        403 -> "Bạn không có quyền hoàn tất"
                        404 -> "Không tìm thấy công việc"
                        409 ->
                            "Công việc chưa đủ điều kiện hoàn tất. " +
                                    "Hãy kiểm tra báo giá và trạng thái."
                        else ->
                            "Hoàn tất thất bại (${response.code()})"
                    }

                    // Chỉ cho thử lại trực tiếp nếu Backend báo lỗi dữ liệu
                    allowRetry = response.code() == 400
                }

            } catch (e: Exception) {

                message.text =
                    "Chưa xác nhận được kết quả. " +
                            "Hãy mở lại công việc để kiểm tra " +
                            "trước khi thử lần nữa."

            } finally {

                isCompletingRepair = false
                progress.visibility = View.GONE

                if (!isFinishing && !isDestroyed) {
                    setPartInputsEnabled(
                        canRecordPart &&
                                repairParts.isNotEmpty() &&
                                allowRetry &&
                                !isRecordingPart &&
                                !isStartingRepair
                    )
                    setCompletionFieldsEnabled(
                        canCompleteRepair && allowRetry
                    )
                    btnComplete.isEnabled =
                        canCompleteRepair && allowRetry

                    btnRecord.isEnabled =
                        canRecordPart &&
                                allowRetry &&
                                repairParts.isNotEmpty()

                    spinner.isEnabled =
                        canRecordPart &&
                                allowRetry &&
                                repairParts.isNotEmpty()
                }
            }
        }
    }

    private fun setPartInputsEnabled(enabled: Boolean) {

        findViewById<EditText>(
            R.id.edtRepairPartQuantity
        ).isEnabled = enabled

        findViewById<EditText>(
            R.id.edtRepairPartNote
        ).isEnabled = enabled
    }

    private fun loadRepairParts(shopId: Int) {
        val currentPartsRequest = ++partsLoadVersion

        val spinner = findViewById<Spinner>(
            R.id.spinnerRepairPart
        )

        val message = findViewById<TextView>(
            R.id.tvRepairMessage
        )

        spinner.isEnabled = false
        setPartInputsEnabled(false)
        findViewById<Button>(
            R.id.btnRecordRepairPart
        ).isEnabled = false
        repairParts = emptyList()

        if (shopId <= 0) {
            message.text = "Mã cửa hàng không hợp lệ"
            return
        }

        lifecycleScope.launch {
            try {
                val allParts = mutableListOf<ShopRepairPart>()

                var page = 1
                var totalPages: Int

                do {
                    val response = RetrofitClient.technicianApi
                        .getRepairParts(
                            shopId = shopId,
                            page = page,
                            pageSize = 50
                        )

                    if (
                        currentPartsRequest != partsLoadVersion ||
                        !canRecordPart ||
                        isRecordingPart ||
                        isCompletingRepair ||
                        isStartingRepair
                    ) {
                        return@launch
                    }


                    if (!response.isSuccessful) {
                        message.text =
                            "Không tải được linh kiện (${response.code()})"
                        return@launch
                    }

                    val result = response.body()

                    if (result == null) {
                        message.text = "Dữ liệu linh kiện trống"
                        return@launch
                    }

                    allParts.addAll(
                        result.data.filter { it.isActive }
                    )

                    totalPages = result.totalPages
                    page++

                } while (page <= totalPages)
                if (
                    currentPartsRequest != partsLoadVersion ||
                    !canRecordPart ||
                    isRecordingPart ||
                    isCompletingRepair ||
                    isStartingRepair
                ) {
                    return@launch
                }
                repairParts = allParts

                if (repairParts.isEmpty()) {
                    message.text =
                        "Cửa hàng chưa có linh kiện đang hoạt động"
                    return@launch
                }

                val names = repairParts.map {
                    "${it.partName} (${it.partCode})"
                }

                val adapter = ArrayAdapter(
                    this@RepairProgressActivity,
                    android.R.layout.simple_spinner_item,
                    names
                ).apply {
                    setDropDownViewResource(
                        android.R.layout.simple_spinner_dropdown_item
                    )
                }

                spinner.adapter = adapter
                setPartInputsEnabled(
                    canRecordPart &&
                            repairParts.isNotEmpty() &&
                            !isRecordingPart &&
                            !isCompletingRepair &&
                            !isStartingRepair
                )
                spinner.isEnabled =
                    canRecordPart &&
                            !isRecordingPart &&
                            !isCompletingRepair &&
                            !isStartingRepair
                findViewById<Button>(
                    R.id.btnRecordRepairPart
                ).isEnabled =
                    canRecordPart &&
                            !isRecordingPart &&
                            !isCompletingRepair &&
                            !isStartingRepair

            } catch (e: Exception) {
                if (
                    currentPartsRequest == partsLoadVersion &&
                    canRecordPart &&
                    !isRecordingPart &&
                    !isCompletingRepair &&
                    !isStartingRepair
                ) {
                    message.text =
                        "Không thể tải linh kiện. Kiểm tra kết nối."
                }
            }
        }
    }

    private fun setCompletionFieldsEnabled(enabled: Boolean) {

        findViewById<EditText>(
            R.id.edtRepairResult
        ).isEnabled = enabled

        findViewById<EditText>(
            R.id.edtRepairWorkDescription
        ).isEnabled = enabled

        findViewById<EditText>(
            R.id.edtRepairRecommendation
        ).isEnabled = enabled
    }

    override fun onRestart() {
        super.onRestart()

        val repairRequestId = intent.getIntExtra(
            "repairRequestId",
            -1
        )

        if (
            repairRequestId > 0 &&
            !isFinishing &&
            !isStartingRepair &&
            !isRecordingPart &&
            !isCompletingRepair
        ) {
            loadRepairStatus(repairRequestId)
        }
    }

    private fun loadRepairStatus(repairRequestId: Int) {
        val requestVersion = ++statusLoadVersion
        partsLoadVersion++

        findViewById<Spinner>(
            R.id.spinnerRepairPart
        ).isEnabled = false
        val tvStatus = findViewById<TextView>(R.id.tvRepairStatus)
        val tvMessage = findViewById<TextView>(R.id.tvRepairMessage)
        val btnStart = findViewById<Button>(R.id.btnStartRepair)
        val progress = findViewById<View>(R.id.progressRepair)
        val btnRetry = findViewById<Button>(
            R.id.btnRetryRepairStatus
        )
        btnStart.isEnabled = false
        setPartInputsEnabled(false)
        setCompletionFieldsEnabled(false)
        canCompleteRepair = false

        findViewById<Button>(
            R.id.btnCompleteRepair
        ).isEnabled = false
        progress.visibility = View.VISIBLE
        btnRetry.visibility = View.GONE
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.technicianApi
                    .getAssignments()
                if (requestVersion != statusLoadVersion) {
                    return@launch
                }

                if (!response.isSuccessful) {
                    tvStatus.text = "Trạng thái: Chưa tải được"
                    tvMessage.text =
                        "Không tải được công việc (${response.code()})"
                    btnRetry.visibility = View.VISIBLE
                    return@launch
                }

                val assignment = response.body()?.data
                    ?.firstOrNull {
                        it.repairRequestId == repairRequestId &&
                                it.assignmentStatus == AssignStatus.ACCEPTED
                    }

                if (assignment == null) {
                    tvStatus.text = "Trạng thái: Không xác định"
                    tvMessage.text =
                        "Không tìm thấy công việc đã nhận tương ứng"
                    btnRetry.visibility = View.VISIBLE
                    return@launch
                }

                val status = assignment.repairStatus
                setCompletionFieldsEnabled(
                    status == RepairStatus.REPAIRING &&
                            !isCompletingRepair
                )
                canCompleteRepair =
                    status == RepairStatus.REPAIRING

                findViewById<Button>(
                    R.id.btnCompleteRepair
                ).isEnabled =
                    canCompleteRepair && !isCompletingRepair

                findViewById<TextView>(
                    R.id.tvCompleteRepairMessage
                ).text = when (status) {
                    RepairStatus.REPAIRING ->
                        "Nhập kết quả sửa chữa để hoàn tất"

                    RepairStatus.COMPLETED ->
                        "Công việc đã hoàn thành"

                    else ->
                        "Chỉ có thể hoàn tất khi đang sửa chữa"
                }
                canRecordPart = status == RepairStatus.REPAIRING
                if (status == RepairStatus.REPAIRING) {
                    loadRepairParts(assignment.shopId)
                } else {
                    repairParts = emptyList()

                    findViewById<Spinner>(
                        R.id.spinnerRepairPart
                    ).isEnabled = false

                    findViewById<Button>(
                        R.id.btnRecordRepairPart
                    ).isEnabled = false
                }

                val statusText = when (status) {
                    4 -> "Đã phân công"
                    5 -> "Đang khảo sát"
                    6 -> "Đã báo giá"
                    7 -> "Đã duyệt báo giá"
                    8 -> "Đang sửa chữa"
                    9 -> "Hoàn thành"
                    10 -> "Đã thanh toán"
                    else -> "Trạng thái #$status"
                }

                tvStatus.text = "Trạng thái: $statusText"

                btnStart.isEnabled =
                    status == RepairStatus.QUOTE_APPROVED

                tvMessage.text = when (status) {
                    RepairStatus.QUOTE_APPROVED ->
                        "Có thể bắt đầu sửa chữa nếu Backend xác nhận đủ điều kiện"

                    RepairStatus.REPAIRING ->
                        "Công việc đang trong quá trình sửa chữa"

                    RepairStatus.COMPLETED, 10 ->
                        "Công việc đã hoàn thành"

                    else ->
                        "Chưa đủ điều kiện bắt đầu sửa chữa"
                }

            } catch (e: Exception) {
                if (requestVersion != statusLoadVersion) {
                    return@launch
                }
                tvStatus.text = "Trạng thái: Lỗi kết nối"
                tvMessage.text =
                    "Không thể kết nối Backend. Vui lòng thử lại."
                btnRetry.visibility = View.VISIBLE

            } finally {
                if (
                    requestVersion == statusLoadVersion &&
                    !isStartingRepair &&
                    !isRecordingPart &&
                    !isCompletingRepair
                ) {
                    progress.visibility = View.GONE
                }
            }
        }
    }

}
