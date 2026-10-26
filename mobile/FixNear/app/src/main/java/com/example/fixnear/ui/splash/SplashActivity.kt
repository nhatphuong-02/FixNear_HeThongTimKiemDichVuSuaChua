package com.example.fixnear.ui.splash

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.databinding.ActivitySplashBinding
import com.example.fixnear.ui.auth.LoginActivity
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

class SplashActivity : AppCompatActivity() {

    private lateinit var binding: ActivitySplashBinding

    override fun onCreate(savedInstanceState: Bundle?) {

        // Phải gọi trước super.onCreate()
        installSplashScreen()

        super.onCreate(savedInstanceState)

        // Hiển thị giao diện activity_splash.xml
        binding = ActivitySplashBinding.inflate(layoutInflater)
        setContentView(binding.root)

        // Tạm thời hiển thị Splash 1,2 giây rồi chuyển sang Login
        lifecycleScope.launch {
            delay(1200)

            startActivity(
                Intent(this@SplashActivity, LoginActivity::class.java)
            )

            finish()
        }
    }
}