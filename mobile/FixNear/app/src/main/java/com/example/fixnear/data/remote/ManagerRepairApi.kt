
package com.example.fixnear.data.remote

import com.example.fixnear.data.model.AcceptRepairRequest
import com.example.fixnear.data.model.RejectRepairRequest
import com.google.gson.JsonElement
import com.google.gson.JsonObject
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.PATCH
import retrofit2.http.Path
import retrofit2.http.Query
import retrofit2.http.POST

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

    @PATCH("api/manager/repair-requests/{id}/accept")
    suspend fun acceptRepair(
        @Header("Authorization") token: String,
        @Path("id") repairRequestId: Int,
        @Query("shopId") shopId: Int,
        @Body request: AcceptRepairRequest
    ): Response<JsonElement>

    @PATCH("api/manager/repair-requests/{id}/reject")
    suspend fun rejectRepair(
        @Header("Authorization") token: String,
        @Path("id") repairRequestId: Int,
        @Query("shopId") shopId: Int,
        @Body request: RejectRepairRequest
    ): Response<JsonElement>

    @GET("api/manager/repair-schedules")
    suspend fun getRepairSchedules(
        @Header("Authorization") token: String,
        @Query("shopId") shopId: Int,
        @Query("includeHistory") includeHistory: Boolean = false
    ): Response<JsonElement>

    @POST("api/manager/repair-assignments")
    suspend fun assignTechnician(
        @Header("Authorization") token: String,
        @Query("shopId") shopId: Int,
        @Body request: JsonObject
    ): Response<JsonElement>
}
