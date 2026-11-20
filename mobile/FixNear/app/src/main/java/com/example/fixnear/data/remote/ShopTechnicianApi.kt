
package com.example.fixnear.data.remote

import com.google.gson.JsonElement
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

data class InviteTechnicianRequest(
    val email: String
)

data class ManagerAssignTechnicianRequest(
    val repairRequestId: Int,
    val technicianId: Int,
    val note: String? = null
)

interface ShopTechnicianApi {

    // Danh sách kỹ thuật viên thuộc Shop
    @GET("api/shops/{shopId}/technicians")
    suspend fun getTechnicians(
        @Header("Authorization") token: String,
        @Path("shopId") shopId: Int
    ): Response<JsonElement>

    // Danh sách lời mời kỹ thuật viên
    @GET("api/shops/{shopId}/technician-invitations")
    suspend fun getInvitations(
        @Header("Authorization") token: String,
        @Path("shopId") shopId: Int
    ): Response<JsonElement>

    // Chủ Shop gửi lời mời
    @POST("api/shops/{shopId}/technician-invitations")
    suspend fun inviteTechnician(
        @Header("Authorization") token: String,
        @Path("shopId") shopId: Int,
        @Body body: InviteTechnicianRequest
    ): Response<JsonElement>

    // Manager phân công Technician cho RepairRequest
    @POST("api/manager/repair-assignments")
    suspend fun assignTechnician(
        @Header("Authorization") token: String,
        @Query("shopId") shopId: Int,
        @Body body: ManagerAssignTechnicianRequest
    ): Response<JsonElement>
}

object ShopTechnicianNetwork {

    private const val BASE_URL = "http://192.168.1.51:5150/"

    val api: ShopTechnicianApi by lazy {
        Retrofit.Builder()
            .baseUrl(BASE_URL)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ShopTechnicianApi::class.java)
    }
}
