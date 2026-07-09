package com.satinalmapro.android

import android.Manifest
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.provider.Settings
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import com.satinalmapro.android.core.helpers.BildirimLog
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.services.LocalNotificationHelper
import com.satinalmapro.android.ui.AppRoot
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.AppViewModelFactory
import com.satinalmapro.android.ui.theme.MetrikColors
import com.satinalmapro.android.ui.theme.MetrikTheme

class MainActivity : AppCompatActivity() {
    private val viewModel: AppViewModel by viewModels {
        AppViewModelFactory(SatinalmaProApp.get(this).container)
    }

    private var lastPermissionAskAt = 0L
    private var lastSettingsPromptAt = 0L
    private var permissionDialogVisible = false

    private val notificationPermissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
            permissionDialogVisible = false
            if (granted) {
                BildirimLog.i("PERMISSION", "POST_NOTIFICATIONS izni verildi")
                viewModel.onNotificationPermissionGranted()
            } else {
                BildirimLog.w("PERMISSION", "POST_NOTIFICATIONS izni reddedildi")
                // Kalıcı red: sistem ayarlarına yönlendir (tray için zorunlu).
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
                    !shouldShowRequestPermissionRationale(Manifest.permission.POST_NOTIFICATIONS)
                ) {
                    promptOpenNotificationSettings()
                }
            }
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        consumeNotificationIntent(intent)
        setContent {
            CompositionLocalProvider(LocalFragmentActivity provides this) {
                MetrikTheme {
                    Surface(modifier = Modifier.fillMaxSize(), color = MetrikColors.Background) {
                        AppRoot(viewModel)
                    }
                }
            }
        }
        SatinalmaProApp.consumeLastCrash(this)?.let { crash ->
            android.app.AlertDialog.Builder(this)
                .setTitle("Önceki çökme kaydı")
                .setMessage(crash.take(3500))
                .setPositiveButton("Tamam", null)
                .setNeutralButton("Kopyala") { _, _ ->
                    val cm = getSystemService(CLIPBOARD_SERVICE) as android.content.ClipboardManager
                    cm.setPrimaryClip(android.content.ClipData.newPlainText("crash", crash))
                }
                .show()
        }
    }

    override fun onPause() {
        super.onPause()
        viewModel.onAppPause()
    }

    override fun onResume() {
        super.onResume()
        viewModel.onAppResume()
        // Oturum açıkken izin yoksa her dönüşte tekrar dene (throttle'lı).
        if (viewModel.isLoggedIn.value || viewModel.user.value != null) {
            ensureNotificationPermission(fromResume = true)
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        consumeNotificationIntent(intent)
    }

    private fun consumeNotificationIntent(intent: Intent?) {
        val safeIntent = intent ?: return

        val notificationId = firstExtra(
            safeIntent,
            "bildirim_id", "bildirimId", "notificationId"
        )
        val tip = firstExtra(safeIntent, "tip", "type", "event", "status")
        val talepId = firstExtra(
            safeIntent,
            "talepId", "request_id", "requestId", "entityId",
            TalepDetayActivity.EXTRA_REQUEST_ID
        )
        val explicitRoute = firstExtra(safeIntent, "bildirim_route", "route")
            ?.takeIf { it.isNotBlank() && it != "dashboard" && it != "bildirimler" }

        val route = when {
            !explicitRoute.isNullOrBlank() -> explicitRoute
            !tip.isNullOrBlank() || !talepId.isNullOrBlank() ->
                BildirimRota.hedefRoute(
                    BildirimRota.normalizeTip(tip.orEmpty()),
                    talepId,
                    viewModel.user.value?.role
                )
            else -> null
        }

        if (route.isNullOrBlank()) return
        clearNotificationExtras(safeIntent)
        viewModel.handleNotificationRoute(route, notificationId)
    }

    private fun firstExtra(intent: Intent, vararg keys: String): String? {
        for (key in keys) {
            val v = intent.getStringExtra(key)?.trim()
            if (!v.isNullOrBlank()) return v
        }
        return null
    }

    private fun clearNotificationExtras(intent: Intent) {
        listOf(
            "bildirim_route", "route", "screen", "bildirim_id", "bildirimId", "notificationId",
            "tip", "type", "event", "status", "talepId", "request_id", "requestId", "entityId",
            TalepDetayActivity.EXTRA_REQUEST_ID, TalepDetayActivity.EXTRA_STATUS,
            "title", "body", "baslik", "mesaj", "message"
        ).forEach { intent.removeExtra(it) }
    }

    /**
     * Bildirim izni her oturumda açık olmalı.
     * Android 13+ kullanıcı onayı ister; reddedilirse ayarlara yönlendiririz.
     */
    fun ensureNotificationPermission(fromResume: Boolean = false) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return
        if (LocalNotificationHelper.hasPermission(this)) {
            BildirimLog.d("PERMISSION", "POST_NOTIFICATIONS zaten verilmiş")
            return
        }
        if (permissionDialogVisible) return
        val now = System.currentTimeMillis()
        val minGapMs = if (fromResume) 8_000L else 1_000L
        if (now - lastPermissionAskAt < minGapMs) return
        lastPermissionAskAt = now

        val canAskAgain = shouldShowRequestPermissionRationale(Manifest.permission.POST_NOTIFICATIONS) ||
            !wasNotificationPermissionAskedBefore()
        if (!canAskAgain) {
            promptOpenNotificationSettings()
            return
        }
        markNotificationPermissionAsked()
        permissionDialogVisible = true
        notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
    }

    /** Login sonrası çağrılır. */
    fun requestNotificationPermissionAfterLogin() = ensureNotificationPermission(fromResume = false)

    private fun promptOpenNotificationSettings() {
        if (isFinishing || isDestroyed) return
        val now = System.currentTimeMillis()
        if (now - lastSettingsPromptAt < 60_000L) return
        lastSettingsPromptAt = now
        android.app.AlertDialog.Builder(this)
            .setTitle("Bildirim izni gerekli")
            .setMessage("Anlık uyarılar için bildirim izninin açık olması gerekir. Ayarlardan açabilirsiniz.")
            .setPositiveButton("Ayarları aç") { _, _ -> openAppNotificationSettings() }
            .setNegativeButton("Sonra", null)
            .show()
    }

    private fun openAppNotificationSettings() {
        val intent = Intent(Settings.ACTION_APP_NOTIFICATION_SETTINGS).apply {
            putExtra(Settings.EXTRA_APP_PACKAGE, packageName)
            putExtra("android.provider.extra.APP_PACKAGE", packageName)
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        }
        runCatching { startActivity(intent) }
            .onFailure {
                startActivity(
                    Intent(
                        Settings.ACTION_APPLICATION_DETAILS_SETTINGS,
                        Uri.fromParts("package", packageName, null)
                    )
                )
            }
    }

    private fun prefs() = getSharedPreferences("satinalma_permissions", MODE_PRIVATE)

    private fun wasNotificationPermissionAskedBefore(): Boolean =
        prefs().getBoolean(KEY_NOTIF_ASKED, false)

    private fun markNotificationPermissionAsked() {
        prefs().edit().putBoolean(KEY_NOTIF_ASKED, true).apply()
    }

    companion object {
        private const val KEY_NOTIF_ASKED = "post_notifications_asked"
    }
}
