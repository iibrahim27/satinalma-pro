package com.satinalmapro.android

import android.content.Intent
import android.os.Bundle
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.appcompat.app.AppCompatActivity
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import com.satinalmapro.android.core.roles.TalepDurumlari
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.AppViewModelFactory
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.auth.LoginScreen
import com.satinalmapro.android.ui.auth.SplashScreen
import com.satinalmapro.android.ui.procurement.TalepDetayScreen
import com.satinalmapro.android.ui.theme.MetrikColors
import com.satinalmapro.android.ui.theme.MetrikTheme

/**
 * FCM bildirim tıklamalarından açılan talep detay ekranı.
 * Mevcut [TalepDetayScreen] Compose bileşenini kullanır; UI yapısı değiştirilmez.
 */
class TalepDetayActivity : AppCompatActivity() {

    private val viewModel: AppViewModel by viewModels {
        AppViewModelFactory(SatinalmaProApp.get(this).container)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        val requestId = intent.getStringExtra(EXTRA_REQUEST_ID).orEmpty()
        val status = intent.getStringExtra(EXTRA_STATUS).orEmpty()
        val viewMode = mapStatusToViewMode(status)

        setContent {
            val splashDone by viewModel.splashDone.collectAsState()
            val isLoggedIn by viewModel.isLoggedIn.collectAsState()
            val splashMessage by viewModel.splashMessage.collectAsState()

            LaunchedEffect(Unit) {
                viewModel.startSplash()
            }

            LaunchedEffect(splashDone, isLoggedIn, requestId, status) {
                if (splashDone && !isLoggedIn) {
                    val mainIntent = Intent(this@TalepDetayActivity, MainActivity::class.java).apply {
                        addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP or Intent.FLAG_ACTIVITY_SINGLE_TOP)
                        putExtra(EXTRA_REQUEST_ID, requestId)
                        putExtra(EXTRA_STATUS, status)
                    }
                    startActivity(mainIntent)
                    finish()
                }
            }

            MetrikTheme {
                CompositionLocalProvider(LocalFragmentActivity provides this) {
                    Surface(modifier = Modifier.fillMaxSize(), color = MetrikColors.Background) {
                        when {
                            !splashDone -> SplashScreen(splashMessage)
                            !isLoggedIn -> LoginScreen(viewModel)
                            requestId.isBlank() -> SplashScreen("Talep kimliği bulunamadı.")
                            else -> TalepDetayScreen(
                                viewModel = viewModel,
                                talepId = requestId,
                                viewMode = viewMode
                            )
                        }
                    }
                }
            }
        }
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        recreate()
    }

    private fun mapStatusToViewMode(status: String): String? {
        if (status.isBlank()) return null
        val normalized = status.trim()
        return when {
            normalized.equals(TalepDurumlari.SIPARIS, ignoreCase = true) ||
                normalized.equals("siparis", ignoreCase = true) ||
                normalized.equals("siparis_olusturuldu", ignoreCase = true) -> "siparis"
            normalized.contains("mal kabul", ignoreCase = true) ||
                normalized.equals("malkabul", ignoreCase = true) ||
                normalized.equals("mal_kabul_edildi", ignoreCase = true) -> "malkabul"
            normalized.equals(TalepDurumlari.ONAYLANDI, ignoreCase = true) ||
                normalized.equals("onaylandi", ignoreCase = true) -> "onaylanan"
            else -> null
        }
    }

    companion object {
        const val EXTRA_REQUEST_ID = "request_id"
        const val EXTRA_STATUS = "status"
    }
}
