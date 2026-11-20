package com.example.fixnear.data.remote

import android.content.Context
import com.example.fixnear.data.local.SessionManager
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private const val BASE_URL = "http://192.168.1.149:5150/"

    private lateinit var session: SessionManager

    // Khởi tạo SessionManager từ Application
    fun init(context: Context) {
        session = SessionManager(context.applicationContext)
    }

    // Retrofit dành cho API không cần Token
    private val publicClient by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    // OkHttp tự động gắn JWT Token
    private val authedClient by lazy {
        OkHttpClient.Builder()
            .addInterceptor { chain ->

                val token = if (::session.isInitialized) {
                    session.getToken()
                } else {
                    null
                }

                val request = chain.request()
                    .newBuilder()
                    .apply {
                        if (!token.isNullOrBlank()) {
                            addHeader(
                                "Authorization",
                                "Bearer $token"
                            )
                        }
                    }
                    .build()

                chain.proceed(request)
            }
            .build()
    }

    // Retrofit dành cho API có Token
    private fun <T> authed(clazz: Class<T>): T {
        return Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(authedClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(clazz)
    }

    // API cũ - giữ nguyên
    val authApi: AuthApi by lazy {
        publicClient.create(AuthApi::class.java)
    }

    val customerApi: CustomerApi by lazy {
        publicClient.create(CustomerApi::class.java)
    }

    val locationApi: LocationApi by lazy {
        publicClient.create(LocationApi::class.java)
    }

    // API Technician - tự động gửi Bearer Token
    val technicianApi: TechnicianApi by lazy {
        authed(TechnicianApi::class.java)
    }
}