package com.example.fixnear.data.model.customer

data class NotificationPage(
    val page: Int,
    val pageSize: Int,
    val total: Int,
    val totalPages: Int,
    val data: List<CustomerNotification>
)

data class CustomerNotification(
    val userNotificationId: Int,
    val notificationId: Int,
    val title: String?,
    val body: String?,
    val refType: Int?,
    val refId: Int?,
    val templateCode: String?,
    val isRead: Boolean,
    val readAt: String?,
    val createdAt: String?
)

data class NotificationUnreadCount(val unreadCount: Int)
