package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.MenuItem

object KullaniciRolleri {
    const val ADMIN = "Admin"
    const val YONETIM = "Yönetim"
    const val SATINALMA = "Satınalma"
    const val SEF = "Şef"
    const val SAHA = "Saha"
    const val ATOLYE = "Atölye"
    const val DEPO = "Depo"

    fun normalize(role: String?): String {
        if (role.isNullOrBlank()) return SAHA
        val r = role.trim()
        return when {
            r.equals("Okuma", true) -> SAHA
            r.equals("Şantiye", true) || r.equals("Santiye", true) -> SEF
            r.equals("Sef", true) -> SEF
            r.equals("Atolye", true) -> ATOLYE
            r.equals(ADMIN, true) -> ADMIN
            r.equals(YONETIM, true) -> YONETIM
            r.equals(SATINALMA, true) -> SATINALMA
            r.equals(SEF, true) -> SEF
            r.equals(SAHA, true) -> SAHA
            r.equals(ATOLYE, true) -> ATOLYE
            r.equals(DEPO, true) -> DEPO
            else -> r
        }
    }

    fun isAdmin(role: String?) = normalize(role) == ADMIN
    fun canCreateRequest(role: String?) = normalize(role) in setOf(ADMIN, YONETIM, SAHA, SEF, SATINALMA)
    fun canEnterQuotes(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA, YONETIM)
    fun canApproveQuotes(role: String?) = normalize(role) in setOf(ADMIN, YONETIM, SATINALMA)
    fun canManagementDecide(role: String?) = normalize(role) in setOf(ADMIN, YONETIM, SATINALMA)
    fun canMalKabul(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA)
    fun canStockWrite(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA, DEPO)
}

object RolNavigasyon {
    private val dashboard = MenuItem("Ana Sayfa", "dashboard", "Genel")
    private val profil = MenuItem("Profil", "profil")
    private val bildirimler = MenuItem("Bildirimler", "bildirimler", "Genel")
    private val yeniTalep = MenuItem("Yeni Talep", "yeni-talep", "Talep")
    private val taleplerim = MenuItem("Taleplerim", "taleplerim", "Talep")
    private val onayBekleyen = MenuItem("Onay Bekleyen", "onay-bekleyen", "Talep")
    private val onaylananTalepler = MenuItem("Onaylanan Talepler", "onaylanan-talepler", "Talep")
    private val gelenTalepler = MenuItem("Gelen Talepler", "gelen-talepler", "Talep")
    private val teklifBekleyen = MenuItem("Teklif Bekleyen", "teklif-bekleyen", "Talep")
    private val teklifGir = MenuItem("Teklif Girişi", "teklif-gir", "Teklif")
    private val teklifKarsilastirma = MenuItem("Karşılaştırma", "teklif-karsilastirma", "Teklif")
    private val teklifsizFirmaFiyat = MenuItem("Firma/Fiyat Girişi", "teklifsiz-firma-fiyat", "Teklif")
    private val teklifOnay = MenuItem("Teklif Onay", "teklif-onay", "Teklif")
    private val onaylananTeklifler = MenuItem("Onaylanan Teklifler", "onaylanan-teklifler", "Teklif")
    private val onayGecmisi = MenuItem("Onay Geçmişi", "onay-gecmisi", "Teklif")
    private val onaylananMalzemeler = MenuItem("Alınan Malzemeler", "onaylanan-malzemeler", "Malzeme")
    private val stokDurum = MenuItem("Stok Durumu", "stok-durum", "Stok")
    private val stokGiris = MenuItem("Stok Girişi", "stok-giris", "Stok")
    private val stokCikis = MenuItem("Stok Çıkışı", "stok-cikis", "Stok")
    private val stokHareket = MenuItem("Stok Hareketleri", "stok-hareket", "Stok")
    private val stokSayim = MenuItem("Stok Sayım", "stok-sayim", "Stok")
    private val gecmisTalepler = MenuItem("Geçmiş Talepler", "gecmis-talepler", "Talep")
    private val gecmisTeklifli = MenuItem("Geçmiş Teklifli Onaylar", "gecmis-teklifli-onaylar", "Talep")
    private val redTalepler = MenuItem("Red Talepler", "red-talepler", "Talep")

