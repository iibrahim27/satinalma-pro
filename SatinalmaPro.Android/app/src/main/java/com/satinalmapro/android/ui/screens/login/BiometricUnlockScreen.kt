package com.satinalmapro.android.ui.screens.login

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Fingerprint
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.components.AnimatedAppIcon
import com.satinalmapro.android.ui.components.AppPrimaryButton
import com.satinalmapro.android.ui.components.BottomWaveDecoration
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppSpacing
import kotlinx.coroutines.delay

@Composable
fun BiometricUnlockScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val error by viewModel.biometricError.collectAsState()
    val activity = LocalFragmentActivity.current

    LaunchedEffect(activity) {
        val act = activity ?: return@LaunchedEffect
        delay(300)
        viewModel.promptBiometricUnlock(act)
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(AppColors.Background)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(horizontal = AppSpacing.screenHorizontal, vertical = 48.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            AnimatedAppIcon(size = 88.dp)
            Spacer(Modifier.height(20.dp))
            Text(
                "Satınalma Pro",
                style = MaterialTheme.typography.headlineMedium,
                color = AppColors.TextPrimary
            )
            Text(
                user?.fullName.orEmpty(),
                style = MaterialTheme.typography.bodyMedium,
                color = AppColors.TextSecondary
            )
            Spacer(Modifier.height(32.dp))
            Icon(
                Icons.Rounded.Fingerprint,
                contentDescription = null,
                modifier = Modifier.size(72.dp),
                tint = AppColors.Primary
            )
            Spacer(Modifier.height(16.dp))
            Text(
                "Uygulamaya devam etmek için parmak izi veya ekran kilidi ile doğrulayın.",
                style = MaterialTheme.typography.bodyMedium,
                color = AppColors.TextSecondary,
                textAlign = TextAlign.Center
            )
            error?.let {
                Spacer(Modifier.height(12.dp))
                Text(it, color = AppColors.Danger, textAlign = TextAlign.Center)
            }
            Spacer(Modifier.height(28.dp))
            AppPrimaryButton(
                text = "DOĞRULA",
                onClick = {
                    activity?.let { viewModel.promptBiometricUnlock(it) }
                        ?: run { viewModel.setBiometricError("Doğrulama başlatılamadı") }
                },
                modifier = Modifier.fillMaxWidth()
            )
        }
        BottomWaveDecoration(modifier = Modifier.align(Alignment.BottomCenter))
    }
}
