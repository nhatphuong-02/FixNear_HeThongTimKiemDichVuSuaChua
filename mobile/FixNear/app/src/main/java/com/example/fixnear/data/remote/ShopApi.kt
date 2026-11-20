package com.example.fixnear.data.remote

import com.example.fixnear.data.model.ApiResponse
import com.example.fixnear.data.model.ShopListResponse
import com.example.fixnear.data.model.ShopProfileResponse
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Path

interface ShopApi {

    @GET("api/shops/me")
    suspend fun getMyShops(
        @Header("Authorization") authorization: String
    ): Response<ApiResponse<List<ShopListResponse>>>

    @GET("api/shops/{shopId}/profile")
    suspend fun getShopProfile(
        @Header("Authorization") authorization: String,
        @Path("shopId") shopId: Int
    ): Response<ApiResponse<ShopProfileResponse>>
}
