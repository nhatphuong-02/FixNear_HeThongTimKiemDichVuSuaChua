package com.example.fixnear.data.model.technician

data class CreateQuoteRequest(
    val repairRequestId: Int,
    val validUntil: String? = null,
    val details: List<CreateQuoteDetailRequest>
)

data class CreateQuoteDetailRequest(
    val itemType: Int,
    val serviceRefId: Int? = null,
    val repairItemRefId: Int? = null,
    val repairPartRefId: Int? = null,
    val description: String? = null,
    val quantity: Double
)