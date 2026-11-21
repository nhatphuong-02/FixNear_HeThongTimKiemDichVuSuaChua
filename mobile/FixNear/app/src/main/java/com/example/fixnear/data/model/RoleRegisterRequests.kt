package com.example.fixnear.data.model

data class RegisterShopRequest(
    val fullName: String,
    val email: String,
    val phone: String?,
    val password: String,
    val shopName: String,
    val description: String? = null,
    val shopPhone: String? = null,
    val shopEmail: String? = null,
    val taxCode: String? = null,
    val provinceId: Int,
    val wardId: Int,
    val addressLine: String,
    val recipientName: String?,
    val recipientPhone: String?
)

data class RegisterTechnicianRequest(
    val fullName: String,
    val email: String,
    val phone: String?,
    val password: String,
    val experienceYears: Int?,
    val introduction: String?,
    val serviceIds: List<Int> = emptyList()
)

data class VerifyEmailRequest(
    val email: String,
    val otp: String
)

data class ResendOtpRequest(
    val email: String
)