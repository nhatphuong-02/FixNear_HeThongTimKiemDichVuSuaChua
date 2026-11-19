package com.example.fixnear.data.model

data class ProvinceOption(
    val provinceId: Int,
    val provinceName: String
)

data class WardOption(
    val wardId: Int,
    val provinceId: Int,
    val wardName: String
)
