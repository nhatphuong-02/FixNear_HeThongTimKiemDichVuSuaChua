package com.example.fixnear.data.model

class LoginResponse (
    val userId : Int,
    val fullName : String,
    val roleType : Int,
    val token : String? = null
)