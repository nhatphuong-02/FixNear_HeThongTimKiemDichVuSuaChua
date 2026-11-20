package com.example.fixnear.data.remote

import com.google.gson.JsonElement
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Path
import retrofit2.http.Query

interface ManagerRepairApi {

    @GET("api/manager/repair-requests")
    suspend fun getRepairRequests(
        @Header("Authorization") token: String,
        @Query("shopId") shopId: Int
    ): Response<JsonElement>

    @GET("api/manager/repair-requests/{id}")
    suspend fun getRepairDetail(
        @Header("Authorization") token: String,
        @Path("id") repairRequestId: Int,
        @Query("shopId") shopId: Int
    ): Response<JsonElement>
}
