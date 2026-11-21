package com.example.fixnear.data.remote

import com.example.fixnear.data.model.customer.CustomerProfileResponse
import com.example.fixnear.data.model.customer.UpdateCustomerProfileRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.PUT

interface CustomerApi {

    @GET("api/customers/me/profile")
    suspend fun getProfile(
        @Header("Authorization") token: String
    ): Response<CustomerProfileResponse>

    @PUT("api/customers/me/profile")
    suspend fun updateProfile(
        @Header("Authorization") token: String,
        @Body request: UpdateCustomerProfileRequest
    ): Response<okhttp3.ResponseBody>
}