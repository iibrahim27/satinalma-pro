package com.satinalmapro.android.ui.components

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.offset
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.CheckCircle
import androidx.compose.material.icons.rounded.BarChart
import androidx.compose.material.icons.rounded.Home
import androidx.compose.material.icons.rounded.Menu
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material.icons.rounded.Person
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.FloatingActionButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Path
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppElevation
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.AppSizes
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.android.ui.theme.RoleColors
import com.satinalmapro.android.ui.theme.RoleVisual

@Composable
fun AppMainHeader(
    title: String,
    showMenu: Boolean,
    showBack: Boolean,
    notificationCount: Int,
    onMenuClick: () -> Unit,
    onBackClick: () -> Unit,
    onNotificationClick: () -> Unit,
    onProfileClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Surface(
        modifier = modifier
            .fillMaxWidth()
            .statusBarsPadding(),
        color = AppColors.Surface,
        shadowElevation = AppElevation.header
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .height(AppSizes.headerHeight)
                .padding(horizontal = 8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            when {
                showBack -> IconButton(onClick = onBackClick) {
                    Icon(Icons.AutoMirrored.Rounded.ArrowBack, contentDescription = "Geri", tint = AppColors.TextPrimary)
                }
                showMenu -> IconButton(onClick = onMenuClick) {
                    Icon(Icons.Rounded.Menu, contentDescription = "Menü", tint = AppColors.TextPrimary)
                }
                else -> Spacer(Modifier.size(48.dp))
            }
            Text(
                text = title,
                modifier = Modifier.weight(1f),
                style = MaterialTheme.typography.titleLarge,
                color = AppColors.TextPrimary,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
                textAlign = TextAlign.Center
            )
            BadgedBox(
                badge = {
                    if (notificationCount > 0) {
                        Badge { Text(if (notificationCount > 99) "99+" else "$notificationCount") }
                    }
                }
            ) {
                IconButton(onClick = onNotificationClick) {
                    Icon(Icons.Rounded.Notifications, contentDescription = "Bildirimler", tint = AppColors.TextPrimary)
                }
            }
            IconButton(onClick = onProfileClick) {
                Icon(Icons.Rounded.Person, contentDescription = "Profil", tint = AppColors.TextPrimary)
            }
        }
    }
}

@Composable
fun AppBottomNavigationBar(
    selectedRoute: String,
    showReports: Boolean,
    showFab: Boolean,
    onHome: () -> Unit,
    onNotifications: () -> Unit,
    onReports: () -> Unit,
    onProfile: () -> Unit,
    onFabClick: () -> Unit
) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .navigationBarsPadding()
    ) {
        NavigationBar(
            containerColor = AppColors.Surface,
            tonalElevation = AppElevation.card
        ) {
            NavigationBarItem(
                selected = selectedRoute == "dashboard" || selectedRoute == "stok-durum",
                onClick = onHome,
                icon = { Icon(Icons.Rounded.Home, contentDescription = "Ana Sayfa") },
                label = { Text("Ana Sayfa") },
                colors = navColors()
            )
            NavigationBarItem(
                selected = selectedRoute == "bildirimler",
                onClick = onNotifications,
                icon = { Icon(Icons.Rounded.Notifications, contentDescription = "Bildirimler") },
                label = { Text("Bildirim") },
                colors = navColors()
            )
            NavigationBarItem(
                selected = false,
                onClick = {},
                enabled = false,
                icon = { Spacer(Modifier.size(24.dp)) },
                label = { Text("") },
                colors = navColors()
            )
            if (showReports) {
                NavigationBarItem(
                    selected = selectedRoute == "raporlar",
                    onClick = onReports,
                    icon = { Icon(Icons.Rounded.BarChart, contentDescription = "Raporlar") },
                    label = { Text("Raporlar") },
                    colors = navColors()
                )
            } else {
                NavigationBarItem(
                    selected = false,
                    onClick = {},
                    enabled = false,
                    icon = { Spacer(Modifier.size(24.dp)) },
                    label = { Text("") },
                    colors = navColors()
                )
            }
            NavigationBarItem(
                selected = selectedRoute == "profil",
                onClick = onProfile,
                icon = { Icon(Icons.Rounded.Person, contentDescription = "Profil") },
                label = { Text("Profil") },
                colors = navColors()
            )
        }
        if (showFab) {
            FloatingActionButton(
                onClick = onFabClick,
                modifier = Modifier
                    .align(Alignment.TopCenter)
                    .offset(y = (-20).dp)
                    .size(AppSizes.fabSize)
                    .shadow(AppElevation.fab, CircleShape),
                shape = CircleShape,
                containerColor = AppColors.Primary,
                contentColor = Color.White,
                elevation = FloatingActionButtonDefaults.elevation(defaultElevation = AppElevation.fab)
            ) {
                Icon(Icons.Rounded.Add, contentDescription = "Hızlı işlem", modifier = Modifier.size(28.dp))
            }
        }
    }
}

