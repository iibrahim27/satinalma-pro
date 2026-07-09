package com.satinalmapro.android.ui.procurement

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.KeyboardArrowRight
import androidx.compose.material.icons.rounded.Assignment
import androidx.compose.material.icons.rounded.CheckCircle
import androidx.compose.material.icons.rounded.History
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.LocalShipping
import androidx.compose.material.icons.rounded.RequestQuote
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.roles.RolNavigasyon
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.theme.MetrikLight
import com.satinalmapro.android.ui.theme.MetrikSpace

@Composable
fun QueuesHubScreen(viewModel: AppViewModel) {
    val user by viewModel.user.collectAsState()
    val badges by viewModel.menuBadges.collectAsState()
    val itemCounts by viewModel.queueItemCounts.collectAsState()
    val queues = remember(user?.role) {
        runCatching { RolNavigasyon.queueMenus(user?.role) }.getOrDefault(emptyList())
    }
    val grouped = remember(queues) {
        queues.groupBy { it.group?.ifBlank { "Diğer" } ?: "Diğer" }
    }
    val waiting = badges.values.sum()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(MetrikLight.Background)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .background(MetrikLight.Surface)
                .padding(horizontal = MetrikSpace.screen, vertical = 18.dp)
        ) {
            Text(
                "İşler",
                style = MaterialTheme.typography.headlineMedium,
                color = MetrikLight.TextPrimary,
                fontWeight = FontWeight.Bold
            )
            Spacer(Modifier.height(4.dp))
            Text(
                if (waiting > 0) "$waiting bekleyen iş" else "Tüm kuyruklar burada",
                style = MaterialTheme.typography.bodyMedium,
                color = MetrikLight.TextSecondary
            )
        }

        if (queues.isEmpty()) {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                Text("Bu rol için kuyruk yok", color = MetrikLight.TextSecondary)
            }
        } else {
            LazyColumn(
                contentPadding = PaddingValues(
                    horizontal = MetrikSpace.screen,
                    vertical = MetrikSpace.lg
                ),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                grouped.forEach { (group, itemsInGroup) ->
                    item(key = "h-$group") {
                        Text(
                            group.uppercase(),
                            style = MaterialTheme.typography.labelMedium,
                            color = MetrikLight.TextTertiary,
                            fontWeight = FontWeight.SemiBold,
                            modifier = Modifier.padding(top = 8.dp, bottom = 4.dp, start = 4.dp)
                        )
                    }
                    items(itemsInGroup, key = { it.route }) { item ->
                        val count = itemCounts[item.route] ?: 0
                        val action = RolNavigasyon.isActionQueue(item.route)
                        QueueRow(
                            title = item.title,
                            count = count,
                            actionQueue = action,
                            icon = queueIcon(item.route),
                            tint = queueTint(item.route),
                            onClick = { viewModel.navigateFromMenu(item.route) }
                        )
                    }
                }
                item { Spacer(Modifier.height(24.dp)) }
            }
        }
    }
}

@Composable
private fun QueueRow(
    title: String,
    count: Int,
    actionQueue: Boolean,
    icon: ImageVector,
    tint: Color,
    onClick: () -> Unit
) {
    val subtitle = when {
        count <= 0 -> "Boş"
        actionQueue -> "$count bekliyor"
        else -> "$count kayıt"
    }
    val showActionBadge = actionQueue && count > 0
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(14.dp))
            .background(MetrikLight.Surface)
            .clickable(onClick = onClick)
            .padding(horizontal = 14.dp, vertical = 14.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Box(
            modifier = Modifier
                .size(42.dp)
                .clip(CircleShape)
                .background(tint.copy(alpha = 0.12f)),
            contentAlignment = Alignment.Center
        ) {
            Icon(icon, contentDescription = null, tint = tint, modifier = Modifier.size(22.dp))
        }
        Spacer(Modifier.width(14.dp))
        Column(modifier = Modifier.weight(1f)) {
            Text(
                title,
                style = MaterialTheme.typography.titleMedium,
                color = MetrikLight.TextPrimary,
                fontWeight = FontWeight.SemiBold
            )
            Text(
                subtitle,
                style = MaterialTheme.typography.bodySmall,
                color = when {
                    count <= 0 -> MetrikLight.TextTertiary
                    actionQueue -> tint
                    else -> MetrikLight.TextSecondary
                }
            )
        }
        if (showActionBadge) {
            Box(
                modifier = Modifier
                    .background(tint, RoundedCornerShape(8.dp))
                    .padding(horizontal = 10.dp, vertical = 4.dp)
            ) {
                Text(
                    count.toString(),
                    color = Color.White,
                    style = MaterialTheme.typography.labelLarge,
                    fontWeight = FontWeight.Bold
                )
            }
            Spacer(Modifier.width(6.dp))
        } else if (count > 0) {
            Text(
                count.toString(),
                color = MetrikLight.TextTertiary,
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.Medium
            )
            Spacer(Modifier.width(6.dp))
        }
        Icon(
            Icons.AutoMirrored.Rounded.KeyboardArrowRight,
            contentDescription = null,
            tint = MetrikLight.TextTertiary
        )
    }
}

private fun queueIcon(route: String): ImageVector = when {
    route.contains("malzeme") || route.contains("siparis") || route.contains("mal-kabul") ->
        Icons.Rounded.LocalShipping
    route.contains("teklif") -> Icons.Rounded.RequestQuote
    route.contains("onay") || route.contains("gelen") -> Icons.Rounded.CheckCircle
    route.contains("gecmis") || route.contains("red") -> Icons.Rounded.History
    route.contains("stok") || route.contains("depo") -> Icons.Rounded.Inventory2
    else -> Icons.Rounded.Assignment
}

private fun queueTint(route: String): Color = when {
    route.contains("red") -> MetrikLight.Danger
    route.contains("malzeme") || route.contains("siparis") || route.contains("mal-kabul") ->
        MetrikLight.Info
    route.contains("teklif") -> MetrikLight.Accent
    route.contains("onay") || route.contains("gelen") -> MetrikLight.Success
    route.contains("stok") -> MetrikLight.Success
    route.contains("gecmis") -> MetrikLight.TextSecondary
    else -> MetrikLight.Primary
}
