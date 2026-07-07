package com.satinalmapro.android.services

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.util.Log
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import androidx.core.content.ContextCompat
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.satinalmapro.android.R
import com.satinalmapro.android.SatinalmaProApp
import com.satinalmapro.android.TalepDetayActivity
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

class MyFirebaseMessagingService : FirebaseMessagingService() {

    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)

    override fun onCreate() {
        super.onCreate()
        ensureNotificationChannel()
    }

    override fun onMessageReceived(remoteMessage: RemoteMessage) {
        Log.i(TAG, "FCM mesajı alındı from=${remoteMessage.from} dataKeys=${remoteMessage.data.keys}")

        val data = remoteMessage.data
        val requestId = extractRequestId(data)
        val status = extractStatus(data)

        val title = remoteMessage.notification?.title
            ?: data["title"]
            ?: data["baslik"]
            ?: "Satınalma Pro"

        val body = remoteMessage.notification?.body
            ?: data["body"]
            ?: data["message"]
            ?: data["mesaj"]
            ?: ""

        if (!canShowNotifications()) {
            Log.w(TAG, "POST_NOTIFICATIONS izni yok — bildirim gösterilemedi")
            return
        }

        ensureNotificationChannel()
        showTalepNotification(
            title = title,
            body = body,
            requestId = requestId,
            status = status
        )

        scope.launch {
            runCatching { SatinalmaProApp.get(applicationContext).container.refreshNotifications() }
                .onFailure { error -> Log.e(TAG, "Bildirim listesi yenileme hatası", error) }
        }
    }

    override fun onNewToken(token: String) {
        Log.i(TAG, "Yeni FCM token alındı (${token.take(12)}…)")

        val container = runCatching { SatinalmaProApp.get(this).container }.getOrNull()
        val uid = container?.auth?.uid
        if (uid.isNullOrBlank()) {
            getSharedPreferences(PREFS_NAME, MODE_PRIVATE)
                .edit()
                .putString(KEY_PENDING_FCM_TOKEN, token)
                .apply()
            Log.w(TAG, "Oturum yok — FCM token beklemeye alındı")
            return
        }

        scope.launch {
            runCatching { container.firestore.updateFcmToken(uid, token) }
                .onSuccess { Log.i(TAG, "FCM token Firestore'a yazıldı uid=$uid") }
                .onFailure { error -> Log.e(TAG, "FCM token Firestore yazım hatası", error) }
        }
    }

    private fun extractRequestId(data: Map<String, String>): String {
        return data["request_id"]
            ?: data["requestId"]
            ?: data["talepId"]
            ?: data["entityId"]
            ?: ""
    }

    private fun extractStatus(data: Map<String, String>): String {
        return data["status"]
            ?: data["durum"]
            ?: data["requestStatus"]
            ?: ""
    }

    private fun showTalepNotification(
        title: String,
        body: String,
        requestId: String,
        status: String
    ) {
        val intent = Intent(this, TalepDetayActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP)
            putExtra(TalepDetayActivity.EXTRA_REQUEST_ID, requestId)
            putExtra(TalepDetayActivity.EXTRA_STATUS, status)
        }

        val notificationId = if (requestId.isNotBlank()) {
            notificationIdFromRequest(requestId, status)
        } else {
            (System.currentTimeMillis() % Int.MAX_VALUE).toInt()
        }

        val pendingIntent = PendingIntent.getActivity(
            this,
            notificationId,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )

        val notification = NotificationCompat.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.app_icon)
            .setContentTitle(title)
            .setContentText(body)
            .setStyle(NotificationCompat.BigTextStyle().bigText(body))
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setDefaults(NotificationCompat.DEFAULT_ALL)
            .setCategory(NotificationCompat.CATEGORY_MESSAGE)
            .setContentIntent(pendingIntent)
            .build()

        NotificationManagerCompat.from(this).notify(notificationId, notification)
        Log.i(TAG, "Tray bildirimi gösterildi requestId=$requestId status=$status")
    }

    private fun notificationIdFromRequest(requestId: String, status: String): Int {
        return (requestId + status).hashCode()
    }

    private fun ensureNotificationChannel() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) {
            return
        }

        val manager = getSystemService(NotificationManager::class.java) ?: return
        if (manager.getNotificationChannel(CHANNEL_ID) != null) {
            return
        }

        val channel = NotificationChannel(
            CHANNEL_ID,
            CHANNEL_NAME,
            NotificationManager.IMPORTANCE_HIGH
        ).apply {
            description = "Talep durumu ve sipariş akışı bildirimleri"
            enableVibration(true)
            enableLights(true)
        }
        manager.createNotificationChannel(channel)
        Log.i(TAG, "NotificationChannel oluşturuldu: $CHANNEL_ID")
    }

    private fun canShowNotifications(): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) {
            return true
        }
        return ContextCompat.checkSelfPermission(
            this,
            android.Manifest.permission.POST_NOTIFICATIONS
        ) == PackageManager.PERMISSION_GRANTED
    }

    companion object {
        private const val TAG = "MyFirebaseMessaging"
        const val CHANNEL_ID = "talep_akisi_kanali"
        private const val CHANNEL_NAME = "Talep ve Sipariş Bildirimleri"
        private const val PREFS_NAME = "satinalma_pro"
        const val KEY_PENDING_FCM_TOKEN = "pending_fcm_token"
    }
}
