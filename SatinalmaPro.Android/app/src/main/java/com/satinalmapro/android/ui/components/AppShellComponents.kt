package com.satinalmapro.android.ui.components

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
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.automirrored.rounded.KeyboardArrowRight
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.BarChart
import androidx.compose.material.icons.rounded.CheckCircle
import androidx.compose.material.icons.rounded.Home
import androidx.compose.material.icons.rounded.Menu
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material.icons.rounded.Person
import androidx.compose.material.icons.rounded.Search
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.FloatingActionButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.OutlinedTextField
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
import com.satinalmapro.android.ui.theme.heroSearchFieldColors

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
                .padding(horizontal = 4.dp),
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
                textAlign = TextAlign.Center,
                fontWeight = FontWeight.SemiBold
            )
            BadgedBox(
                badge = {
                    if (notificationCount > 0) {
                        Badge(containerColor = AppColors.Danger) {
                            Text(if (notificationCount > 99) "99+" else "$notificationCount")
                        }
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DashboardHeroHeader(
    userName: String,
    role: String?,
    searchQuery: String,
    onSearchChange: (String) -> Unit,
    notificationCount: Int,
    onMenuClick: () -> Unit,
    onNotificationClick: () -> Unit,
    onProfileClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val visual = RoleColors.forRole(role)
    val initials = userName.split(' ')
        .mapNotNull { it.firstOrNull()?.uppercaseChar() }
        .take(2)
        .joinToString("")
        .ifBlank { "SP" }

    Box(
        modifier = modifier
            .fillMaxWidth()
            .background(
                Brush.verticalGradient(
                    listOf(AppColors.PrimaryDark, AppColors.Primary, AppColors.Secondary.copy(alpha = 0.85f))
                )
            )
            .statusBarsPadding()
            .padding(horizontal = AppSpacing.screenHorizontal)
            .padding(top = 8.dp, bottom = AppSpacing.heroBottom)
    ) {
        Column(verticalArrangement = Arrangement.spacedBy(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                IconButton(onClick = onMenuClick) {
                    Icon(Icons.Rounded.Menu, contentDescription = "Menü", tint = AppColors.TextOnPrimary)
                }
                Row(horizontalArrangement = Arrangement.spacedBy(4.dp), verticalAlignment = Alignment.CenterVertically) {
                    BadgedBox(
                        badge = {
                            if (notificationCount > 0) {
                                Badge(containerColor = AppColors.Danger) {
                                    Text(if (notificationCount > 99) "99+" else "$notificationCount")
                                }
                            }
                        }
                    ) {
                        IconButton(onClick = onNotificationClick) {
                            Icon(Icons.Rounded.Notifications, contentDescription = "Bildirimler", tint = AppColors.TextOnPrimary)
                        }
                    }
                    Surface(
                        onClick = onProfileClick,
                        shape = CircleShape,
                        color = AppColors.TextOnPrimary.copy(alpha = 0.18f)
                    ) {
                        Box(
                            modifier = Modifier.size(AppSizes.avatarSize),
                            contentAlignment = Alignment.Center
                        ) {
                            Text(initials, color = AppColors.TextOnPrimary, fontWeight = FontWeight.Bold, style = MaterialTheme.typography.labelLarge)
                        }
                    }
                }
            }

            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text("Hoş geldiniz", style = MaterialTheme.typography.bodyMedium, color = AppColors.TextOnPrimary.copy(alpha = 0.75f))
                Text(
                    userName.ifBlank { "Kullanıcı" },
                    style = MaterialTheme.typography.headlineMedium,
                    color = AppColors.TextOnPrimary,
                    fontWeight = FontWeight.Bold
                )
                Surface(shape = AppShapes.extraSmall, color = AppColors.TextOnPrimary.copy(alpha = 0.15f)) {
                    Text(
                        visual.title,
                        modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp),
                        style = MaterialTheme.typography.labelMedium,
                        color = AppColors.TextOnPrimary
                    )
                }
            }

            OutlinedTextField(
                value = searchQuery,
                onValueChange = onSearchChange,
                modifier = Modifier.fillMaxWidth(),
                placeholder = { Text("Ara...", color = AppColors.TextOnPrimary.copy(alpha = 0.6f)) },
                leadingIcon = { Icon(Icons.Rounded.Search, contentDescription = null, tint = AppColors.TextOnPrimary.copy(alpha = 0.75f)) },
                singleLine = true,
                shape = RoundedCornerShape(16.dp),
                colors = heroSearchFieldColors()
            )
        }
    }
}

@Composable
fun AppBottomNavigationBar(
    selectedRoute: String,
    showReports: Boolean,
    showFab: Boolean,
    notificationCount: Int = 0,
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
                label = { Text("Ana Sayfa", style = MaterialTheme.typography.labelSmall) },
                colors = navColors()
            )
            NavigationBarItem(
                selected = selectedRoute == "bildirimler",
                onClick = onNotifications,
                icon = {
                    BadgedBox(
                        badge = {
                            if (notificationCount > 0) {
                                Badge(containerColor = AppColors.Danger) {
                                    Text(if (notificationCount > 99) "99+" else "$notificationCount")
                                }
                            }
                        }
                    ) {
                        Icon(Icons.Rounded.Notifications, contentDescription = "Bildirimler")
                    }
                },
                label = { Text("Bildirim", style = MaterialTheme.typography.labelSmall) },
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
                    label = { Text("Raporlar", style = MaterialTheme.typography.labelSmall) },
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
                label = { Text("Profil", style = MaterialTheme.typography.labelSmall) },
                colors = navColors()
            )
        }
        if (showFab) {
            FloatingActionButton(
                onClick = onFabClick,
                modifier = Modifier
                    .align(Alignment.TopCenter)
                    .offset(y = (-22).dp)
                    .size(AppSizes.fabSize)
                    .shadow(AppElevation.fab, CircleShape),
                shape = CircleShape,
                containerColor = AppColors.Primary,
                contentColor = AppColors.TextOnPrimary,
                elevation = FloatingActionButtonDefaults.elevation(defaultElevation = AppElevation.fab)
            ) {
                Icon(Icons.Rounded.Add, contentDescription = "Yeni talep", modifier = Modifier.size(28.dp))
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
    DashboardHeroHeader(
        userName = userName,
        role = role,
        searchQuery = "",
        onSearchChange = {},
        notificationCount = 0,
        onMenuClick = {},
        onNotificationClick = {},
        onProfileClick = {},
        modifier = modifier
    )
}

@Composable
fun RoleSelectionCard(
    visual: RoleVisual,
    modifier: Modifier = Modifier,
    selected: Boolean = false,
    onClick: (() -> Unit)? = null
) {
    val content: @Composable () -> Unit = {
        Row(
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
                Icon(Icons.Rounded.CheckCircle, contentDescription = null, tint = AppColors.Primary, modifier = Modifier.size(22.dp))
            }
        }
    }
    if (onClick != null) {
        AppCard(modifier = modifier, onClick = onClick, containerColor = if (selected) AppColors.PrimaryContainer else AppColors.Surface, content = { content() })
    } else {
        AppCard(modifier = modifier, containerColor = if (selected) AppColors.PrimaryContainer else AppColors.Surface, content = { content() })
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
            drawPath(path, primary.copy(alpha = 0.10f))
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
        shape = AppShapes.medium,
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
    AppCard(modifier = modifier, onClick = onClick, contentPadding = 16.dp) {
        Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
            Box(
                modifier = Modifier
                    .size(AppSizes.iconBox)
                    .clip(AppShapes.small)
                    .background(iconBg),
                contentAlignment = Alignment.Center
            ) {
                Icon(icon, contentDescription = null, tint = iconFg, modifier = Modifier.size(24.dp))
            }
            Text(title, style = MaterialTheme.typography.labelMedium, color = AppColors.TextSecondary)
            Text(
                value,
                style = MaterialTheme.typography.headlineMedium,
                color = AppColors.TextPrimary,
                fontWeight = FontWeight.Bold
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
    AppCard(modifier = modifier, onClick = onClick, contentPadding = 16.dp) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.spacedBy(10.dp),
            modifier = Modifier.fillMaxWidth()
        ) {
            Box(
                modifier = Modifier
                    .size(AppSizes.iconBox)
                    .clip(AppShapes.small)
                    .background(iconBg),
                contentAlignment = Alignment.Center
            ) {
                Icon(icon, contentDescription = null, tint = iconFg, modifier = Modifier.size(24.dp))
            }
            Text(
                label,
                style = MaterialTheme.typography.labelMedium,
                color = AppColors.TextPrimary,
                textAlign = TextAlign.Center,
                fontWeight = FontWeight.Medium,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis
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
    modifier: Modifier = Modifier,
    trailing: String? = null
) {
    AppCard(modifier = modifier, onClick = onClick, contentPadding = 16.dp) {
        Row(
            Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Column(Modifier.weight(1f), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(title, style = MaterialTheme.typography.titleMedium, color = AppColors.TextPrimary, maxLines = 1, overflow = TextOverflow.Ellipsis)
                Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = AppColors.TextSecondary, maxLines = 2, overflow = TextOverflow.Ellipsis)
                StatusBadge(status, statusBg, statusFg)
            }
            Column(horizontalAlignment = Alignment.End, verticalArrangement = Arrangement.spacedBy(4.dp)) {
                trailing?.let {
                    Text(it, style = MaterialTheme.typography.labelSmall, color = AppColors.TextSecondary)
                }
                Icon(
                    Icons.AutoMirrored.Rounded.KeyboardArrowRight,
                    contentDescription = null,
                    tint = AppColors.TextSecondary.copy(alpha = 0.5f),
                    modifier = Modifier.size(22.dp)
                )
            }
        }
    }
}
