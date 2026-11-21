package com.example.fixnear.data.model

data class AcceptRepairRequest(
    val note: String? = null
)

data class RejectRepairRequest(
    val reason: String
)