package com.example.fixnear.data.remote

import android.content.Context
import com.example.fixnear.data.local.SessionManager
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private const val BASE_URL = "http://192.168.1.16:5150/"

    private lateinit var session: SessionManager

    // Khoi tao SessionManager tu Application
    fun init(context: Context) {
        session = SessionManager(context.applicationContext)
    }

    // Client cho API cong khai
    private val publicClient: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    // Tu dong gan JWT neu da dang nhap
    private val authHttpClient: OkHttpClient by lazy {
        OkHttpClient.Builder()
            .addInterceptor { chain ->

                val token = if (::session.isInitialized) {
                    session.getToken()
                } else {
                    null
                }

                val requestBuilder = chain.request().newBuilder()

                if (!token.isNullOrBlank()) {
                    requestBuilder.header(
                        "Authorization",
                        "Bearer $token"
                    )
                }

                chain.proceed(requestBuilder.build())
            }
            .build()
    }

    // Retrofit cho API can xac thuc
    private val authenticatedClient: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(authHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    private fun <T> authed(clazz: Class<T>): T {
        return authenticatedClient.create(clazz)
    }

    // ==============================
    // AUTH
    // ==============================

    val authApi: AuthApi by lazy {
        publicClient.create(AuthApi::class.java)
    }

    // ==============================
    // LOCATION
    // ==============================

    val locationApi: LocationApi by lazy {
        publicClient.create(LocationApi::class.java)
    }

    // ==============================
    // CUSTOMER
    // ==============================

    val customerApi: CustomerApi by lazy {
        authed(CustomerApi::class.java)
    }

    val customerCatalogApi: CustomerCatalogApi by lazy {
        publicClient.create(CustomerCatalogApi::class.java)
    }

    val customerDiscoveryApi: CustomerDiscoveryApi by lazy {
        publicClient.create(CustomerDiscoveryApi::class.java)
    }

    val customerAddressApi: CustomerAddressApi by lazy {
        authed(CustomerAddressApi::class.java)
    }

    val customerRepairQueryApi: CustomerRepairQueryApi by lazy {
        authed(CustomerRepairQueryApi::class.java)
    }

    val customerRepairApi: CustomerRepairApi by lazy {
        authed(CustomerRepairApi::class.java)
    }

    val customerQuotesApi: CustomerQuotesApi by lazy {
        authed(CustomerQuotesApi::class.java)
    }

    val customerNotificationsApi: CustomerNotificationsApi by lazy {
        authed(CustomerNotificationsApi::class.java)
    }

    val customerChatApi: CustomerChatApi by lazy {
        authed(CustomerChatApi::class.java)
    }

    val customerBillingReviewApi: CustomerBillingReviewApi by lazy {
        authed(CustomerBillingReviewApi::class.java)
    }

    // ==============================
    // SHOP
    // ==============================

    val shopApi: ShopApi by lazy {
        authed(ShopApi::class.java)
    }

    val shopPublicApi: ShopPublicApi by lazy {
        publicClient.create(ShopPublicApi::class.java)
    }

    // ==============================
    // MANAGER
    // ==============================

    val managerServiceApi: ManagerServiceApi by lazy {
        authed(ManagerServiceApi::class.java)
    }

    val managerRepairApi: ManagerRepairApi by lazy {
        authed(ManagerRepairApi::class.java)
    }

    // ==============================
    // TECHNICIAN
    // ==============================

    val technicianApi: TechnicianApi by lazy {
        authed(TechnicianApi::class.java)
    }
}
