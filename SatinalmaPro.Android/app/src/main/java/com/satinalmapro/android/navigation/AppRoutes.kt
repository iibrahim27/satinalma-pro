package com.satinalmapro.android.navigation

object AppRoutes {
    const val LOGIN = "login"
    const val HOME = "home"
    const val MATERIALS = "materials"
    const val MATERIAL_DETAIL = "material_detail/{materialId}"
    const val NEW_REQUEST = "new_request"
    const val QUOTE_COMPARISON = "quote_comparison"
    const val NOTIFICATIONS = "notifications"
    const val REPORTS = "reports"
    const val PROFILE = "profile"

    fun materialDetail(materialId: String) = "material_detail/$materialId"
}

enum class BottomNavItem(val route: String, val label: String) {
    Home(AppRoutes.HOME, "Ana Sayfa"),
    Materials(AppRoutes.MATERIALS, "Malzemeler"),
    Reports(AppRoutes.REPORTS, "Raporlar"),
    Profile(AppRoutes.PROFILE, "Profil")
}
