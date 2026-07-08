package com.satinalmapro.android

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.appcompat.app.AppCompatActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Modifier
import com.satinalmapro.android.core.helpers.BildirimLog
import com.satinalmapro.android.services.LocalNotificationHelper
import com.satinalmapro.android.ui.AppRoot
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.AppViewModelFactory
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.SatinalmaProTheme

class MainActivity : AppCompatActivity() {
    private val viewModel: AppViewModel by viewModels {
        AppViewModelFactory(SatinalmaProApp.get(this).container)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        requestNotificationPermission()
        // FCM aboneliği oturum geri yüklemeden sonra AppContainer üzerinden yapılır;
        // burada Firebase hazır olmayabilir / kiracı oturumu yoktur.
        consumeNotificationIntent(intent)
        setContent {
            CompositionLocalProvider(LocalFragmentActivity provides this) {
                SatinalmaProTheme {
                    Surface(modifier = Modifier.fillMaxSize(), color = AppColors.Background) {
                        AppRoot(viewModel)
                    }
                }
            }
        }
    }

    override fun onPause() {
        super.onPause()
        viewModel.onAppPause()
    }

    override fun onResume() {
        super.onResume()
        viewModel.onAppResume()
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        consumeNotificationIntent(intent)
    }

    @Deprecated("Deprecated in Java")
    override fun onRequestPermissionsResult(
        requestCode: Int,
        permissions: Array<out String>,
        grantResults: IntArray
    ) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults)
        if (requestCode != REQUEST_NOTIFICATIONS) return
        val granted = grantResults.isNotEmpty() && grantResults[0] == PackageManager.PERMISSION_GRANTED
        if (granted) {
            BildirimLog.i("PERMISSION", "POST_NOTIFICATIONS izni verildi")
            viewModel.onNotificationPermissionGranted()
        } else {
            BildirimLog.w("PERMISSION", "POST_NOTIFICATIONS izni reddedildi — tray bildirimleri gösterilemez")
        }
    }

    private fun consumeNotificationIntent(intent: Intent?) {
        val safeIntent = intent ?: return

        val requestId = safeIntent.getStringExtra(TalepDetayActivity.EXTRA_REQUEST_ID)
            ?: safeIntent.getStringExtra("request_id")
        val status = safeIntent.getStringExtra(TalepDetayActivity.EXTRA_STATUS)
            ?: safeIntent.getStringExtra("status")

        if (!requestId.isNullOrBlank()) {
            val route = buildString {
                append("talep-detay?id=$requestId")
                mapStatusQueryParam(status)?.let { append("&view=$it") }
            }
            safeIntent.removeExtra(TalepDetayActivity.EXTRA_REQUEST_ID)
            safeIntent.removeExtra(TalepDetayActivity.EXTRA_STATUS)
            safeIntent.removeExtra("request_id")
            safeIntent.removeExtra("status")
            viewModel.handleNotificationRoute(route, null)
            return
        }

        val route = safeIntent.getStringExtra("bildirim_route")
            ?: safeIntent.getStringExtra("route")
            ?: return
        val notificationId = safeIntent.getStringExtra("bildirim_id")
            ?: safeIntent.getStringExtra("bildirimId")
        safeIntent.removeExtra("bildirim_route")
        safeIntent.removeExtra("bildirim_id")
        safeIntent.removeExtra("route")
        safeIntent.removeExtra("bildirimId")
        viewModel.handleNotificationRoute(route, notificationId)
    }

    private fun mapStatusQueryParam(status: String?): String? {
        if (status.isNullOrBlank()) return null
        val normalized = status.trim()
        return when {
            normalized.equals("siparis", ignoreCase = true) ||
                normalized.equals("siparis_olusturuldu", ignoreCase = true) -> "siparis"
            normalized.contains("mal kabul", ignoreCase = true) ||
                normalized.equals("malkabul", ignoreCase = true) ||
                normalized.equals("mal_kabul_edildi", ignoreCase = true) -> "malkabul"
            normalized.equals("onaylandi", ignoreCase = true) -> "onaylanan"
            else -> null
        }
    }

    private fun requestNotificationPermission() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return
        if (LocalNotificationHelper.hasPermission(this)) {
            BildirimLog.d("PERMISSION", "POST_NOTIFICATIONS zaten verilmiş")
            return
        }
        ActivityCompat.requestPermissions(
            this,
            arrayOf(Manifest.permission.POST_NOTIFICATIONS),
            REQUEST_NOTIFICATIONS
        )
    }

    companion object {
        const val REQUEST_NOTIFICATIONS = 1001
    }
}
