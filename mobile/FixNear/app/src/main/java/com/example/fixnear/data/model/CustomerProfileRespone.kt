package com.example.fixnear.data.model.customer

data class CustomerProfileResponse(
    val customerId: Int,
    val userId: Int,
    val avatarUrl: String?,
    val fullName: String,
    val phone: String?,
    val email: String?,
    val dateOfBirth: String?,
    val gender: Int?,
    val roleType: Int?,
    val defaultAddressId: Int?
)
