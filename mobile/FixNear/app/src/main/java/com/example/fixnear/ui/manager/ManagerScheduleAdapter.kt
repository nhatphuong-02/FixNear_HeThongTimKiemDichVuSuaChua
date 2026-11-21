package com.example.fixnear.ui.manager

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.ManagerScheduleItem
import com.example.fixnear.databinding.ItemManagerScheduleBinding

class ManagerScheduleAdapter(
    private val onClick: (ManagerScheduleItem) -> Unit
) : RecyclerView.Adapter<ManagerScheduleAdapter.Holder>() {

    private val items = mutableListOf<ManagerScheduleItem>()

    fun submitItems(data: List<ManagerScheduleItem>) {
        items.clear()
        items.addAll(data)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): Holder {
        return Holder(
            ItemManagerScheduleBinding.inflate(
                LayoutInflater.from(parent.context),
                parent,
                false
            )
        )
    }

    override fun onBindViewHolder(
        holder: Holder,
        position: Int
    ) {
        val item = items[position]
        val b = holder.binding

        b.tvScheduleId.text =
            "Lịch #${item.scheduleId}"

        b.tvStatus.text =
            "Trạng thái: ${item.status.ifBlank { "—" }}"

        fun formatTime(value: String): String {
            return value
                .replace("T", " ")
                .take(16)
                .ifBlank { "Chưa có thời gian" }
        }

        b.tvTime.text =
            "${formatTime(item.startTime)} → " +
                    formatTime(item.endTime)

        b.tvTechnician.text =
            "Kỹ thuật viên: " +
                    item.technicianName.ifBlank {
                        "Chưa có thông tin"
                    }

        b.tvCustomer.text =
            "Khách hàng: " +
                    item.customerName.ifBlank {
                        "Chưa có thông tin"
                    }

        b.tvDescription.text =
            item.description.ifBlank {
                "Không có mô tả công việc"
            }

        b.root.setOnClickListener {
            onClick(item)
        }
    }

    override fun getItemCount(): Int = items.size

    class Holder(
        val binding: ItemManagerScheduleBinding
    ) : RecyclerView.ViewHolder(binding.root)
}
