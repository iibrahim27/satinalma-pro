package com.satinalmapro.android.services

import android.app.PendingIntent
import android.content.Intent
import com.google.firebase.messaging.FirebaseMessagingService
import com.google.firebase.messaging.RemoteMessage
import com.satinalmapro.android.MainActivity
import com.satinalmapro.android.SatinalmaProApp
import com.satinalmapro.android.core.roles.BildirimRota
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch

class SatinalmaFcmService : FirebaseMessagingService() {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.Main)

    override fun onMessageReceived(message: RemoteMessage) {
        val data = message.data
        val title = message.notification?.title ?: data["title"] ?: "Satınalma Pro"
        val body = message.notification?.body ?: data["body"] ?: ""
        val container = runCatching { SatinalmaProApp.get(this).container }.getOrNull()
        val role = container?.user?.value?.role
        val talepId = data["talepId"]?.takeIf { it.isNotBlank() }
        val tip = BildirimRota.normalizeTip(data["tip"].orEmpty())
        val route = data["route"]?.takeIf { it.isNotBlank() }
            ?: BildirimRota.hedefRoute(tip, talepId, role)
        val notificationId = data["bildirimId"]?.takeIf { it.isNotBlank() }
            ?: "${tip}_${talepId.orEmpty()}_${System.currentTimeMillis()}"

        scope.launch {
            runCatching { container?.refreshNotifications() }
            runCatching { container?.syncData() }
        }
        showNotification(title, body, route, notificationId)
    }

    override fun onNewToken(token: String) {
        val container = runCatching { SatinalmaProApp.get(this).container }.getOrNull() ?: return
        val uid = container.auth.uid ?: return
        CoroutineScope(Dispatchers.IO).launch {
            runCatching { container.firestore.updateFcmToken(uid, token) }
        }
    }

    private fun showNotification(title: String, body: String, route: String, notificationId: String) {
        val safeRoute = route.ifBlank { "bildirimler" }
        val intent = Intent(this, MainActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP)
            putExtra("bildirim_route", safeRoute)
            putExtra("bildirim_id", notificationId)
        }
        val pending = PendingIntent.getActivity(
            this,
            notificationId.hashCode(),
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        val notification = androidx.core.app.NotificationCompat.Builder(this, "satinalma_pro")
            .setSmallIcon(com.satinalmapro.android.R.drawable.app_icon)
            .setContentTitle(title)
            .setContentText(body)
            .setStyle(androidx.core.app.NotificationCompat.BigTextStyle().bigText(body))
            .setAutoCancel(true)
            .setPriority(androidx.core.app.NotificationCompat.PRIORITY_HIGH)
            .setCategory(androidx.core.app.NotificationCompat.CATEGORY_MESSAGE)
            .setContentIntent(pending)
            .build()
        androidx.core.app.NotificationManagerCompat.from(this)
            .notify(notificationId.hashCode(), notification)
    }
}
