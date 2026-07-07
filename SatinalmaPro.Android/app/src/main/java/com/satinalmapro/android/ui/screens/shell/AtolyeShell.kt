package com.satinalmapro.android.ui.screens.shell

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import com.satinalmapro.android.core.roles.RolNavigasyon
import com.satinalmapro.android.ui.AppViewModel
import com.satinalmapro.android.ui.LocalFragmentActivity
import com.satinalmapro.android.ui.components.AppBottomNavigationBar
import com.satinalmapro.android.ui.components.AppMainHeader
import com.satinalmapro.android.ui.screens.home.HomeScreen
import com.satinalmapro.android.ui.screens.notifications.NotificationsScreen
import com.satinalmapro.android.ui.screens.profile.ProfileScreen
import com.satinalmapro.android.ui.screens.stok.StokDurumScreen
import com.satinalmapro.android.ui.theme.AppColors

@Composable
fun AtolyeShell(viewModel: AppViewModel) {
    val activity = LocalFragmentActivity.current
    val currentRoute by viewModel.currentRoute.collectAsState()
    val notifications by viewModel.notifications.collectAsState()
    val user by viewModel.user.collectAsState()
    val base = ShellRoutes.baseRoute(currentRoute ?: "stok-durum")
    val isMainTab = ShellRoutes.isMainTab(currentRoute)
    val unreadCount = notifications.count { !it.read }
    val homeRoute = RolNavigasyon.defaultRoute(user?.role)

    BackHandler {
        when {
            !isMainTab -> viewModel.navigateBack()
            base != homeRoute -> viewModel.navigateFromMenu(homeRoute)
            else -> activity?.moveTaskToBack(true)
        }
    }

    Scaffold(
        containerColor = AppColors.Background,
        topBar = {
            AppMainHeader(
                title = when (base) {
                    "bildirimler" -> "Bildirimler"
                    "profil" -> "Profil"
                    "dashboard" -> "Atölye"
                    else -> "Stok Durumu"
                },
                showMenu = false,
                showBack = !isMainTab,
                notificationCount = unreadCount,
                onMenuClick = {},
                onBackClick = { viewModel.navigateBack() },
                onNotificationClick = { viewModel.navigateFromMenu("bildirimler") },
                onProfileClick = { viewModel.navigateFromMenu("profil") }
            )
        },
        bottomBar = {
            if (isMainTab) {
                AppBottomNavigationBar(
                    selectedRoute = base,
                    showReports = false,
                    showFab = false,
                    onHome = {
                        viewModel.navigateFromMenu(homeRoute)
                    },
                    onNotifications = { viewModel.navigateFromMenu("bildirimler") },
                    onReports = {},
                    onProfile = { viewModel.navigateFromMenu("profil") },
                    onFabClick = {}
                )
            }
        }
    ) { padding ->
        Box(Modifier.padding(padding).fillMaxSize()) {
            when (base) {
                "dashboard" -> HomeScreen(viewModel = viewModel, modifier = Modifier.fillMaxSize())
                "bildirimler" -> NotificationsScreen(viewModel = viewModel)
                "profil" -> ProfileScreen(viewModel = viewModel)
                else -> StokDurumScreen(
                    viewModel = viewModel,
                    atolyeMode = true,
                    modifier = Modifier.fillMaxSize()
                )
            }
        }
    }
}
