package com.satinalmapro.android

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build
import com.google.firebase.FirebaseApp
import com.satinalmapro.android.core.GoogleServicesLoader
import com.satinalmapro.android.core.AppContainer

class SatinalmaProApp : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        runCatching { initFirebase() }
            .onFailure { android.util.Log.e("SatinalmaProApp", "Firebase init fatal", it) }
        runCatching { createNotificationChannel() }
        container = AppContainer(this)
    }

    private fun initFirebase() {
        if (FirebaseApp.getApps(this).isNotEmpty()) return
        val config = AppContainer.loadFirebaseConfig(this)
        val options = GoogleServicesLoader.loadOptions(this, config.apiKey, config.projectId)
        if (options == null) {
            android.util.Log.w(
                "SatinalmaProApp",
                "google-services.json yok — Firebase SDK atlandı (REST oturum devam eder)"
            )
            return
        }
        runCatching { FirebaseApp.initializeApp(this, options) }
            .onFailure { android.util.Log.e("SatinalmaProApp", "Firebase init hatası", it) }
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        runCatching {
            val manager = getSystemService(NotificationManager::class.java) ?: return
            val legacyChannel = NotificationChannel(
                "satinalma_pro",
                "Satınalma Pro",
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                description = "Satınalma bildirimleri"
                enableVibration(true)
                enableLights(true)
            }
            manager.createNotificationChannel(legacyChannel)

            val talepChannel = NotificationChannel(
                com.satinalmapro.android.services.MyFirebaseMessagingService.CHANNEL_ID,
                "Talep ve Sipariş Bildirimleri",
                NotificationManager.IMPORTANCE_HIGH
            ).apply {
                description = "Talep durumu ve sipariş akışı bildirimleri"
                enableVibration(true)
                enableLights(true)
            }
            manager.createNotificationChannel(talepChannel)
        }.onFailure { android.util.Log.e("SatinalmaProApp", "Notification channel hatası", it) }
    }

    companion object {
        fun get(context: android.content.Context): SatinalmaProApp =
            context.applicationContext as SatinalmaProApp
    }
}
