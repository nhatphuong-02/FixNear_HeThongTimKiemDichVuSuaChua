package com.example.fixnear.ui.home

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.example.fixnear.R
import com.example.fixnear.data.model.customer.NearbyShopItem
import com.example.fixnear.databinding.ItemNearbyShopBinding
import java.util.Locale

class NearbyShopAdapter(
    private val onShopClick: (NearbyShopItem) -> Unit = {}
) : RecyclerView.Adapter<NearbyShopAdapter.Holder>() {

    private val items = mutableListOf<NearbyShopItem>()

    fun submitItems(data: List<NearbyShopItem>) {
        items.clear()
        items.addAll(data)
        notifyDataSetChanged()
    }

    fun appendItems(data: List<NearbyShopItem>) {
        val start = items.size
        items.addAll(data)
        notifyItemRangeInserted(start, data.size)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder {
        return Holder(
            ItemNearbyShopBinding.inflate(
                LayoutInflater.from(parent.context),
                parent,
                false
            )
        )
    }

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val item = items[position]
        val b = holder.binding

        b.root.setOnClickListener { onShopClick(item) }

        b.tvShopName.text = item.shopName
        b.tvDistance.text = String.format(Locale("vi", "VN"), "%.1f km", item.distanceKm)
        b.tvRating.text = String.format(Locale("vi", "VN"), "★ %.1f", item.ratingAverage)
        b.tvShopAddress.text = item.address
        b.tvShopDescription.text = item.description.orEmpty()
        b.tvShopDescription.visibility = if (item.description.isNullOrBlank()) View.GONE else View.VISIBLE
        b.tvShopPhone.text = if (item.phone.isNullOrBlank()) {
            "Chưa cập nhật số điện thoại"
        } else {
            "Liên hệ: ${item.phone}"
        }

        if (item.logoUrl.isNullOrBlank()) {
            b.imgShopLogo.setImageResource(R.drawable.logo_fixnear)
        } else {
            Glide.with(b.root)
                .load(item.logoUrl)
                .centerCrop()
                .placeholder(R.drawable.logo_fixnear)
                .error(R.drawable.logo_fixnear)
                .into(b.imgShopLogo)
        }
    }

    override fun getItemCount(): Int = items.size

    class Holder(val binding: ItemNearbyShopBinding) : RecyclerView.ViewHolder(binding.root)
}
