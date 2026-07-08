package com.satinalmapro.android.ui.screens.home

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Description
import androidx.compose.material.icons.rounded.Inbox
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.LocalShipping
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material.icons.rounded.RequestQuote
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.DashboardActivity
import com.satinalmapro.android.core.model.DashboardCard
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.DashboardActivityRow
import com.satinalmapro.android.ui.components.DashboardHeroHeader
import com.satinalmapro.android.ui.components.DashboardStatCard
import com.satinalmapro.android.ui.components.ModernListCard
import com.satinalmapro.android.ui.components.QuickActionTile
import com.satinalmapro.android.ui.components.SectionTitle
import com.satinalmapro.android.ui.components.SectionTitleWithAction
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppSpacing
import com.satinalmapro.android.ui.theme.StatusType
import com.satinalmapro.android.ui.theme.statusColorsFromText

@Composable
fun HomeScreen(
    viewModel: AppViewModel,
    onMenuClick: () -> Unit = {},
    onNotificationClick: () -> Unit = {},
    onProfileClick: () -> Unit = {},
    modifier: Modifier = Modifier
) {
    var searchQuery by remember { mutableStateOf("") }
    val user by viewModel.user.collectAsState()
    val role = user?.role
    val canRequest = KullaniciRolleri.canCreateRequest(role)
    val canQuote = KullaniciRolleri.canEnterQuotes(role)
    val canMaterials = viewModel.canAccess("onaylanan-malzemeler")
    val canStock = KullaniciRolleri.canStockWrite(role)
    val isDepo = KullaniciRolleri.normalize(role) == KullaniciRolleri.DEPO
    val cards by viewModel.dashboardCards.collectAsState()
    val activities by viewModel.dashboardActivities.collectAsState()
    val notifications by viewModel.notifications.collectAsState()
    val menuBadges by viewModel.menuBadges.collectAsState()
    val menus = viewModel.menus()
    val unreadCount = notifications.count { !it.read }

    val filteredCards = cards.filter { it.matchesSearch(searchQuery) }
    val filteredActivities = activities.filter { it.matchesSearch(searchQuery) }
    val pendingItems = menus.filter { (menuBadges[it.route] ?: 0) > 0 && it.route != "dashboard" && it.route != "profil" }
        .filter { it.title.contains(searchQuery, true) || searchQuery.isBlank() }

    LazyColumn(
        modifier = modifier
            .fillMaxSize()
            .background(AppColors.Background),
        contentPadding = PaddingValues(bottom = AppSpacing.screenVertical)
    ) {
        item {
            DashboardHeroHeader(
                userName = user?.fullName.orEmpty(),
                role = role,
                searchQuery = searchQuery,
                onSearchChange = { searchQuery = it },
                notificationCount = unreadCount,
                onMenuClick = onMenuClick,
                onNotificationClick = onNotificationClick,
                onProfileClick = onProfileClick
            )
        }

        if (filteredCards.isNotEmpty()) {
            item {
                Column(
                    modifier = Modifier.padding(horizontal = AppSpacing.screenHorizontal),
                    verticalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
                ) {
                    SectionTitle("Özet")
                    Spacer(Modifier.height(4.dp))
                    filteredCards.take(4).chunked(2).forEach { rowCards ->
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
                        ) {
                            rowCards.forEach { card ->
                                val (iconBg, iconFg) = statColorsFor(card)
                                DashboardStatCard(
                                    title = card.title,
                                    value = card.value,
                                    subtitle = card.subtitle,
                                    icon = statIconFor(card),
                                    iconBg = iconBg,
                                    iconFg = iconFg,
                                    onClick = { viewModel.navigate(card.route) },
                                    modifier = Modifier.weight(1f)
                                )
                            }
                            if (rowCards.size == 1) Spacer(Modifier.weight(1f))
                        }
                    }
                }
            }
        }

        item {
            Column(
                modifier = Modifier.padding(horizontal = AppSpacing.screenHorizontal),
                verticalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
            ) {
                SectionTitle("Hızlı İşlemler")
                Spacer(Modifier.height(4.dp))
                val actions = buildList {
                    if (canRequest) add(Triple(Icons.Rounded.Add, "Yeni Talep", "yeni-talep"))
                    if (canMaterials) add(Triple(Icons.Rounded.Inventory2, "Malzeme Girişi", "onaylanan-malzemeler"))
                    if (canQuote) add(Triple(Icons.Rounded.RequestQuote, "Teklif Girişi", "teklif-bekleyen"))
                    if (isDepo && canStock) {
                        add(Triple(Icons.Rounded.Inventory2, "Stok Girişi", "stok-giris"))
                        add(Triple(Icons.Rounded.LocalShipping, "Stok Çıkışı", "stok-cikis"))
                    }
                }.filter { searchQuery.isBlank() || it.second.contains(searchQuery, true) }
                actions.chunked(2).forEach { row ->
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
                    ) {
                        row.forEach { (icon, label, route) ->
                            val (bg, fg) = when (route) {
                                "yeni-talep" -> AppColors.PrimaryContainer to AppColors.Primary
                                "onaylanan-malzemeler", "stok-giris" -> AppColors.SuccessContainer to AppColors.Success
                                else -> AppColors.WarningContainer to AppColors.Warning
                            }
                            QuickActionTile(
                                icon = icon,
                                label = label,
                                iconBg = bg,
                                iconFg = fg,
                                onClick = { viewModel.navigate(route) },
                                modifier = Modifier.weight(1f)
                            )
                        }
                        if (row.size == 1) Spacer(Modifier.weight(1f))
                    }
                }
            }
        }

        if (pendingItems.isNotEmpty()) {
            item {
                Column(
                    modifier = Modifier.padding(horizontal = AppSpacing.screenHorizontal),
                    verticalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
                ) {
                    SectionTitle("Bekleyen İşler")
                    pendingItems.take(5).forEach { menu ->
                        val count = menuBadges[menu.route] ?: 0
                        ModernListCard(
                            title = menu.title,
                            description = "Onayınızı veya işleminizi bekliyor",
                            icon = Icons.Rounded.Inbox,
                            iconBg = AppColors.WarningContainer,
                            iconFg = AppColors.Warning,
                            counter = count,
                            statusLabel = "Bekliyor",
                            statusType = StatusType.Pending,
                            onClick = { viewModel.navigate(menu.route) }
                        )
                    }
                }
            }
        }

        item {
            Column(
                modifier = Modifier.padding(horizontal = AppSpacing.screenHorizontal),
                verticalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
            ) {
                val sonIslemBaslik = when (KullaniciRolleri.normalize(role)) {
                    KullaniciRolleri.DEPO -> "Son Stok İşlemleri"
                    KullaniciRolleri.ATOLYE -> "Stok Uyarıları"
                    KullaniciRolleri.SAHA, KullaniciRolleri.SEF -> "Son Taleplerim"
                    KullaniciRolleri.YONETIM -> "Son Yönetim İşlemleri"
                    KullaniciRolleri.SATINALMA -> "Son Satınalma İşlemleri"
                    else -> "Son İşlemler"
                }
                SectionTitleWithAction(
                    title = sonIslemBaslik,
                    actionLabel = "Tümü",
                    onAction = { viewModel.navigateFromMenu("bildirimler") }
                )
                if (filteredActivities.isEmpty()) {
                    Text("Henüz işlem yok.", color = AppColors.TextSecondary, style = MaterialTheme.typography.bodyMedium)
                } else {
                    filteredActivities.take(6).forEach { activity ->
                        RecentActivityRow(activity) {
                            activity.route?.let { viewModel.navigate(it) }
                        }
                    }
                }
            }
        }

        if (notifications.isNotEmpty()) {
            item {
                Column(
                    modifier = Modifier.padding(horizontal = AppSpacing.screenHorizontal),
                    verticalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
                ) {
                    SectionTitleWithAction(
                        title = "Bildirimler",
                        actionLabel = "Tümü",
                        onAction = { viewModel.navigateFromMenu("bildirimler") }
                    )
                    notifications
                        .filter { searchQuery.isBlank() || it.title.contains(searchQuery, true) || it.message.contains(searchQuery, true) }
                        .take(3)
                        .forEach { notification ->
                            ModernListCard(
                                title = notification.title,
                                description = notification.message,
                                icon = Icons.Rounded.Notifications,
                                iconBg = AppColors.InfoContainer,
                                iconFg = AppColors.Info,
                                statusLabel = if (!notification.read) "Yeni" else null,
                                statusType = if (!notification.read) StatusType.Info else StatusType.Approved,
                                trailing = notification.time,
                                onClick = {
                                    val route = notification.route?.takeIf { it.isNotBlank() }
                                        ?: BildirimRota.hedefRoute(
                                            BildirimRota.normalizeTip(notification.type),
                                            notification.requestId,
                                            role
                                        )
                                    viewModel.openNotification(notification.id, route)
                                }
                            )
                        }
                }
            }
        }
    }
}

