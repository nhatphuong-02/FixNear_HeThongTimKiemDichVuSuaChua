
package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.databinding.ActivityHomeBinding
import com.example.fixnear.ui.profile.ProfileActivity

class HomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityHomeBinding
    private lateinit var sessionManager: SessionManager

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        setupGreeting()
        setupNavigation()
    }

    private fun setupGreeting() {
        val fullName = sessionManager.getFullName()

        binding.tvGreeting.text = if (!fullName.isNullOrBlank()) {
            "Xin chào, $fullName!"
        } else {
            "Xin chào!"
        }
    }

    private fun setupNavigation() {
        // Nhấn avatar để mở hồ sơ cá nhân
        binding.tvProfileAvatar.setOnClickListener {
            openProfile()
        }

        // Tab tài khoản cũng mở hồ sơ cá nhân
        binding.navAccount.setOnClickListener {
            openProfile()
        }
    }

    private fun openProfile() {
        val intent = Intent(this, ProfileActivity::class.java)
        startActivity(intent)
    }

    /**
     * Cập nhật lại lời chào khi quay lại Home.
     */
    override fun onResume() {
        super.onResume()

        if (::sessionManager.isInitialized) {
            setupGreeting()
        }
    }
}
