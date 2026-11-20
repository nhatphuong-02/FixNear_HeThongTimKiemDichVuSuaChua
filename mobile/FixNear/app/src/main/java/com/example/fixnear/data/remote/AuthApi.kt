package com.example.fixnear.data.remote

import com.example.fixnear.data.model.ApiResponse
import com.example.fixnear.data.model.LoginRequest
import com.example.fixnear.data.model.LoginResponse
import com.example.fixnear.data.model.RegisterRequest
import com.example.fixnear.data.model.RegisterShopRequest
import com.example.fixnear.data.model.RegisterTechnicianRequest
import com.example.fixnear.data.model.VerifyEmailRequest
import com.example.fixnear.data.model.ResendOtpRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface AuthApi {

    @POST("api/auth/login")
    suspend fun login(
        @Body request: LoginRequest
    ): Response<ApiResponse<LoginResponse>>

    @POST("api/auth/register/customer")
    suspend fun register(
        @Body request: RegisterRequest
    ): Response<ApiResponse<Any>>

    @POST("api/auth/register/shop")
    suspend fun registerShop(
        @Body request: RegisterShopRequest
    ): Response<ApiResponse<Any>>

    @POST("api/auth/register/technician")
    suspend fun registerTechnician(
        @Body request: RegisterTechnicianRequest
    ): Response<ApiResponse<Any>>

    @POST("api/auth/verify-email")
    suspend fun verifyEmail(
        @Body request: VerifyEmailRequest
    ): Response<ApiResponse<Any>>

    @POST("api/auth/resend-otp")
    suspend fun resendOtp(
        @Body request: ResendOtpRequest
    ): Response<ApiResponse<Any>>
}