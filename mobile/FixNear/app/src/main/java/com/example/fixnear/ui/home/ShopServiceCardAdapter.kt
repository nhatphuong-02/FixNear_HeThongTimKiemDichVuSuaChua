package com.example.fixnear.ui.home

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.customer.ShopPublicServiceItem
import com.example.fixnear.databinding.ItemShopServiceCardBinding
import java.text.NumberFormat
import java.util.Locale

class ShopServiceCardAdapter(
    private val onClick: (ShopPublicServiceItem) -> Unit
) : RecyclerView.Adapter<ShopServiceCardAdapter.Holder>() {

    private val items = mutableListOf<ShopPublicServiceItem>()
    private var selectedServiceId: Int? = null

    fun submitItems(data: List<ShopPublicServiceItem>, append: Boolean = false) {
        if (!append) items.clear()
        items.addAll(data)
        notifyDataSetChanged()
    }

    fun setSelectedService(serviceId: Int?) {
        selectedServiceId = serviceId
        notifyDataSetChanged()
    }

    fun getSelectedItem(): ShopPublicServiceItem? {
        return items.firstOrNull { it.serviceId == selectedServiceId }
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder {
        return Holder(
            ItemShopServiceCardBinding.inflate(
                LayoutInflater.from(parent.context),
                parent,
                false
            )
        )
    }

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val item = items[position]
        val b = holder.binding
        b.tvServiceName.text = item.serviceName
        b.tvCategory.text = item.categoryName
        b.tvDescription.text = item.shopDescription?.takeIf { it.isNotBlank() }
            ?: item.serviceDescription?.takeIf { it.isNotBlank() }
            ?: "Liên hệ để nhận tư vấn chi tiết"
        b.tvPrice.text = formatPrice(item)
        val selected = item.serviceId == selectedServiceId
        b.root.isSelected = selected
        b.tvSelectedState.text = if (selected) "Đã chọn" else "Chạm để chọn"
        b.root.setOnClickListener {
            selectedServiceId = item.serviceId
            notifyDataSetChanged()
            onClick(item)
        }
    }

    override fun getItemCount(): Int = items.size

    private fun formatPrice(item: ShopPublicServiceItem): String {
        val price = item.currentPrice ?: return "Liên hệ để nhận báo giá"
        val formatter = NumberFormat.getNumberInstance(Locale("vi", "VN"))
        return when {
            price.price != null -> {
                buildString {
                    append(formatter.format(price.price))
                    append(" đ")
                    if (!price.priceUnit.isNullOrBlank()) append(" / ${price.priceUnit}")
                }
            }
            price.minPrice != null && price.maxPrice != null -> {
                buildString {
                    append(formatter.format(price.minPrice))
                    append(" - ")
                    append(formatter.format(price.maxPrice))
                    append(" đ")
                    if (!price.priceUnit.isNullOrBlank()) append(" / ${price.priceUnit}")
                }
            }
            else -> "Liên hệ để nhận báo giá"
        }
    }

    class Holder(val binding: ItemShopServiceCardBinding) : RecyclerView.ViewHolder(binding.root)
}