@Composable
private fun navColors() = NavigationBarItemDefaults.colors(
    selectedIconColor = AppColors.Primary,
    selectedTextColor = AppColors.Primary,
    unselectedIconColor = AppColors.TextSecondary,
    unselectedTextColor = AppColors.TextSecondary,
    indicatorColor = AppColors.PrimaryContainer
)

@Composable
fun DashboardGreetingCard(
    userName: String,
    role: String?,
    modifier: Modifier = Modifier
) {
    val visual = RoleColors.forRole(role)
    Surface(
        modifier = modifier.fillMaxWidth(),
        shape = AppShapes.medium,
        color = AppColors.Surface,
        shadowElevation = AppElevation.card
    ) {
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .background(
                    Brush.horizontalGradient(
                        listOf(visual.container, AppColors.Surface, AppColors.PrimaryContainer.copy(alpha = 0.35f))
                    )
                )
                .padding(AppSpacing.cardPadding)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                Box(
                    modifier = Modifier
                        .size(AppSizes.roleIconBox)
                        .clip(AppShapes.small)
                        .background(visual.color),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        visual.title.take(1),
                        style = MaterialTheme.typography.titleLarge,
                        color = Color.White,
                        fontWeight = FontWeight.Bold
                    )
                }
                Column(Modifier.weight(1f)) {
                    Text("Hoş Geldiniz", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                    Text(
                        userName,
                        style = MaterialTheme.typography.headlineMedium,
                        color = AppColors.TextPrimary,
                        fontWeight = FontWeight.SemiBold
                    )
                    Text(visual.subtitle, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
                }
            }
        }
    }
}

