package com.satinalmapro.android.ui.screens.home

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.Add
import androidx.compose.material.icons.rounded.Description
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.LocalShipping
import androidx.compose.material.icons.rounded.RequestQuote
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.DashboardActivity
import com.satinalmapro.android.core.model.DashboardCard
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.components.DashboardActivityRow
import com.satinalmapro.android.ui.components.DashboardGreetingCard
import com.satinalmapro.android.ui.components.DashboardStatCard
import com.satinalmapro.android.ui.components.QuickActionTile
import com.satinalmapro.android.ui.components.SectionTitle
import com.satinalmapro.android.ui.theme.AppColors
import com.satinalmapro.android.ui.theme.AppSpacing

@Composable
fun HomeScreen(
    viewModel: AppViewModel,
    modifier: Modifier = Modifier
) {
    val user by viewModel.user.collectAsState()
    val role = user?.role
    val canRequest = KullaniciRolleri.canCreateRequest(role)
    val canQuote = KullaniciRolleri.canEnterQuotes(role)
    val canMaterials = viewModel.canAccess("onaylanan-malzemeler")
    val canStock = KullaniciRolleri.canStockWrite(role)
    val isDepo = KullaniciRolleri.normalize(role) == KullaniciRolleri.DEPO
    val cards by viewModel.dashboardCards.collectAsState()
    val activities by viewModel.dashboardActivities.collectAsState()

    LazyColumn(
        modifier = modifier.fillMaxSize(),
        contentPadding = PaddingValues(
            horizontal = AppSpacing.screenHorizontal,
            vertical = AppSpacing.screenVertical
        ),
        verticalArrangement = Arrangement.spacedBy(AppSpacing.sectionGap)
    ) {
        item {
            DashboardGreetingCard(
                userName = user?.fullName.orEmpty(),
                role = role
            )
        }

        item {
            SectionTitle("Bugünkü Özet")
            Spacer(Modifier.height(12.dp))
            cards.take(4).chunked(2).forEach { rowCards ->
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(AppSpacing.cardGap)
                ) {
                    rowCards.forEach { card ->
                        DashboardStatCard(
                            title = card.title,
                            value = card.value,
                            subtitle = card.subtitle,
                            icon = Icons.Rounded.Description,
                            iconBg = AppColors.PrimaryContainer,
                            iconFg = AppColors.Primary,
                            onClick = { viewModel.navigate(card.route) },
                            modifier = Modifier.weight(1f)
                        )
                    }
                    if (rowCards.size == 1) {
                        Spacer(Modifier.weight(1f))
                    }
                }
                Spacer(Modifier.height(AppSpacing.cardGap))
            }
        }

        item {
            SectionTitle("Hızlı İşlemler")
            Spacer(Modifier.height(12.dp))
            val actions = buildList {
                if (canRequest) add(Triple(Icons.Rounded.Add, "Yeni Talep", "yeni-talep"))
                if (canMaterials) add(Triple(Icons.Rounded.Inventory2, "Malzeme Girişi", "onaylanan-malzemeler"))
                if (canQuote) add(Triple(Icons.Rounded.RequestQuote, "Teklif Girişi", "teklif-bekleyen"))
                if (isDepo && canStock) {
                    add(Triple(Icons.Rounded.Inventory2, "Stok Girişi", "stok-giris"))
                    add(Triple(Icons.Rounded.LocalShipping, "Stok Çıkışı", "stok-cikis"))
                }
            }
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
                Spacer(Modifier.height(AppSpacing.cardGap))
            }
        }

        item {
            val sonIslemBaslik = when (KullaniciRolleri.normalize(role)) {
                KullaniciRolleri.DEPO -> "Son Stok İşlemleri"
                KullaniciRolleri.ATOLYE -> "Stok Uyarıları"
                KullaniciRolleri.SAHA, KullaniciRolleri.SEF -> "Son Taleplerim"
                KullaniciRolleri.YONETIM -> "Son Yönetim İşlemleri"
                KullaniciRolleri.SATINALMA -> "Son Satınalma İşlemleri"
                else -> "Son Bildirimler"
            }
            SectionTitle(sonIslemBaslik)
            Spacer(Modifier.height(8.dp))
            if (activities.isEmpty()) {
                Text("Henüz işlem yok.", color = AppColors.TextSecondary, style = MaterialTheme.typography.bodyMedium)
            } else {
                activities.forEach { activity ->
                    RecentActivityRow(activity) {
                        activity.route?.let { viewModel.navigate(it) }
                    }
                    Spacer(Modifier.height(AppSpacing.cardGap))
                }
            }
        }
    }
}

@Composable
private fun RecentActivityRow(item: DashboardActivity, onClick: () -> Unit) {
    val (bg, fg) = activityStatusColors(item.status)
    DashboardActivityRow(
        title = item.title,
        subtitle = item.subtitle,
        status = item.status,
        statusBg = bg,
        statusFg = fg,
        onClick = onClick
    )
}

@Composable
private fun activityStatusColors(status: String): Pair<androidx.compose.ui.graphics.Color, androidx.compose.ui.graphics.Color> {
    val lower = status.lowercase()
    return when {
        "onay" in lower || "tamam" in lower -> AppColors.SuccessContainer to AppColors.Success
        "bekl" in lower || "bekleyen" in lower -> AppColors.WarningContainer to AppColors.Warning
        "red" in lower || "iptal" in lower -> AppColors.DangerContainer to AppColors.Danger
        else -> AppColors.InfoContainer to AppColors.Info
    }
}
