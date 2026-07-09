package com.satinalmapro.android.ui.procurement



import androidx.compose.foundation.layout.Arrangement

import androidx.compose.foundation.layout.Column

import androidx.compose.foundation.layout.Row

import androidx.compose.foundation.layout.Spacer

import androidx.compose.foundation.layout.fillMaxSize

import androidx.compose.foundation.layout.fillMaxWidth

import androidx.compose.foundation.layout.height

import androidx.compose.foundation.layout.padding

import androidx.compose.foundation.layout.size

import androidx.compose.foundation.rememberScrollState

import androidx.compose.foundation.text.KeyboardOptions

import androidx.compose.foundation.verticalScroll

import androidx.compose.material.icons.Icons

import androidx.compose.material.icons.automirrored.rounded.Logout

import androidx.compose.material.icons.rounded.Fingerprint

import androidx.compose.material.icons.rounded.Lock

import androidx.compose.material.icons.rounded.Settings

import androidx.compose.material.icons.rounded.SystemUpdate

import androidx.compose.material.icons.rounded.Visibility

import androidx.compose.material.icons.rounded.VisibilityOff

import androidx.compose.material3.AlertDialog

import androidx.compose.material3.HorizontalDivider

import androidx.compose.material3.Icon

import androidx.compose.material3.IconButton

import androidx.compose.material3.MaterialTheme

import androidx.compose.material3.OutlinedTextField

import androidx.compose.material3.Surface

import androidx.compose.material3.Switch

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

import androidx.compose.ui.text.font.FontWeight

import androidx.compose.ui.text.input.KeyboardType

import androidx.compose.ui.text.input.PasswordVisualTransformation

import androidx.compose.ui.text.input.VisualTransformation

import androidx.compose.ui.unit.dp
import com.satinalmapro.android.BuildConfig

import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.saas.TenantSession

import com.satinalmapro.android.ui.AppViewModel

import com.satinalmapro.android.ui.LocalFragmentActivity

import com.satinalmapro.android.ui.components.AppCard

import com.satinalmapro.android.ui.components.DetailRow

import com.satinalmapro.android.ui.components.AppScreenContent
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes



@Composable

fun ProfileScreen(viewModel: AppViewModel) {

    val user by viewModel.user.collectAsState()

    val updateMessage by viewModel.updateMessage.collectAsState()

    val updateError by viewModel.updateError.collectAsState()

    val biometricEnabled by viewModel.biometricEnabled.collectAsState()

    val biometricAvailable by viewModel.biometricAvailable.collectAsState()

    val profileMessage by viewModel.profileMessage.collectAsState()

    val profileError by viewModel.profileError.collectAsState()

    val loading by viewModel.loading.collectAsState()

    val activity = LocalFragmentActivity.current

    var showPasswordDialog by remember { mutableStateOf(false) }

    var showLogoutDialog by remember { mutableStateOf(false) }

    val profile = user ?: return



    if (showPasswordDialog) {

        ChangePasswordDialog(

            loading = loading,

            onDismiss = { showPasswordDialog = false },

            onConfirm = { current, newPass, confirm ->

                viewModel.changePassword(current, newPass, confirm) {

                    showPasswordDialog = false

                }

            }

        )

    }

    if (showLogoutDialog) {
        AlertDialog(
            onDismissRequest = { showLogoutDialog = false },
            title = { Text("Hesaptan çıkış") },
            text = { Text("Oturumu kapatıp giriş ekranına dönmek istiyor musunuz?") },
            confirmButton = {
                TextButton(
                    onClick = {
                        showLogoutDialog = false
                        viewModel.logout()
                    }
                ) {
                    Text("Çıkış yap", color = AppColors.Danger)
                }
            },
            dismissButton = {
                TextButton(onClick = { showLogoutDialog = false }) {
                    Text("İptal")
                }
            }
        )
    }



    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
    ) {
    AppScreenContent(horizontalAlignment = Alignment.CenterHorizontally) {

        Surface(modifier = Modifier.size(88.dp), shape = AppShapes.extraLarge, color = AppColors.PrimaryContainer) {

            Column(

                modifier = Modifier.fillMaxSize(),

                horizontalAlignment = Alignment.CenterHorizontally,

                verticalArrangement = Arrangement.Center

            ) {

                Text(

                    text = profile.fullName.split(' ').mapNotNull { it.firstOrNull()?.uppercaseChar() }.take(2).joinToString(""),

                    style = MaterialTheme.typography.headlineMedium,

                    color = AppColors.Primary,

                    fontWeight = FontWeight.Bold

                )

            }

        }

        Spacer(Modifier.height(12.dp))

        Text(profile.fullName, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)

        Text(profile.role, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)



        Spacer(Modifier.height(24.dp))

        AppCard {

            Column {

                DetailRow("E-posta", profile.email)

                HorizontalDivider(color = AppColors.Border)

                DetailRow("Rol", profile.role)

                profile.phone?.let {

                    HorizontalDivider(color = AppColors.Border)

                    DetailRow("Telefon", it)

                }

                profile.site?.let {

                    HorizontalDivider(color = AppColors.Border)

                    DetailRow("Şantiye / Saha", it)

                }

            }

        }



        Spacer(Modifier.height(16.dp))

        if (KullaniciRolleri.normalize(profile.role) == KullaniciRolleri.SATINALMA) {

            AppCard(onClick = { viewModel.navigateFromMenu("ayarlar") }) {

                RowMenuItem(Icons.Rounded.Settings, "Ayarlar (Roller ve Terimler)", AppColors.Primary)

            }

            Spacer(Modifier.height(12.dp))

        }

        AppCard(onClick = { viewModel.checkForUpdates(userInitiated = true) }) {

            RowMenuItem(Icons.Rounded.SystemUpdate, "Güncelleme Kontrol Et", AppColors.Primary)

        }

        Spacer(Modifier.height(8.dp))

        Text(

            "Yüklü sürüm: ${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})",

            style = MaterialTheme.typography.labelMedium,

            color = AppColors.TextSecondary

        )

        TenantSession.license()?.kisaDurumMetni?.let { lisans ->
            Spacer(Modifier.height(4.dp))
            Text(
                lisans,
                style = MaterialTheme.typography.labelMedium,
                color = if (lisans.contains("doldu", ignoreCase = true)) AppColors.Danger else AppColors.TextSecondary
            )
        }

        updateMessage?.let {

            Spacer(Modifier.height(6.dp))

            Text(it, style = MaterialTheme.typography.bodySmall, color = AppColors.Primary)

        }

        updateError?.let {

            Spacer(Modifier.height(6.dp))

            Text(it, style = MaterialTheme.typography.bodySmall, color = AppColors.Danger)

        }

        profileMessage?.let {

            Spacer(Modifier.height(6.dp))

            Text(it, style = MaterialTheme.typography.bodySmall, color = AppColors.Primary)

        }

        profileError?.let {

            Spacer(Modifier.height(6.dp))

            Text(it, style = MaterialTheme.typography.bodySmall, color = AppColors.Danger)

        }



        Spacer(Modifier.height(16.dp))

        if (biometricAvailable) {

            AppCard {

                Row(

                    modifier = Modifier.fillMaxWidth().padding(18.dp),

                    verticalAlignment = Alignment.CenterVertically

                ) {

                    Icon(Icons.Rounded.Fingerprint, null, tint = AppColors.Primary)

                    Spacer(Modifier.size(12.dp))

                    Column(Modifier.weight(1f)) {

                        Text("Biyometrik Kilit", style = MaterialTheme.typography.titleMedium)

                        Text(

                            "Uygulama açılışında parmak izi veya ekran kilidi",

                            style = MaterialTheme.typography.bodySmall,

                            color = AppColors.TextSecondary

                        )

                    }

                    Switch(

                        checked = biometricEnabled,

                        onCheckedChange = { enabled ->

                            if (enabled) {

                                val host = activity

                                if (host != null) {

                                    viewModel.enableBiometricWithVerification(host)

                                } else {

                                    viewModel.setBiometricError("Doğrulama başlatılamadı, uygulamayı yeniden açın")

                                }

                            } else {

                                viewModel.setBiometricEnabled(false)

                            }

                        }

                    )

                }

            }

            Spacer(Modifier.height(12.dp))

        }

        AppCard(onClick = { showPasswordDialog = true }) {

            RowMenuItem(Icons.Rounded.Lock, "Şifre Değiştir", AppColors.TextPrimary)

        }

        Spacer(Modifier.height(12.dp))

        AppCard(onClick = { showLogoutDialog = true }) {
            RowMenuItem(Icons.AutoMirrored.Rounded.Logout, "Hesaptan çıkış yap", AppColors.Danger)
        }

        Spacer(Modifier.height(24.dp))

    }

    }

}



