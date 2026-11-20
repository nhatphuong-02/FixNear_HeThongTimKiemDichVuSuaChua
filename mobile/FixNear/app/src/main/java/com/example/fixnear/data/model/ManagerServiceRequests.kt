package com.example.fixnear.data.model

data class AddShopServiceRequest(
    val serviceId: Int,
    val description: String?
)

data class AddServicePriceRequest(
    val shopServiceId: Int,
    val price: Double,
    val minPrice: Double?,
    val maxPrice: Double?,
    val priceUnit: String?
)