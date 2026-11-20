package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerUpcomingSchedulesBinding
import com.google.gson.JsonObject
import kotlinx.coroutines.launch

class CustomerUpcomingSchedulesActivity : AppCompatActivity() {
    private lateinit var binding: ActivityCustomerUpcomingSchedulesBinding
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityCustomerUpcomingSchedulesBinding.inflate(layoutInflater)
        setContentView(binding.root)
        binding.btnBack.setOnClickListener { finish() }
        binding.btnRefresh.setOnClickListener { load() }
        load()
    }
    private fun load() {
        val raw = SessionManager(this).getToken()
        if (raw.isNullOrBlank()) { binding.tvState.text = "Vui lòng đăng nhập lại."; return }
        val token = if (raw.startsWith("Bearer ", true)) raw else "Bearer $raw"
        binding.progress.visibility = View.VISIBLE
        binding.content.removeAllViews()
        binding.tvState.text = "Đang tải lịch..."
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerRepairQueryApi.getUpcoming(token)
                if (!response.isSuccessful) { binding.tvState.text = "Lỗi HTTP ${response.code()}"; return@launch }
                val root = response.body() ?: return@launch
                val items = root.getAsJsonArray("data")
                binding.tvState.text = if (items == null || items.size() == 0) "Chưa có lịch sửa chữa sắp tới." else "${items.size()} lịch sắp tới (tối đa 5)"
                items?.forEach { element ->
                    if (!element.isJsonObject) return@forEach
                    val obj: JsonObject = element.asJsonObject
                    val requestId = RepairUi.number(obj, "repairRequestId")
                    val body = RepairUi.column(this@CustomerUpcomingSchedulesActivity)
                    RepairUi.addLine(this@CustomerUpcomingSchedulesActivity, body, RepairUi.field(obj, "shopName"), 17f, RepairUi.WHITE, true)
                    RepairUi.addLine(this@CustomerUpcomingSchedulesActivity, body, "Yêu cầu #$requestId  •  ${RepairUi.status(RepairUi.number(obj, "repairStatus"))}", 13f, RepairUi.GOLD)
                    RepairUi.addLine(this@CustomerUpcomingSchedulesActivity, body, "Bắt đầu: ${RepairUi.date(RepairUi.field(obj, "startTime"))}")
                    RepairUi.addLine(this@CustomerUpcomingSchedulesActivity, body, "Kết thúc: ${RepairUi.date(RepairUi.field(obj, "endTime"))}")
                    RepairUi.addLine(this@CustomerUpcomingSchedulesActivity, body, "Kỹ thuật viên: ${RepairUi.field(obj, "technicianName")}", 13f, RepairUi.MUTED)
                    val services = obj.getAsJsonArray("serviceNames")?.mapNotNull { try { it.asString } catch (_: Exception) { null } }.orEmpty()
                    RepairUi.addLine(this@CustomerUpcomingSchedulesActivity, body, services.joinToString(" • "), 13f, RepairUi.MUTED)
                    val card = RepairUi.card(this@CustomerUpcomingSchedulesActivity, body)
                    card.setOnClickListener {
                        startActivity(Intent(this@CustomerUpcomingSchedulesActivity, CustomerRepairDetailActivity::class.java)
                            .putExtra("REPAIR_REQUEST_ID", requestId))
                    }
                    binding.content.addView(card)
                }
            } catch (e: Exception) { binding.tvState.text = "Lỗi kết nối: ${e.localizedMessage}" }
            finally { binding.progress.visibility = View.GONE }
        }
    }
}
