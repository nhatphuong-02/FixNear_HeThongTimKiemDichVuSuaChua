
package com.example.fixnear.ui.manager

import android.graphics.drawable.GradientDrawable
import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.data.model.ManagerTechnicianItem
import com.example.fixnear.databinding.ItemManagerTechnicianBinding

class ManagerTechnicianAdapter(
    private val onSelected: (ManagerTechnicianItem) -> Unit
) : RecyclerView.Adapter<ManagerTechnicianAdapter.Holder>() {

    private val items = mutableListOf<ManagerTechnicianItem>()
    private var selectedId: Int? = null

    fun submitItems(data: List<ManagerTechnicianItem>) {
        items.clear()
        items.addAll(data)
        selectedId = null
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): Holder {
        return Holder(
            ItemManagerTechnicianBinding.inflate(
                LayoutInflater.from(parent.context),
                parent,
                false
            )
        )
    }

    override fun onBindViewHolder(
        holder: Holder,
        position: Int
    ) {
        val item = items[position]
        val b = holder.binding
        val selected = selectedId == item.technicianId

        b.tvName.text = item.fullName
        b.tvInfo.text = item.description.ifBlank {
            "Kỹ thuật viên"
        }

        b.rbSelected.isChecked = selected

        val density = b.root.resources.displayMetrics.density

        b.root.background = GradientDrawable().apply {
            setColor(0xFFFFF8F1.toInt())
            cornerRadius = 16 * density
            setStroke(
                (if (selected) 2 else 0).times(density).toInt(),
                0xFFD8975B.toInt()
            )
        }

        b.root.setOnClickListener {
            selectedId = item.technicianId
            notifyDataSetChanged()
            onSelected(item)
        }
    }

    override fun getItemCount() = items.size

    class Holder(
        val binding: ItemManagerTechnicianBinding
    ) : RecyclerView.ViewHolder(binding.root)
}
