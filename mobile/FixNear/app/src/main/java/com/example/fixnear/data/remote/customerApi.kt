package com.example.fixnear.data.remote

import com.example.fixnear.data.model.customer.CustomerProfileResponse
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header

interface CustomerApi {

    @GET("api/Customer/me")
    suspend fun getProfile(
        @Header("Authorization") token: String
    ): Response<CustomerProfileResponse>
}
