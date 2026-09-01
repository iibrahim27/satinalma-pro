package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.MenuItem

/**
 * Mobil uygulama Talep Pro akışı — onay sonrası yalnızca geçmiş sekmeleri.
 * Masaüstü [SatinalmaPro.Shared.Helpers.TalepProRuntime] ile aynı kural seti.
 */
object TalepProModu {
    const val AKTIF = true

    val HARIC_ROUTES = setOf(
        "satinalma-onaylanan",
        "onaylanan-teklifler",
        "yonetim-onaylanan-teklifler",
        "onaylanan-talepler",
        "satinalma-siparis",
        "satinalma-mal-kabul",
        "satinalma-iade",
        "satinalma-tedarikciler",
        "onaylanan-malzemeler",
        "yonetim-direk-onaylanan",
        "gecmis-talepler",
        "gecmis-teklifli-onaylar",
        "teklifsiz-firma-fiyat"
    )

    fun onaySonrasiRoute(role: String?): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.YONETIM, KullaniciRolleri.ADMIN -> "onay-gecmisi"
        else -> "satinalma-onay-gecmisi"
    }

    fun menuleriDuzenle(items: List<MenuItem>, role: String): List<MenuItem> {
        if (!AKTIF) return items

        val filtered = items.filter { it.route !in HARIC_ROUTES }.toMutableList()
        if (filtered.none { it.route == "satinalma-onay-gecmisi" || it.route == "onay-gecmisi" }) {
            val gecmis = when (KullaniciRolleri.normalize(role)) {
                KullaniciRolleri.YONETIM ->
                    MenuItem("Geçmiş Onaylananlar", "onay-gecmisi", "Talep")
                else ->
                    MenuItem("Geçmiş Onaylananlar", "satinalma-onay-gecmisi", "Talep")
            }
            val redIdx = filtered.indexOfFirst { it.route == "red-talepler" }
            if (redIdx >= 0) filtered.add(redIdx, gecmis) else filtered.add(gecmis)
        }
        return filtered
    }

    fun bildirimRouteDonustur(route: String, role: String?): String {
        if (!AKTIF) return route
        val base = route.substringBefore('?')
        if (base !in HARIC_ROUTES) return route
        val query = route.substringAfter('?', "")
        val suffix = if (query.isNotEmpty() && query != route) "?$query" else ""
        return onaySonrasiRoute(role) + suffix
    }

    fun aksiyonKuyrugu(route: String): Boolean {
        if (!AKTIF) return true
        val r = route.substringBefore('?')
        return r !in HARIC_ROUTES
    }
}
