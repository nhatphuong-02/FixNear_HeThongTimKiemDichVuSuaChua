package com.example.fixnear.ui.manager

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.databinding.ItemShopTechnicianBinding

data class ShopTechnicianDisplay(
    val name: String,
    val email: String,
    val status: String,
    val extra: String
)

class ShopTechnicianAdapter :
    RecyclerView.Adapter<ShopTechnicianAdapter.Holder>() {

    private val items = mutableListOf<ShopTechnicianDisplay>()

    class Holder(
        val binding: ItemShopTechnicianBinding
    ) : RecyclerView.ViewHolder(binding.root)

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): Holder {
        val binding = ItemShopTechnicianBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return Holder(binding)
    }

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val item = items[position]

        holder.binding.tvName.text = item.name
        holder.binding.tvEmail.text = item.email
        holder.binding.tvStatus.text = item.status
        holder.binding.tvExtra.text = item.extra
    }

    override fun getItemCount(): Int = items.size

    fun submitItems(newItems: List<ShopTechnicianDisplay>) {
        items.clear()
        items.addAll(newItems)
        notifyDataSetChanged()
    }
}
