package com.example.fixnear.data.remote

import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object RetrofitClient {

    private const val BASE_URL = "http://192.168.1.51:5150/"

    private val retrofit: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    val authApi: AuthApi by lazy { retrofit.create(AuthApi::class.java) }
    val customerApi: CustomerApi by lazy { retrofit.create(CustomerApi::class.java) }
    val locationApi: LocationApi by lazy { retrofit.create(LocationApi::class.java) }
    val shopApi: ShopApi by lazy { retrofit.create(ShopApi::class.java) }
    val managerServiceApi: ManagerServiceApi by lazy { retrofit.create(ManagerServiceApi::class.java) }
    val customerCatalogApi: CustomerCatalogApi by lazy { retrofit.create(CustomerCatalogApi::class.java) }
    val customerDiscoveryApi: CustomerDiscoveryApi by lazy { retrofit.create(CustomerDiscoveryApi::class.java) }
    val customerAddressApi: CustomerAddressApi by lazy { retrofit.create(CustomerAddressApi::class.java) }
    val customerRepairQueryApi: CustomerRepairQueryApi by lazy { retrofit.create(CustomerRepairQueryApi::class.java) }
    val customerRepairApi: CustomerRepairApi by lazy { retrofit.create(CustomerRepairApi::class.java) }
    val customerQuotesApi: CustomerQuotesApi by lazy { retrofit.create(CustomerQuotesApi::class.java) }
    val shopPublicApi: ShopPublicApi by lazy { retrofit.create(ShopPublicApi::class.java) }
    val customerNotificationsApi: CustomerNotificationsApi by lazy { retrofit.create(CustomerNotificationsApi::class.java) }
    val customerChatApi: CustomerChatApi by lazy { retrofit.create(CustomerChatApi::class.java) }
    val customerBillingReviewApi: CustomerBillingReviewApi by lazy { retrofit.create(CustomerBillingReviewApi::class.java) }
    val managerRepairApi: ManagerRepairApi by lazy { retrofit.create(ManagerRepairApi::class.java) }
}
