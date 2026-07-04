package com.satinalmapro.android

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build
import com.google.firebase.FirebaseApp
import com.google.firebase.FirebaseOptions
import com.satinalmapro.android.core.AppContainer

class SatinalmaProApp : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        initFirebase()
        createNotificationChannel()
        container = AppContainer(this)
    }

    private fun initFirebase() {
        if (FirebaseApp.getApps(this).isNotEmpty()) return
        val config = AppContainer.loadFirebaseConfig(this)
        if (!config.isConfigured) return
        FirebaseApp.initializeApp(
            this,
            FirebaseOptions.Builder()
                .setProjectId(config.projectId)
                .setApplicationId("1:524965229207:android:metrik_satinalmapro")
                .setApiKey(config.apiKey)
                .build()
        )
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val channel = NotificationChannel(
            "satinalma_pro",
            "Satınalma Pro",
            NotificationManager.IMPORTANCE_HIGH
        ).apply { description = "Satınalma bildirimleri" }
        val nm = getSystemService(NotificationManager::class.java)
        nm?.createNotificationChannel(channel)
    }

    companion object {
        fun get(context: android.content.Context): SatinalmaProApp =
            context.applicationContext as SatinalmaProApp
    }
}
