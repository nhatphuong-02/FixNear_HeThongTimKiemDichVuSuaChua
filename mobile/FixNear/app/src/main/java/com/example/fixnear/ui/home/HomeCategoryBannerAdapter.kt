package com.example.fixnear.ui.home

import android.graphics.drawable.GradientDrawable
import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.customer.ServiceCategoryItem
import com.example.fixnear.databinding.ItemHomeCategoryBannerBinding

class HomeCategoryBannerAdapter(
    private val onCategoryClick: (Int?) -> Unit
) : RecyclerView.Adapter<HomeCategoryBannerAdapter.Holder>() {

    private val categories = mutableListOf<ServiceCategoryItem>()

    fun submitCategories(data: List<ServiceCategoryItem>) {
        categories.clear()
        categories.addAll(data.take(4))
        notifyDataSetChanged()
    }

    override fun getItemCount(): Int = categories.size + 1

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): Holder {
        val binding = ItemHomeCategoryBannerBinding.inflate(
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
        val b = holder.binding
        val allCategoriesCard = position == categories.size

        val backgroundColor =
            if (allCategoriesCard) 0xFF302925.toInt()
            else 0xFFFFF8F1.toInt()

        val titleColor =
            if (allCategoriesCard) 0xFFFFF4EA.toInt()
            else 0xFF251A14.toInt()

        val subtitleColor =
            if (allCategoriesCard) 0xFFC7B9AF.toInt()
            else 0xFF8B7160.toInt()

        val accentColor = 0xFFD8975B.toInt()

        val radius = 18f * b.root.resources.displayMetrics.density

        b.root.background = GradientDrawable().apply {
            setColor(backgroundColor)
            cornerRadius = radius
        }

        b.tvCategoryName.setTextColor(titleColor)
        b.tvCategoryDescription.setTextColor(subtitleColor)
        b.tvCategoryIcon.setTextColor(accentColor)
        b.tvCategoryAction.setTextColor(accentColor)

        if (allCategoriesCard) {
            b.tvCategoryIcon.text = "▦"
            b.tvCategoryName.text = "Tất cả danh mục"
            b.tvCategoryDescription.text =
                "Khám phá đầy đủ dịch vụ FixNear"
            b.tvCategoryAction.text = "Xem tất cả ›"

            b.root.setOnClickListener {
                onCategoryClick(null)
            }
        } else {
            val item = categories[position]

            b.tvCategoryIcon.text = "⚙"
            b.tvCategoryName.text = item.categoryName
            b.tvCategoryDescription.text =
                item.description
                    ?.takeIf { it.isNotBlank() }
                    ?: "Khám phá các dịch vụ sửa chữa"

            b.tvCategoryAction.text = "Xem dịch vụ ›"

            b.root.setOnClickListener {
                onCategoryClick(item.serviceCategoryId)
            }
        }
    }

    class Holder(
        val binding: ItemHomeCategoryBannerBinding
    ) : RecyclerView.ViewHolder(binding.root)
}
