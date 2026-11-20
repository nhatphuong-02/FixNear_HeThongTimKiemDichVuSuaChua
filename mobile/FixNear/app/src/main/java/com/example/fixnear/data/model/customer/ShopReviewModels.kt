package com.example.fixnear.data.model.customer

data class ShopReviewsEnvelope(val message: String?, val data: ShopReviewPage)

data class ShopReviewPage(
    val shopId: Int,
    val reviewType: Int,
    val totalCount: Int,
    val page: Int,
    val pageSize: Int,
    val reviews: List<ShopReview>
)

data class ShopReview(
    val reviewId: Int,
    val shopId: Int,
    val reviewType: Int,
    val technicianId: Int?,
    val rating: Int,
    val comment: String?,
    val createdAt: String,
    val images: List<ShopReviewImage> = emptyList()
)

data class ShopReviewImage(
    val reviewImageId: Int,
    val imageUrl: String,
    val displayOrder: Int
)
