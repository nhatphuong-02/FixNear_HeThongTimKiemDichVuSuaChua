package com.example.fixnear.ui.home

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.model.customer.ChatConversation
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerConversationsBinding
import kotlinx.coroutines.launch

class CustomerConversationsActivity:AppCompatActivity(){
    private lateinit var b:ActivityCustomerConversationsBinding
    private val items=mutableListOf<ChatConversation>()
    private var page=0
    private var pages=1
    private var busy=false
    private var version=0

    override fun onCreate(savedInstanceState:Bundle?){
        super.onCreate(savedInstanceState)
        b=ActivityCustomerConversationsBinding.inflate(layoutInflater)
        setContentView(b.root)
        b.btnBack.setOnClickListener{finish()}
        b.btnRefresh.setOnClickListener{refresh()}
        b.btnLoadMore.setOnClickListener{load(page+1)}
        refresh()
    }
    override fun onResume(){super.onResume(); if(page>0) refresh()}
    private fun refresh(){version++;busy=false;page=0;pages=1;items.clear();b.container.removeAllViews();load(1)}
    private fun load(next:Int){
        if(busy || next>pages)return
        val token=ChatUi.token(this)?:run{b.tvState.text="Vui lòng đăng nhập lại.";return}
        val requestVersion=version
        busy=true;b.progress.visibility=View.VISIBLE;b.btnLoadMore.visibility=View.GONE
        lifecycleScope.launch {
            try {
                val response=RetrofitClient.customerChatApi.list(token,next,10)
                if(requestVersion!=version)return@launch
                if(!response.isSuccessful){b.tvState.text="Không tải được hội thoại (HTTP ${response.code()}).";return@launch}
                val data=response.body()?:return@launch
                items.addAll(data.data);page=data.page;pages=data.totalPages
                render()
                b.tvState.text=if(data.total==0)"Chưa có hội thoại. Mở một yêu cầu sửa chữa để bắt đầu chat." else "${data.total} hội thoại • Trang $page/$pages"
            }catch(e:Exception){if(requestVersion==version)b.tvState.text="Lỗi kết nối: ${e.localizedMessage}"
            }finally{if(requestVersion==version){busy=false;b.progress.visibility=View.GONE;b.btnLoadMore.visibility=if(page<pages)View.VISIBLE else View.GONE}}
        }
    }
    private fun render(){
        b.container.removeAllViews()
        items.forEach { item ->
            val card=ChatUi.row(this)
            card.addView(ChatUi.text(this,"Yêu cầu #${item.repairRequestId ?: "—"}",17f,ChatUi.GOLD,true))
            card.addView(ChatUi.text(this,item.lastMessage?.content?.take(140) ?: "Chưa có tin nhắn",14f))
            card.addView(ChatUi.text(this,ChatUi.readableDate(item.lastMessage?.createdAt ?: item.createdAt),12f,"#C7B9AF"))
            card.setOnClickListener{startActivity(Intent(this,CustomerChatActivity::class.java).putExtra("CONVERSATION_ID",item.conversationId))}
            b.container.addView(card,ChatUi.margin(this))
        }
    }
}
