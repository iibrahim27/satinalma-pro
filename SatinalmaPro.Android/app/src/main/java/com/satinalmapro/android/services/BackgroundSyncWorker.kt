package com.satinalmapro.android.services

import android.content.Context
import android.util.Log
import androidx.work.Constraints
import androidx.work.CoroutineWorker
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.NetworkType
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import com.satinalmapro.android.SatinalmaProApp
import java.util.concurrent.TimeUnit

/**
 * Uygulama kapalıyken periyodik talep/bildirim senkronu.
 * FCM kaçsa bile inbox + talepler güncellenir; açılışta cache hazır olur.
 */
class BackgroundSyncWorker(
    appContext: Context,
    params: WorkerParameters
) : CoroutineWorker(appContext, params) {

    override suspend fun doWork(): Result {
        val container = runCatching { SatinalmaProApp.get(applicationContext).container }
            .getOrNull() ?: return Result.retry()

        if (!container.hasPersistedSession()) {
            Log.i(TAG, "Kayıtlı oturum yok — arka plan senkron atlandı")
            return Result.success()
        }

        if (!container.hasActiveSession()) {
            val restored = runCatching { container.restoreSession() }.getOrDefault(false)
            if (!restored && !container.hasActiveSession()) {
                Log.w(TAG, "Oturum yenilenemedi — retry")
                return Result.retry()
            }
        }

        container.hydrateFromOfflineCache()
        runCatching { container.syncLiveData() }
            .onFailure {
                Log.e(TAG, "syncLiveData başarısız", it)
                return Result.retry()
            }

        Log.i(TAG, "Arka plan senkron tamam")
        return Result.success()
    }

    companion object {
        private const val TAG = "BackgroundSyncWorker"
        const val UNIQUE_NAME = "satinalma_background_sync"
    }
}

object BackgroundSyncScheduler {
    private const val INTERVAL_MINUTES = 15L

    fun ensureScheduled(context: Context) {
        val constraints = Constraints.Builder()
            .setRequiredNetworkType(NetworkType.CONNECTED)
            .build()

        val request = PeriodicWorkRequestBuilder<BackgroundSyncWorker>(
            INTERVAL_MINUTES,
            TimeUnit.MINUTES
        )
            .setConstraints(constraints)
            .build()

        WorkManager.getInstance(context.applicationContext).enqueueUniquePeriodicWork(
            BackgroundSyncWorker.UNIQUE_NAME,
            ExistingPeriodicWorkPolicy.KEEP,
            request
        )
    }

    fun cancel(context: Context) {
        WorkManager.getInstance(context.applicationContext)
            .cancelUniqueWork(BackgroundSyncWorker.UNIQUE_NAME)
    }
}
