package com.example.fixnear.ui.profile

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.customer.CustomerAddress
import com.example.fixnear.databinding.ItemCustomerAddressBinding

class CustomerAddressAdapter(
    private val onEdit: (CustomerAddress) -> Unit,
    private val onSetDefault: (CustomerAddress) -> Unit
) : RecyclerView.Adapter<CustomerAddressAdapter.Holder>() {

    private val items = mutableListOf<CustomerAddress>()

    fun submitItems(data: List<CustomerAddress>) {
        items.clear()
        items.addAll(data)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): Holder {
        val binding = ItemCustomerAddressBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return Holder(binding)
    }

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val item = items[position]
        val b = holder.binding

        b.tvAddress.text = listOf(
            item.addressLine,
            item.wardName,
            item.provinceName
        ).filter { it.isNotBlank() }.joinToString(", ")

        b.tvRecipient.text = listOf(
            item.recipientName.orEmpty(),
            item.recipientPhone.orEmpty()
        ).filter { it.isNotBlank() }.joinToString(" • ")

        b.tvDefault.visibility =
            if (item.isDefault) View.VISIBLE else View.GONE

        b.tvLocked.visibility =
            if (item.isLocked) View.VISIBLE else View.GONE

        b.btnEditAddress.visibility =
            if (item.isLocked) View.GONE else View.VISIBLE

        b.btnSetDefault.visibility =
            if (item.isLocked || item.isDefault)
                View.GONE
            else
                View.VISIBLE

        b.btnEditAddress.setOnClickListener { onEdit(item) }
        b.btnSetDefault.setOnClickListener { onSetDefault(item) }
    }

    override fun getItemCount(): Int = items.size

    class Holder(
        val binding: ItemCustomerAddressBinding
    ) : RecyclerView.ViewHolder(binding.root)
}
