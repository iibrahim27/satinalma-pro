package com.satinalmapro.android.ui.auth

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Visibility
import androidx.compose.material.icons.rounded.VisibilityOff
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.BuildConfig
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.components.MetrikButton
import com.satinalmapro.android.ui.components.MetrikField
import com.satinalmapro.android.ui.theme.MetrikColors
import com.satinalmapro.android.ui.theme.MetrikSpace

@Composable
fun SplashScreen(message: String) {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(
                Brush.verticalGradient(
                    listOf(MetrikColors.PrimaryDark, MetrikColors.Primary, MetrikColors.Background)
                )
            ),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Text(
                "METRİK",
                style = MaterialTheme.typography.displayLarge,
                color = MetrikColors.TextOnPrimary,
                fontWeight = FontWeight.Bold
            )
            Spacer(Modifier.height(8.dp))
            Text(
                "Satınalma",
                style = MaterialTheme.typography.titleLarge,
                color = MetrikColors.AccentMuted
            )
            Spacer(Modifier.height(24.dp))
            Text(message, color = MetrikColors.TextOnPrimary.copy(alpha = 0.8f), style = MaterialTheme.typography.bodyMedium)
        }
    }
}

@Composable
fun LoginScreen(viewModel: AppViewModel) {
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var showPassword by remember { mutableStateOf(false) }
    var rememberMe by remember { mutableStateOf(false) }
    val loading by viewModel.loading.collectAsState()
    val loginError by viewModel.loginError.collectAsState()
    val loginMessage by viewModel.loginMessage.collectAsState()

    LaunchedEffect(Unit) {
        val saved = viewModel.rememberedLoginEmail()
        if (saved.isNotBlank()) {
            username = saved
            rememberMe = true
        }
    }

    Column(modifier = Modifier.fillMaxSize()) {
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .weight(0.42f)
                .background(
                    Brush.verticalGradient(listOf(MetrikColors.PrimaryDark, MetrikColors.Primary))
                )
                .padding(MetrikSpace.xl),
            contentAlignment = Alignment.BottomStart
        ) {
            Column {
                Text("METRİK", style = MaterialTheme.typography.displayLarge, color = MetrikColors.TextOnPrimary, fontWeight = FontWeight.Bold)
                Spacer(Modifier.height(6.dp))
                Text("Satınalma operasyon paneli", style = MaterialTheme.typography.titleMedium, color = MetrikColors.AccentMuted)
                Spacer(Modifier.height(4.dp))
                Text("v${BuildConfig.VERSION_NAME}", style = MaterialTheme.typography.labelMedium, color = MetrikColors.TextOnPrimary.copy(alpha = 0.55f))
            }
        }

        Column(
            modifier = Modifier
                .weight(0.58f)
                .fillMaxWidth()
                .background(MetrikColors.Background)
                .verticalScroll(rememberScrollState())
                .padding(MetrikSpace.screen),
            verticalArrangement = Arrangement.Top
        ) {
            Spacer(Modifier.height(MetrikSpace.lg))
            Text("Giriş", style = MaterialTheme.typography.headlineMedium, color = MetrikColors.TextPrimary)
            Spacer(Modifier.height(4.dp))
            Text("Kullanıcı adı ve şifrenizle devam edin.", style = MaterialTheme.typography.bodyMedium, color = MetrikColors.TextSecondary)
            Spacer(Modifier.height(MetrikSpace.xl))

            MetrikField(value = username, onValueChange = { username = it; viewModel.clearLoginFeedback() }, label = "Kullanıcı adı")
            Spacer(Modifier.height(MetrikSpace.md))
            MetrikField(
                value = password,
                onValueChange = { password = it; viewModel.clearLoginFeedback() },
                label = "Şifre",
                visualTransformation = if (showPassword) VisualTransformation.None else PasswordVisualTransformation(),
                trailingIcon = {
                    IconButton(onClick = { showPassword = !showPassword }) {
                        Icon(
                            if (showPassword) Icons.Rounded.VisibilityOff else Icons.Rounded.Visibility,
                            contentDescription = null,
                            tint = MetrikColors.TextSecondary
                        )
                    }
                }
            )
            Spacer(Modifier.height(MetrikSpace.sm))
            RowCheck(rememberMe) { rememberMe = it }
            Spacer(Modifier.height(MetrikSpace.lg))

            if (!loginError.isNullOrBlank()) {
                Text(loginError!!, color = MetrikColors.Danger, style = MaterialTheme.typography.bodySmall)
                Spacer(Modifier.height(MetrikSpace.sm))
            }
            if (!loginMessage.isNullOrBlank()) {
                Text(loginMessage!!, color = MetrikColors.Success, style = MaterialTheme.typography.bodySmall)
                Spacer(Modifier.height(MetrikSpace.sm))
            }

            MetrikButton(
                text = "Giriş yap",
                onClick = { viewModel.login(username.trim(), password, rememberMe) },
                modifier = Modifier.fillMaxWidth(),
                accent = true,
                loading = loading,
                enabled = username.isNotBlank() && password.isNotBlank()
            )
            TextButton(onClick = {
                if (username.isNotBlank()) viewModel.forgotPassword(username.trim())
            }) {
                Text("Şifremi unuttum", color = MetrikColors.Primary)
            }
        }
    }
}

@Composable
private fun RowCheck(checked: Boolean, onChange: (Boolean) -> Unit) {
    androidx.compose.foundation.layout.Row(verticalAlignment = Alignment.CenterVertically) {
        Checkbox(
            checked = checked,
            onCheckedChange = onChange,
            colors = CheckboxDefaults.colors(checkedColor = MetrikColors.Accent)
        )
        Text("Beni hatırla", style = MaterialTheme.typography.bodyMedium, color = MetrikColors.TextSecondary)
    }
}

@Composable
fun BiometricUnlockScreen(viewModel: AppViewModel) {
    val activity = LocalFragmentActivity.current
    val error by viewModel.biometricError.collectAsState()

    LaunchedEffect(Unit) {
        activity?.let { viewModel.promptBiometricUnlock(it) }
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikColors.Background)
            .padding(MetrikSpace.xl),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Text("Kilidi aç", style = MaterialTheme.typography.headlineMedium, color = MetrikColors.TextPrimary)
            Spacer(Modifier.height(8.dp))
            Text("Parmak izi veya cihaz kilidi ile devam edin.", style = MaterialTheme.typography.bodyMedium, color = MetrikColors.TextSecondary)
            if (!error.isNullOrBlank()) {
                Spacer(Modifier.height(12.dp))
                Text(error!!, color = MetrikColors.Danger)
            }
            Spacer(Modifier.height(24.dp))
            MetrikButton(text = "Tekrar dene", onClick = { activity?.let { viewModel.promptBiometricUnlock(it) } }, accent = true)
            Spacer(Modifier.height(8.dp))
            TextButton(onClick = { viewModel.logout() }) {
                Text("Çıkış yap", color = MetrikColors.Primary)
            }
        }
    }
}
