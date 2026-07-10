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
import com.satinalmapro.android.MainActivity
import com.satinalmapro.android.R
import com.satinalmapro.android.SatinalmaProApp
import com.satinalmapro.android.core.roles.BildirimRota
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
            Log.w(TAG, "POST_NOTIFICATIONS izni yok — tray atlandı, veri yine yenilenecek")
        } else {
            ensureNotificationChannel()
            val route = resolveRoute(data)
            val bildirimId = data["bildirimId"]
                ?: data["bildirim_id"]
                ?: data["notificationId"]
                ?: ""
            showActionNotification(title, body, route, bildirimId)
        }

        // FCM geldiğinde oturumu geri yükle + canlı veri çek (uygulama kapalıyken de).
        scope.launch {
            val container = runCatching { SatinalmaProApp.get(applicationContext).container }.getOrNull()
                ?: return@launch
            if (!container.hasActiveSession() && container.hasPersistedSession()) {
                runCatching { container.restoreSession() }
                    .onFailure { error -> Log.e(TAG, "FCM sonrası oturum yenileme hatası", error) }
            }
            container.hydrateFromOfflineCache()
            runCatching { container.syncLiveData() }
                .onFailure { error -> Log.e(TAG, "Canlı senkron hatası", error) }
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

    private fun resolveRoute(data: Map<String, String>): String {
        val explicit = data["route"]?.takeIf { it.isNotBlank() }
            ?: data["bildirim_route"]?.takeIf { it.isNotBlank() }
            ?: data["screen"]?.takeIf { it.isNotBlank() }
        if (!explicit.isNullOrBlank()) return explicit

        val eventCode = data["eventCode"].orEmpty()
        val rawTip = data["tip"].orEmpty().ifBlank { data["type"].orEmpty() }
        val tip = when {
            eventCode.isNotBlank() -> eventCodeToLegacyTip(eventCode)
            rawTip.uppercase() in setOf("APPROVAL", "INFO", "TASK", "WARNING", "REMINDER", "URGENT", "CRITICAL") ->
                eventCodeToLegacyTip(eventCode).ifBlank { rawTip }
            else -> rawTip
        }
        val talepId = data["talepId"]
            ?: data["request_id"]
            ?: data["requestId"]
            ?: data["entityId"]
        val role = runCatching {
            SatinalmaProApp.get(applicationContext).container.user.value?.role
        }.getOrNull()
        return BildirimRota.hedefRoute(BildirimRota.normalizeTip(tip), talepId, role)
    }

    private fun eventCodeToLegacyTip(eventCode: String): String = when (eventCode) {
        "talep.yonetime_gonderildi", "talep.olusturuldu", "talep.sla_yaklasiyor", "talep.sla_asildi" ->
            "yonetime_gonderildi"
        "teklif.istendi" -> "teklif_istendi"
        "teklif.yonetime_gonderildi" -> "teklif_onayda"
        "teklif.duzeltme_istendi" -> "teklif_duzeltme_istendi"
        "talep.onaylandi" -> "onaylandi"
        "talep.reddedildi" -> "reddedildi"
        "siparis.olusturuldu" -> "siparis_olusturuldu"
        "depo.mal_kabul_yapildi" -> "mal_kabul_edildi"
        else -> eventCode
    }

    private fun showActionNotification(
        title: String,
        body: String,
        route: String,
        bildirimId: String
    ) {
        // Doğrudan ilgili işleme git — ana ekranı açma.
        val intent = Intent(this, MainActivity::class.java).apply {
            action = Intent.ACTION_VIEW
            addFlags(
                Intent.FLAG_ACTIVITY_CLEAR_TOP or
                    Intent.FLAG_ACTIVITY_SINGLE_TOP or
                    Intent.FLAG_ACTIVITY_NEW_TASK
            )
            putExtra("bildirim_route", route)
            if (bildirimId.isNotBlank()) putExtra("bildirim_id", bildirimId)
        }

        val notificationId = if (bildirimId.isNotBlank()) {
            bildirimId.hashCode()
        } else {
            route.hashCode()
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
        Log.i(TAG, "Tray bildirimi gösterildi route=$route")
    }

    private fun ensureNotificationChannel() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val manager = getSystemService(NotificationManager::class.java) ?: return
        if (manager.getNotificationChannel(CHANNEL_ID) != null) return
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
    }

    private fun canShowNotifications(): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return true
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
