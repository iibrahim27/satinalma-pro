package com.satinalmapro.android.ui.screens.notifications

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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.data.DemoData
import com.satinalmapro.android.data.NotificationItem
import com.satinalmapro.android.ui.theme.AppColors

@Composable
fun NotificationsScreen(onBack: () -> Unit) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 20.dp, vertical = 16.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            IconButton(onClick = onBack) {
                Icon(Icons.AutoMirrored.Rounded.ArrowBack, contentDescription = "Geri")
            }
            Text("Bildirimler", style = MaterialTheme.typography.headlineMedium, color = AppColors.TextPrimary)
        }

        Spacer(Modifier.height(12.dp))

        LazyColumn(verticalArrangement = Arrangement.spacedBy(4.dp)) {
            items(DemoData.notifications) { item ->
                NotificationTimelineItem(item)
            }
        }
    }
}

@Composable
private fun NotificationTimelineItem(item: NotificationItem) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 10.dp),
        verticalAlignment = Alignment.Top
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Surface(shape = CircleShape, color = item.accent, modifier = Modifier.size(12.dp)) {}
            Box(
                modifier = Modifier
                    .width(2.dp)
                    .height(48.dp)
                    .padding(top = 4.dp)
            ) {
                Surface(color = AppColors.Border, modifier = Modifier.fillMaxSize()) {}
            }
        }
        Spacer(Modifier.width(14.dp))
        Column(Modifier.weight(1f)) {
            Text(item.time, style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Rounded.Notifications, null, tint = item.accent, modifier = Modifier.size(18.dp))
                Spacer(Modifier.width(6.dp))
                Text(item.title, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
            }
            Text(item.description, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary, modifier = Modifier.padding(top = 4.dp))
        }
    }
}
