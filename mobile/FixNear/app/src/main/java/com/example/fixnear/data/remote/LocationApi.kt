package com.example.fixnear.data.remote

import com.example.fixnear.data.model.ProvinceOption
import com.example.fixnear.data.model.WardOption
import retrofit2.http.GET
import retrofit2.http.Query

interface LocationApi {

    @GET("api/locations/provinces")
    suspend fun getProvinces(): List<ProvinceOption>

    @GET("api/locations/wards")
    suspend fun getWards(
        @Query("provinceId") provinceId: Int
    ): List<WardOption>
}