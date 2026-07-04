package com.satinalmapro.android.ui

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.components.UpdateDialog
import com.satinalmapro.android.ui.screens.login.LoginScreen
import com.satinalmapro.android.ui.screens.shell.RoleShell
import com.satinalmapro.android.ui.theme.AppColors

@Composable
fun AppRoot(viewModel: AppViewModel) {
    val splashDone by viewModel.splashDone.collectAsState()
    val isLoggedIn by viewModel.isLoggedIn.collectAsState()
    val splashMessage by viewModel.splashMessage.collectAsState()
    val showUpdateDialog by viewModel.showUpdateDialog.collectAsState()
    val pendingUpdate by viewModel.pendingUpdate.collectAsState()
    val updateProgress by viewModel.updateProgress.collectAsState()
    val updateMessage by viewModel.updateMessage.collectAsState()
    val updateError by viewModel.updateError.collectAsState()

    LaunchedEffect(Unit) {
        viewModel.startSplash()
        viewModel.startBackgroundRefresh()
    }

    Box(Modifier.fillMaxSize()) {
        when {
            !splashDone -> SplashScreen(splashMessage)
            !isLoggedIn -> LoginScreen(viewModel)
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
        }
    }
}

@Composable
private fun SplashScreen(message: String) {
    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Column(horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.spacedBy(16.dp)) {
            CircularProgressIndicator(color = AppColors.Primary)
            Text(message, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
        }
    }
}
