package com.example.fixnear.data.remote

import com.example.fixnear.data.model.customer.ShopReviewsEnvelope
import com.example.fixnear.data.model.customer.ShopPublicProfileResponse
import com.example.fixnear.data.model.customer.ShopPublicServicesResponse
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

interface ShopPublicApi {
    @GET("api/shops/{shopId}/reviews")
    suspend fun getShopReviews(
        @Path("shopId") shopId: Int,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 10
    ): Response<ShopReviewsEnvelope>

    @GET("api/shops/{shopId}/public-profile")
    suspend fun getProfile(
        @Path("shopId") shopId: Int
    ): Response<ShopPublicProfileResponse>

    @GET("api/shops/{shopId}/public-services")
    suspend fun getServices(
        @Path("shopId") shopId: Int,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 10,
        @Query("keyword") keyword: String? = null
    ): Response<ShopPublicServicesResponse>
}
