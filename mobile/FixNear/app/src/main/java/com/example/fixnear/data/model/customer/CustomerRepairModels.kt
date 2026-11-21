package com.example.fixnear.data.model.customer

data class CreateRepairRequest(
    val shopId: Int,
    val serviceId: Int,
    val repairMethod: Int,
    val addressId: Int?,
    val description: String,
    val preferredDate: String,
    val preferredTimeFrom: String,
    val preferredTimeTo: String
)

data class CreateRepairResponse(
    val message: String,
    val repairRequestId: Int,
    val status: Int,
    val statusName: String
)
