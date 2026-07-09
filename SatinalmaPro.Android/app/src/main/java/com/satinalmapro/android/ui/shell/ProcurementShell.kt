package com.satinalmapro.android.ui.shell

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material.icons.rounded.Person
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.MenuItem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.RolNavigasyon
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.components.TenantFooter
import com.satinalmapro.android.ui.procurement.DashboardScreen
import com.satinalmapro.android.ui.procurement.MaterialsScreen
import com.satinalmapro.android.ui.procurement.NewRequestScreen
import com.satinalmapro.android.ui.procurement.NotificationsScreen
import com.satinalmapro.android.ui.procurement.OnayGecmisiScreen
import com.satinalmapro.android.ui.procurement.ProfileScreen
import com.satinalmapro.android.ui.procurement.SettingsScreen
import com.satinalmapro.android.ui.procurement.TalepDetayScreen
import com.satinalmapro.android.ui.procurement.TalepListScreen
import com.satinalmapro.android.ui.procurement.TeklifGirisScreen
import com.satinalmapro.android.ui.procurement.TeklifKarsilastirmaScreen
import com.satinalmapro.android.ui.procurement.TeklifOnayDetayScreen
import com.satinalmapro.android.ui.procurement.TeklifsizFirmaFiyatScreen
import com.satinalmapro.android.ui.theme.AppShapes
import com.satinalmapro.android.ui.theme.MetrikLight
import com.satinalmapro.android.ui.theme.MetrikSpace

@Composable
fun ProcurementShell(viewModel: AppViewModel) {
    val currentRoute by viewModel.currentRoute.collectAsState()
    val user by viewModel.user.collectAsState()
    val notifications by viewModel.notifications.collectAsState()
    val menuBadges by viewModel.menuBadges.collectAsState()
    val activity = LocalFragmentActivity.current
    val route = currentRoute ?: "dashboard"
    val base = route.substringBefore('?')
    val menus = viewModel.menus()
    val queueMenus = RolNavigasyon.queueMenus(user?.role)
    val title = menus.firstOrNull { it.route == base }?.title
        ?: if (base == "dashboard") "Satınalma" else "Satınalma Pro"
    val unread = notifications.count { !it.read }
    val isRoot = base in setOf("dashboard", "bildirimler", "profil", "ayarlar") ||
        queueMenus.any { it.route == base }

    BackHandler {
        when {
            viewModel.navigateBack() -> Unit
            else -> activity?.moveTaskToBack(true)
        }
    }

    Scaffold(
        containerColor = MetrikLight.Background,
        topBar = {
            ShellTopChrome(
                title = title,
                showBack = !isRoot && base != "dashboard",
                unread = unread,
                queueMenus = queueMenus,
                selectedRoute = base,
                badges = menuBadges,
                onBack = { viewModel.navigateBack() },
                onNotifications = { viewModel.navigateFromMenu("bildirimler") },
                onProfile = { viewModel.navigateFromMenu("profil") },
                onSelectQueue = { viewModel.navigateFromMenu(it) }
            )
        },
        bottomBar = {
            TenantFooter(
                firma = TenantSession.tenantName().orEmpty(),
                kullanici = user?.fullName?.ifBlank { user?.email }.orEmpty(),
                rol = user?.role.orEmpty()
            )
        }
    ) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .background(MetrikLight.Background)
        ) {
            RouteHost(route = route, viewModel = viewModel)
        }
    }
}

@Composable
private fun ShellTopChrome(
    title: String,
    showBack: Boolean,
    unread: Int,
    queueMenus: List<MenuItem>,
    selectedRoute: String,
    badges: Map<String, Int>,
    onBack: () -> Unit,
    onNotifications: () -> Unit,
    onProfile: () -> Unit,
    onSelectQueue: (String) -> Unit
) {
    Column(
        modifier = Modifier.background(MetrikLight.Surface)
    ) {
        TopBar(
            title = title,
            showBack = showBack,
            unread = unread,
            onBack = onBack,
            onNotifications = onNotifications,
            onProfile = onProfile
        )
        if (queueMenus.isNotEmpty()) {
            RoleQueueStrip(
                items = queueMenus,
                selectedRoute = selectedRoute,
                badges = badges,
                onSelect = onSelectQueue
            )
        }
        HorizontalDivider(color = MetrikLight.Divider, thickness = 1.dp)
    }
}

@Composable
private fun TopBar(
    title: String,
    showBack: Boolean,
    unread: Int,
    onBack: () -> Unit,
    onNotifications: () -> Unit,
    onProfile: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .height(56.dp)
            .padding(horizontal = MetrikSpace.sm),
        verticalAlignment = Alignment.CenterVertically
    ) {
        if (showBack) {
            IconButton(onClick = onBack) {
                Icon(Icons.AutoMirrored.Rounded.ArrowBack, contentDescription = "Geri", tint = MetrikLight.Primary)
            }
        } else {
            Spacer(Modifier.width(8.dp))
        }
        Text(
            text = title,
            style = MaterialTheme.typography.titleLarge,
            color = MetrikLight.TextPrimary,
            fontWeight = FontWeight.SemiBold,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
            modifier = Modifier.weight(1f)
        )
        IconButton(onClick = onNotifications) {
            BadgedBox(badge = {
                if (unread > 0) Badge { Text(if (unread > 99) "99+" else unread.toString()) }
            }) {
                Icon(Icons.Rounded.Notifications, contentDescription = "Bildirimler", tint = MetrikLight.Primary)
            }
        }
        IconButton(onClick = onProfile) {
            Icon(Icons.Rounded.Person, contentDescription = "Profil", tint = MetrikLight.Primary)
        }
    }
}

