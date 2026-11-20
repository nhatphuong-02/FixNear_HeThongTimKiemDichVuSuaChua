package com.example.fixnear.ui.home

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.customer.CustomerNotification
import com.example.fixnear.databinding.ItemCustomerNotificationBinding

class CustomerNotificationAdapter(
    private val onClick: (CustomerNotification) -> Unit
) : RecyclerView.Adapter<CustomerNotificationAdapter.Holder>() {
    private val items = mutableListOf<CustomerNotification>()

    fun replace(data: List<CustomerNotification>) {
        items.clear()
        items.addAll(data)
        notifyDataSetChanged()
    }

    fun append(data: List<CustomerNotification>) {
        val start = items.size
        items.addAll(data)
        notifyItemRangeInserted(start, data.size)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder = Holder(
        ItemCustomerNotificationBinding.inflate(LayoutInflater.from(parent.context), parent, false)
    )

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val item = items[position]
        holder.binding.apply {
            tvTitle.text = item.title?.takeIf { it.isNotBlank() } ?: "Thông báo FixNear"
            tvBody.text = item.body?.takeIf { it.isNotBlank() } ?: "Không có nội dung."
            tvTimestamp.text = item.createdAt?.replace('T', ' ')?.take(16).orEmpty()
            tvReadStatus.text = if (item.isRead) "Đã đọc" else "Chưa đọc"
            root.alpha = if (item.isRead) 0.75f else 1f
            root.setOnClickListener { onClick(item) }
        }
    }

    override fun getItemCount() = items.size
    class Holder(val binding: ItemCustomerNotificationBinding) : RecyclerView.ViewHolder(binding.root)
}
