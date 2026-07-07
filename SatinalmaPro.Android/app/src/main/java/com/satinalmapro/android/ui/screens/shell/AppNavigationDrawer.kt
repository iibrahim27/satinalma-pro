package com.satinalmapro.android.ui.screens.shell

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
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ListAlt
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Approval
import androidx.compose.material.icons.rounded.Assessment
import androidx.compose.material.icons.automirrored.rounded.CompareArrows
import androidx.compose.material.icons.rounded.Category
import androidx.compose.material.icons.rounded.Dashboard
import androidx.compose.material.icons.rounded.History
import androidx.compose.material.icons.rounded.Home
import androidx.compose.material.icons.rounded.Inbox
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.LocalShipping
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material.icons.rounded.Person
import androidx.compose.material.icons.rounded.PriceChange
import androidx.compose.material.icons.rounded.RequestQuote
import androidx.compose.material.icons.rounded.SwapHoriz
import androidx.compose.material.icons.rounded.TaskAlt
import androidx.compose.material.icons.automirrored.rounded.Undo
import androidx.compose.material.icons.automirrored.rounded.Logout
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationDrawerItem
import androidx.compose.material3.NavigationDrawerItemDefaults
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.BuildConfig
import com.satinalmapro.android.core.model.MenuItem
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppShapes

@Composable
fun AppNavigationDrawer(
    user: UserProfile?,
    menus: List<MenuItem>,
    selectedRoute: String,
    menuBadges: Map<String, Int> = emptyMap(),
    onItemClick: (MenuItem) -> Unit,
    onLogout: () -> Unit
) {
    Column(modifier = Modifier.fillMaxSize()) {
        DrawerHeader(user)
        LazyColumn(
            modifier = Modifier.weight(1f),
            contentPadding = androidx.compose.foundation.layout.PaddingValues(bottom = 16.dp)
        ) {
            val grouped = menus.groupBy { item ->
                when (item.route) {
                    "dashboard", "profil" -> ""
                    else -> item.group ?: "Genel"
                }
            }
            val order = listOf("", "Genel", "Satınalma", "Talep", "Teklif", "Malzeme", "Stok", "Yönetim")
            order.forEach { groupKey ->
                val items = grouped[groupKey] ?: return@forEach
                if (groupKey.isNotBlank()) {
                    item {
                        Text(
                            text = groupKey.uppercase(),
                            style = MaterialTheme.typography.labelSmall,
                            color = AppColors.TextSecondary,
                            fontWeight = FontWeight.SemiBold,
                            modifier = Modifier.padding(start = 28.dp, top = 12.dp, bottom = 4.dp)
                        )
                    }
                }
                items(items, key = { it.route }) { item ->
                    val selected = item.route == selectedRoute
                    val badge = menuBadges[item.route] ?: 0
                    NavigationDrawerItem(
                        label = {
                            if (badge > 0) {
                                BadgedBox(badge = { Badge { Text(if (badge > 99) "99+" else badge.toString()) } }) {
                                    Text(
                                        item.title,
                                        fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Normal
                                    )
                                }
                            } else {
                                Text(
                                    item.title,
                                    fontWeight = if (selected) FontWeight.SemiBold else FontWeight.Normal
                                )
                            }
                        },
                        icon = {
                            Icon(
                                routeIcon(item.route),
                                contentDescription = null,
                                tint = if (selected) AppColors.Primary else AppColors.TextSecondary
                            )
                        },
                        selected = selected,
                        onClick = { onItemClick(item) },
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 2.dp),
                        colors = NavigationDrawerItemDefaults.colors(
                            selectedContainerColor = AppColors.PrimaryContainer,
                            selectedIconColor = AppColors.Primary,
                            selectedTextColor = AppColors.Primary,
                            unselectedContainerColor = Color.Transparent,
                            unselectedIconColor = AppColors.TextSecondary,
                            unselectedTextColor = AppColors.TextPrimary
                        )
                    )
                }
            }
        }
        HorizontalDivider(color = AppColors.Border)
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .navigationBarsPadding()
        ) {
            Button(
                onClick = onLogout,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp, vertical = 8.dp),
                colors = ButtonDefaults.buttonColors(
                    containerColor = AppColors.Danger.copy(alpha = 0.12f),
                    contentColor = AppColors.Danger
                )
            ) {
                Icon(Icons.AutoMirrored.Rounded.Logout, contentDescription = null, modifier = Modifier.padding(end = 8.dp))
                Text("Çıkış Yap", fontWeight = FontWeight.SemiBold)
            }
            HorizontalDivider(color = AppColors.Border)
            Text(
                text = "Satınalma Pro v${BuildConfig.VERSION_NAME} (${BuildConfig.VERSION_CODE})",
                style = MaterialTheme.typography.labelSmall,
                color = AppColors.TextSecondary,
                modifier = Modifier.padding(horizontal = 24.dp, vertical = 12.dp)
            )
        }
    }
}

