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
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.data.DemoData
import com.satinalmapro.android.data.RecentActivity
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.IconBadge
import com.satinalmapro.android.ui.components.SectionTitle
import com.satinalmapro.android.ui.components.StatusBadge
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun HomeScreen(
    onNotificationsClick: () -> Unit,
    onQuickAction: (String) -> Unit,
    modifier: Modifier = Modifier
) {
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
                        "${DemoData.USER_NAME} 👋",
                        style = MaterialTheme.typography.headlineMedium,
                        color = AppColors.TextPrimary
                    )
                }
                BadgedBox(badge = { Badge { Text("3") } }) {
                    IconButton(onClick = onNotificationsClick) {
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
                modifier = Modifier.height(220.dp),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
                userScrollEnabled = false
            ) {
                items(DemoData.summaryStats) { (title, value, color) ->
                    SummaryCard(title, value, color)
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
                QuickAction(
                    modifier = Modifier.weight(1f),
                    icon = Icons.Rounded.Add,
                    label = "Talep\nOluştur",
                    bg = AppColors.PrimaryContainer,
                    fg = AppColors.Primary
                ) { onQuickAction("request") }
                QuickAction(
                    modifier = Modifier.weight(1f),
                    icon = Icons.Rounded.Inventory2,
                    label = "Malzeme\nGirişi",
                    bg = AppColors.SuccessContainer,
                    fg = AppColors.Success
                ) { onQuickAction("material") }
                QuickAction(
                    modifier = Modifier.weight(1f),
                    icon = Icons.Rounded.RequestQuote,
                    label = "Teklif\nEkle",
                    bg = AppColors.WarningContainer,
                    fg = AppColors.Warning
                ) { onQuickAction("quote") }
                QuickAction(
                    modifier = Modifier.weight(1f),
                    icon = Icons.Rounded.QrCodeScanner,
                    label = "QR Barkod\nOku",
                    bg = AppColors.PrimaryContainer,
                    fg = AppColors.IconPurple
                ) { onQuickAction("qr") }
            }
        }

        item {
            SectionTitle("Son İşlemler")
            Spacer(Modifier.height(8.dp))
            DemoData.recentActivities.forEach { activity ->
                RecentActivityCard(activity)
                Spacer(Modifier.height(10.dp))
            }
        }
    }
}

@Composable
private fun SummaryCard(title: String, value: String, accent: Color) {
    AppCard {
        Column(Modifier.padding(16.dp)) {
            IconBadge(accent) {
                Icon(Icons.Rounded.Description, null, tint = accent, modifier = Modifier.size(22.dp))
            }
            Spacer(Modifier.height(12.dp))
            Text(title, style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
            Text(value, style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
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
private fun RecentActivityCard(item: RecentActivity) {
    AppCard {
        Row(
            Modifier.padding(16.dp).fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Text(item.company, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                Text(item.material, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
            }
            StatusBadge(item.status.label, item.status.bg, item.status.fg)
        }
    }
}
