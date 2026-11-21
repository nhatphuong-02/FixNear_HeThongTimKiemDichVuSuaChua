
package com.example.fixnear.data.model.customer

data class CustomerAddress(
    val addressId: Int,
    val provinceId: Int,
    val provinceName: String,
    val wardId: Int,
    val wardName: String,
    val addressLine: String,
    val latitude: Double?,
    val longitude: Double?,
    val recipientName: String?,
    val recipientPhone: String?,
    val isDefault: Boolean,
    val isLocked: Boolean
)

data class CustomerAddressRequest(
    val provinceId: Int,
    val wardId: Int,
    val addressLine: String,
    val recipientName: String,
    val recipientPhone: String
)
