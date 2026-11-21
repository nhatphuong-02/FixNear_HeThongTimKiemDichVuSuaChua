
package com.example.fixnear.ui.assignment

import android.content.res.ColorStateList
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.core.content.ContextCompat
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.R
import com.example.fixnear.data.model.technician.AssignStatus
import com.example.fixnear.data.model.technician.RepairStatus
import com.example.fixnear.data.model.technician.TechnicianAssignment
import com.example.fixnear.databinding.ItemAssignmentBinding
import java.text.SimpleDateFormat
import java.util.Locale

class AssignmentAdapter(
    private val onAccept: (TechnicianAssignment) -> Unit,
    private val onReject: (TechnicianAssignment) -> Unit,
    private val onItemClick: (TechnicianAssignment) -> Unit
) : RecyclerView.Adapter<AssignmentAdapter.AssignmentViewHolder>() {

    private val items = mutableListOf<TechnicianAssignment>()

    // Cap nhat danh sach cong viec
    fun submitList(newItems: List<TechnicianAssignment>) {
        val updatedItems = newItems.toList()

        items.clear()
        items.addAll(updatedItems)

        notifyDataSetChanged()
    }

    inner class AssignmentViewHolder(
        private val binding: ItemAssignmentBinding
    ) : RecyclerView.ViewHolder(binding.root) {

        fun bind(item: TechnicianAssignment) {

            // ============================
            // THONG TIN KHACH HANG
            // ============================

            binding.tvCustomerName.text =
                item.customerName.ifBlank {
                    "Khách hàng"
                }

            binding.tvShopName.text =
                "Cửa hàng: ${item.shopName}"

            // ============================
            // DANH SACH DICH VU
            // ============================

            binding.tvAssignmentServices.text =
                item.services.joinToString(", ") {
                    it.serviceName
                }.ifBlank {
                    "Chưa có thông tin dịch vụ"
                }

            // ============================
            // MO TA CONG VIEC
            // ============================

            binding.tvAssignmentDescription.text =
                item.description
                    ?.takeIf { it.isNotBlank() }
                    ?: "Không có mô tả"

            // ============================
            // LICH HEN
            // ============================

            binding.tvAssignmentDate.text =
                formatAppointment(item)

            // ============================
            // HINH THUC SUA CHUA
            // ============================

            binding.tvRepairType.text =
                when (item.repairMethod) {
                    1 -> "Hình thức: Tại nhà"
                    2 -> "Hình thức: Tại cửa hàng"
                    else -> "Hình thức: Chưa xác định"
                }

            // ============================
            // TRANG THAI CONG VIEC
            // ============================

            binding.tvAssignmentStatus.text =
                getStatusText(item)

            binding.tvAssignmentStatus.setTextColor(
                getStatusColor(item)
            )

            // ============================
            // DIEU KIEN NHAN / TU CHOI
            // ============================

            // Backend:
            // AssignmentStatus.Offered = 1
            // RepairRequestStatus.Accepted = 2

            val canRespond =
                item.assignmentStatus == AssignStatus.OFFERED &&
                        item.repairStatus == 2

            binding.layoutAssignmentActions.visibility =
                if (canRespond) {
                    View.VISIBLE
                } else {
                    View.GONE
                }

            // ============================
            // FIXNEAR BUTTON COLORS
            // ============================

            val context = binding.root.context

            val primaryColor = ContextCompat.getColor(
                context,
                R.color.accent_gold
            )

            val primaryTextColor = ContextCompat.getColor(
                context,
                R.color.button_primary_text
            )

            val secondaryColor = ContextCompat.getColor(
                context,
                R.color.button_secondary
            )

            val secondaryTextColor = ContextCompat.getColor(
                context,
                R.color.button_secondary_text
            )

            // Nut nhan cong viec
            binding.btnAcceptAssignment.backgroundTintList =
                ColorStateList.valueOf(primaryColor)

            binding.btnAcceptAssignment.setTextColor(
                primaryTextColor
            )

            binding.btnAcceptAssignment.isAllCaps = false

            // Nut tu choi cong viec
            binding.btnRejectAssignment.backgroundTintList =
                ColorStateList.valueOf(secondaryColor)

            binding.btnRejectAssignment.setTextColor(
                secondaryTextColor
            )

            binding.btnRejectAssignment.isAllCaps = false

            // ============================
            // XU LY SU KIEN
            // ============================

            binding.btnAcceptAssignment.setOnClickListener {
                if (canRespond) {
                    onAccept(item)
                }
            }

            binding.btnRejectAssignment.setOnClickListener {
                if (canRespond) {
                    onReject(item)
                }
            }

            binding.root.setOnClickListener {
                onItemClick(item)
            }
        }
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): AssignmentViewHolder {

        val binding = ItemAssignmentBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )

        return AssignmentViewHolder(binding)
    }

    override fun onBindViewHolder(
        holder: AssignmentViewHolder,
        position: Int
    ) {
        holder.bind(items[position])
    }

    override fun getItemCount(): Int = items.size

    // ============================
    // DINH DANG LICH HEN
    // ============================

    private fun formatAppointment(
        item: TechnicianAssignment
    ): String {

        val date = item.preferredDate
            ?.takeIf { it.isNotBlank() }
            ?.let { formatDate(it) }
            ?: "Chưa có ngày hẹn"

        val from = formatTime(
            item.preferredTimeFrom
        )

        val to = formatTime(
            item.preferredTimeTo
        )

        return when {
            from != null && to != null ->
                "Lịch hẹn: $date | $from - $to"

            from != null ->
                "Lịch hẹn: $date | $from"

            to != null ->
                "Lịch hẹn: $date | Đến $to"

            else ->
                "Lịch hẹn: $date"
        }
    }

    private fun formatDate(value: String): String {

        val inputFormats = listOf(
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.SSS",
            "yyyy-MM-dd"
        )

        for (pattern in inputFormats) {
            try {
                val input = SimpleDateFormat(
                    pattern,
                    Locale.getDefault()
                )

                input.isLenient = false

                val date = input.parse(value)
                    ?: continue

                return SimpleDateFormat(
                    "dd/MM/yyyy",
                    Locale.getDefault()
                ).format(date)

            } catch (_: Exception) {
                // Thu dinh dang tiep theo
            }
        }

        return value
    }

    private fun formatTime(value: String?): String? {

        if (value.isNullOrBlank()) {
            return null
        }

        return Regex("""\d{2}:\d{2}""")
            .find(value)
            ?.value
    }

    // ============================
    // TRANG THAI HIEN THI
    // ============================

    private fun getStatusText(
        item: TechnicianAssignment
    ): String {

        // Trang thai phan cong duoc uu tien
        // neu KTV da tu choi hoac bi huy phan cong.

        when (item.assignmentStatus) {
            AssignStatus.REJECTED ->
                return "●  ĐÃ TỪ CHỐI PHÂN CÔNG"

            AssignStatus.CANCELLED ->
                return "●  PHÂN CÔNG ĐÃ HỦY"
        }

        return when (item.repairStatus) {

            1 -> "●  CHỜ CỬA HÀNG TIẾP NHẬN"

            2 -> "●  CHỜ NHẬN VIỆC"

            3 -> "●  YÊU CẦU BỊ TỪ CHỐI"

            RepairStatus.ASSIGNED ->
                "●  ĐÃ PHÂN CÔNG"

            RepairStatus.INSPECTING ->
                "●  ĐANG KHẢO SÁT"

            RepairStatus.QUOTED ->
                "●  ĐÃ BÁO GIÁ"

            RepairStatus.QUOTE_APPROVED ->
                "●  ĐÃ DUYỆT BÁO GIÁ"

            RepairStatus.REPAIRING ->
                "●  ĐANG SỬA CHỮA"

            RepairStatus.COMPLETED ->
                "●  ĐÃ HOÀN THÀNH"

            10 -> "●  ĐÃ THANH TOÁN"

            11 -> "●  YÊU CẦU ĐÃ HỦY"

            else -> "●  TRẠNG THÁI ${item.repairStatus}"
        }
    }

    // ============================
    // MAU TRANG THAI FIXNEAR
    // ============================

    private fun getStatusColor(
        item: TechnicianAssignment
    ): Int {

        val context = itemColorContext
            ?: return android.graphics.Color.parseColor(
                "#66442F"
            )

        val colorRes = when {

            item.assignmentStatus == AssignStatus.REJECTED ||
                    item.assignmentStatus == AssignStatus.CANCELLED ->
                R.color.status_cancelled

            item.repairStatus == 1 ||
                    item.repairStatus == 2 ->
                R.color.status_pending

            item.repairStatus == 3 ||
                    item.repairStatus == 11 ->
                R.color.status_cancelled

            item.repairStatus == RepairStatus.ASSIGNED ||
                    item.repairStatus == RepairStatus.INSPECTING ||
                    item.repairStatus == RepairStatus.QUOTED ||
                    item.repairStatus == RepairStatus.REPAIRING ->
                R.color.status_processing

            item.repairStatus == RepairStatus.QUOTE_APPROVED ||
                    item.repairStatus == RepairStatus.COMPLETED ||
                    item.repairStatus == 10 ->
                R.color.status_completed

            else ->
                R.color.accent_brown
        }

        return ContextCompat.getColor(
            context,
            colorRes
        )
    }

    // Context duoc gan khi RecyclerView tao ViewHolder
    private var itemColorContext: android.content.Context? = null
}
