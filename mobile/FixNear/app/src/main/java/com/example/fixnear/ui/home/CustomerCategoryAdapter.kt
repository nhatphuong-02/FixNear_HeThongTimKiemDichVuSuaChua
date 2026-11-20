package com.example.fixnear.ui.home

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.customer.ServiceCategoryItem
import com.example.fixnear.databinding.ItemCustomerCategoryBinding

class CustomerCategoryAdapter(
    private val onClick: (ServiceCategoryItem) -> Unit
) : RecyclerView.Adapter<CustomerCategoryAdapter.CategoryHolder>() {

    private val items = mutableListOf<ServiceCategoryItem>()
    private var selectedId = -1

    fun submitItems(data: List<ServiceCategoryItem>) {
        items.clear()
        items.addAll(data)
        notifyDataSetChanged()
    }

    fun setSelectedCategory(id: Int) {
        selectedId = id
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): CategoryHolder {
        val binding = ItemCustomerCategoryBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return CategoryHolder(binding)
    }

    override fun onBindViewHolder(
        holder: CategoryHolder,
        position: Int
    ) {
        val item = items[position]
        val binding = holder.binding

        binding.tvCategoryName.text = item.categoryName
        binding.tvCategoryDescription.text =
            item.description ?: "Khám phá dịch vụ"

        // Dùng màu viền để nhận biết danh mục đang chọn.
        val selected = item.serviceCategoryId == selectedId
        val density = binding.root.resources.displayMetrics.density

        binding.root.background =
            android.graphics.drawable.GradientDrawable().apply {
                setColor(0xFFFFF8F1.toInt())
                cornerRadius = 16 * density

                setStroke(
                    (if (selected) 2 else 1).times(density).toInt(),
                    if (selected)
                        0xFFD8975B.toInt()
                    else
                        0xFFE9DED4.toInt()
                )
            }

        binding.root.setOnClickListener {
            onClick(item)
        }
    }

    override fun getItemCount(): Int = items.size

    class CategoryHolder(
        val binding: ItemCustomerCategoryBinding
    ) : RecyclerView.ViewHolder(binding.root)
}
