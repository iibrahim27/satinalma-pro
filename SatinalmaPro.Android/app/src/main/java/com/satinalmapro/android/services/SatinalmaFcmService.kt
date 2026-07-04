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
import kotlinx.coroutines.launch

class SatinalmaFcmService : FirebaseMessagingService() {
    override fun onMessageReceived(message: RemoteMessage) {
        val data = message.data
        val title = message.notification?.title ?: data["title"] ?: "Satınalma Pro"
        val body = message.notification?.body ?: data["body"] ?: ""
        var route = data["route"].orEmpty()
        if (route.isBlank() && data["talepId"].orEmpty().isNotBlank()) {
            val container = runCatching { SatinalmaProApp.get(this).container }.getOrNull()
            val role = container?.user?.value?.role
            route = BildirimRota.hedefRoute(data["tip"].orEmpty(), data["talepId"], role)
        }
        showNotification(title, body, route, data["bildirimId"])
    }

    override fun onNewToken(token: String) {
        val container = runCatching { SatinalmaProApp.get(this).container }.getOrNull() ?: return
        val uid = container.auth.uid ?: return
        CoroutineScope(Dispatchers.IO).launch {
            runCatching { container.firestore.updateFcmToken(uid, token) }
        }
    }

    private fun showNotification(title: String, body: String, route: String, notificationId: String?) {
        val intent = Intent(this, MainActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP)
            putExtra("bildirim_route", route)
            notificationId?.let { putExtra("bildirim_id", it) }
        }
        val pending = PendingIntent.getActivity(
            this,
            route.hashCode(),
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        val notification = androidx.core.app.NotificationCompat.Builder(this, "satinalma_pro")
            .setSmallIcon(com.satinalmapro.android.R.drawable.app_icon)
            .setContentTitle(title)
            .setContentText(body)
            .setAutoCancel(true)
            .setContentIntent(pending)
            .build()
        androidx.core.app.NotificationManagerCompat.from(this)
            .notify(route.hashCode(), notification)
    }
}
