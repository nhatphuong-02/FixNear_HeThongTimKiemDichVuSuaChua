package com.example.fixnear.data.model

data class ShopListResponse(
    val shopId: Int,
    val shopName: String,
    val description: String?,
    val logoUrl: String?,
    val phone: String?,
    val email: String?,
    val isVerified: Boolean,
    val currentUserRole: Int
)

data class ShopProfileResponse(
    val shopId: Int,
    val managerId: Int,
    val addressId: Int,
    val shopName: String,
    val description: String?,
    val phone: String?,
    val email: String?,
    val logoUrl: String?,
    val taxCode: String?,
    val ratingAverage: Double,
    val status: Int,
    val isVerified: Boolean,
    val currentUserRole: Int
)
