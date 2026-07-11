package com.satinalmapro.android.ui.shell

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowBack
import androidx.compose.material.icons.automirrored.rounded.ListAlt
import androidx.compose.material.icons.rounded.Home
import androidx.compose.material.icons.rounded.Inventory2
import androidx.compose.material.icons.rounded.Notifications
import androidx.compose.material.icons.rounded.Person
import androidx.compose.material.icons.rounded.ShoppingCart
import androidx.compose.material3.Badge
import androidx.compose.material3.BadgedBox
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.NavigationBarItemDefaults
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.RolNavigasyon
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.procurement.AgregaListScreen
import com.satinalmapro.android.ui.procurement.CimentoListScreen
import com.satinalmapro.android.ui.procurement.DashboardScreen
import com.satinalmapro.android.ui.procurement.IadeListScreen
import com.satinalmapro.android.ui.procurement.MaterialsScreen
import com.satinalmapro.android.ui.procurement.NewRequestScreen
import com.satinalmapro.android.ui.procurement.NotificationsScreen
import com.satinalmapro.android.ui.procurement.OnayGecmisiScreen
import com.satinalmapro.android.ui.procurement.ProfileScreen
import com.satinalmapro.android.ui.procurement.QueuesHubScreen
import com.satinalmapro.android.ui.procurement.SettingsScreen
import com.satinalmapro.android.ui.procurement.StokCikisScreen
import com.satinalmapro.android.ui.procurement.StokDurumScreen
import com.satinalmapro.android.ui.procurement.StokGirisScreen
import com.satinalmapro.android.ui.procurement.StokHareketScreen
import com.satinalmapro.android.ui.procurement.TalepDetayScreen
import com.satinalmapro.android.ui.procurement.TalepListScreen
import com.satinalmapro.android.ui.procurement.TedarikciListScreen
import com.satinalmapro.android.ui.procurement.TeklifGirisScreen
import com.satinalmapro.android.ui.procurement.TeklifKarsilastirmaScreen
import com.satinalmapro.android.ui.procurement.TeklifOnayDetayScreen
import com.satinalmapro.android.ui.procurement.TeklifsizFirmaFiyatScreen
import com.satinalmapro.android.ui.theme.MetrikLight

private data class BottomTab(
    val route: String,
    val label: String,
    val icon: ImageVector
)

