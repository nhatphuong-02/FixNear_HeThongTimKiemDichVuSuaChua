package com.example.fixnear.data.model.customer

data class ShopPublicProfileResponse(
    val data: ShopPublicProfile
)

data class ShopPublicProfile(
    val shopId: Int,
    val shopName: String,
    val description: String?,
    val phone: String?,
    val email: String?,
    val logoUrl: String?,
    val ratingAverage: Double,
    val isVerified: Boolean,
    val address: ShopPublicAddress?,
    val images: List<ShopPublicImage>,
    val serviceCount: Int
)

data class ShopPublicAddress(
    val addressLine: String?,
    val wardName: String?,
    val provinceName: String?,
    val latitude: Double?,
    val longitude: Double?
)

data class ShopPublicImage(
    val shopImageId: Int,
    val imageUrl: String?,
    val caption: String?,
    val isPrimary: Boolean,
    val displayOrder: Int
)

data class ShopPublicServicesResponse(
    val shopId: Int,
    val page: Int,
    val pageSize: Int,
    val total: Int,
    val totalPages: Int,
    val data: List<ShopPublicServiceItem>
)

data class ShopPublicServiceItem(
    val shopServiceId: Int,
    val serviceId: Int,
    val serviceName: String,
    val categoryId: Int,
    val categoryName: String,
    val serviceDescription: String?,
    val shopDescription: String?,
    val currentPrice: ShopPublicServicePrice?
)

data class ShopPublicServicePrice(
    val price: Double?,
    val minPrice: Double?,
    val maxPrice: Double?,
    val priceUnit: String?
)
