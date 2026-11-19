package com.example.fixnear.ui.auth

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.example.fixnear.databinding.ActivityChooseRegisterRoleBinding

class ChooseRegisterRoleActivity : AppCompatActivity() {

    private lateinit var binding: ActivityChooseRegisterRoleBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        binding = ActivityChooseRegisterRoleBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.btnCustomer.setOnClickListener {
            startActivity(Intent(this, RegisterActivity::class.java))
        }

        binding.btnShop.setOnClickListener {
            openRoleRegister(2)
        }

        binding.btnTechnician.setOnClickListener {
            openRoleRegister(3)
        }

        binding.btnBack.setOnClickListener {
            finish()
        }
    }

    private fun openRoleRegister(roleType: Int) {
        val intent = Intent(this, RoleRegisterActivity::class.java)
        intent.putExtra("ROLE_TYPE", roleType)
        startActivity(intent)
    }
}
