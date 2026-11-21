package com.example.fixnear.ui.manager

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.ManagerRepairItem
import com.example.fixnear.databinding.ItemManagerRepairRequestBinding

class ManagerRepairAdapter(
    private val onClick: (ManagerRepairItem) -> Unit
) : RecyclerView.Adapter<ManagerRepairAdapter.Holder>() {

    private val items = mutableListOf<ManagerRepairItem>()

    fun submitItems(data: List<ManagerRepairItem>) {
        items.clear()
        items.addAll(data)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): Holder {
        val binding = ItemManagerRepairRequestBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )

        return Holder(binding)
    }

    override fun onBindViewHolder(
        holder: Holder,
        position: Int
    ) {
        val item = items[position]
        val b = holder.binding

        b.tvRequestId.text =
            "Yêu cầu #${item.repairRequestId}"

        b.tvCustomerName.text =
            item.customerName.ifBlank {
                "Khách hàng chưa xác định"
            }

        b.tvIssueDescription.text =
            item.description.ifBlank {
                "Chưa có mô tả"
            }

        b.tvStatus.text = item.statusLabel
        b.tvCreatedAt.text = item.createdAt

        b.root.setOnClickListener {
            onClick(item)
        }
    }

    override fun getItemCount(): Int = items.size

    class Holder(
        val binding: ItemManagerRepairRequestBinding
    ) : RecyclerView.ViewHolder(binding.root)
}
