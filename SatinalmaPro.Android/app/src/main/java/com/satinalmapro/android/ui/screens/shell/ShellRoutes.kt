package com.satinalmapro.android.ui.screens.shell

object ShellRoutes {
    private val mainTabs = setOf("dashboard", "stok-durum", "bildirimler", "raporlar", "profil")

    fun baseRoute(route: String?) = route?.substringBefore('?').orEmpty()

    fun isMainTab(route: String?) = baseRoute(route) in mainTabs
}
