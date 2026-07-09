package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.RolNavigasyon
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.EmptyState
import com.satinalmapro.android.ui.components.SectionHeader
import com.satinalmapro.android.ui.components.StatusPill
import com.satinalmapro.android.ui.theme.MetrikLight
import com.satinalmapro.android.ui.theme.MetrikSpace

@Composable
fun DashboardScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val talepler by viewModel.talepler.collectAsState()
    val badges by viewModel.menuBadges.collectAsState()
    val queues = RolNavigasyon.queueMenus(user?.role)
    val firma = TenantSession.tenantName().orEmpty()
    val recent = talepler.sortedByDescending { it.guncellemeUtc }.take(8)

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .background(MetrikLight.Primary)
                .padding(MetrikSpace.screen)
        ) {
            Text(
                firma.ifBlank { "Satınalma" },
                style = MaterialTheme.typography.headlineMedium,
                color = MetrikLight.TextOnPrimary,
                fontWeight = FontWeight.SemiBold
            )
            Spacer(Modifier.height(4.dp))
            Text(
                listOfNotNull(user?.fullName?.takeIf { it.isNotBlank() }, user?.role).joinToString(" · "),
                style = MaterialTheme.typography.bodyMedium,
                color = MetrikLight.TextOnPrimary.copy(alpha = 0.8f)
            )
            if (KullaniciRolleri.canCreateRequest(user?.role)) {
                Spacer(Modifier.height(MetrikSpace.lg))
                Text(
                    text = "Yeni talep oluştur →",
                    modifier = Modifier
                        .clickable { viewModel.navigateFromMenu("yeni-talep") }
                        .background(MetrikLight.Accent)
                        .padding(horizontal = 14.dp, vertical = 10.dp),
                    color = MetrikLight.TextOnAccent,
                    style = MaterialTheme.typography.labelLarge
                )
            }
        }

        SectionHeader("İş kuyrukları")
        if (queues.isEmpty()) {
            EmptyState(
                title = "Bu rol için kuyruk yok",
                subtitle = "Bildirimler ve profil üzerinden devam edebilirsiniz.",
                modifier = Modifier.height(180.dp)
            )
        } else {
            for (item in queues) {
                val count = badges[item.route] ?: 0
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { viewModel.navigateFromMenu(item.route) }
                        .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.md),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Column(modifier = Modifier.weight(1f)) {
                        Text(item.title, style = MaterialTheme.typography.titleMedium, color = MetrikLight.TextPrimary)
                        val group = item.group
                        if (!group.isNullOrBlank()) {
                            Text(group, style = MaterialTheme.typography.labelMedium, color = MetrikLight.TextTertiary)
                        }
                    }
                    if (count > 0) StatusPill("$count")
                }
                HorizontalDivider(color = MetrikLight.Divider)
            }
        }

        SectionHeader("Son hareketler")
        if (recent.isEmpty()) {
            EmptyState(
                title = "Bu firmada henüz kayıt yok",
                subtitle = "Talep oluşturulduğunda burada görünür.",
                modifier = Modifier.height(200.dp)
            )
        } else {
            for (t in recent) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { viewModel.navigate("talep-detay?id=${t.id}") }
                        .padding(horizontal = MetrikSpace.screen, vertical = MetrikSpace.md),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Column(modifier = Modifier.weight(1f)) {
                        Text(t.talepNo.ifBlank { "Talep" }, style = MaterialTheme.typography.titleMedium, color = MetrikLight.TextPrimary)
                        Text(t.malzemeOzeti, style = MaterialTheme.typography.bodySmall, color = MetrikLight.TextSecondary, maxLines = 1)
                    }
                    StatusPill(t.durum)
                }
                HorizontalDivider(color = MetrikLight.Divider)
            }
        }
        Spacer(Modifier.height(MetrikSpace.xl))
    }
}
