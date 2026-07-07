package com.satinalmapro.android.ui.screens.login

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
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
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.BuildConfig
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AnimatedAppIcon
import com.satinalmapro.android.ui.components.AppPrimaryButton
import com.satinalmapro.android.ui.components.BottomWaveDecoration
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.android.ui.theme.appFieldColors

@Composable
fun LoginScreen(viewModel: AppViewModel) {
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var showPassword by remember { mutableStateOf(false) }
    var rememberMe by remember { mutableStateOf(true) }

    val error by viewModel.loginError.collectAsState()
    val loginMessage by viewModel.loginMessage.collectAsState()
    val loading by viewModel.loading.collectAsState()

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(AppColors.Background)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = AppSpacing.screenHorizontal, vertical = 40.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            AnimatedAppIcon(size = 88.dp)
            Spacer(Modifier.height(20.dp))
            Text(
                "Satınalma Pro",
                style = MaterialTheme.typography.headlineMedium,
                color = AppColors.TextPrimary
            )
            Text(
                "Kurumsal Satınalma Yönetim Sistemi",
                style = MaterialTheme.typography.bodyMedium,
                color = AppColors.TextSecondary,
                textAlign = TextAlign.Center
            )

            Spacer(Modifier.height(28.dp))

            OutlinedTextField(
                value = username,
                onValueChange = {
                    username = it
                    viewModel.clearLoginFeedback()
                },
                modifier = Modifier.fillMaxWidth(),
                label = { Text("Kullanıcı Adı") },
                singleLine = true,
                shape = AppShapes.small,
                colors = appFieldColors()
            )
            Spacer(Modifier.height(14.dp))
            OutlinedTextField(
                value = password,
                onValueChange = {
                    password = it
                    viewModel.clearLoginFeedback()
                },
                modifier = Modifier.fillMaxWidth(),
                label = { Text("Şifre") },
                singleLine = true,
                shape = AppShapes.small,
                visualTransformation = if (showPassword) VisualTransformation.None else PasswordVisualTransformation(),
                trailingIcon = {
                    IconButton(onClick = { showPassword = !showPassword }) {
                        Icon(
                            if (showPassword) Icons.Rounded.VisibilityOff else Icons.Rounded.Visibility,
                            contentDescription = if (showPassword) "Gizle" else "Göster"
                        )
                    }
                },
                colors = appFieldColors()
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
                        colors = CheckboxDefaults.colors(checkedColor = AppColors.Primary)
                    )
                    Text("Beni Hatırla", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                }
                TextButton(onClick = { viewModel.forgotPassword(username) }, enabled = !loading) {
                    Text("Şifremi Unuttum", color = AppColors.Primary)
                }
            }

            error?.let {
                Text(it, color = AppColors.Danger, modifier = Modifier.padding(top = 4.dp), textAlign = TextAlign.Center)
            }
            loginMessage?.let {
                Text(it, color = AppColors.Success, modifier = Modifier.padding(top = 4.dp), textAlign = TextAlign.Center)
            }

            Spacer(Modifier.height(20.dp))
            AppPrimaryButton(
                text = "GİRİŞ YAP",
                onClick = { viewModel.login(username, password, rememberMe = rememberMe) },
                enabled = username.isNotBlank() && password.isNotBlank(),
                loading = loading
            )

            Spacer(Modifier.height(32.dp))
            Text(
                "Versiyon ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})",
                style = MaterialTheme.typography.labelMedium,
                color = AppColors.TextSecondary,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth()
            )
            Spacer(Modifier.height(100.dp))
        }
        BottomWaveDecoration(modifier = Modifier.align(Alignment.BottomCenter))
    }
}
