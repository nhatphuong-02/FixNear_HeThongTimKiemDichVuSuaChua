package com.example.fixnear.data.remote

import com.example.fixnear.data.model.customer.ServiceCategoryItem
import com.example.fixnear.data.model.customer.CustomerServiceItem
import retrofit2.Response
import retrofit2.http.GET

interface CustomerCatalogApi {

    @GET("api/service-categories")
    suspend fun getCategories(): Response<List<ServiceCategoryItem>>

    @GET("api/services")
    suspend fun getServices(): Response<List<CustomerServiceItem>>
}