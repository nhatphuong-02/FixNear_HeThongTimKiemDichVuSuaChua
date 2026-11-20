package com.example.fixnear.ui.home

import android.os.Bundle
import android.view.Gravity
import android.view.View
import android.widget.LinearLayout
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.example.fixnear.data.local.SessionManager
import com.example.fixnear.data.model.customer.ChatMessage
import com.example.fixnear.data.model.customer.ChatSendMessage
import com.example.fixnear.data.remote.RetrofitClient
import com.example.fixnear.databinding.ActivityCustomerChatBinding
import kotlinx.coroutines.launch

class CustomerChatActivity:AppCompatActivity(){
    private lateinit var b:ActivityCustomerChatBinding
    private val messages=mutableListOf<ChatMessage>()
    private var conversationId=-1
    private var page=0
    private var totalPages=1
    private var busy=false
    private var sending=false
    private var version=0

    override fun onCreate(savedInstanceState:Bundle?){
        super.onCreate(savedInstanceState)
        b=ActivityCustomerChatBinding.inflate(layoutInflater)
        setContentView(b.root)
        conversationId=intent.getIntExtra("CONVERSATION_ID",-1)
        b.btnBack.setOnClickListener{finish()}
        b.btnRefresh.setOnClickListener{refresh()}
        b.btnOlder.setOnClickListener{load(page+1)}
        b.btnSend.setOnClickListener{send()}
        if(conversationId<=0){b.tvState.text="Hội thoại không hợp lệ.";b.btnSend.isEnabled=false;return}
        loadDetail()
        refresh()
    }
    private fun loadDetail(){
        val token=ChatUi.token(this)?:return
        lifecycleScope.launch{try{
            val response=RetrofitClient.customerChatApi.detail(token,conversationId)
            if(response.isSuccessful){
                val info=response.body()
                val own=SessionManager(this@CustomerChatActivity).getUserId()
                val others=info?.members.orEmpty().filter{it.userId!=own}.mapNotNull{it.fullName}
                b.tvTitle.text=if(others.isEmpty())"Hội thoại #$conversationId" else others.joinToString(", ")
                b.tvSubtitle.text="Yêu cầu #${info?.repairRequestId ?: "—"}"
            }
        }catch(_:Exception){}}
    }
    private fun refresh(){version++;busy=false;page=0;totalPages=1;messages.clear();b.container.removeAllViews();load(1)}
    private fun load(next:Int){
        if(busy||next>totalPages)return
        val token=ChatUi.token(this)?:run{b.tvState.text="Hãy đăng nhập lại.";return}
        busy=true
        val requestVersion=version
        b.progress.visibility=View.VISIBLE;b.btnOlder.visibility=View.GONE
        lifecycleScope.launch{try{
            val response=RetrofitClient.customerChatApi.messages(token,conversationId,next,20)
            if(requestVersion!=version)return@launch
            if(!response.isSuccessful){b.tvState.text="Không tải được tin nhắn (HTTP ${response.code()}).";return@launch}
            val result=response.body()?:return@launch
            page=result.page;totalPages=result.totalPages
            // Backend trả mới nhất trước; UI hiển thị cũ nhất ở trên.
            messages.addAll(result.data)
            render()
            b.tvState.text=if(result.total==0)"Chưa có tin nhắn. Hãy gửi lời chào!" else "${result.total} tin nhắn"
            try{RetrofitClient.customerChatApi.markRead(token,conversationId)}catch(_:Exception){}
        }catch(e:Exception){if(requestVersion==version)b.tvState.text="Lỗi kết nối: ${e.localizedMessage}"
        }finally{if(requestVersion==version){busy=false;b.progress.visibility=View.GONE;b.btnOlder.visibility=if(page<totalPages)View.VISIBLE else View.GONE}}
        }
    }
    private fun render(){
        b.container.removeAllViews()
        val myId=SessionManager(this).getUserId()
        messages.distinctBy{it.messageId}.sortedBy{it.messageId}.forEach{item->
            val mine=item.senderUserId==myId
            val line=LinearLayout(this).apply{orientation=LinearLayout.VERTICAL;gravity=if(mine)Gravity.END else Gravity.START}
            val bubble=ChatUi.row(this).apply{background=ChatUi.card(this@CustomerChatActivity,!mine)}
            bubble.addView(ChatUi.text(this,if(mine)"Bạn" else item.senderName?:"Thành viên",12f,if(mine)"#66442F" else ChatUi.GOLD,true))
            bubble.addView(ChatUi.text(this,item.content.orEmpty(),15f,if(mine)"#251A14" else "#FFF8F1"))
            if(item.attachments.isNotEmpty())bubble.addView(ChatUi.text(this,"📎 ${item.attachments.size} tệp đính kèm",12f,if(mine)"#66442F" else "#C7B9AF"))
            bubble.addView(ChatUi.text(this,ChatUi.readableDate(item.createdAt),11f,if(mine)"#66442F" else "#C7B9AF"))
            line.addView(bubble,LinearLayout.LayoutParams(-2,-2).apply{width=(resources.displayMetrics.widthPixels*0.76f).toInt()})
            b.container.addView(line,ChatUi.margin(this))
        }
        b.scrollMessages.post{b.scrollMessages.fullScroll(View.FOCUS_DOWN)}
    }
    private fun send(){
        val content=b.edtMessage.text?.toString()?.trim().orEmpty()
        if(content.isBlank()){b.edtMessage.error="Nhập nội dung tin nhắn";return}
        val token=ChatUi.token(this)?:run{b.tvState.text="Hãy đăng nhập lại.";return}
        if(sending)return
        sending=true;b.btnSend.isEnabled=false
        lifecycleScope.launch{try{
            val response=RetrofitClient.customerChatApi.send(token,conversationId,ChatSendMessage(content))
            if(response.isSuccessful){b.edtMessage.setText("");refresh()}
            else b.tvState.text="Gửi không thành công (HTTP ${response.code()})."
        }catch(e:Exception){b.tvState.text="Lỗi gửi: ${e.localizedMessage}"
        }finally{sending=false;b.btnSend.isEnabled=true}}
    }
}
