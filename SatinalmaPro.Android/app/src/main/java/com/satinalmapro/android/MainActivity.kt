package com.satinalmapro.android

import android.Manifest
import android.content.Intent
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import com.satinalmapro.android.ui.AppRoot
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.AppViewModelFactory
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.SatinalmaProTheme

class MainActivity : ComponentActivity() {
    private val viewModel: AppViewModel by viewModels {
        AppViewModelFactory(SatinalmaProApp.get(this).container)
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        requestNotificationPermission()
        consumeNotificationIntent(intent)
        setContent {
            SatinalmaProTheme {
                Surface(modifier = Modifier.fillMaxSize(), color = AppColors.Background) {
                    AppRoot(viewModel)
                }
            }
        }
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

    private fun consumeNotificationIntent(intent: Intent?) {
        val route = intent?.getStringExtra("bildirim_route") ?: return
        intent.removeExtra("bildirim_route")
        viewModel.handleNotificationRoute(route)
    }

    private fun requestNotificationPermission() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return
        if (ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) == PackageManager.PERMISSION_GRANTED) return
        ActivityCompat.requestPermissions(this, arrayOf(Manifest.permission.POST_NOTIFICATIONS), 1001)
    }
}
