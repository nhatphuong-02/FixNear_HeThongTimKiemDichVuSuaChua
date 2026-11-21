package com.example.fixnear.ui.home

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.example.fixnear.data.model.customer.ShopReview
import com.example.fixnear.databinding.ItemShopReviewBinding
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

class ShopReviewAdapter : RecyclerView.Adapter<ShopReviewAdapter.Holder>() {
    private val items = mutableListOf<ShopReview>()

    fun submit(rows: List<ShopReview>, append: Boolean) {
        if (!append) items.clear()
        items.addAll(rows)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder = Holder(
        ItemShopReviewBinding.inflate(LayoutInflater.from(parent.context), parent, false)
    )

    override fun onBindViewHolder(holder: Holder, position: Int) {
        val b = holder.binding
        val item = items[position]
        b.tvCustomer.text = "Khách hàng"
        b.tvStars.text = "★".repeat(item.rating.coerceIn(0, 5)) +
            "☆".repeat((5 - item.rating).coerceIn(0, 5))
        b.tvComment.text = item.comment?.takeIf { it.isNotBlank() }
            ?: "Khách hàng không để lại nhận xét."
        b.tvDate.text = try {
            OffsetDateTime.parse(item.createdAt)
                .format(DateTimeFormatter.ofPattern("dd/MM/yyyy", Locale("vi", "VN")))
        } catch (_: Exception) {
            item.createdAt.take(10)
        }
        val image = item.images.sortedBy { it.displayOrder }.firstOrNull()?.imageUrl
        b.imgReview.visibility = if (image.isNullOrBlank()) View.GONE else View.VISIBLE
        if (!image.isNullOrBlank()) {
            Glide.with(b.root).load(image).centerCrop().into(b.imgReview)
        } else {
            Glide.with(b.root).clear(b.imgReview)
        }
        b.tvImagesCount.visibility = if (item.images.size > 1) View.VISIBLE else View.GONE
        b.tvImagesCount.text = "+${item.images.size - 1} ảnh khác"
    }

    override fun getItemCount(): Int = items.size

    class Holder(val binding: ItemShopReviewBinding): RecyclerView.ViewHolder(binding.root)
}
