package com.satinalmapro.android.ui

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import com.satinalmapro.android.MainActivity
import com.satinalmapro.android.ui.auth.BiometricUnlockScreen
import com.satinalmapro.android.ui.auth.LoginScreen
import com.satinalmapro.android.ui.auth.SplashScreen
import com.satinalmapro.android.ui.components.UpdateDialog
import com.satinalmapro.android.ui.shell.ProcurementShell
import com.satinalmapro.android.ui.theme.MetrikColors
import kotlinx.coroutines.delay

@Composable
fun AppRoot(viewModel: AppViewModel) {
    val splashDone by viewModel.splashDone.collectAsState()
    val isLoggedIn by viewModel.isLoggedIn.collectAsState()
    val needsBiometricUnlock by viewModel.needsBiometricUnlock.collectAsState()
    val splashMessage by viewModel.splashMessage.collectAsState()
    val showUpdateDialog by viewModel.showUpdateDialog.collectAsState()
    val pendingUpdate by viewModel.pendingUpdate.collectAsState()
    val updateProgress by viewModel.updateProgress.collectAsState()
    val updateMessage by viewModel.updateMessage.collectAsState()
    val updateError by viewModel.updateError.collectAsState()
    val user by viewModel.user.collectAsState()
    val activity = LocalFragmentActivity.current

    val showApp = isLoggedIn || (user != null && !needsBiometricUnlock)

    LaunchedEffect(Unit) { viewModel.startSplash() }
    LaunchedEffect(showApp) { if (showApp) viewModel.startBackgroundRefresh() }
    LaunchedEffect(user, isLoggedIn, needsBiometricUnlock) {
        if (user != null && !isLoggedIn && !needsBiometricUnlock) {
            viewModel.ensureLoggedInFromSession()
        }
    }
    LaunchedEffect(showApp) {
        if (!showApp) return@LaunchedEffect
        delay(800)
        (activity as? MainActivity)?.requestNotificationPermissionAfterLogin()
    }

    Box(modifier = Modifier.fillMaxSize()) {
        when {
            showApp -> ProcurementShell(viewModel)
            needsBiometricUnlock -> BiometricUnlockScreen(viewModel)
            !splashDone -> SplashScreen(splashMessage)
            else -> LoginScreen(viewModel)
        }

        if (showApp && showUpdateDialog && pendingUpdate != null) {
            UpdateDialog(
                manifest = pendingUpdate!!,
                progress = updateProgress,
                message = updateMessage,
                error = updateError,
                onUpdate = { viewModel.startUpdateDownload() },
                onDismiss = { viewModel.dismissUpdateDialog() }
            )
        } else if (showApp && showUpdateDialog && pendingUpdate == null) {
            AlertDialog(
                onDismissRequest = { viewModel.dismissUpdateDialog() },
                title = {
                    Text(if (updateError != null) "Güncelleme hatası" else "Güncelleme kontrolü")
                },
                text = {
                    Text(
                        updateError ?: updateMessage ?: "Güncelleme kontrol ediliyor...",
                        color = if (updateError != null) MetrikColors.Danger else MetrikColors.TextPrimary
                    )
                },
                confirmButton = {
                    TextButton(onClick = { viewModel.dismissUpdateDialog() }) { Text("Tamam") }
                }
            )
        }
    }
}