@Composable
private fun RecentActivityRow(item: DashboardActivity, onClick: () -> Unit) {
    val (bg, fg) = statusColorsFromText(item.status)
    DashboardActivityRow(
        title = item.title,
        subtitle = item.subtitle,
        status = item.status,
        statusBg = bg,
        statusFg = fg,
        onClick = onClick
    )
}

private fun DashboardCard.matchesSearch(query: String): Boolean {
    if (query.isBlank()) return true
    return title.contains(query, true) || subtitle.contains(query, true) || value.contains(query, true)
}

private fun DashboardActivity.matchesSearch(query: String): Boolean {
    if (query.isBlank()) return true
    return title.contains(query, true) || subtitle.contains(query, true) || status.contains(query, true)
}

private fun statIconFor(card: DashboardCard) = when {
    card.title.contains("Bildirim", true) -> Icons.Rounded.Notifications
    card.title.contains("Stok", true) -> Icons.Rounded.Inventory2
    card.title.contains("Teklif", true) -> Icons.Rounded.RequestQuote
    else -> Icons.Rounded.Description
}

@Composable
private fun statColorsFor(card: DashboardCard): Pair<androidx.compose.ui.graphics.Color, androidx.compose.ui.graphics.Color> {
    val value = card.value.toIntOrNull() ?: 0
    return when {
        value > 0 && card.title.contains("Bildirim", true) -> AppColors.InfoContainer to AppColors.Info
        value > 0 -> AppColors.WarningContainer to AppColors.Warning
        else -> AppColors.PrimaryContainer to AppColors.Primary
    }
}