@OptIn(ExperimentalMaterial3Api::class)
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
    val title = when (base) {
        "dashboard" -> null // kendi hero'su var
        "isler" -> null
        else -> menus.firstOrNull { it.route == base }?.title
            ?: when (base) {
                "bildirimler" -> "Bildirimler"
                "profil" -> "Profil"
                "ayarlar" -> "Ayarlar"
                "yeni-talep" -> "Yeni Talep"
                "stok-durum" -> "Stok Durumu"
                "stok-hareket" -> "Stok Hareketleri"
                "stok-giris" -> "Stok Girişi"
                "stok-cikis" -> "Stok Çıkışı"
                else -> "Satınalma Pro"
            }
    }
    val unread = notifications.count { !it.read }
    val waiting = menuBadges.values.sum()
    val bottomTabs = remember(user?.role) { bottomTabsFor(user?.role) }
    val hideChrome = base in setOf("dashboard", "isler")
    val isRoot = base in bottomTabs.map { it.route } || base == "ayarlar"

    BackHandler {
        when {
            viewModel.navigateBack() -> Unit
            else -> activity?.moveTaskToBack(true)
        }
    }

    Scaffold(
        containerColor = MetrikLight.Background,
        topBar = {
            if (!hideChrome) {
                TopAppBar(
                    title = {
                        Column {
                            Text(
                                title.orEmpty(),
                                maxLines = 1,
                                overflow = TextOverflow.Ellipsis,
                                fontWeight = FontWeight.SemiBold
                            )
                            val firma = TenantSession.tenantName().orEmpty()
                            if (firma.isNotBlank() && base != "profil") {
                                Text(
                                    firma,
                                    style = MaterialTheme.typography.labelSmall,
                                    color = MetrikLight.TextSecondary,
                                    maxLines = 1,
                                    overflow = TextOverflow.Ellipsis
                                )
                            }
                        }
                    },
                    navigationIcon = {
                        if (!isRoot) {
                            IconButton(onClick = { viewModel.navigateBack() }) {
                                Icon(Icons.AutoMirrored.Rounded.ArrowBack, contentDescription = "Geri")
                            }
                        }
                    },
                    actions = {
                        if (base != "bildirimler") {
                            IconButton(onClick = { viewModel.navigateFromMenu("bildirimler") }) {
                                BadgedBox(badge = {
                                    if (unread > 0) {
                                        Badge { Text(if (unread > 99) "99+" else unread.toString()) }
                                    }
                                }) {
                                    Icon(Icons.Rounded.Notifications, contentDescription = "Bildirimler")
                                }
                            }
                        }
                    },
                    colors = TopAppBarDefaults.topAppBarColors(
                        containerColor = MetrikLight.Surface,
                        titleContentColor = MetrikLight.TextPrimary,
                        navigationIconContentColor = MetrikLight.TextPrimary,
                        actionIconContentColor = MetrikLight.TextPrimary
                    )
                )
            }
        },
        bottomBar = {
            if (bottomTabs.isNotEmpty()) {
                NavigationBar(
                    containerColor = MetrikLight.Surface,
                    tonalElevation = 0.dp,
                    modifier = Modifier
                        .fillMaxWidth()
                        .background(MetrikLight.Surface)
                ) {
                    bottomTabs.forEach { tab ->
                        val selected = when (tab.route) {
                            "isler" -> {
                                val queues = RolNavigasyon.queueMenus(user?.role)
                                base == "isler" || (
                                    base !in setOf("dashboard", "bildirimler", "profil", "ayarlar", "yeni-talep") &&
                                        !base.startsWith("stok-") &&
                                        queues.any { it.route == base }
                                    )
                            }
                            "stok-durum" -> base.startsWith("stok-")
                            else -> base == tab.route
                        }
                        val badge = when (tab.route) {
                            "isler" -> waiting
                            "bildirimler" -> unread
                            else -> menuBadges[tab.route] ?: 0
                        }
                        NavigationBarItem(
                            selected = selected,
                            onClick = { viewModel.navigateFromMenu(tab.route) },
                            icon = {
                                if (badge > 0) {
                                    BadgedBox(badge = {
                                        Badge(
                                            containerColor = MetrikLight.Accent,
                                            contentColor = MetrikLight.TextOnAccent
                                        ) {
                                            Text(if (badge > 99) "99+" else badge.toString())
                                        }
                                    }) {
                                        Icon(tab.icon, contentDescription = tab.label)
                                    }
                                } else {
                                    Icon(tab.icon, contentDescription = tab.label)
                                }
                            },
                            label = {
                                Text(tab.label, maxLines = 1, overflow = TextOverflow.Ellipsis)
                            },
                            colors = NavigationBarItemDefaults.colors(
                                selectedIconColor = MetrikLight.Accent,
                                selectedTextColor = MetrikLight.Accent,
                                indicatorColor = MetrikLight.AccentMuted,
                                unselectedIconColor = MetrikLight.TextSecondary,
                                unselectedTextColor = MetrikLight.TextSecondary
                            )
                        )
                    }
                }
            }
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

private fun bottomTabsFor(role: String?): List<BottomTab> {
    val normalized = KullaniciRolleri.normalize(role)
    val tabs = mutableListOf(
        BottomTab("dashboard", "Ana", Icons.Rounded.Home),
        BottomTab("isler", "İşler", Icons.AutoMirrored.Rounded.ListAlt)
    )
    val queues = RolNavigasyon.queueMenus(role)
    when {
        queues.any { it.route == "onaylanan-malzemeler" } || KullaniciRolleri.canMalKabul(role) ->
            tabs += BottomTab("onaylanan-malzemeler", "Malzeme", Icons.Rounded.ShoppingCart)
        normalized in setOf(KullaniciRolleri.DEPO, KullaniciRolleri.ATOLYE) ||
            queues.any { it.route.startsWith("stok-") } ->
            tabs += BottomTab("stok-durum", "Stok", Icons.Rounded.Inventory2)
    }
    tabs += BottomTab("bildirimler", "Bildirim", Icons.Rounded.Notifications)
    tabs += BottomTab("profil", "Profil", Icons.Rounded.Person)
    return tabs.take(5)
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
        "isler" -> QueuesHubScreen(viewModel)
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
        "stok-durum" -> StokDurumScreen(viewModel)
        "stok-hareket" -> StokHareketScreen(viewModel)
        "stok-giris" -> StokGirisScreen(viewModel)
        "stok-cikis" -> StokCikisScreen(viewModel)
        "satinalma-panosu" -> DashboardScreen(viewModel)
        "satinalma-onay-gecmisi" -> OnayGecmisiScreen(viewModel)
        "yonetim-red-verilen" -> TalepListScreen(viewModel, TalepQueue.RED_TALEPLER)
        "agrega" -> AgregaListScreen(viewModel)
        "cimento" -> CimentoListScreen(viewModel)
        "satinalma-tedarikciler" -> TedarikciListScreen(viewModel)
        "satinalma-iade" -> IadeListScreen(viewModel)
        else -> DashboardScreen(viewModel)
    }
}