@Composable
fun RoleSelectionCard(
    visual: RoleVisual,
    modifier: Modifier = Modifier,
    selected: Boolean = false,
    onClick: (() -> Unit)? = null
) {
    Surface(
        modifier = modifier.fillMaxWidth(),
        onClick = onClick ?: {},
        enabled = onClick != null,
        shape = AppShapes.small,
        color = AppColors.Surface,
        shadowElevation = AppElevation.card,
        border = androidx.compose.foundation.BorderStroke(1.dp, if (selected) AppColors.Primary else AppColors.Border)
    ) {
        Row(
            modifier = Modifier.padding(AppSpacing.cardPadding),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            Box(
                modifier = Modifier
                    .size(AppSizes.roleIconBox)
                    .clip(AppShapes.small)
                    .background(visual.color),
                contentAlignment = Alignment.Center
            ) {
                Text(visual.title.take(1), color = Color.White, fontWeight = FontWeight.Bold)
            }
            Column(Modifier.weight(1f)) {
                Text(visual.title, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                Text(visual.subtitle, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
            }
            if (selected) {
                Icon(
                    Icons.Rounded.CheckCircle,
                    contentDescription = null,
                    tint = AppColors.Primary,
                    modifier = Modifier.size(22.dp)
                )
            }
        }
    }
}

@Composable
fun BottomWaveDecoration(modifier: Modifier = Modifier) {
    val primary = AppColors.Primary
    val background = AppColors.Background
    Box(
        modifier = modifier
            .fillMaxWidth()
            .height(120.dp)
            .background(background)
    ) {
        androidx.compose.foundation.Canvas(modifier = Modifier.fillMaxSize()) {
            val w = size.width
            val h = size.height
            val path = Path().apply {
                moveTo(0f, h * 0.35f)
                cubicTo(w * 0.25f, h * 0.05f, w * 0.55f, h * 0.65f, w, h * 0.25f)
                lineTo(w, h)
                lineTo(0f, h)
                close()
            }
            drawPath(path, primary.copy(alpha = 0.12f))
            val path2 = Path().apply {
                moveTo(0f, h * 0.55f)
                cubicTo(w * 0.35f, h * 0.2f, w * 0.65f, h * 0.85f, w, h * 0.45f)
                lineTo(w, h)
                lineTo(0f, h)
                close()
            }
            drawPath(path2, primary.copy(alpha = 0.22f))
        }
    }
}

@Composable
fun AppPrimaryButton(
    text: String,
    onClick: () -> Unit,
    modifier: Modifier = Modifier,
    enabled: Boolean = true,
    loading: Boolean = false
) {
    androidx.compose.material3.Button(
        onClick = onClick,
        modifier = modifier
            .fillMaxWidth()
            .height(AppSizes.buttonHeight),
        enabled = enabled && !loading,
        shape = AppShapes.small,
        colors = androidx.compose.material3.ButtonDefaults.buttonColors(containerColor = AppColors.Primary)
    ) {
        Text(
            if (loading) "Lütfen bekleyin..." else text,
            style = MaterialTheme.typography.labelLarge,
            fontWeight = FontWeight.SemiBold
        )
    }
}

@Composable
fun DashboardStatCard(
    title: String,
    value: String,
    subtitle: String,
    icon: ImageVector,
    iconBg: Color,
    iconFg: Color,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    AppCard(modifier = modifier, onClick = onClick) {
        Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
            Box(
                modifier = Modifier
                    .size(AppSizes.iconBox)
                    .clip(AppShapes.small)
                    .background(iconBg),
                contentAlignment = Alignment.Center
            ) {
                Icon(icon, contentDescription = null, tint = iconFg, modifier = Modifier.size(22.dp))
            }
            Text(title, style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
            Text(
                value,
                style = MaterialTheme.typography.headlineMedium,
                color = AppColors.TextPrimary,
                fontWeight = FontWeight.SemiBold
            )
            if (subtitle.isNotBlank()) {
                Text(subtitle, style = MaterialTheme.typography.labelSmall, color = AppColors.TextSecondary)
            }
        }
    }
}

@Composable
fun QuickActionTile(
    icon: ImageVector,
    label: String,
    iconBg: Color,
    iconFg: Color,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    AppCard(modifier = modifier, onClick = onClick) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Box(
                modifier = Modifier
                    .size(AppSizes.iconBox)
                    .clip(AppShapes.small)
                    .background(iconBg),
                contentAlignment = Alignment.Center
            ) {
                Icon(icon, contentDescription = null, tint = iconFg, modifier = Modifier.size(22.dp))
            }
            Text(
                label,
                style = MaterialTheme.typography.labelMedium,
                color = AppColors.TextPrimary,
                textAlign = TextAlign.Center,
                fontWeight = FontWeight.Medium
            )
        }
    }
}

@Composable
fun DashboardActivityRow(
    title: String,
    subtitle: String,
    status: String,
    statusBg: Color,
    statusFg: Color,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    AppCard(modifier = modifier, onClick = onClick) {
        Row(
            Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(Modifier.weight(1f)) {
                Text(title, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary)
                Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary)
            }
            Surface(shape = AppShapes.extraSmall, color = statusBg) {
                Text(
                    status,
                    modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
                    style = MaterialTheme.typography.labelMedium,
                    color = statusFg
                )
            }
        }
    }
}
