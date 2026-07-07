package com.satinalmapro.android.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.ui.components.UpdateDialog
import com.satinalmapro.android.ui.screens.login.BiometricUnlockScreen
import com.satinalmapro.android.ui.screens.login.LoginScreen
import com.satinalmapro.android.ui.screens.shell.AtolyeShell
import com.satinalmapro.android.ui.screens.shell.RoleShell
import com.satinalmapro.android.ui.screens.login.SplashScreen
import com.satinalmapro.android.ui.theme.AppColors

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
    val isAtolye = KullaniciRolleri.isAtolyeOnly(user?.role)

    LaunchedEffect(Unit) {
        viewModel.startSplash()
        viewModel.startBackgroundRefresh()
    }

    Box(Modifier.fillMaxSize()) {
        when {
            !splashDone -> SplashScreen(splashMessage)
            needsBiometricUnlock -> BiometricUnlockScreen(viewModel)
            !isLoggedIn -> LoginScreen(viewModel)
            isAtolye -> AtolyeShell(viewModel)
            else -> RoleShell(viewModel)
        }

        if (isLoggedIn && showUpdateDialog && pendingUpdate != null) {
            UpdateDialog(
                manifest = pendingUpdate!!,
                progress = updateProgress,
                message = updateMessage,
                error = updateError,
                onUpdate = { viewModel.startUpdateDownload() },
                onDismiss = { viewModel.dismissUpdateDialog() }
            )
        } else if (isLoggedIn && showUpdateDialog && pendingUpdate == null) {
            AlertDialog(
                onDismissRequest = { viewModel.dismissUpdateDialog() },
                title = {
                    Text(
                        if (updateError != null) "Güncelleme hatası"
                        else "Güncelleme kontrolü"
                    )
                },
                text = {
                    Text(
                        updateError ?: updateMessage ?: "Güncelleme kontrol ediliyor...",
                        color = if (updateError != null) AppColors.Danger else AppColors.TextPrimary
                    )
                },
                confirmButton = {
                    TextButton(onClick = { viewModel.dismissUpdateDialog() }) { Text("Tamam") }
                }
            )
        }
    }
}

