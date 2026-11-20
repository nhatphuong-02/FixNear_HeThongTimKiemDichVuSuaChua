package com.example.fixnear.ui.home

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.customer.CustomerServiceItem
import com.example.fixnear.databinding.ItemCustomerServiceBinding

class CustomerServiceAdapter(
    private val onClick: (CustomerServiceItem) -> Unit
) : RecyclerView.Adapter<CustomerServiceAdapter.ServiceHolder>() {

    private val items = mutableListOf<CustomerServiceItem>()

    fun submitItems(data: List<CustomerServiceItem>) {
        items.clear()
        items.addAll(data)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): ServiceHolder {
        val binding = ItemCustomerServiceBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return ServiceHolder(binding)
    }

    override fun onBindViewHolder(
        holder: ServiceHolder,
        position: Int
    ) {
        val service = items[position]
        val binding = holder.binding

        binding.tvServiceName.text = service.serviceName
        binding.tvServiceCategory.text = service.categoryName

        binding.tvServiceDescription.text =
            service.description
                ?.takeIf { it.isNotBlank() }
                ?: "Xem thông tin dịch vụ sửa chữa"

        binding.root.setOnClickListener {
            onClick(service)
        }
    }

    override fun getItemCount(): Int = items.size

    class ServiceHolder(
        val binding: ItemCustomerServiceBinding
    ) : RecyclerView.ViewHolder(binding.root)
}
