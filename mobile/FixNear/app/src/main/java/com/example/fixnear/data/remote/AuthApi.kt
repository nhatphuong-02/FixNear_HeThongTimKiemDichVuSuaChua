package com.example.fixnear.data.remote

import com.example.fixnear.data.model.ApiResponse
import com.example.fixnear.data.model.LoginRequest
import com.example.fixnear.data.model.LoginResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.POST

interface AuthApi {

    @POST("api/Auth/login")
    suspend fun login(@Body request: LoginRequest): Response<ApiResponse<LoginResponse>>
}