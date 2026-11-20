package com.example.fixnear.data.remote

import com.example.fixnear.data.model.customer.CustomerAddress
import com.example.fixnear.data.model.customer.CustomerAddressRequest
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path

interface CustomerAddressApi {

    @GET("api/customers/me/addresses")
    suspend fun getAddresses(
        @Header("Authorization") token: String
    ): Response<List<CustomerAddress>>

    @POST("api/customers/me/addresses")
    suspend fun addAddress(
        @Header("Authorization") token: String,
        @Body request: CustomerAddressRequest
    ): Response<CustomerAddress>

    @PUT("api/customers/me/addresses/{addressId}")
    suspend fun updateAddress(
        @Header("Authorization") token: String,
        @Path("addressId") addressId: Int,
        @Body request: CustomerAddressRequest
    ): Response<CustomerAddress>

    @PUT("api/customers/me/addresses/{addressId}/default")
    suspend fun setDefaultAddress(
        @Header("Authorization") token: String,
        @Path("addressId") addressId: Int
    ): Response<ResponseBody>
}
