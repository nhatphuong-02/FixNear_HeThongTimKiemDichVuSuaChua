
package com.example.fixnear.data.model.technician

import com.example.fixnear.data.model.customer.ShopReview

data class TechnicianReviewProfile(
    val technicianId: Int,
    val ratingAverage: Double
)

data class TechnicianReviewPage(
    val technicianId: Int,
    val totalCount: Int,
    val page: Int,
    val pageSize: Int,
    val reviews: List<ShopReview>
)
