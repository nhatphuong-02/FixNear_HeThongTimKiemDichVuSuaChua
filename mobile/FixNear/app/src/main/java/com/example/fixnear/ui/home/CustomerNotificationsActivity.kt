package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.customer.CustomerNotification
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerNotificationsBinding
import kotlinx.coroutines.launch

class CustomerNotificationsActivity : AppCompatActivity() {
    private lateinit var binding: ActivityCustomerNotificationsBinding
    private lateinit var adapter: CustomerNotificationAdapter
    private var page = 0
    private var totalPages = 1
    private var busy = false
    private var generation = 0
    private var filter: Boolean? = null

    private fun auth(): String? = SessionManager(this).getToken()?.takeIf { it.isNotBlank() }
        ?.let { if (it.startsWith("Bearer ", true)) it else "Bearer $it" }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityCustomerNotificationsBinding.inflate(layoutInflater)
        setContentView(binding.root)
        adapter = CustomerNotificationAdapter { notification -> onNotificationClick(notification) }
        binding.rvNotifications.layoutManager = LinearLayoutManager(this)
        binding.rvNotifications.adapter = adapter
        binding.btnBack.setOnClickListener { finish() }
        binding.btnRefresh.setOnClickListener { resetAndLoad() }
        binding.btnLoadMore.setOnClickListener { loadPage(page + 1) }
        binding.btnMarkAll.setOnClickListener { markAllRead() }
        binding.btnAll.setOnClickListener { changeFilter(null) }
        binding.btnUnread.setOnClickListener { changeFilter(false) }
        binding.btnRead.setOnClickListener { changeFilter(true) }
        highlightFilter()
        resetAndLoad()
    }

    override fun onResume() {
        super.onResume()
        if (page > 0) resetAndLoad()
    }

    private fun changeFilter(newFilter: Boolean?) {
        if (filter == newFilter) return
        filter = newFilter
        highlightFilter()
        resetAndLoad()
    }

    private fun highlightFilter() {
        binding.btnAll.alpha = if (filter == null) 1f else 0.6f
        binding.btnUnread.alpha = if (filter == false) 1f else 0.6f
        binding.btnRead.alpha = if (filter == true) 1f else 0.6f
    }

    private fun resetAndLoad() {
        generation++
        busy = false
        page = 0
        totalPages = 1
        adapter.replace(emptyList())
        loadPage(1)
        loadUnreadCount()
    }

    private fun loadPage(nextPage: Int) {
        if (busy || nextPage > totalPages) return
        val token = auth() ?: run { binding.tvState.text = "Hãy đăng nhập lại."; return }
        val requestId = generation
        busy = true
        binding.progress.visibility = View.VISIBLE
        binding.btnLoadMore.visibility = View.GONE
        binding.tvState.text = "Đang tải thông báo..."
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerNotificationsApi.list(token, nextPage, 10, filter)
                if (requestId != generation) return@launch
                if (!response.isSuccessful) {
                    binding.tvState.text = "Không tải được thông báo (HTTP ${response.code()})."
                    return@launch
                }
                val body = response.body() ?: run {
                    binding.tvState.text = "Dữ liệu thông báo rỗng."
                    return@launch
                }
                page = body.page
                totalPages = body.totalPages
                if (nextPage == 1) adapter.replace(body.data) else adapter.append(body.data)
                binding.tvState.text = if (body.total == 0) "Chưa có thông báo phù hợp." else "${body.total} thông báo • Trang $page/$totalPages"
            } catch (e: Exception) {
                if (requestId == generation) binding.tvState.text = "Lỗi kết nối: ${e.localizedMessage}"
            } finally {
                if (requestId == generation) {
                    busy = false
                    binding.progress.visibility = View.GONE
                    binding.btnLoadMore.visibility = if (page > 0 && page < totalPages) View.VISIBLE else View.GONE
                }
            }
        }
    }

    private fun loadUnreadCount() {
        val token = auth() ?: return
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerNotificationsApi.unreadCount(token)
                if (response.isSuccessful) {
                    binding.tvUnreadCount.text = "${response.body()?.unreadCount ?: 0} chưa đọc"
                }
            } catch (_: Exception) { }
        }
    }

    private fun markAllRead() {
        val token = auth() ?: run { toast("Hãy đăng nhập lại."); return }
        binding.btnMarkAll.isEnabled = false
        lifecycleScope.launch {
            try {
                val response = RetrofitClient.customerNotificationsApi.markAllRead(token)
                if (response.isSuccessful) {
                    toast("Đã đánh dấu tất cả đã đọc")
                    resetAndLoad()
                } else toast("Không đánh dấu được (HTTP ${response.code()})")
            } catch (e: Exception) { toast("Lỗi kết nối: ${e.localizedMessage}") }
            finally { binding.btnMarkAll.isEnabled = true }
        }
    }

    private fun onNotificationClick(item: CustomerNotification) {
        val token = auth() ?: run { toast("Hãy đăng nhập lại."); return }
        lifecycleScope.launch {
            if (!item.isRead) {
                try {
                    val response = RetrofitClient.customerNotificationsApi.markRead(token, item.userNotificationId)
                    if (!response.isSuccessful) {
                        toast("Không đánh dấu được thông báo (HTTP ${response.code()}).")
                        return@launch
                    }
                } catch (e: Exception) {
                    toast("Không thể đánh dấu thông báo: ${e.localizedMessage}")
                    return@launch
                }
            }
            val targetId = item.refId ?: -1
            when {
                item.refType == 2 && targetId > 0 -> startActivity(
                    Intent(this@CustomerNotificationsActivity, CustomerRepairDetailActivity::class.java)
                        .putExtra("REPAIR_REQUEST_ID", targetId)
                )
                item.refType == 5 && targetId > 0 -> startActivity(
                    Intent(this@CustomerNotificationsActivity, CustomerQuoteDetailActivity::class.java)
                        .putExtra("QUOTE_ID", targetId)
                )
                else -> toast("Đã đọc thông báo")
            }
            resetAndLoad()
        }
    }

    private fun toast(message: String) = Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
}
