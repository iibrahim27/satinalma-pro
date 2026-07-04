package com.satinalmapro.android.ui.screens.notifications

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.AppNotification
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.theme.AppColors

@Composable
fun NotificationsScreen(viewModel: AppViewModel) {
    val notifications by viewModel.notifications.collectAsState()
    val user by viewModel.user.collectAsState()

    LazyColumn(
        modifier = Modifier.fillMaxSize().padding(horizontal = 20.dp, vertical = 16.dp),
        verticalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        if (notifications.isEmpty()) {
            item {
                Text("Bildirim yok.", color = AppColors.TextSecondary, modifier = Modifier.padding(vertical = 24.dp))
            }
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

@Composable
private fun NotificationTimelineItem(item: AppNotification, onClick: () -> Unit) {
    val accent = when (item.type) {
        "Onaylandi", "MalKabulEdildi" -> AppColors.Success
        "Reddedildi" -> AppColors.Danger
        else -> AppColors.Primary
    }
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .padding(vertical = 10.dp),
        verticalAlignment = Alignment.Top
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Surface(shape = CircleShape, color = accent, modifier = Modifier.size(12.dp)) {}
            Spacer(Modifier.height(48.dp))
        }
        Spacer(Modifier.width(14.dp))
        Column(Modifier.weight(1f)) {
            Text(item.time, style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Rounded.Notifications, null, tint = accent, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(6.dp))
                Text(item.title, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
            }
            Text(item.message, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary, modifier = Modifier.padding(top = 4.dp))
        }
    }
}
