package com.example.fixnear

import android.app.Application
import com.example.fixnear.data.remote.RetrofitClient

class FixNearApp : Application() {

    override fun onCreate() {
        super.onCreate()

        RetrofitClient.init(this)
    }
}