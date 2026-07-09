package com.satinalmapro.android.ui.auth

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Visibility
import androidx.compose.material.icons.rounded.VisibilityOff
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
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
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.Dp
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
                    listOf(
                        MetrikColors.PrimaryDark,
                        MetrikColors.Primary,
                        MetrikColors.Accent.copy(alpha = 0.85f)
                    )
                )
            )
            .statusBarsPadding(),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier.padding(horizontal = 32.dp)
        ) {
            BrandMark(size = 72.dp)
            Spacer(Modifier.height(20.dp))
            Text(
                "Satınalma Pro",
                style = MaterialTheme.typography.headlineLarge,
                color = MetrikColors.TextOnPrimary,
                fontWeight = FontWeight.Bold
            )
            Spacer(Modifier.height(6.dp))
            Text(
                "Kurumsal Satınalma Yönetim Sistemi",
                style = MaterialTheme.typography.bodyMedium,
                color = MetrikColors.TextOnPrimary.copy(alpha = 0.78f),
                textAlign = TextAlign.Center
            )
            Spacer(Modifier.height(28.dp))
            CircularProgressIndicator(
                color = MetrikColors.TextOnPrimary,
                strokeWidth = 2.dp,
                modifier = Modifier.size(28.dp)
            )
            Spacer(Modifier.height(12.dp))
            Text(
                message.ifBlank { "Yükleniyor..." },
                color = MetrikColors.TextOnPrimary.copy(alpha = 0.7f),
                style = MaterialTheme.typography.bodySmall,
                textAlign = TextAlign.Center
            )
            Spacer(Modifier.height(40.dp))
            Text(
                "v${BuildConfig.VERSION_NAME}",
                color = MetrikColors.TextOnPrimary.copy(alpha = 0.45f),
                style = MaterialTheme.typography.labelSmall
            )
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

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(
                Brush.verticalGradient(
                    listOf(MetrikColors.PrimaryDark, MetrikColors.Primary, MetrikColors.Background)
                )
            )
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .statusBarsPadding()
                .imePadding()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.xl),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(Modifier.height(24.dp))
            BrandMark(size = 64.dp)
            Spacer(Modifier.height(16.dp))
            Text(
                "Hoş Geldiniz",
                style = MaterialTheme.typography.headlineLarge,
                color = MetrikColors.TextOnPrimary,
                fontWeight = FontWeight.Bold
            )
            Spacer(Modifier.height(6.dp))
            Text(
                "Talep, teklif, onay ve stok süreçlerinizi tek merkezden yönetin.",
                style = MaterialTheme.typography.bodyMedium,
                color = MetrikColors.TextOnPrimary.copy(alpha = 0.75f),
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(horizontal = 12.dp)
            )
            Spacer(Modifier.height(28.dp))

            Surface(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(24.dp),
                color = MetrikColors.Surface,
                tonalElevation = 2.dp,
                shadowElevation = 8.dp
            ) {
                Column(
                    modifier = Modifier.padding(horizontal = 22.dp, vertical = 24.dp)
                ) {
                    Text(
                        "Giriş Yap",
                        style = MaterialTheme.typography.headlineMedium,
                        color = MetrikColors.TextPrimary,
                        fontWeight = FontWeight.Bold
                    )
                    Spacer(Modifier.height(4.dp))
                    Text(
                        "Hesabınıza erişmek için bilgilerinizi girin",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MetrikColors.TextSecondary
                    )
                    Spacer(Modifier.height(20.dp))

                    MetrikField(
                        value = username,
                        onValueChange = {
                            username = it
                            viewModel.clearLoginFeedback()
                        },
                        label = "Kullanıcı adı",
                        placeholder = "kullanici.adi"
                    )
                    Spacer(Modifier.height(12.dp))
                    MetrikField(
                        value = password,
                        onValueChange = {
                            password = it
                            viewModel.clearLoginFeedback()
                        },
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
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Checkbox(
                                checked = rememberMe,
                                onCheckedChange = { rememberMe = it },
                                colors = CheckboxDefaults.colors(checkedColor = MetrikColors.Accent)
                            )
                            Text(
                                "Beni hatırla",
                                style = MaterialTheme.typography.bodyMedium,
                                color = MetrikColors.TextSecondary
                            )
                        }
                        TextButton(onClick = {
                            if (username.isNotBlank()) viewModel.forgotPassword(username.trim())
                        }) {
                            Text("Şifremi unuttum", color = MetrikColors.Accent)
                        }
                    }

                    if (!loginError.isNullOrBlank()) {
                        Text(
                            loginError!!,
                            color = MetrikColors.Danger,
                            style = MaterialTheme.typography.bodySmall,
                            modifier = Modifier.padding(bottom = 8.dp)
                        )
                    }
                    if (!loginMessage.isNullOrBlank()) {
                        Text(
                            loginMessage!!,
                            color = MetrikColors.Success,
                            style = MaterialTheme.typography.bodySmall,
                            modifier = Modifier.padding(bottom = 8.dp)
                        )
                    }

                    MetrikButton(
                        text = "Giriş Yap",
                        onClick = { viewModel.login(username.trim(), password, rememberMe) },
                        modifier = Modifier.fillMaxWidth(),
                        accent = true,
                        loading = loading,
                        enabled = username.isNotBlank() && password.isNotBlank()
                    )
                }
            }

            Spacer(Modifier.height(20.dp))
            Text(
                "Satınalma Pro · v${BuildConfig.VERSION_NAME}",
                color = MetrikColors.TextOnPrimary.copy(alpha = 0.55f),
                style = MaterialTheme.typography.labelMedium
            )
            Spacer(Modifier.height(12.dp))
        }
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
            BrandMark(size = 56.dp)
            Spacer(Modifier.height(20.dp))
            Text(
                "Kilidi aç",
                style = MaterialTheme.typography.headlineMedium,
                color = MetrikColors.TextPrimary,
                fontWeight = FontWeight.Bold
            )
            Spacer(Modifier.height(8.dp))
            Text(
                "Parmak izi veya cihaz kilidi ile devam edin.",
                style = MaterialTheme.typography.bodyMedium,
                color = MetrikColors.TextSecondary,
                textAlign = TextAlign.Center
            )
            if (!error.isNullOrBlank()) {
                Spacer(Modifier.height(12.dp))
                Text(error!!, color = MetrikColors.Danger, textAlign = TextAlign.Center)
            }
            Spacer(Modifier.height(24.dp))
            MetrikButton(
                text = "Tekrar dene",
                onClick = { activity?.let { viewModel.promptBiometricUnlock(it) } },
                accent = true,
                modifier = Modifier.fillMaxWidth(0.7f)
            )
            Spacer(Modifier.height(8.dp))
            TextButton(onClick = { viewModel.logout() }) {
                Text("Çıkış yap", color = MetrikColors.Primary)
            }
        }
    }
}

@Composable
private fun BrandMark(size: Dp) {
    Box(
        modifier = Modifier
            .size(size)
            .background(
                Brush.linearGradient(listOf(Color(0xFF4F46E5), Color(0xFF6D5BFF))),
                RoundedCornerShape(16.dp)
            ),
        contentAlignment = Alignment.Center
    ) {
        Text(
            "SP",
            color = Color.White,
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Bold
        )
    }
}
