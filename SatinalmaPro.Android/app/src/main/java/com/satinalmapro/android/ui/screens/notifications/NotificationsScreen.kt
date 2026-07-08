package com.satinalmapro.android.ui.screens.notifications

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.CheckCircle
import androidx.compose.material.icons.rounded.Info
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material.icons.rounded.Schedule
import androidx.compose.material.icons.rounded.Cancel
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.AppNotification
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.AppEmptyState
import com.satinalmapro.android.ui.components.AppPullRefreshBox
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.android.ui.theme.StatusType
import com.satinalmapro.android.ui.theme.notificationStatusColor
import com.satinalmapro.android.ui.theme.statusColors

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NotificationsScreen(viewModel: AppViewModel) {
    val notifications by viewModel.notifications.collectAsState()
    val user by viewModel.user.collectAsState()
    val loading by viewModel.loading.collectAsState()
    val unreadCount = notifications.count { !it.read }

    AppPullRefreshBox(isRefreshing = loading, onRefresh = { viewModel.refreshData() }) {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            FilledTonalButton(
                onClick = { viewModel.markAllNotificationsRead() },
                enabled = !loading && unreadCount > 0,
                modifier = Modifier.weight(1f),
                shape = AppShapes.small
            ) {
                Icon(Icons.Rounded.CheckCircle, contentDescription = null, modifier = Modifier.size(18.dp))
                Text(" Okundu", style = MaterialTheme.typography.labelMedium)
            }
            OutlinedButton(
                onClick = { viewModel.clearNotifications() },
                enabled = !loading && notifications.isNotEmpty(),
                modifier = Modifier.weight(1f),
                shape = AppShapes.small
            ) {
                Text("Temizle", style = MaterialTheme.typography.labelMedium)
            }
        }

        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
        ) {
            if (notifications.isEmpty()) {
                item { AppEmptyState("Bildirim yok.") }
            }
            items(notifications, key = { it.id }) { item ->
                NotificationCard(item) {
                    val route = item.route?.takeIf { it.isNotBlank() }
                        ?: BildirimRota.hedefRoute(
                            BildirimRota.normalizeTip(item.type),
                            item.requestId,
                            user?.role
                        )
                    viewModel.openNotification(item.id, route)
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun NotificationCard(item: AppNotification, onClick: () -> Unit) {
    val accent = notificationStatusColor(item.type)
    val statusType = when (item.type) {
        "Onaylandi", "MalKabulEdildi" -> StatusType.Approved
        "Reddedildi" -> StatusType.Rejected
        "Bekliyor", "OnayBekliyor" -> StatusType.Pending
        else -> StatusType.Info
    }
    val (statusBg, statusFg) = statusColors(statusType)
    val icon = when (statusType) {
        StatusType.Approved -> Icons.Rounded.CheckCircle
        StatusType.Rejected -> Icons.Rounded.Cancel
        StatusType.Pending -> Icons.Rounded.Schedule
        StatusType.Info -> Icons.Rounded.Info
    }

    AppCard(
        onClick = onClick,
        containerColor = if (!item.read) AppColors.Surface else AppColors.Background,
        contentPadding = 0.dp
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(horizontal = 16.dp, vertical = 14.dp),
            verticalAlignment = Alignment.Top,
            horizontalArrangement = Arrangement.spacedBy(14.dp)
        ) {
            Box(contentAlignment = Alignment.Center) {
                Surface(
                    shape = CircleShape,
                    color = accent.copy(alpha = 0.14f),
                    modifier = Modifier.size(48.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Icon(icon, contentDescription = null, tint = accent, modifier = Modifier.size(24.dp))
                    }
                }
                if (!item.read) {
                    Box(
                        modifier = Modifier
                            .align(Alignment.TopEnd)
                            .size(10.dp)
                            .clip(CircleShape)
                            .background(AppColors.Primary)
                    )
                }
            }
            Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        item.title,
                        style = MaterialTheme.typography.titleMedium,
                        color = AppColors.TextPrimary,
                        fontWeight = if (!item.read) FontWeight.Bold else FontWeight.SemiBold,
                        modifier = Modifier.weight(1f)
                    )
                    Text(item.time, style = MaterialTheme.typography.labelSmall, color = AppColors.TextSecondary)
                }
                Text(
                    item.message,
                    style = MaterialTheme.typography.bodyMedium,
                    color = AppColors.TextSecondary
                )
                Surface(shape = AppShapes.extraSmall, color = statusBg) {
                    Text(
                        notificationStatusLabel(item.type),
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp),
                        style = MaterialTheme.typography.labelSmall,
                        color = statusFg,
                        fontWeight = FontWeight.Medium
                    )
                }
            }
        }
    }
}

private fun notificationStatusLabel(type: String): String = when (type) {
    "Onaylandi", "MalKabulEdildi" -> "Onaylandı"
    "Reddedildi" -> "Reddedildi"
    "Bekliyor", "OnayBekliyor" -> "Bekliyor"
    else -> "Bilgilendirme"
}
