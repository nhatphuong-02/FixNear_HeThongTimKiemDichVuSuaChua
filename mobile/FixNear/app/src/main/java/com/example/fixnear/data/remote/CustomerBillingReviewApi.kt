package com.example.fixnear.data.remote

import com.google.gson.JsonObject
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path

interface CustomerBillingReviewApi {
    @GET("api/customers/me/repair-requests/{id}/invoice")
    suspend fun findInvoice(@Header("Authorization") token: String, @Path("id") requestId: Int): Response<JsonObject>

    @GET("api/invoices/{id}")
    suspend fun invoice(@Header("Authorization") token: String, @Path("id") invoiceId: Int): Response<JsonObject>

    @POST("api/payments")
    suspend fun createPayment(@Header("Authorization") token: String, @Body body: PaymentCreateBody): Response<JsonObject>

    @GET("api/payments/{id}")
    suspend fun payment(@Header("Authorization") token: String, @Path("id") paymentId: Int): Response<JsonObject>

    @POST("api/reviews")
    suspend fun createReview(@Header("Authorization") token: String, @Body body: ReviewCreateBody): Response<JsonObject>
}

data class PaymentCreateBody(val invoiceId: Int, val paymentMethod: Int)

data class ReviewCreateBody(
    val repairRequestId: Int,
    val reviewType: Int,
    val rating: Int,
    val comment: String?,
    val technicianId: Int? = null,
    val imageUrls: List<String> = emptyList()
)
