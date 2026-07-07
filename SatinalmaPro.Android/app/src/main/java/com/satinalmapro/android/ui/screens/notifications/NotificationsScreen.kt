package com.satinalmapro.android.ui.screens.notifications

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material3.ExperimentalMaterial3Api
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.AppNotification
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.AppCard
import com.satinalmapro.android.ui.components.AppEmptyState
import com.satinalmapro.android.ui.components.AppPullRefreshBox
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppSpacing

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
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            OutlinedButton(
                onClick = { viewModel.markAllNotificationsRead() },
                enabled = !loading && unreadCount > 0,
                modifier = Modifier.weight(1f)
            ) {
                Text("Tümünü okundu işaretle")
            }
            OutlinedButton(
                onClick = { viewModel.clearNotifications() },
                enabled = !loading && notifications.isNotEmpty(),
                modifier = Modifier.weight(1f)
            ) {
                Text("Temizle")
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
                NotificationTimelineItem(item) {
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

@Composable
private fun NotificationTimelineItem(item: AppNotification, onClick: () -> Unit) {
    val accent = when (item.type) {
        "Onaylandi", "MalKabulEdildi" -> AppColors.Success
        "Reddedildi" -> AppColors.Danger
        else -> AppColors.Primary
    }
    AppCard(onClick = onClick) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.Top
        ) {
            Surface(shape = CircleShape, color = accent, modifier = Modifier.size(12.dp)) {}
            Spacer(Modifier.width(14.dp))
            Column(Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        item.time,
                        style = MaterialTheme.typography.labelMedium,
                        color = AppColors.TextSecondary,
                        modifier = Modifier.weight(1f)
                    )
                    if (!item.read) {
                        Text(
                            "Yeni",
                            style = MaterialTheme.typography.labelSmall,
                            color = AppColors.Primary,
                            fontWeight = FontWeight.SemiBold
                        )
                    }
                }
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(
                        Icons.Rounded.Notifications,
                        null,
                        tint = accent,
                        modifier = Modifier.size(18.dp)
                    )
                    Spacer(Modifier.width(6.dp))
                    Text(
                        item.title,
                        style = MaterialTheme.typography.titleMedium,
                        color = AppColors.TextPrimary
                    )
                }
                Text(
                    item.message,
                    style = MaterialTheme.typography.bodyMedium,
                    color = AppColors.TextSecondary,
                    modifier = Modifier.padding(top = 4.dp)
                )
            }
        }
    }
}