    fun menus(role: String?): List<MenuItem> {
        val normalized = KullaniciRolleri.normalize(role)
        val items = when (normalized) {
            KullaniciRolleri.ADMIN -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler, gelenTalepler, teklifBekleyen,
                teklifGir, teklifKarsilastirma, teklifsizFirmaFiyat, teklifOnay, onaylananTeklifler, onayGecmisi,
                redTalepler, onaylananMalzemeler, stokDurum, stokHareket, stokGiris, stokCikis, stokSayim, bildirimler
            )
            KullaniciRolleri.YONETIM -> listOf(
                gelenTalepler, teklifOnay, gecmisTalepler, gecmisTeklifli, redTalepler, stokDurum, bildirimler
            )
            KullaniciRolleri.SATINALMA -> listOf(
                yeniTalep, taleplerim, gelenTalepler, onayBekleyen, onaylananTalepler, redTalepler,
                teklifBekleyen, teklifGir, teklifKarsilastirma, teklifsizFirmaFiyat, teklifOnay,
                onaylananTeklifler, onayGecmisi, onaylananMalzemeler,
                stokDurum, stokHareket, stokGiris, stokCikis, stokSayim, bildirimler
            )
            KullaniciRolleri.SEF -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler, onaylananMalzemeler,
                stokDurum, stokHareket, bildirimler
            )
            KullaniciRolleri.SAHA -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler, stokDurum, stokHareket, bildirimler
            )
            KullaniciRolleri.ATOLYE -> listOf(stokDurum, bildirimler)
            KullaniciRolleri.DEPO -> listOf(stokDurum, stokGiris, stokCikis, stokHareket, bildirimler)
            else -> listOf(yeniTalep, taleplerim, bildirimler)
        }
        return listOf(dashboard) + items + profil
    }

    fun accessibleRoutes(role: String?): Set<String> = menus(role).map { it.route }.toSet()

    fun defaultRoute(role: String?): String = "dashboard"

    fun canAccess(role: String?, route: String): Boolean {
        val base = route.substringBefore('?')
        return accessibleRoutes(role).contains(base)
    }
}

object BildirimRota {
    fun hedefRoute(type: String, requestId: String?, role: String?): String {
        val tip = normalizeTip(type)
        val r = KullaniciRolleri.normalize(role)
        if (requestId != null && tip == "Reddedildi") {
            return if (r == KullaniciRolleri.YONETIM) "red-talepler" else "talep-detay?id=$requestId"
        }
        return when (tip) {
            "YonetimeGonderildi" -> "gelen-talepler"
            "TeklifIstendi" -> if (r == KullaniciRolleri.YONETIM) "gelen-talepler" else "teklif-gir"
            "TeklifDuzeltmeIstendi" -> "teklif-gir"
            "TeklifOnayda" -> if (requestId != null) "teklif-onay-detay?id=$requestId" else "teklif-onay"
            "Onaylandi" -> when {
                requestId == null && r == KullaniciRolleri.YONETIM -> "gecmis-talepler"
                requestId == null -> "bildirimler"
                r == KullaniciRolleri.SATINALMA -> "onaylanan-malzemeler"
                r == KullaniciRolleri.YONETIM -> "gecmis-talepler"
                else -> "talep-detay?id=$requestId"
            }
            "SiparisOlusturuldu" -> "onaylanan-malzemeler"
            "MalKabulEdildi" -> if (r == KullaniciRolleri.DEPO) "stok-durum" else "onaylanan-malzemeler"
            "Reddedildi" -> if (r == KullaniciRolleri.YONETIM) "red-talepler"
            else if (requestId != null) "talep-detay?id=$requestId" else "bildirimler"
            else -> if (requestId != null) "talep-detay?id=$requestId" else "bildirimler"
        }
    }

    fun safeRoute(route: String?, role: String?): String {
        if (route.isNullOrBlank()) return RolNavigasyon.defaultRoute(role)
        val base = route.substringBefore('?')
        if (RolNavigasyon.canAccess(role, base)) return route
        val requestId = route.substringAfter("id=", "").substringBefore('&').takeIf { it.isNotBlank() }
        if (requestId != null && RolNavigasyon.canAccess(role, "teklif-onay"))
            return "teklif-onay-detay?id=$requestId"
        return RolNavigasyon.defaultRoute(role)
    }

    /** Firestore/FCM tip kodlarını rota anahtarına çevirir. */
    fun normalizeTip(type: String): String = when (type.trim().lowercase()) {
        "yonetime_gonderildi" -> "YonetimeGonderildi"
        "teklif_istendi" -> "TeklifIstendi"
        "teklif_duzeltme_istendi" -> "TeklifDuzeltmeIstendi"
        "teklif_onayda" -> "TeklifOnayda"
        "onaylandi" -> "Onaylandi"
        "reddedildi" -> "Reddedildi"
        "siparis_olusturuldu" -> "SiparisOlusturuldu"
        "mal_kabul_edildi" -> "MalKabulEdildi"
        else -> type.trim()
    }
}

object MalzemeOneri {
    const val MAX = 12

    fun filtrele(source: Collection<String>, query: String?): List<String> {
        if (query.isNullOrBlank()) return emptyList()
        val q = query.trim()
        return source.asSequence()
            .map { it.trim() }
            .filter { it.isNotBlank() }
            .distinctBy { it.lowercase() }
            .filter { it.contains(q, ignoreCase = true) }
            .sortedWith(compareBy<String> { score(it, q) }.thenBy { it.lowercase() })
            .take(MAX)
            .toList()
    }

    private fun score(name: String, query: String): Int = when {
        name.equals(query, true) -> 0
        name.startsWith(query, true) -> 1
        else -> 2
    }
}
