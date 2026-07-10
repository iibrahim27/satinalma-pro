package com.satinalmapro.android.core.roles

/** İşlem sonrası ve liste rotalarını role göre eşler (masaüstü menü yapısıyla uyumlu). */
object IsAkisRotalari {
    private val masaustuRotaTakmaAdlari = mapOf(
        "yonetim-gelen-talepler" to "gelen-talepler",
        "yonetim-teklif-bekleyen" to "teklif-bekleyen",
        "yonetim-teklif-girilen" to "yonetim-teklif-girilen",
        "yonetim-direk-onaylanan" to "yonetim-direk-onaylanan",
        "yonetim-onay-gecmisi" to "onay-gecmisi",
        "yonetim-gecmis" to "onay-gecmisi",
        "yonetim-onaylanan-teklifler" to "onaylanan-teklifler",
        "yonetim-red-verilen" to "red-talepler"
    )

    fun teklifOnayListesi(role: String?): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.YONETIM, KullaniciRolleri.SATINALMA -> "yonetim-teklif-girilen"
        else -> "teklif-onay"
    }

    fun teklifOnaySonrasi(role: String?, talepId: String): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.YONETIM -> "talep-detay?id=$talepId"
        KullaniciRolleri.SATINALMA -> "onaylanan-malzemeler"
        else -> "onaylanan-malzemeler"
    }

    fun teklifsizOnaySonrasi(role: String?, talepId: String): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.YONETIM -> "talep-detay?id=$talepId"
        else -> "teklifsiz-firma-fiyat?id=$talepId"
    }

    fun teklifIsteSonrasi(role: String?): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.YONETIM -> "teklif-bekleyen"
        else -> "gelen-talepler"
    }

    fun yonetimGonderSonrasi(role: String?): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.SATINALMA -> "yonetim-teklif-girilen"
        else -> teklifOnayListesi(role)
    }

    fun duzeltmeGonderSonrasi(role: String?): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.SATINALMA -> "satinalma-teklif-duzeltme"
        else -> teklifOnayListesi(role)
    }

    fun gecmisTeklifliListe(role: String?): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.YONETIM -> "onaylanan-teklifler"
        KullaniciRolleri.SATINALMA -> "satinalma-onaylanan"
        else -> "gecmis-teklifli-onaylar"
    }

    fun gecmisTalepListe(role: String?): String = when (KullaniciRolleri.normalize(role)) {
        KullaniciRolleri.YONETIM -> "onay-gecmisi"
        else -> "gecmis-talepler"
    }

    /** Erişilemeyen admin rotalarını role uygun menü rotasına çevirir. */
    fun normalize(route: String, role: String?): String {
        val base = route.substringBefore('?')
        val query = route.substringAfter('?', "")
        val suffix = if (query.isNotEmpty() && query != route) "?$query" else ""
        val r = KullaniciRolleri.normalize(role)
        val talepId = route.substringAfter("id=", "").substringBefore('&').takeIf { it.isNotBlank() }

        val aliasedBase = masaustuRotaTakmaAdlari[base] ?: base

        return when {
            aliasedBase == "teklif-onay" && r == KullaniciRolleri.YONETIM ->
                "yonetim-teklif-girilen$suffix"
            aliasedBase == "teklif-onay" && r == KullaniciRolleri.SATINALMA ->
                "yonetim-teklif-girilen$suffix"
            aliasedBase == "satinalma-teklif-girilen" && r == KullaniciRolleri.SATINALMA ->
                "yonetim-teklif-girilen$suffix"
            aliasedBase == "onaylanan-malzemeler" && r == KullaniciRolleri.YONETIM ->
                if (talepId != null) "talep-detay?id=$talepId" else "onaylanan-teklifler"
            aliasedBase == "teklifsiz-firma-fiyat" && r == KullaniciRolleri.YONETIM ->
                if (talepId != null) "talep-detay?id=$talepId" else "yonetim-direk-onaylanan"
            aliasedBase == "gecmis-teklifli-onaylar" && r == KullaniciRolleri.YONETIM ->
                "onaylanan-teklifler$suffix"
            aliasedBase != base -> "$aliasedBase$suffix"
            else -> route
        }
    }
}