@Composable
private fun DrawerHeader(user: UserProfile?) {
    val initials = user?.fullName
        ?.split(' ')
        ?.mapNotNull { it.firstOrNull()?.uppercaseChar() }
        ?.take(2)
        ?.joinToString("")
        .orEmpty()
        .ifBlank { "SP" }

    Box(
        modifier = Modifier
            .fillMaxWidth()
            .statusBarsPadding()
            .background(
                Brush.verticalGradient(
                    listOf(Color(0xFF1D4ED8), Color(0xFF2563EB), Color(0xFF3B82F6))
                )
            )
            .padding(horizontal = 24.dp, vertical = 28.dp)
    ) {
        Column {
            Surface(shape = CircleShape, color = Color.White.copy(alpha = 0.18f)) {
                Box(
                    modifier = Modifier.size(56.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = initials,
                        style = MaterialTheme.typography.titleLarge,
                        color = Color.White,
                        fontWeight = FontWeight.Bold
                    )
                }
            }
            Spacer(Modifier.height(14.dp))
            Text(
                text = user?.fullName ?: "Satınalma Pro",
                style = MaterialTheme.typography.titleLarge,
                color = Color.White,
                fontWeight = FontWeight.Bold
            )
            Spacer(Modifier.height(6.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                Surface(shape = AppShapes.small, color = Color.White.copy(alpha = 0.16f)) {
                    Text(
                        text = user?.role ?: "",
                        style = MaterialTheme.typography.labelMedium,
                        color = Color.White,
                        modifier = Modifier.padding(horizontal = 10.dp, vertical = 4.dp)
                    )
                }
            }
        }
    }
}

private fun routeIcon(route: String): ImageVector = when (route) {
    "dashboard" -> Icons.Rounded.Home
    "yeni-talep" -> Icons.Rounded.Add
    "taleplerim" -> Icons.AutoMirrored.Rounded.ListAlt
    "onay-bekleyen" -> Icons.Rounded.Approval
    "onaylanan-talepler" -> Icons.Rounded.TaskAlt
    "gelen-talepler" -> Icons.Rounded.Inbox
    "teklif-bekleyen" -> Icons.Rounded.RequestQuote
    "teklif-gir" -> Icons.Rounded.PriceChange
    "teklif-karsilastirma" -> Icons.AutoMirrored.Rounded.CompareArrows
    "teklifsiz-firma-fiyat" -> Icons.Rounded.PriceChange
    "teklif-onay" -> Icons.Rounded.Approval
    "onaylanan-teklifler" -> Icons.Rounded.TaskAlt
    "onay-gecmisi" -> Icons.Rounded.History
    "gecmis-talepler" -> Icons.Rounded.History
    "gecmis-teklifli-onaylar" -> Icons.Rounded.History
    "red-talepler" -> Icons.AutoMirrored.Rounded.Undo
    "onaylanan-malzemeler" -> Icons.Rounded.LocalShipping
    "alinan-malzemeler" -> Icons.Rounded.Inventory2
    "agrega" -> Icons.Rounded.Category
    "cimento" -> Icons.Rounded.Inventory2
    "stok-durum" -> Icons.Rounded.Inventory2
    "stok-giris" -> Icons.Rounded.Inventory2
    "stok-cikis" -> Icons.Rounded.SwapHoriz
    "stok-hareket" -> Icons.Rounded.Dashboard
    "stok-sayim" -> Icons.Rounded.Inventory2
    "yonetim-teklif-girilen" -> Icons.Rounded.Approval
    "yonetim-direk-onaylanan" -> Icons.Rounded.TaskAlt
    "satinalma-teklif-istenen" -> Icons.Rounded.RequestQuote
    "satinalma-teklif-girilen" -> Icons.Rounded.PriceChange
    "satinalma-onaylanan" -> Icons.Rounded.TaskAlt
    "satinalma-siparis" -> Icons.Rounded.LocalShipping
    "satinalma-mal-kabul" -> Icons.Rounded.Inventory2
    "raporlar" -> Icons.Rounded.Assessment
    "bildirimler" -> Icons.Rounded.Notifications
    "profil" -> Icons.Rounded.Person
    else -> Icons.Rounded.Dashboard
}