@Composable
private fun RoleQueueStrip(
    items: List<MenuItem>,
    selectedRoute: String,
    badges: Map<String, Int>,
    onSelect: (String) -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .horizontalScroll(rememberScrollState())
            .padding(horizontal = MetrikSpace.md, vertical = MetrikSpace.strip),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        for (item in items) {
            val selected = item.route == selectedRoute
            val badge = badges[item.route] ?: 0
            val label = if (badge > 0) "${item.title} ($badge)" else item.title
            Text(
                text = label,
                modifier = Modifier
                    .background(
                        if (selected) MetrikLight.Primary else MetrikLight.SurfaceMuted,
                        AppShapes.chip
                    )
                    .border(
                        width = 1.dp,
                        color = if (selected) MetrikLight.Primary else MetrikLight.Border,
                        shape = AppShapes.chip
                    )
                    .clickable { onSelect(item.route) }
                    .padding(horizontal = 12.dp, vertical = 8.dp),
                color = if (selected) MetrikLight.TextOnPrimary else MetrikLight.TextPrimary,
                style = MaterialTheme.typography.labelLarge,
                maxLines = 1
            )
        }
    }
}

@Composable
private fun RouteHost(route: String, viewModel: AppViewModel) {
    val base = route.substringBefore('?')
    val query = route.substringAfter('?', "")
    val talepId = query.substringAfter("id=", "").substringBefore('&').takeIf { it.isNotBlank() }
    val section = query.substringAfter("section=", "").substringBefore('&').takeIf { it.isNotBlank() }
    val viewMode = query.substringAfter("view=", "").substringBefore('&').takeIf { it.isNotBlank() }

    when (base) {
        "dashboard" -> DashboardScreen(viewModel)
        "talep-duzenle" -> if (talepId != null) NewRequestScreen(viewModel, editTalepId = talepId)
        else DashboardScreen(viewModel)
        "yeni-talep" -> NewRequestScreen(viewModel)
        "onaylanan-malzemeler" -> MaterialsScreen(viewModel, initialSection = section)
        "bildirimler" -> NotificationsScreen(viewModel)
        "profil" -> ProfileScreen(viewModel)
        "ayarlar" -> SettingsScreen(viewModel)
        "teklif-gir" -> if (talepId != null) {
            val user by viewModel.user.collectAsState()
            if (KullaniciRolleri.canEnterQuotes(user?.role)) TeklifGirisScreen(viewModel, talepId)
            else TalepDetayScreen(viewModel, talepId)
        } else TalepListScreen(viewModel, TalepQueue.TEKLIF_GIR)
        "satinalma-teklif-istenen" -> TalepListScreen(viewModel, TalepQueue.SATINALMA_TEKLIF_ISTENEN)
        "satinalma-teklif-girilen" -> TalepListScreen(viewModel, TalepQueue.SATINALMA_TEKLIF_GIRILEN)
        "satinalma-teklif-duzeltme", "teklif-duzeltme" -> TalepListScreen(viewModel, TalepQueue.SATINALMA_TEKLIF_DUZELTME)
        "yonetim-teklif-girilen" -> TalepListScreen(viewModel, TalepQueue.TEKLIF_ONAY)
        "yonetim-direk-onaylanan" -> TalepListScreen(viewModel, TalepQueue.YONETIM_DIREK_ONAYLANAN)
        "satinalma-onaylanan" -> TalepListScreen(viewModel, TalepQueue.SATINALMA_ONAYLANAN)
        "satinalma-siparis" -> TalepListScreen(viewModel, TalepQueue.SATINALMA_SIPARIS)
        "satinalma-mal-kabul" -> TalepListScreen(viewModel, TalepQueue.SATINALMA_MAL_KABUL)
        "taleplerim" -> TalepListScreen(viewModel, TalepQueue.TALEPLERIM)
        "onay-bekleyen" -> TalepListScreen(viewModel, TalepQueue.ONAY_BEKLEYEN)
        "onaylanan-talepler" -> TalepListScreen(viewModel, TalepQueue.ONAYLANAN_TALEPLER)
        "gelen-talepler" -> TalepListScreen(viewModel, TalepQueue.GELEN_TALEPLER)
        "teklif-bekleyen" -> if (talepId != null) TalepDetayScreen(viewModel, talepId)
        else TalepListScreen(viewModel, TalepQueue.TEKLIF_BEKLEYEN)
        "gecmis-talepler" -> TalepListScreen(viewModel, TalepQueue.GECMIS_TALEPLER)
        "gecmis-teklifli-onaylar" -> TalepListScreen(viewModel, TalepQueue.GECMIS_TEKLIFLI)
        "red-talepler" -> TalepListScreen(viewModel, TalepQueue.RED_TALEPLER)
        "onaylanan-teklifler" -> TalepListScreen(viewModel, TalepQueue.ONAYLANAN_TEKLIFLER)
        "teklif-karsilastirma", "satinalma-karsilastirma" -> TeklifKarsilastirmaScreen(viewModel, talepId)
        "teklif-onay" -> TalepListScreen(viewModel, TalepQueue.TEKLIF_ONAY)
        "teklif-onay-detay" -> TeklifOnayDetayScreen(viewModel, talepId.orEmpty())
        "teklifsiz-firma-fiyat" -> TeklifsizFirmaFiyatScreen(viewModel, talepId)
        "onay-gecmisi", "yonetim-onay-gecmisi", "yonetim-gecmis" -> OnayGecmisiScreen(viewModel)
        "talep-detay" -> TalepDetayScreen(viewModel, talepId.orEmpty(), viewMode = viewMode)
        else -> DashboardScreen(viewModel)
    }
}
