
package com.example.fixnear.ui.review

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.example.fixnear.R
import com.example.fixnear.data.model.customer.ShopReview

class TechnicianReviewAdapter :
    RecyclerView.Adapter<TechnicianReviewAdapter.ReviewViewHolder>() {

    private val items = mutableListOf<ShopReview>()

    fun submitReviews(
        reviews: List<ShopReview>,
        append: Boolean = false
    ) {
        if (!append) {
            items.clear()
        }

        items.addAll(reviews)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): ReviewViewHolder {

        val view = LayoutInflater.from(parent.context)
            .inflate(
                R.layout.item_technician_review,
                parent,
                false
            )

        return ReviewViewHolder(view)
    }

    override fun onBindViewHolder(
        holder: ReviewViewHolder,
        position: Int
    ) {
        val item = items[position]

        // TEN KHACH HANG
        holder.tvCustomer.text = "Khách hàng"

        // NGAY DANH GIA
        holder.tvDate.text = formatReviewDate(
            item.createdAt
        )

        // SO SAO DANH GIA
        val rating = item.rating.coerceIn(0, 5)

        holder.tvStars.text =
            "★".repeat(rating) +
                    "☆".repeat(5 - rating)

        // NOI DUNG NHAN XET
        holder.tvComment.text =
            item.comment?.takeIf {
                it.isNotBlank()
            } ?: "Khách hàng không để lại nhận xét."

        // ANH DANH GIA
        val images = item.images.sortedBy {
            it.displayOrder
        }

        val firstImage = images.firstOrNull()
            ?.imageUrl
            ?.takeIf { it.isNotBlank() }

        if (firstImage != null) {

            holder.imgReview.visibility = View.VISIBLE

            Glide.with(holder.itemView)
                .load(firstImage)
                .centerCrop()
                .into(holder.imgReview)

        } else {

            Glide.with(holder.itemView)
                .clear(holder.imgReview)

            holder.imgReview.visibility = View.GONE
        }

        // SO ANH CON LAI
        val remainingImages = images.size - 1

        if (remainingImages > 0) {

            holder.tvImagesCount.visibility =
                View.VISIBLE

            holder.tvImagesCount.text =
                "+$remainingImages ảnh khác"

        } else {

            holder.tvImagesCount.visibility =
                View.GONE
        }
    }

    override fun getItemCount(): Int = items.size

    // Ho tro Android API 24 tro len
    private fun formatReviewDate(
        dateString: String
    ): String {

        val date = dateString
            .substringBefore("T")
            .take(10)

        val parts = date.split("-")

        return if (parts.size == 3) {

            val year = parts[0]
            val month = parts[1]
            val day = parts[2]

            "$day/$month/$year"

        } else {

            dateString.take(10)
        }
    }

    class ReviewViewHolder(
        itemView: View
    ) : RecyclerView.ViewHolder(itemView) {

        val tvCustomer: TextView =
            itemView.findViewById(
                R.id.tvTechnicianReviewCustomer
            )

        val tvDate: TextView =
            itemView.findViewById(
                R.id.tvTechnicianReviewDate
            )

        val tvStars: TextView =
            itemView.findViewById(
                R.id.tvTechnicianReviewStars
            )

        val tvComment: TextView =
            itemView.findViewById(
                R.id.tvTechnicianReviewComment
            )

        val imgReview: ImageView =
            itemView.findViewById(
                R.id.imgTechnicianReview
            )

        val tvImagesCount: TextView =
            itemView.findViewById(
                R.id.tvTechnicianReviewImagesCount
            )
    }
}
