package com.example.fixnear.data.model

data class RegisterRequest(
    val fullName: String,
    val email: String,
    val phone: String,
    val dateOfBirth: String,
    val gender: Int,
    val password: String
)