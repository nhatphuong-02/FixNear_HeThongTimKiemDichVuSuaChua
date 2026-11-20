package com.example.fixnear.data.remote

import com.google.gson.JsonObject
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Path
import retrofit2.http.Query

interface CustomerRepairQueryApi {
    @GET("api/customers/me/repair-requests")
    suspend fun getRequests(
        @Header("Authorization") token: String,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 10,
        @Query("status") status: Int? = null,
        @Query("keyword") keyword: String? = null
    ): Response<JsonObject>

    @GET("api/customers/me/repair-requests/{repairRequestId}")
    suspend fun getDetail(
        @Header("Authorization") token: String,
        @Path("repairRequestId") id: Int
    ): Response<JsonObject>

    @GET("api/customers/me/upcoming-schedules")
    suspend fun getUpcoming(@Header("Authorization") token: String): Response<JsonObject>
}
