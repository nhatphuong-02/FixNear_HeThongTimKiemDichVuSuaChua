package com.example.fixnear.data.remote

import com.example.fixnear.data.model.customer.*
import retrofit2.Response
import retrofit2.http.*

interface CustomerChatApi {
    @GET("api/conversations")
    suspend fun list(@Header("Authorization") token:String, @Query("page") page:Int, @Query("pageSize") pageSize:Int=10): Response<ChatPage<ChatConversation>>

    @POST("api/conversations")
    suspend fun create(@Header("Authorization") token:String, @Body request:ChatConversationCreate):Response<ChatConversationCreated>

    @GET("api/conversations/{id}")
    suspend fun detail(@Header("Authorization") token:String, @Path("id") id:Int):Response<ChatConversationDetail>

    @GET("api/conversations/{id}/messages")
    suspend fun messages(@Header("Authorization") token:String, @Path("id") id:Int, @Query("page") page:Int, @Query("pageSize") pageSize:Int=20):Response<ChatPage<ChatMessage>>

    @POST("api/conversations/{id}/messages")
    suspend fun send(@Header("Authorization") token:String, @Path("id") id:Int, @Body body:ChatSendMessage):Response<com.google.gson.JsonObject>

    @PATCH("api/conversations/{id}/read")
    suspend fun markRead(@Header("Authorization") token:String, @Path("id") id:Int):Response<com.google.gson.JsonObject>

    @GET("api/conversations/unread-count")
    suspend fun unreadCount(@Header("Authorization") token:String):Response<ChatUnreadCount>
}
