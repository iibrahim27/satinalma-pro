package com.satinalmapro.android.ui.screens.shell

object ShellRoutes {
    private val mainTabs = setOf("dashboard", "stok-durum", "bildirimler", "raporlar", "profil")

    private val moduleTabRoutes = setOf(
        "taleplerim", "onay-bekleyen", "onaylanan-talepler", "gelen-talepler",
        "teklif-bekleyen", "teklif-gir", "teklif-karsilastirma", "satinalma-karsilastirma",
        "satinalma-teklif-istenen", "satinalma-teklif-girilen", "satinalma-teklif-duzeltme",
        "teklif-duzeltme", "yonetim-teklif-girilen", "yonetim-direk-onaylanan",
        "satinalma-onaylanan", "satinalma-siparis", "satinalma-mal-kabul",
        "gecmis-talepler", "gecmis-teklifli-onaylar", "red-talepler", "onaylanan-teklifler",
        "teklif-onay", "onay-gecmisi", "yonetim-onay-gecmisi", "yonetim-gecmis",
        "onaylanan-malzemeler", "stok-giris", "stok-cikis", "stok-hareket", "stok-sayim",
        "agrega", "cimento", "alinan-malzemeler"
    )

    fun baseRoute(route: String?) = route?.substringBefore('?').orEmpty()

    fun isMainTab(route: String?) = baseRoute(route) in mainTabs

    fun isModuleTabRoute(route: String) = route in moduleTabRoutes
}
