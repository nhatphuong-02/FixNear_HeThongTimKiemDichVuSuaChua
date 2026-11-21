package com.example.fixnear.ui.home

import android.content.Context
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.view.Gravity
import android.widget.LinearLayout
import android.widget.TextView
import com.example.fixnear.data.local.SessionManager

internal object ChatUi {
    const val GOLD = "#D8975B"
    private fun dp(context:Context, n:Int)=(n*context.resources.displayMetrics.density).toInt()
    fun token(context:Context):String? = SessionManager(context).getToken()?.takeIf { it.isNotBlank() }
        ?.let { if (it.startsWith("Bearer ",true)) it else "Bearer $it" }
    fun card(context:Context, dark:Boolean=true):GradientDrawable = GradientDrawable().apply {
        setColor(Color.parseColor(if (dark) "#29221E" else "#FFF4EA")); cornerRadius=dp(context,16).toFloat()
        setStroke(dp(context,1),Color.parseColor(if (dark) "#46372C" else "#D4B899"))
    }
    fun text(context:Context, value:String, size:Float=14f, color:String="#FFF8F1", bold:Boolean=false):TextView = TextView(context).apply {
        text=value; textSize=size; setTextColor(Color.parseColor(color)); if (bold) setTypeface(null,Typeface.BOLD)
        setPadding(dp(context,3),dp(context,4),dp(context,3),dp(context,4))
    }
    fun row(context:Context)=LinearLayout(context).apply {orientation=LinearLayout.VERTICAL; setPadding(dp(context,14),dp(context,12),dp(context,14),dp(context,12)); background=card(context)}
    fun margin(context:Context):LinearLayout.LayoutParams=LinearLayout.LayoutParams(-1,-2).apply {bottomMargin=dp(context,10)}
    fun readableDate(value:String?):String = value?.replace("T"," ")?.take(16) ?: ""
}
