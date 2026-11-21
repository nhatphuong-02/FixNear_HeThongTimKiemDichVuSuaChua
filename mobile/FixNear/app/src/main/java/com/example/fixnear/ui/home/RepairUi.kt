package com.example.fixnear.ui.home

import android.content.Context
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.view.View
import android.widget.LinearLayout
import android.widget.TextView
import com.google.gson.JsonObject
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

internal object RepairUi {
    val GOLD = Color.parseColor("#D8975B")
    val WHITE = Color.parseColor("#FFF8F1")
    val MUTED = Color.parseColor("#C7B9AF")
    fun dp(ctx: Context, amount: Int) = (ctx.resources.displayMetrics.density * amount).toInt()
    fun bg(ctx: Context, fill: String = "#29221E", stroke: String = "#493A30"): GradientDrawable =
        GradientDrawable().apply {
            setColor(Color.parseColor(fill))
            cornerRadius = dp(ctx, 16).toFloat()
            setStroke(dp(ctx, 1), Color.parseColor(stroke))
        }
    fun text(ctx: Context, value: String, size: Float = 14f, color: Int = WHITE, bold: Boolean = false): TextView =
        TextView(ctx).apply {
            text = value
            textSize = size
            setTextColor(color)
            if (bold) setTypeface(null, Typeface.BOLD)
        }
    fun field(obj: JsonObject?, key: String): String = try {
        obj?.get(key)?.takeUnless { it.isJsonNull }?.asString.orEmpty()
    } catch (_: Exception) { "" }
    fun number(obj: JsonObject?, key: String, fallback: Int = 0): Int = try {
        obj?.get(key)?.asInt ?: fallback
    } catch (_: Exception) { fallback }
    fun obj(obj: JsonObject?, key: String): JsonObject? = try {
        obj?.getAsJsonObject(key)
    } catch (_: Exception) { null }
    fun status(code: Int): String = when (code) {
        1 -> "Chờ Shop phản hồi"
        2 -> "Đã tiếp nhận"
        3 -> "Shop từ chối"
        4 -> "Đã phân công kỹ thuật viên"
        5 -> "Đang khảo sát"
        6 -> "Đã gửi báo giá"
        7 -> "Đã duyệt báo giá"
        8 -> "Đang sửa chữa"
        9 -> "Đã sửa xong"
        10 -> "Đã thanh toán"
        11 -> "Đã hủy"
        else -> "Trạng thái $code"
    }
    fun date(value: String): String {
        if (value.isBlank()) return "Chưa cập nhật"
        return try {
            val temporal = OffsetDateTime.parse(value)
            temporal.atZoneSameInstant(ZoneId.of("Asia/Ho_Chi_Minh")).format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm"))
        } catch (_: Exception) {
            try { LocalDateTime.parse(value).format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm")) }
            catch (_: Exception) { value.replace("T", " ").take(19) }
        }
    }
    fun card(ctx: Context, content: LinearLayout, margin: Int = 10): LinearLayout =
        LinearLayout(ctx).apply {
            orientation = LinearLayout.VERTICAL
            background = bg(ctx)
            setPadding(dp(ctx, 16), dp(ctx, 14), dp(ctx, 16), dp(ctx, 14))
            val lp = LinearLayout.LayoutParams(-1, -2)
            lp.bottomMargin = dp(ctx, margin)
            layoutParams = lp
            addView(content)
        }
    fun column(ctx: Context) = LinearLayout(ctx).apply { orientation = LinearLayout.VERTICAL }
    fun addLine(ctx: Context, parent: LinearLayout, value: String, size: Float = 14f, color: Int = WHITE, bold: Boolean = false) {
        val label = text(ctx, value, size, color, bold)
        val lp = LinearLayout.LayoutParams(-1, -2)
        lp.bottomMargin = dp(ctx, 6)
        parent.addView(label, lp)
    }
}
