package com.example.fixnear.ui.auth

import android.app.AlertDialog
import android.content.Intent
import android.text.InputType
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.model.ResendOtpRequest
import com.example.fixnear.data.model.VerifyEmailRequest
import com.example.fixnear.data.remote.RetrofitClient
import kotlinx.coroutines.launch
import org.json.JSONObject

object RegistrationOtpHelper {

    fun show(activity: AppCompatActivity, email: String) {

        val input = EditText(activity).apply {
            hint = "Nhập mã OTP 6 chữ số"
            inputType = InputType.TYPE_CLASS_NUMBER
            filters = arrayOf(
                android.text.InputFilter.LengthFilter(6)
            )
            setSingleLine(true)
        }

        val container = LinearLayout(activity).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(42, 12, 42, 0)
            addView(input)
        }

        val dialog = AlertDialog.Builder(activity)
            .setTitle("Xác thực email")
            .setMessage(
                "Mã OTP đã được gửi đến $email.\n" +
                        "Mã có hiệu lực 5 phút."
            )
            .setView(container)
            .setPositiveButton("Xác thực", null)
            .setNeutralButton("Gửi lại mã", null)
            .setNegativeButton("Đóng", null)
            .create()

        dialog.setOnShowListener {

            dialog.getButton(
                AlertDialog.BUTTON_POSITIVE
            ).setOnClickListener {

                val otp = input.text.toString().trim()

                if (!Regex("\\d{6}").matches(otp)) {
                    input.error = "Nhập đủ 6 chữ số"
                    return@setOnClickListener
                }

                activity.lifecycleScope.launch {
                    try {
                        val result =
                            RetrofitClient.authApi.verifyEmail(
                                VerifyEmailRequest(email, otp)
                            )

                        if (result.isSuccessful) {
                            Toast.makeText(
                                activity,
                                "Xác thực thành công! Hãy đăng nhập.",
                                Toast.LENGTH_LONG
                            ).show()

                            dialog.dismiss()

                            val intent = Intent(
                                activity,
                                LoginActivity::class.java
                            ).apply {
                                flags =
                                    Intent.FLAG_ACTIVITY_CLEAR_TOP or
                                            Intent.FLAG_ACTIVITY_SINGLE_TOP
                            }

                            activity.startActivity(intent)
                            activity.finish()

                        } else {
                            Toast.makeText(
                                activity,
                                errorMessage(
                                    result.errorBody()?.string()
                                ),
                                Toast.LENGTH_LONG
                            ).show()
                        }

                    } catch (e: Exception) {
                        Toast.makeText(
                            activity,
                            "Lỗi xác thực: ${e.localizedMessage}",
                            Toast.LENGTH_LONG
                        ).show()
                    }
                }
            }

            dialog.getButton(
                AlertDialog.BUTTON_NEUTRAL
            ).setOnClickListener {

                activity.lifecycleScope.launch {
                    try {
                        val response =
                            RetrofitClient.authApi.resendOtp(
                                ResendOtpRequest(email)
                            )

                        Toast.makeText(
                            activity,
                            if (response.isSuccessful)
                                "Đã gửi lại OTP"
                            else
                                errorMessage(
                                    response.errorBody()?.string()
                                ),
                            Toast.LENGTH_LONG
                        ).show()

                    } catch (e: Exception) {
                        Toast.makeText(
                            activity,
                            "Lỗi gửi OTP: ${e.localizedMessage}",
                            Toast.LENGTH_LONG
                        ).show()
                    }
                }
            }
        }

        dialog.show()
    }

    private fun errorMessage(body: String?): String {
        return try {
            JSONObject(body ?: "{}")
                .optString("message", "Yêu cầu thất bại")
        } catch (_: Exception) {
            "Yêu cầu thất bại"
        }
    }
}
