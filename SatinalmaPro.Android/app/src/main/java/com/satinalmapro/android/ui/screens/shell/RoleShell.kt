package com.satinalmapro.android.ui.screens.shell

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.RolNavigasyon
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.components.AppBottomNavigationBar
import com.satinalmapro.android.ui.components.AppMainHeader
import com.satinalmapro.android.ui.screens.home.HomeScreen
import com.satinalmapro.android.ui.screens.materials.MaterialsScreen
import com.satinalmapro.android.ui.screens.notifications.NotificationsScreen
import com.satinalmapro.android.ui.screens.profile.ProfileScreen
import com.satinalmapro.android.ui.screens.request.NewRequestScreen
import com.satinalmapro.android.ui.screens.settings.SettingsScreen
import com.satinalmapro.android.ui.screens.stok.StokCikisScreen
import com.satinalmapro.android.ui.screens.stok.StokDurumScreen
import com.satinalmapro.android.ui.screens.stok.StokGirisScreen
import com.satinalmapro.android.ui.screens.stok.StokHareketScreen
import com.satinalmapro.android.ui.screens.stok.StokSayimScreen
import com.satinalmapro.android.ui.screens.talep.TalepDetayScreen
import com.satinalmapro.android.ui.screens.talep.TalepListScreen
import com.satinalmapro.android.ui.screens.teklif.TeklifGirisScreen
import com.satinalmapro.android.ui.theme.AppColors
import androidx.compose.material3.DrawerValue
import androidx.compose.material3.ModalDrawerSheet
import androidx.compose.material3.ModalNavigationDrawer
import androidx.compose.material3.rememberDrawerState
import kotlinx.coroutines.launch

@Composable
fun RoleShell(viewModel: AppViewModel) {
    val currentRoute by viewModel.currentRoute.collectAsState()
    val user by viewModel.user.collectAsState()
    val notifications by viewModel.notifications.collectAsState()
    val drawerState = rememberDrawerState(DrawerValue.Closed)
    val scope = rememberCoroutineScope()
    val activity = LocalFragmentActivity.current
    val menus = viewModel.menus()
    val menuBadges by viewModel.menuBadges.collectAsState()
    val route = currentRoute ?: "dashboard"
    val base = ShellRoutes.baseRoute(route)
    val isMainTab = ShellRoutes.isMainTab(route)
    val title = menus.firstOrNull { it.route == base }?.title ?: "Satınalma Pro"
    val unreadCount = notifications.count { !it.read }
    val role = user?.role
    val showFab = KullaniciRolleri.canCreateRequest(role)
    val showReports = viewModel.canAccess("raporlar")
    val homeRoute = RolNavigasyon.defaultRoute(role)

    BackHandler {
        when {
            drawerState.isOpen -> scope.launch { drawerState.close() }
            viewModel.navigateBack() -> Unit
            else -> activity?.moveTaskToBack(true)
        }
    }

    ModalNavigationDrawer(
        drawerState = drawerState,
        drawerContent = {
            ModalDrawerSheet(drawerContainerColor = AppColors.Surface) {
                AppNavigationDrawer(
                    user = user,
                    menus = menus,
                    selectedRoute = base,
                    menuBadges = menuBadges,
                    onItemClick = { item ->
                        viewModel.navigateFromMenu(item.route)
                        scope.launch { drawerState.close() }
                    },
                    onLogout = {
                        scope.launch { drawerState.close() }
                        viewModel.logout()
                    }
                )
            }
        }
    ) {
        Scaffold(
            containerColor = AppColors.Background,
            topBar = {
                AppMainHeader(
                    title = title,
                    showMenu = isMainTab,
                    showBack = !isMainTab,
                    notificationCount = unreadCount,
                    onMenuClick = { scope.launch { drawerState.open() } },
                    onBackClick = { viewModel.navigateBack() },
                    onNotificationClick = { viewModel.navigateFromMenu("bildirimler") },
                    onProfileClick = { viewModel.navigateFromMenu("profil") }
                )
            },
            bottomBar = {
                if (isMainTab) {
                    AppBottomNavigationBar(
                        selectedRoute = base,
                        showReports = showReports,
                        showFab = showFab,
                        onHome = { viewModel.navigateFromMenu(homeRoute) },
                        onNotifications = { viewModel.navigateFromMenu("bildirimler") },
                        onReports = { viewModel.navigateFromMenu("raporlar") },
                        onProfile = { viewModel.navigateFromMenu("profil") },
                        onFabClick = { viewModel.navigate("yeni-talep") }
                    )
                }
            }
        ) { padding ->
            RoleRouteContent(
                route = route,
                viewModel = viewModel,
                modifier = Modifier.padding(padding)
            )
        }
    }
}