@Composable

private fun ChangePasswordDialog(

    loading: Boolean,

    onDismiss: () -> Unit,

    onConfirm: (current: String, newPassword: String, confirm: String) -> Unit

) {

    var current by remember { mutableStateOf("") }

    var newPassword by remember { mutableStateOf("") }

    var confirm by remember { mutableStateOf("") }

    var showCurrent by remember { mutableStateOf(false) }

    var showNew by remember { mutableStateOf(false) }

    var showConfirm by remember { mutableStateOf(false) }



    AlertDialog(

        onDismissRequest = { if (!loading) onDismiss() },

        title = { Text("Şifre Değiştir") },

        text = {

            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {

                PasswordField("Mevcut Şifre", current, { current = it }, showCurrent) { showCurrent = !showCurrent }

                PasswordField("Yeni Şifre", newPassword, { newPassword = it }, showNew) { showNew = !showNew }

                PasswordField("Yeni Şifre (Tekrar)", confirm, { confirm = it }, showConfirm) { showConfirm = !showConfirm }

            }

        },

        confirmButton = {

            TextButton(

                onClick = { onConfirm(current, newPassword, confirm) },

                enabled = !loading && current.isNotBlank() && newPassword.isNotBlank() && confirm.isNotBlank()

            ) {

                Text(if (loading) "Kaydediliyor..." else "Kaydet")

            }

        },

        dismissButton = {

            TextButton(onClick = onDismiss, enabled = !loading) { Text("İptal") }

        }

    )

}



@Composable

private fun PasswordField(

    label: String,

    value: String,

    onValueChange: (String) -> Unit,

    visible: Boolean,

    onToggleVisibility: () -> Unit

) {

    OutlinedTextField(

        value = value,

        onValueChange = onValueChange,

        modifier = Modifier.fillMaxWidth(),

        label = { Text(label) },

        singleLine = true,

        shape = AppShapes.medium,

        visualTransformation = if (visible) VisualTransformation.None else PasswordVisualTransformation(),

        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),

        trailingIcon = {

            IconButton(onClick = onToggleVisibility) {

                Icon(

                    if (visible) Icons.Rounded.VisibilityOff else Icons.Rounded.Visibility,

                    contentDescription = null

                )

            }

        }

    )

}



@Composable

private fun RowMenuItem(icon: androidx.compose.ui.graphics.vector.ImageVector, label: String, color: androidx.compose.ui.graphics.Color) {

    Row(

        modifier = Modifier.fillMaxWidth().padding(18.dp),

        verticalAlignment = Alignment.CenterVertically

    ) {

        Icon(icon, null, tint = color)

        Spacer(Modifier.size(12.dp))

        Text(label, style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Medium), color = color)

    }

}

