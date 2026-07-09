package com.satinalmapro.android

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.content.Context
import android.os.Build
import android.os.Environment
import com.google.firebase.FirebaseApp
import com.satinalmapro.android.core.AppContainer
import com.satinalmapro.android.core.GoogleServicesLoader
import java.io.File
import java.io.PrintWriter
import java.io.StringWriter
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class SatinalmaProApp : Application() {
    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        val previous = Thread.getDefaultUncaughtExceptionHandler()
        Thread.setDefaultUncaughtExceptionHandler { thread, throwable ->
            runCatching { writeCrashFile(thread.name, throwable) }
            android.util.Log.e(
                "SatinalmaProCrash",
                "Uncaught on ${thread.name}: ${throwable.message}",
                throwable
            )
            previous?.uncaughtException(thread, throwable)
        }
        runCatching { initFirebase() }
            .onFailure { android.util.Log.e("SatinalmaProApp", "Firebase init fatal", it) }
        runCatching { createNotificationChannel() }
        container = AppContainer(this)
    }

    private fun writeCrashFile(threadName: String, throwable: Throwable) {
        val stamp = SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(Date())
        val sw = StringWriter()
        throwable.printStackTrace(PrintWriter(sw))
        val text = buildString {
            appendLine("time=$stamp")
            appendLine("thread=$threadName")
            appendLine("message=${throwable.message}")
            appendLine()
            append(sw.toString())
        }

        // 1) App private
        val privateDir = File(filesDir, "crashes").apply { mkdirs() }
        File(privateDir, "crash_$stamp.txt").writeText(text)
        File(filesDir, "last_crash.txt").writeText(text)

        // 2) App external (Android/data/.../files)
        runCatching {
            getExternalFilesDir(null)?.let { File(it, "last_crash.txt").writeText(text) }
        }

        // 3) Shared prefs — bir sonraki açılışta dialog
        getSharedPreferences(CRASH_PREFS, Context.MODE_PRIVATE)
            .edit()
            .putString(KEY_LAST_CRASH, text.take(12_000))
            .apply()

        // 4) Downloads (kullanıcı dosya yöneticisinden görsün)
        runCatching {
            val downloads = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS)
            if (downloads != null) {
                downloads.mkdirs()
                File(downloads, "SatinalmaPro_last_crash.txt").writeText(text)
            }
        }
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
        const val CRASH_PREFS = "satinalma_crash"
        const val KEY_LAST_CRASH = "last_crash_text"

        fun get(context: Context): SatinalmaProApp =
            context.applicationContext as SatinalmaProApp

        fun consumeLastCrash(context: Context): String? {
            val prefs = context.getSharedPreferences(CRASH_PREFS, Context.MODE_PRIVATE)
            val text = prefs.getString(KEY_LAST_CRASH, null)
            if (!text.isNullOrBlank()) {
                prefs.edit().remove(KEY_LAST_CRASH).apply()
            }
            return text
        }
    }
}
