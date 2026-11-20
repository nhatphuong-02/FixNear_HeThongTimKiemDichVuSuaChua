package com.example.fixnear.data.remote

import com.google.gson.JsonObject
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.PATCH
import retrofit2.http.Path
import retrofit2.http.Query

interface CustomerQuotesApi {
    @GET("api/customers/me/repair-requests/{id}/quotes")
    suspend fun list(
        @Header("Authorization") token: String,
        @Path("id") repairRequestId: Int,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 10
    ): Response<JsonObject>

    @GET("api/quotes/{id}")
    suspend fun detail(
        @Header("Authorization") token: String,
        @Path("id") quoteId: Int
    ): Response<JsonObject>

    @PATCH("api/quotes/{id}/accept")
    suspend fun accept(
        @Header("Authorization") token: String,
        @Path("id") quoteId: Int
    ): Response<JsonObject>

    @PATCH("api/quotes/{id}/reject")
    suspend fun reject(
        @Header("Authorization") token: String,
        @Path("id") quoteId: Int
    ): Response<JsonObject>
}
