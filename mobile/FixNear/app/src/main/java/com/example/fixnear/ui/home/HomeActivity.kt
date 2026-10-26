package com.example.fixnear.ui.home

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.example.fixnear.databinding.ActivityHomeBinding

class HomeActivity : AppCompatActivity() {
    private lateinit var binding: ActivityHomeBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        val fullName = intent.getStringExtra("FULL_NAME")
        val userId = intent.getIntExtra("USER_ID", -1)
        val roleType = intent.getIntExtra("ROLE_TYPE",-1)

        binding.tvWelcome.text = "Xin chào, ${fullName ?: "bạn"}"
    }
}