@Composable
private fun RoleRouteContent(route: String, viewModel: AppViewModel, modifier: Modifier = Modifier) {
    val base = route.substringBefore('?')
    val query = route.substringAfter('?', "")
    val talepId = query.substringAfter("id=", "").substringBefore('&').takeIf { it.isNotBlank() }
    val section = query.substringAfter("section=", "").substringBefore('&').takeIf { it.isNotBlank() }
    val viewMode = query.substringAfter("view=", "").substringBefore('&').takeIf { it.isNotBlank() }

    Box(modifier = modifier.fillMaxSize()) {
        when (base) {
            "dashboard" -> HomeScreen(viewModel = viewModel, modifier = Modifier.fillMaxSize())
            "talep-duzenle" -> if (talepId != null) NewRequestScreen(viewModel, editTalepId = talepId) else HomeScreen(viewModel = viewModel, modifier = Modifier.fillMaxSize())
            "yeni-talep" -> NewRequestScreen(viewModel = viewModel)
            "onaylanan-malzemeler" -> MaterialsScreen(viewModel = viewModel, initialSection = section)
            "raporlar" -> com.satinalmapro.android.ui.screens.reports.ReportsScreen(viewModel)
            "bildirimler" -> NotificationsScreen(viewModel = viewModel)
            "profil" -> ProfileScreen(viewModel = viewModel)
            "teklif-gir" -> if (talepId != null) {
                val user by viewModel.user.collectAsState()
                if (KullaniciRolleri.canEnterQuotes(user?.role)) {
                    TeklifGirisScreen(viewModel, talepId)
                } else {
                    TalepDetayScreen(viewModel, talepId)
                }
            } else {
                TalepListScreen(viewModel, TalepQueue.TEKLIF_GIR)
            }
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
            "teklif-bekleyen" -> if (talepId != null) {
                TalepDetayScreen(viewModel, talepId)
            } else {
                TalepListScreen(viewModel, TalepQueue.TEKLIF_BEKLEYEN)
            }
            "gecmis-talepler" -> TalepListScreen(viewModel, TalepQueue.GECMIS_TALEPLER)
            "gecmis-teklifli-onaylar" -> TalepListScreen(viewModel, TalepQueue.GECMIS_TEKLIFLI)
            "red-talepler" -> TalepListScreen(viewModel, TalepQueue.RED_TALEPLER)
            "onaylanan-teklifler" -> TalepListScreen(viewModel, TalepQueue.ONAYLANAN_TEKLIFLER)
            "teklif-karsilastirma", "satinalma-karsilastirma" -> com.satinalmapro.android.ui.screens.teklif.TeklifKarsilastirmaScreen(viewModel, talepId)
            "teklif-onay" -> TalepListScreen(viewModel, TalepQueue.TEKLIF_ONAY)
            "teklif-onay-detay" -> com.satinalmapro.android.ui.screens.teklif.TeklifOnayDetayScreen(viewModel, talepId.orEmpty())
            "teklifsiz-firma-fiyat" -> com.satinalmapro.android.ui.screens.teklif.TeklifsizFirmaFiyatScreen(viewModel, talepId)
            "onay-gecmisi", "yonetim-onay-gecmisi", "yonetim-gecmis" ->
                com.satinalmapro.android.ui.screens.teklif.OnayGecmisiScreen(viewModel)
            "talep-detay" -> TalepDetayScreen(viewModel, talepId.orEmpty(), viewMode = viewMode)
            "stok-durum" -> StokDurumScreen(viewModel)
            "stok-giris" -> StokGirisScreen(viewModel)
            "stok-cikis" -> StokCikisScreen(viewModel)
            "stok-hareket" -> StokHareketScreen(viewModel)
            "stok-sayim" -> StokSayimScreen(viewModel)
            "agrega" -> com.satinalmapro.android.ui.screens.modul.AgregaScreen(viewModel)
            "cimento" -> com.satinalmapro.android.ui.screens.modul.CimentoScreen(viewModel)
            "alinan-malzemeler" -> com.satinalmapro.android.ui.screens.modul.AlinanMalzemeModulScreen(viewModel)
            "ayarlar" -> SettingsScreen(viewModel)
            else -> HomeScreen(viewModel = viewModel, modifier = Modifier.fillMaxSize())
        }
    }
}
