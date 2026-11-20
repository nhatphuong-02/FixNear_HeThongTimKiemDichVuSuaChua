package com.example.fixnear.data.remote

import com.example.fixnear.data.model.AddShopServiceRequest
import com.example.fixnear.data.model.AddServicePriceRequest
import com.google.gson.JsonElement
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface ManagerServiceApi {

    @GET("api/manager/services")
    suspend fun getSystemServices(
        @Header("Authorization") token: String
    ): Response<JsonElement>

    @GET("api/manager/shops/{shopId}/services")
    suspend fun getShopServices(
        @Header("Authorization") token: String,
        @Path("shopId") shopId: Int,
        @Query("managerId") managerId: Int
    ): Response<JsonElement>

    @POST("api/manager/shops/{shopId}/services")
    suspend fun addShopService(
        @Header("Authorization") token: String,
        @Path("shopId") shopId: Int,
        @Query("managerId") managerId: Int,
        @Body request: AddShopServiceRequest
    ): Response<JsonElement>

    @GET("api/manager/service-prices")
    suspend fun getServicePrices(
        @Header("Authorization") token: String,
        @Query("managerId") managerId: Int,
        @Query("shopId") shopId: Int,
        @Query("includeHistory") includeHistory: Boolean = false
    ): Response<JsonElement>

    @POST("api/manager/service-prices")
    suspend fun addServicePrice(
        @Header("Authorization") token: String,
        @Query("managerId") managerId: Int,
        @Query("shopId") shopId: Int,
        @Body request: AddServicePriceRequest
    ): Response<JsonElement>
}