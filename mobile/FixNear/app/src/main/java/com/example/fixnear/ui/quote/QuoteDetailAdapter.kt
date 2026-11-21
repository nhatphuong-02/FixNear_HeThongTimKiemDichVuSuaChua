
package com.example.fixnear.ui.quote

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.recyclerview.widget.RecyclerView
import com.example.fixnear.R
import java.text.NumberFormat
import java.util.Locale

data class QuoteDraftItem(
    val itemType: Int,
    val referenceId: Int,
    val name: String,
    val quantity: Double,
    val unitPrice: Double?,
    val description: String? = null,
    val serverAmount: Double? = null
) {
    val estimatedAmount: Double?
        get() {
            val price = unitPrice ?: return null

            if (!price.isFinite() || price < 0.0) {
                return null
            }

            if (!quantity.isFinite() || quantity <= 0.0) {
                return null
            }

            val amount = price * quantity

            return amount.takeIf {
                it.isFinite()
            }
        }
}

class QuoteDetailAdapter(
    private val readOnly: Boolean = false,
    private val onRemove: (QuoteDraftItem) -> Unit
) : RecyclerView.Adapter<QuoteDetailAdapter.QuoteViewHolder>() {

    private val items = mutableListOf<QuoteDraftItem>()

    fun submitList(newItems: List<QuoteDraftItem>) {
        val updatedItems = newItems.toList()
        val oldSize = items.size

        if (oldSize > 0) {
            items.clear()
            notifyItemRangeRemoved(0, oldSize)
        }

        if (updatedItems.isNotEmpty()) {
            items.addAll(updatedItems)
            notifyItemRangeInserted(
                0,
                updatedItems.size
            )
        }
    }

    inner class QuoteViewHolder(
        itemView: View
    ) : RecyclerView.ViewHolder(itemView) {

        private val tvName: TextView =
            itemView.findViewById(R.id.tvQuoteItemName)

        private val tvType: TextView =
            itemView.findViewById(R.id.tvQuoteItemType)

        private val tvQuantity: TextView =
            itemView.findViewById(R.id.tvQuoteItemQuantity)

        private val tvUnitPrice: TextView =
            itemView.findViewById(R.id.tvQuoteItemUnitPrice)

        private val tvAmount: TextView =
            itemView.findViewById(R.id.tvQuoteItemAmount)

        private val btnRemove: TextView =
            itemView.findViewById(R.id.btnRemoveQuoteItem)

        fun bind(item: QuoteDraftItem) {

            tvName.text = item.name

            tvType.text = when (item.itemType) {
                1 -> "Dịch vụ"
                2 -> "Hạng mục sửa chữa"
                3 -> "Linh kiện / Vật tư"
                else -> "Hạng mục khác"
            }

            tvQuantity.text =
                "Số lượng: ${formatQuantity(item.quantity)}"

            val price = item.unitPrice

            tvUnitPrice.text =
                if (price != null &&
                    price.isFinite() &&
                    price >= 0.0
                ) {
                    "Đơn giá: ${formatMoney(price)}"
                } else {
                    "Đơn giá: Chưa xác định"
                }

            val amount = (
                    if (readOnly) item.serverAmount
                    else item.estimatedAmount
                    ).takeIf { it != null && it.isFinite() && it >= 0.0 }

            tvAmount.text =
                if (readOnly) "Không có thành tiền"
                else "Chờ Backend xác nhận"

            btnRemove.setOnClickListener {
                onRemove(item)
            }
            btnRemove.visibility =
                if (readOnly) View.GONE else View.VISIBLE

            btnRemove.setOnClickListener {
                if (!readOnly) {
                    onRemove(item)
                }
            }
        }
    }

    override fun onCreateViewHolder(
        parent: ViewGroup,
        viewType: Int
    ): QuoteViewHolder {

        val view = LayoutInflater.from(
            parent.context
        ).inflate(
            R.layout.item_quote_detail,
            parent,
            false
        )

        return QuoteViewHolder(view)
    }

    override fun onBindViewHolder(
        holder: QuoteViewHolder,
        position: Int
    ) {
        holder.bind(items[position])
    }

    override fun getItemCount(): Int {
        return items.size
    }

    private fun formatMoney(value: Double): String {
        val formatter = NumberFormat.getNumberInstance(
            Locale.forLanguageTag("vi-VN")
        )

        formatter.maximumFractionDigits = 2

        return "${formatter.format(value)} đ"
    }

    private fun formatQuantity(value: Double): String {
        val formatter = NumberFormat.getNumberInstance(
            Locale.forLanguageTag("vi-VN")
        )

        formatter.maximumFractionDigits = 3

        return formatter.format(value)
    }
}
