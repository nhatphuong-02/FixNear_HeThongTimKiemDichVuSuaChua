package com.example.fixnear.data.model.customer

data class ChatPage<T>(val page:Int, val pageSize:Int, val total:Int, val totalPages:Int, val data:List<T>)
data class ChatLastMessage(val messageId:Int, val senderUserId:Int, val content:String?, val createdAt:String?)
data class ChatConversation(val conversationId:Int, val repairRequestId:Int?, val createdAt:String?, val lastMessage:ChatLastMessage?)
data class ChatMember(val userId:Int, val fullName:String?, val avatarUrl:String?)
data class ChatConversationDetail(val conversationId:Int, val repairRequestId:Int?, val members:List<ChatMember>)
data class ChatAttachment(val messageAttachmentId:Int, val fileType:String?)
data class ChatMessage(val messageId:Int, val conversationId:Int, val senderUserId:Int, val senderName:String?, val content:String?, val createdAt:String?, val attachments:List<ChatAttachment>)
data class ChatUnreadCount(val unreadCount:Int)
data class ChatConversationCreate(val repairRequestId:Int)
data class ChatConversationCreated(val conversationId:Int, val repairRequestId:Int)
data class ChatSendMessage(val content:String)
