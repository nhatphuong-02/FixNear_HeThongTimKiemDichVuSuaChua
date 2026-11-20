package com.example.fixnear.data.model.customer

data class NearbyShopItem(
    val shopId: Int,
    val shopName: String,
    val description: String?,
    val phone: String?,
    val logoUrl: String?,
    val ratingAverage: Double,
    val address: String,
    val distanceKm: Double
)