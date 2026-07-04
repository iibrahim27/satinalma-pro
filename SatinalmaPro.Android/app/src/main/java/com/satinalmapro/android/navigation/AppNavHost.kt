package com.satinalmapro.android.navigation

import androidx.compose.animation.AnimatedContentTransitionScope
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.currentBackStackEntryAsState
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.satinalmapro.android.ui.screens.home.HomeScreen
import com.satinalmapro.android.ui.screens.login.LoginScreen
import com.satinalmapro.android.ui.screens.materials.MaterialDetailScreen
import com.satinalmapro.android.ui.screens.materials.MaterialsScreen
import com.satinalmapro.android.ui.screens.notifications.NotificationsScreen
import com.satinalmapro.android.ui.screens.profile.ProfileScreen
import com.satinalmapro.android.ui.screens.quote.QuoteComparisonScreen
import com.satinalmapro.android.ui.screens.reports.ReportsScreen
import com.satinalmapro.android.ui.screens.request.NewRequestScreen
import com.satinalmapro.android.ui.theme.AppColors

private val mainRoutes = setOf(
    AppRoutes.HOME,
    AppRoutes.MATERIALS,
    AppRoutes.REPORTS,
    AppRoutes.PROFILE
)

@Composable
fun AppNavHost() {
    val navController = rememberNavController()
    val backStackEntry by navController.currentBackStackEntryAsState()
    val currentRoute = backStackEntry?.destination?.route?.substringBefore("/")
    val showBottomBar = currentRoute in mainRoutes

    Scaffold(
        modifier = Modifier.fillMaxSize(),
        containerColor = AppColors.Background,
        bottomBar = {
            if (showBottomBar) {
                AppBottomBar(
                    currentRoute = currentRoute,
                    onNavigate = { route ->
                        navController.navigate(route) {
                            popUpTo(navController.graph.findStartDestination().id) {
                                saveState = true
                            }
                            launchSingleTop = true
                            restoreState = true
                        }
                    },
                    onFabClick = { navController.navigate(AppRoutes.NEW_REQUEST) }
                )
            }
        }
    ) { padding ->
        NavHost(
            navController = navController,
            startDestination = AppRoutes.LOGIN,
            modifier = Modifier
                .fillMaxSize()
                .padding(padding),
            enterTransition = {
                fadeIn(tween(220)) + slideIntoContainer(
                    AnimatedContentTransitionScope.SlideDirection.Start,
                    tween(280)
                )
            },
            exitTransition = {
                fadeOut(tween(180)) + slideOutOfContainer(
                    AnimatedContentTransitionScope.SlideDirection.Start,
                    tween(240)
                )
            },
            popEnterTransition = {
                fadeIn(tween(220)) + slideIntoContainer(
                    AnimatedContentTransitionScope.SlideDirection.End,
                    tween(280)
                )
            },
            popExitTransition = {
                fadeOut(tween(180)) + slideOutOfContainer(
                    AnimatedContentTransitionScope.SlideDirection.End,
                    tween(240)
                )
            }
        ) {
            composable(AppRoutes.LOGIN) {
                LoginScreen(
                    onLoginSuccess = {
                        navController.navigate(AppRoutes.HOME) {
                            popUpTo(AppRoutes.LOGIN) { inclusive = true }
                        }
                    }
                )
            }

            composable(AppRoutes.HOME) {
                HomeScreen(
                    onNotificationsClick = { navController.navigate(AppRoutes.NOTIFICATIONS) },
                    onQuickAction = { action ->
                        when (action) {
                            "request" -> navController.navigate(AppRoutes.NEW_REQUEST)
                            "quote" -> navController.navigate(AppRoutes.QUOTE_COMPARISON)
                            else -> Unit
                        }
                    }
                )
            }

            composable(AppRoutes.MATERIALS) {
                MaterialsScreen { id ->
                    navController.navigate(AppRoutes.materialDetail(id))
                }
            }

            composable(
                route = AppRoutes.MATERIAL_DETAIL,
                arguments = listOf(navArgument("materialId") { type = NavType.StringType })
            ) {
                MaterialDetailScreen(onBack = { navController.popBackStack() })
            }

            composable(AppRoutes.NEW_REQUEST) {
                NewRequestScreen(
                    onBack = { navController.popBackStack() },
                    onSubmit = { navController.popBackStack() }
                )
            }

            composable(AppRoutes.QUOTE_COMPARISON) {
                QuoteComparisonScreen(onBack = { navController.popBackStack() })
            }

            composable(AppRoutes.NOTIFICATIONS) {
                NotificationsScreen(onBack = { navController.popBackStack() })
            }

            composable(AppRoutes.REPORTS) {
                ReportsScreen()
            }

            composable(AppRoutes.PROFILE) {
                ProfileScreen(
                    onLogout = {
                        navController.navigate(AppRoutes.LOGIN) {
                            popUpTo(0) { inclusive = true }
                        }
                    }
                )
            }
        }
    }
}
