package com.satinalmapro.android.ui.screens.home

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Description
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material.icons.rounded.QrCodeScanner
import androidx.compose.material.icons.rounded.RequestQuote
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.DashboardActivity
import com.satinalmapro.android.core.model.DashboardCard
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.IconBadge
import com.satinalmapro.android.ui.components.SectionTitle
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun HomeScreen(
    viewModel: AppViewModel,
    modifier: Modifier = Modifier
) {
    val user by viewModel.user.collectAsState()
    val role = user?.role
    val canRequest = KullaniciRolleri.canCreateRequest(role)
    val canQuote = KullaniciRolleri.canEnterQuotes(role)
    val canMaterials = viewModel.canAccess("onaylanan-malzemeler")
    val notifications by viewModel.notifications.collectAsState()
    val unreadCount = notifications.count { !it.read }
    val cards by viewModel.dashboardCards.collectAsState()
    val activities by viewModel.dashboardActivities.collectAsState()
    LazyColumn(
        modifier = modifier.fillMaxSize(),
        contentPadding = PaddingValues(horizontal = 20.dp, vertical = 16.dp),
        verticalArrangement = Arrangement.spacedBy(20.dp)
    ) {
        item {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column {
                    Text("Merhaba,", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                    Text(
                        user?.fullName ?: "",
                        style = MaterialTheme.typography.headlineMedium,
                        color = AppColors.TextPrimary
                    )
                }
                BadgedBox(badge = { if (unreadCount > 0) Badge { Text("$unreadCount") } }) {
                    IconButton(onClick = { viewModel.navigate("bildirimler") }) {
                        Icon(Icons.Rounded.Notifications, contentDescription = "Bildirimler")
                    }
                }
            }
        }

        item {
            SectionTitle("Bugünkü Özet")
            Spacer(Modifier.height(12.dp))
            LazyVerticalGrid(
                columns = GridCells.Fixed(2),
                modifier = Modifier.height(((cards.size.coerceAtMost(4) + 1) / 2 * 110).dp),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
                userScrollEnabled = false
            ) {
                items(cards.take(4)) { card ->
                    SummaryCard(card) { viewModel.navigate(card.route) }
                }
            }
        }

        item {
            SectionTitle("Hızlı İşlemler")
            Spacer(Modifier.height(12.dp))
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                if (canRequest) {
                    QuickAction(
                        modifier = Modifier.weight(1f),
                        icon = Icons.Rounded.Add,
                        label = "Talep\nOluştur",
                        bg = AppColors.PrimaryContainer,
                        fg = AppColors.Primary
                    ) { viewModel.navigate("yeni-talep") }
                }
                if (canMaterials) {
                    QuickAction(
                        modifier = Modifier.weight(1f),
                        icon = Icons.Rounded.Inventory2,
                        label = "Malzeme\nGirişi",
                        bg = AppColors.SuccessContainer,
                        fg = AppColors.Success
                    ) { viewModel.navigate("onaylanan-malzemeler") }
                }
                if (canQuote) {
                    QuickAction(
                        modifier = Modifier.weight(1f),
                        icon = Icons.Rounded.RequestQuote,
                        label = "Teklif\nEkle",
                        bg = AppColors.WarningContainer,
                        fg = AppColors.Warning
                    ) { viewModel.navigate("teklif-gir") }
                }
            }
        }

        item {
            SectionTitle("Son İşlemler")
            Spacer(Modifier.height(8.dp))
            if (activities.isEmpty()) {
                Text("Henüz işlem yok.", color = AppColors.TextSecondary)
            } else {
                activities.forEach { activity ->
                    RecentActivityCard(activity) {
                        activity.route?.let { viewModel.navigate(it) }
                    }
                    Spacer(Modifier.height(10.dp))
                }
            }
        }
    }
}

@Composable
private fun SummaryCard(card: DashboardCard, onClick: () -> Unit) {
    AppCard(onClick = onClick) {
        Column(Modifier.padding(16.dp)) {
            IconBadge(AppColors.Primary) {
                Icon(Icons.Rounded.Description, null, tint = AppColors.Primary, modifier = Modifier.size(22.dp))
            }
            Spacer(Modifier.height(12.dp))
            Text(card.title, style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
            Text(card.value, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
            Text(card.subtitle, style = MaterialTheme.typography.labelSmall, color = AppColors.TextSecondary)
        }
    }
}

@Composable
private fun QuickAction(
    modifier: Modifier = Modifier,
    icon: ImageVector,
    label: String,
    bg: Color,
    fg: Color,
    onClick: () -> Unit
) {
    Surface(
        onClick = onClick,
        modifier = modifier,
        shape = AppShapes.medium,
        color = AppColors.Surface,
        border = androidx.compose.foundation.BorderStroke(1.dp, AppColors.Border)
    ) {
        Column(
            modifier = Modifier.padding(12.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            Surface(shape = AppShapes.small, color = bg) {
                Box(Modifier.padding(10.dp), contentAlignment = Alignment.Center) {
                    Icon(icon, null, tint = fg, modifier = Modifier.size(22.dp))
                }
            }
            Text(label, style = MaterialTheme.typography.labelMedium, color = AppColors.TextPrimary, textAlign = androidx.compose.ui.text.style.TextAlign.Center)
        }
    }
}

@Composable
private fun RecentActivityCard(item: DashboardActivity, onClick: () -> Unit) {
    AppCard(onClick = onClick) {
        Row(
            Modifier.padding(16.dp).fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Text(item.title, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                Text(item.subtitle, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
            }
            StatusBadge(item.status, AppColors.PrimaryContainer, AppColors.Primary)
        }
    }
}
