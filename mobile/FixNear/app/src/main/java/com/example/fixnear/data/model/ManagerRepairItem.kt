package com.example.fixnear.data.model

data class ManagerRepairItem(
    val repairRequestId: Int,
    val customerName: String,
    val description: String,
    val statusCode: String,
    val statusLabel: String,
    val createdAt: String
)