package com.example.fixnear.data.remote

import com.example.fixnear.data.model.ApiResponse
import com.example.fixnear.data.model.technician.*
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface TechnicianApi {

    // UI08 - Lay ho so de xac dinh technicianId
    @GET("api/technician/me/profile")
    suspend fun getMyReviewProfile():
            Response<ApiResponse<TechnicianReviewProfile>>

    // UI08 - Lay danh gia cua Technician
    @GET("api/technicians/{technicianId}/reviews")
    suspend fun getTechnicianReviews(
        @Path("technicianId") technicianId: Int,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 10
    ): Response<ApiResponse<TechnicianReviewPage>>


    @GET("api/technician/assignments")
    suspend fun getAssignments(): Response<ApiResponse<List<TechnicianAssignment>>>

    @GET("api/technician/assignments/{id}")
    suspend fun getAssignment(
        @Path("id") id: Int
    ): Response<ApiResponse<TechnicianAssignment>>

    @PATCH("api/technician/assignments/{id}/accept")
    suspend fun accept(
        @Path("id") id: Int
    ): Response<ActionResponse>

    @PATCH("api/technician/assignments/{id}/reject")
    suspend fun reject(
        @Path("id") id: Int,
        @Body body: RejectRequest
    ): Response<ActionResponse>

    @POST("api/technician/repairs/{rid}/start-survey")
    suspend fun startSurvey(
        @Path("rid") rid: Int
    ): Response<ActionResponse>

    @POST("api/technician/repairs/{rid}/survey")
    suspend fun submitSurvey(
        @Path("rid") rid: Int,
        @Body body: SurveyRequest
    ): Response<ActionResponse>

    @POST("api/technician/repairs/{rid}/start-repair")
    suspend fun startRepair(
        @Path("rid") rid: Int
    ): Response<ActionResponse>

    @POST("api/technician/repairs/{rid}/parts")
    suspend fun recordPart(
        @Path("rid") rid: Int,
        @Body body: PartUsageRequest
    ): Response<ActionResponse>

    @POST("api/technician/repairs/{rid}/complete")
    suspend fun complete(
        @Path("rid") rid: Int,
        @Body body: CompleteRequest
    ): Response<ActionResponse>

    @GET("api/technician/me/schedules")
    suspend fun getSchedules(
        @Query("from") from: String? = null,
        @Query("to") to: String? = null
    ): Response<ApiResponse<List<TechnicianSchedule>>>

    @GET("api/shops/{shopId}/repair-items")
    suspend fun getRepairItems(
        @Path("shopId") shopId: Int,
        @Query("keyword") keyword: String? = null,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 50
    ): Response<PagedResponse<ShopRepairItem>>

    @GET("api/shops/{shopId}/repair-parts")
    suspend fun getRepairParts(
        @Path("shopId") shopId: Int,
        @Query("keyword") keyword: String? = null,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 50
    ): Response<PagedResponse<ShopRepairPart>>

    @GET("api/shops/{shopId}/public-services")
    suspend fun getShopServices(
        @Path("shopId") shopId: Int,
        @Query("keyword") keyword: String? = null,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 50
    ): Response<PagedResponse<ShopServiceItem>>

    @POST("api/quotes")
    suspend fun createQuote(
        @Body body: CreateQuoteRequest
    ): Response<ActionResponse>

    @GET("api/quotes/{id}")
    suspend fun getQuote(
        @Path("id") id: Int
    ): Response<ApiResponse<QuoteResponse>>
}