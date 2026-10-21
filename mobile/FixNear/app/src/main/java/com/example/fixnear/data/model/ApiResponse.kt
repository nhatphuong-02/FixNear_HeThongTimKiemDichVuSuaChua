package com.example.fixnear.data.model

class ApiResponse<T>(
    val message : String,
    val data: T? = null
)