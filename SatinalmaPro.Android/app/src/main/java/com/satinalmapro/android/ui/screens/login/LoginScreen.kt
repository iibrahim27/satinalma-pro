package com.satinalmapro.android.ui.screens.login

import androidx.compose.animation.animateColorAsState
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
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Check
import androidx.compose.material.icons.rounded.Cloud
import androidx.compose.material.icons.rounded.CloudOff
import androidx.compose.material.icons.rounded.Visibility
import androidx.compose.material.icons.rounded.VisibilityOff
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.BuildConfig
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.login.LoginBackground
import com.satinalmapro.android.ui.components.login.LoginHeroIcon
import com.satinalmapro.android.ui.components.login.loginCardEntrance
import com.satinalmapro.android.ui.components.login.loginShake
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing
import kotlinx.coroutines.delay

@Composable
fun LoginScreen(viewModel: AppViewModel) {
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var showPassword by remember { mutableStateOf(false) }
    var rememberMe by remember { mutableStateOf(false) }
    var shakeTrigger by remember { mutableIntStateOf(0) }
    var loginSuccess by remember { mutableStateOf(false) }
    var usernameFocused by remember { mutableStateOf(false) }
    var passwordFocused by remember { mutableStateOf(false) }
    var wasLoading by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        val savedEmail = viewModel.rememberedLoginEmail()
        if (savedEmail.isNotBlank()) {
            username = savedEmail
            rememberMe = true
        }
    }

    val error by viewModel.loginError.collectAsState()
    val loginMessage by viewModel.loginMessage.collectAsState()
    val loading by viewModel.loading.collectAsState()
    val serverOk = viewModel.isServerConfigured

    LaunchedEffect(error) {
        if (!error.isNullOrBlank()) shakeTrigger++
    }

    LaunchedEffect(loading) {
        if (wasLoading && !loading && error.isNullOrBlank()) {
            loginSuccess = true
            delay(500)
            loginSuccess = false
        }
        wasLoading = loading
    }

    val fieldError = !error.isNullOrBlank()
    val buttonColor by animateColorAsState(
        targetValue = if (loginSuccess) AppColors.Success else AppColors.Primary,
        label = "btn_color"
    )

    Box(Modifier.fillMaxSize()) {
        LoginBackground()

        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(horizontal = AppSpacing.screenHorizontal, vertical = 32.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(Modifier.height(16.dp))
            LoginHeroIcon(size = 104.dp)
            Spacer(Modifier.height(20.dp))
            Text(
                "Hoş Geldiniz",
                style = MaterialTheme.typography.headlineMedium,
                color = AppColors.TextPrimary,
                fontWeight = FontWeight.Bold
            )
            Text(
                "Kurumsal Satınalma Yönetim Sistemi",
                style = MaterialTheme.typography.bodyMedium,
                color = AppColors.TextSecondary,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = 6.dp, bottom = 24.dp)
            )

            Card(
                modifier = Modifier
                    .widthIn(max = 480.dp)
                    .fillMaxWidth()
                    .loginCardEntrance()
                    .loginShake(shakeTrigger),
                shape = AppShapes.extraLarge,
                colors = CardDefaults.cardColors(containerColor = AppColors.Surface.copy(alpha = 0.94f)),
                elevation = CardDefaults.cardElevation(defaultElevation = 8.dp)
            ) {
                Column(
                    modifier = Modifier.padding(24.dp),
                    verticalArrangement = Arrangement.spacedBy(14.dp)
                ) {
                    LoginHeroIcon(size = 56.dp, playEntrance = false)
                    Text(
                        "Giriş Yap",
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold,
                        color = AppColors.TextPrimary
                    )

                    OutlinedTextField(
                        value = username,
                        onValueChange = {
                            username = it
                            viewModel.clearLoginFeedback()
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .onFocusChanged { usernameFocused = it.isFocused },
                        label = { Text("Kullanıcı Adı") },
                        placeholder = { Text("kullanici.adi") },
                        singleLine = true,
                        shape = AppShapes.medium,
                        colors = loginFieldColors(usernameFocused, fieldError),
                        isError = fieldError
                    )

                    OutlinedTextField(
                        value = password,
                        onValueChange = {
                            password = it
                            viewModel.clearLoginFeedback()
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .onFocusChanged { passwordFocused = it.isFocused },
                        label = { Text("Şifre") },
                        singleLine = true,
                        shape = AppShapes.medium,
                        visualTransformation = if (showPassword) VisualTransformation.None else PasswordVisualTransformation(),
                        trailingIcon = {
                            IconButton(onClick = { showPassword = !showPassword }) {
                                Icon(
                                    if (showPassword) Icons.Rounded.VisibilityOff else Icons.Rounded.Visibility,
                                    contentDescription = if (showPassword) "Gizle" else "Göster"
                                )
                            }
                        },
                        colors = loginFieldColors(passwordFocused, fieldError),
                        isError = fieldError
                    )

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Checkbox(
                                checked = rememberMe,
                                onCheckedChange = {
                                    rememberMe = it
                                    if (!it) {
                                        username = ""
                                        viewModel.clearRememberedLogin()
                                    }
                                },
                                colors = CheckboxDefaults.colors(checkedColor = AppColors.Primary)
                            )
                            Text("Beni Hatırla", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                        }
                        TextButton(onClick = { viewModel.forgotPassword(username) }, enabled = !loading) {
                            Text("Şifremi Unuttum", color = AppColors.Primary, fontWeight = FontWeight.SemiBold)
                        }
                    }

                    error?.let {
                        Text(
                            it,
                            color = AppColors.Danger,
                            style = MaterialTheme.typography.bodySmall,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.fillMaxWidth()
                        )
                    }
                    loginMessage?.let {
                        Text(
                            it,
                            color = AppColors.Success,
                            style = MaterialTheme.typography.bodySmall,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.fillMaxWidth()
                        )
                    }

                    Button(
                        onClick = { viewModel.login(username, password, rememberMe = rememberMe) },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(52.dp),
                        enabled = username.isNotBlank() && password.isNotBlank() && !loading && !loginSuccess,
                        shape = AppShapes.medium,
                        colors = ButtonDefaults.buttonColors(containerColor = buttonColor),
                        elevation = ButtonDefaults.buttonElevation(defaultElevation = 4.dp, pressedElevation = 8.dp)
                    ) {
                        when {
                            loading -> CircularProgressIndicator(Modifier.size(22.dp), color = Color.White, strokeWidth = 2.dp)
                            loginSuccess -> Icon(Icons.Rounded.Check, contentDescription = null, tint = Color.White)
                            else -> Text("GİRİŞ YAP", fontWeight = FontWeight.Bold)
                        }
                    }
                }
            }

            Spacer(Modifier.height(24.dp))
            ServerStatusRow(connected = serverOk)
            Text(
                "Versiyon ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})",
                style = MaterialTheme.typography.labelMedium,
                color = AppColors.TextSecondary,
                textAlign = TextAlign.Center
            )
            Text(
                "Satınalma Pro",
                style = MaterialTheme.typography.labelSmall,
                color = AppColors.TextSecondary.copy(alpha = 0.8f),
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(top = 4.dp, bottom = 24.dp)
            )
        }
    }
}

