package com.example.fixnear.data.remote

import com.example.fixnear.data.model.customer.CreateRepairRequest
import com.example.fixnear.data.model.customer.CreateRepairResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.Header
import retrofit2.http.POST

interface CustomerRepairApi {
    @POST("api/customers/me/repair-requests")
    suspend fun createRepairRequest(
        @Header("Authorization") token: String,
        @Body request: CreateRepairRequest
    ): Response<CreateRepairResponse>
}
