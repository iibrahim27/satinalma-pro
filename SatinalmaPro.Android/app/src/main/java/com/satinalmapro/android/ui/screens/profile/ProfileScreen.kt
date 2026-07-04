package com.satinalmapro.android.ui.screens.profile

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.Logout
import androidx.compose.material.icons.rounded.Lock
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.data.DemoData
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.DetailRow
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun ProfileScreen(onLogout: () -> Unit) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(horizontal = 20.dp, vertical = 16.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Surface(
            modifier = Modifier.size(88.dp),
            shape = AppShapes.extraLarge,
            color = AppColors.PrimaryContainer
        ) {
            Column(
                modifier = Modifier.fillMaxSize(),
                horizontalAlignment = Alignment.CenterHorizontally,
                verticalArrangement = Arrangement.Center
            ) {
                Text(
                    text = DemoData.USER_NAME.split(' ').mapNotNull { it.firstOrNull()?.uppercaseChar() }.take(2).joinToString(""),
                    style = MaterialTheme.typography.headlineMedium,
                    color = AppColors.Primary,
                    fontWeight = FontWeight.Bold
                )
            }
        }
        Spacer(Modifier.height(12.dp))
        Text(DemoData.USER_NAME, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
        Text(DemoData.USER_ROLE, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)

        Spacer(Modifier.height(24.dp))

        AppCard {
            Column(Modifier.padding(20.dp)) {
                DetailRow("Kullanıcı Adı", DemoData.USER_USERNAME)
                HorizontalDivider(color = AppColors.Border)
                DetailRow("Telefon", DemoData.USER_PHONE)
                HorizontalDivider(color = AppColors.Border)
                DetailRow("E-posta", DemoData.USER_EMAIL)
                HorizontalDivider(color = AppColors.Border)
                DetailRow("Departman", DemoData.USER_DEPARTMENT)
            }
        }

        Spacer(Modifier.height(16.dp))

        AppCard(onClick = { }) {
            RowMenuItem(Icons.Rounded.Lock, "Şifre Değiştir", AppColors.TextPrimary)
        }

        Spacer(Modifier.height(12.dp))

        AppCard(onClick = onLogout) {
            RowMenuItem(Icons.AutoMirrored.Rounded.Logout, "Çıkış Yap", AppColors.Danger)
        }

        Spacer(Modifier.height(88.dp))
    }
}

@Composable
private fun RowMenuItem(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
    color: Color
) {
    androidx.compose.foundation.layout.Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(18.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(icon, null, tint = color)
        Spacer(Modifier.size(12.dp))
        Text(label, style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.Medium), color = color)
    }
}