@Composable
private fun ServerStatusRow(connected: Boolean) {
    val color = if (connected) AppColors.Success else AppColors.Warning
    val label = if (connected) "Sunucu bağlantısı aktif" else "Yerel mod — sunucu yapılandırılmamış"
    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        modifier = Modifier.padding(bottom = 8.dp)
    ) {
        Box(Modifier.size(8.dp).background(color, CircleShape))
        Icon(
            if (connected) Icons.Rounded.Cloud else Icons.Rounded.CloudOff,
            contentDescription = null,
            tint = color,
            modifier = Modifier.size(16.dp)
        )
        Text(label, style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
    }
}

@Composable
private fun loginFieldColors(focused: Boolean, error: Boolean) = OutlinedTextFieldDefaults.colors(
    focusedTextColor = AppColors.TextPrimary,
    unfocusedTextColor = AppColors.TextPrimary,
    focusedLabelColor = if (error) AppColors.Danger else AppColors.Primary,
    unfocusedLabelColor = if (error) AppColors.Danger else AppColors.TextSecondary,
    cursorColor = AppColors.Primary,
    focusedBorderColor = when {
        error -> AppColors.Danger
        focused -> Color(0xFF4F46E5)
        else -> AppColors.Border
    },
    unfocusedBorderColor = if (error) AppColors.Danger else AppColors.Border,
    errorBorderColor = AppColors.Danger,
    focusedContainerColor = AppColors.Surface,
    unfocusedContainerColor = AppColors.Surface
)
