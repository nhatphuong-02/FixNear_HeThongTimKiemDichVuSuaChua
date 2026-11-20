package com.example.fixnear.data.remote

import com.example.fixnear.data.model.customer.NotificationPage
import com.example.fixnear.data.model.customer.NotificationUnreadCount
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.PATCH
import retrofit2.http.Path
import retrofit2.http.Query

interface CustomerNotificationsApi {
    @GET("api/notifications/me")
    suspend fun list(
        @Header("Authorization") bearer: String,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 10,
        @Query("isRead") isRead: Boolean? = null
    ): Response<NotificationPage>

    @GET("api/notifications/me/unread-count")
    suspend fun unreadCount(
        @Header("Authorization") bearer: String
    ): Response<NotificationUnreadCount>

    @PATCH("api/notifications/me/{id}/read")
    suspend fun markRead(
        @Header("Authorization") bearer: String,
        @Path("id") userNotificationId: Int
    ): Response<ResponseBody>

    @PATCH("api/notifications/me/read-all")
    suspend fun markAllRead(
        @Header("Authorization") bearer: String
    ): Response<ResponseBody>
}
