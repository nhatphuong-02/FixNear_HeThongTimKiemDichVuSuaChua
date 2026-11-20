package com.example.fixnear.ui.manager

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.example.fixnear.databinding.ActivityShopMembersBinding

class ShopMembersActivity : AppCompatActivity() {

    private lateinit var binding: ActivityShopMembersBinding

    private var shopId = -1
    private var shopRole = 0

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityShopMembersBinding.inflate(layoutInflater)
        setContentView(binding.root)

        shopId = intent.getIntExtra("SHOP_ID", -1)
        shopRole = intent.getIntExtra("SHOP_ROLE", 0)

        binding.btnBack.setOnClickListener {
            finish()
        }

        if (shopId <= 0) {
            binding.tvEmpty.text = "Chưa chọn cửa hàng"
            binding.btnAddMember.visibility = View.GONE
            return
        }

        setupRole()
        showEmptyState()
    }

    private fun setupRole() {

        binding.tvRole.text = when (shopRole) {
            1 -> "Quyền: Chủ cửa hàng"
            2 -> "Quyền: Quản lý"
            3 -> "Quyền: Nhân viên"
            else -> "Chưa xác định quyền"
        }

        // Chỉ chủ cửa hàng có thể mở chức năng
        // quản lý quyền thành viên.
        // Đây chỉ là kiểm soát giao diện.
        binding.btnAddMember.visibility =
            if (shopRole == 1) View.VISIBLE else View.GONE

        binding.btnAddMember.setOnClickListener {
            Toast.makeText(
                this,
                "Chức năng mời thành viên chờ backend bổ sung API",
                Toast.LENGTH_LONG
            ).show()
        }
    }

    private fun showEmptyState() {
        binding.tvEmpty.text =
            "Chưa tải được danh sách thành viên.\n" +
                    "Dữ liệu sẽ được kết nối từ backend FixNear."
    }
}
