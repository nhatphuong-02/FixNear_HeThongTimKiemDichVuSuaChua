package com.example.fixnear.data.model

data class ManagerScheduleItem(
    val scheduleId: Int,
    val repairRequestId: Int,
    val technicianName: String,
    val customerName: String,
    val description: String,
    val startTime: String,
    val endTime: String,
    val status: String
)