package com.example.fixnear.data.model.customer

data class ServiceCategoryItem(
    val serviceCategoryId: Int,
    val categoryName: String,
    val description: String?
)

data class CustomerServiceItem(
    val serviceId: Int,
    val categoryId: Int,
    val categoryName: String,
    val serviceName: String,
    val description: String?
)