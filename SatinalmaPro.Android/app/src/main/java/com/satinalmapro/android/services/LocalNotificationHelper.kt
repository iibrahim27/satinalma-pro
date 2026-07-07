package com.satinalmapro.android.services

import android.Manifest
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import androidx.core.content.ContextCompat
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import com.satinalmapro.android.MainActivity
import com.satinalmapro.android.R
import com.satinalmapro.android.core.helpers.BildirimLog

object LocalNotificationHelper {
    fun show(context: Context, title: String, body: String, route: String, notificationId: String): Boolean {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU
            && ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS)
            != PackageManager.PERMISSION_GRANTED
        ) {
            BildirimLog.w("LOCAL_NOTIF", "POST_NOTIFICATIONS izni yok — poll tray atlandı")
            return false
        }
        val intent = Intent(context, MainActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP)
            putExtra("bildirim_route", route)
            putExtra("bildirim_id", notificationId)
        }
        val pending = PendingIntent.getActivity(
            context,
            notificationId.hashCode(),
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        )
        val notification = NotificationCompat.Builder(context, "satinalma_pro")
            .setSmallIcon(R.drawable.app_icon)
            .setContentTitle(title)
            .setContentText(body)
            .setStyle(NotificationCompat.BigTextStyle().bigText(body))
            .setAutoCancel(true)
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setDefaults(NotificationCompat.DEFAULT_ALL)
            .setCategory(NotificationCompat.CATEGORY_MESSAGE)
            .setContentIntent(pending)
            .build()
        NotificationManagerCompat.from(context).notify(stableNotifyId(notificationId), notification)
        BildirimLog.d("LOCAL_NOTIF", "Tray gösterildi (poll): $title")
        return true
    }

    fun cancel(context: Context, notificationId: String) {
        NotificationManagerCompat.from(context).cancel(stableNotifyId(notificationId))
    }

    private fun stableNotifyId(notificationId: String): Int = notificationId.hashCode()

    fun cancelAll(context: Context) {
        NotificationManagerCompat.from(context).cancelAll()
    }

    fun hasPermission(context: Context): Boolean =
        Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU ||
            ContextCompat.checkSelfPermission(context, Manifest.permission.POST_NOTIFICATIONS) ==
            PackageManager.PERMISSION_GRANTED
}
