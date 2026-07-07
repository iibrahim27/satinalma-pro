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
            r.equals(YONETIM, true) || r.equals("Yönetici", true) || r.equals("Yonetici", true) -> YONETIM
            r.equals(SATINALMA, true) -> SATINALMA
            r.equals(SEF, true) -> SEF
            r.equals(SAHA, true) -> SAHA
            r.equals(ATOLYE, true) -> ATOLYE
            r.equals(DEPO, true) -> DEPO
            else -> r
        }
    }

    val TUM = listOf(ADMIN, YONETIM, SATINALMA, SEF, SAHA, ATOLYE, DEPO)

    fun isAdmin(role: String?) = normalize(role) == ADMIN
    fun canCreateRequest(role: String?) = normalize(role) in setOf(ADMIN, SAHA, SEF, SATINALMA)
    fun canPlaceOrder(role: String?) = canMalKabul(role)
    fun canEnterQuotes(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA)
    fun canApproveQuotes(role: String?) = normalize(role) in setOf(ADMIN, YONETIM, SATINALMA)
    fun canManagementDecide(role: String?) = normalize(role) in setOf(ADMIN, YONETIM)
    fun canMalKabul(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA)
    fun isAtolyeOnly(role: String?) = normalize(role) == ATOLYE
    fun canStockWrite(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA, DEPO)
    fun canModulKayitView(role: String?) = normalize(role) in setOf(ADMIN, YONETIM, SEF, SAHA, SATINALMA)
    fun canModulKayitWrite(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA)
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
    private val teklifsizFirmaFiyat = MenuItem("Teklifsiz Firma/Fiyat", "teklifsiz-firma-fiyat", "Teklif")
    private val teklifOnay = MenuItem("Teklif Onay", "teklif-onay", "Teklif")
    private val onaylananTeklifler = MenuItem("Onaylanan Teklifler", "onaylanan-teklifler", "Teklif")
    private val onayGecmisi = MenuItem("Yönetim Onay Geçmişi", "onay-gecmisi", "Talep")
    private val onaylananMalzemeler = MenuItem("Sipariş & Mal Kabul", "onaylanan-malzemeler", "Malzeme")
    private val alinanMalzemeKayitlari = MenuItem("Alınan Malzemeler", "alinan-malzemeler", "Malzeme")
    private val agregaModul = MenuItem("Agrega", "agrega", "Malzeme")
    private val cimentoModul = MenuItem("Çimento", "cimento", "Malzeme")
    private val stokDurum = MenuItem("Stok Durumu", "stok-durum", "Stok")
    private val stokGiris = MenuItem("Stok Girişi", "stok-giris", "Stok")
    private val stokCikis = MenuItem("Stok Çıkışı", "stok-cikis", "Stok")
    private val stokHareket = MenuItem("Stok Hareketleri", "stok-hareket", "Stok")
    private val stokSayim = MenuItem("Stok Sayım", "stok-sayim", "Stok")
    private val gecmisTalepler = MenuItem("Geçmiş Talepler", "gecmis-talepler", "Talep")
    private val gecmisTeklifli = MenuItem("Geçmiş Teklifli Onaylar", "gecmis-teklifli-onaylar", "Talep")
    private val redTalepler = MenuItem("Red Talepler", "red-talepler", "Talep")
    private val yonetimTeklifGirilen = MenuItem("Teklif Girilen", "yonetim-teklif-girilen", "Talep")
    private val yonetimDirekOnaylanan = MenuItem("Direk Onaylanan", "yonetim-direk-onaylanan", "Talep")
    private val satinalmaTeklifIstenen = MenuItem("Teklif İstenen", "satinalma-teklif-istenen", "Teklif")
    private val satinalmaTeklifGirilen = MenuItem("Yönetime Gönderilen", "satinalma-teklif-girilen", "Teklif")
    private val satinalmaTeklifDuzeltme = MenuItem("Düzeltme Bekleyen", "satinalma-teklif-duzeltme", "Teklif")
    private val satinalmaOnaylanan = MenuItem("Onaylanan (Sipariş Bekleyen)", "satinalma-onaylanan", "Malzeme")
    private val satinalmaSiparis = MenuItem("Sipariş Verilen", "satinalma-siparis", "Malzeme")
    private val satinalmaMalKabul = MenuItem("Mal Kabul Tamamlanan", "satinalma-mal-kabul", "Malzeme")
    private val raporlar = MenuItem("Satınalma Özeti", "raporlar", "Satınalma")
    private val ayarlar = MenuItem("Ayarlar", "ayarlar", "Yönetim")

    private val modulKayitMenuleri = listOf(agregaModul, cimentoModul, alinanMalzemeKayitlari)

    fun menus(role: String?): List<MenuItem> {
        val normalized = KullaniciRolleri.normalize(role)
        val items = when (normalized) {
            KullaniciRolleri.ADMIN -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler, gelenTalepler, teklifBekleyen,
                yonetimTeklifGirilen, yonetimDirekOnaylanan, satinalmaTeklifIstenen, teklifGir, teklifKarsilastirma,
                teklifsizFirmaFiyat, satinalmaTeklifGirilen, satinalmaTeklifDuzeltme, teklifOnay, onaylananTeklifler, onayGecmisi,
                satinalmaOnaylanan, onaylananMalzemeler, satinalmaSiparis, satinalmaMalKabul,
                redTalepler, gecmisTalepler, gecmisTeklifli,
                stokDurum, stokHareket, stokGiris, stokCikis, stokSayim,
                raporlar, bildirimler, ayarlar
            ) + modulKayitMenuleri
            KullaniciRolleri.YONETIM -> listOf(
                gelenTalepler, teklifBekleyen, yonetimTeklifGirilen, yonetimDirekOnaylanan,
                onayGecmisi, onaylananTeklifler, gecmisTalepler, redTalepler, stokDurum, bildirimler
            ) + modulKayitMenuleri
            KullaniciRolleri.SATINALMA -> listOf(
                yeniTalep, taleplerim, gelenTalepler,
                satinalmaTeklifIstenen, satinalmaTeklifGirilen, satinalmaTeklifDuzeltme, teklifKarsilastirma, teklifsizFirmaFiyat,
                satinalmaOnaylanan, satinalmaSiparis, satinalmaMalKabul,
                stokDurum, stokHareket, stokGiris, stokCikis, stokSayim, raporlar, bildirimler
            ) + modulKayitMenuleri + listOf(onaylananMalzemeler)
            KullaniciRolleri.SEF -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler, onaylananMalzemeler,
                stokDurum, stokHareket, bildirimler
            ) + modulKayitMenuleri
            KullaniciRolleri.SAHA -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler, stokDurum, stokHareket, bildirimler
            ) + modulKayitMenuleri
            KullaniciRolleri.ATOLYE -> emptyList()
            KullaniciRolleri.DEPO -> listOf(stokDurum, stokGiris, stokCikis, stokHareket, stokSayim, bildirimler)
            else -> listOf(yeniTalep, taleplerim, bildirimler)
        }
        if (normalized == KullaniciRolleri.ATOLYE) {
            return emptyList()
        }
        return listOf(dashboard) + items + profil
    }

    fun accessibleRoutes(role: String?): Set<String> =
        if (KullaniciRolleri.isAtolyeOnly(role)) setOf("stok-durum", "bildirimler", "profil")
        else menus(role).map { it.route }.toSet()

    fun defaultRoute(role: String?): String =
        if (KullaniciRolleri.isAtolyeOnly(role)) "stok-durum" else "dashboard"

    private val talepDetayKaynakRotalar = setOf(
        "yeni-talep", "taleplerim", "onay-bekleyen", "onaylanan-talepler", "gelen-talepler",
        "teklif-bekleyen", "gecmis-talepler", "gecmis-teklifli-onaylar", "onay-gecmisi",
        "red-talepler", "onaylanan-teklifler", "teklif-onay", "yonetim-teklif-girilen",
        "yonetim-direk-onaylanan", "yonetim-gelen-talepler", "yonetim-teklif-bekleyen",
        "yonetim-onay-gecmisi", "yonetim-onaylanan-teklifler", "yonetim-gecmis", "yonetim-red-verilen",
        "teklif-gir", "satinalma-teklif-istenen", "satinalma-teklif-girilen", "satinalma-teklif-duzeltme",
        "teklif-karsilastirma", "satinalma-karsilastirma", "teklifsiz-firma-fiyat",
        "satinalma-onaylanan", "satinalma-siparis", "satinalma-mal-kabul", "onaylanan-malzemeler",
        "agrega", "cimento", "alinan-malzemeler"
    )

    fun menuBadgeCounts(role: String?, list: List<com.satinalmapro.android.core.model.TalepItem>, uid: String, ad: String): Map<String, Int> =
        menus(role)
            .mapNotNull { item ->
                val count = TalepKuyrugu.menuSayac(item.route, list, uid, ad, role)
                if (count > 0) item.route to count else null
            }
            .toMap()

    fun canAccess(role: String?, route: String): Boolean {
        val base = route.substringBefore('?')
        val menus = accessibleRoutes(role)
        if (menus.contains(base)) return true
        return when (base) {
            "talep-duzenle" -> menus.contains("yeni-talep") || menus.contains("taleplerim") ||
                KullaniciRolleri.isAdmin(role) ||
                KullaniciRolleri.normalize(role) == KullaniciRolleri.SATINALMA
            "talep-detay" -> menus.any { it in talepDetayKaynakRotalar }
            "teklif-gir" -> (menus.contains("teklif-gir") || menus.contains("satinalma-teklif-istenen")) &&
                KullaniciRolleri.canEnterQuotes(role)
            "teklif-karsilastirma", "satinalma-karsilastirma", "satinalma-teklif-duzeltme" ->
                KullaniciRolleri.canEnterQuotes(role) && (
                menus.contains("teklif-karsilastirma") ||
                menus.contains("satinalma-teklif-duzeltme") ||
                menus.contains("satinalma-teklif-istenen") ||
                menus.contains("satinalma-teklif-girilen"))
            "teklifsiz-firma-fiyat" ->
                KullaniciRolleri.canEnterQuotes(role) &&
                menus.contains("teklifsiz-firma-fiyat")
            "teklif-onay-detay" ->
                menus.contains("teklif-onay") ||
                menus.contains("yonetim-teklif-girilen") ||
                menus.contains("satinalma-teklif-girilen")
            "onaylanan-malzemeler" ->
                KullaniciRolleri.canMalKabul(role) ||
                KullaniciRolleri.canPlaceOrder(role) ||
                menus.contains("onaylanan-malzemeler") ||
                menus.contains("satinalma-siparis") ||
                menus.contains("satinalma-onaylanan")
            in setOf("agrega", "cimento", "alinan-malzemeler") ->
                KullaniciRolleri.canModulKayitView(role)
            else -> false
        }
    }
}

object BildirimRota {
    fun hedefRoute(type: String, requestId: String?, role: String?): String {
        val tip = normalizeTip(type)
        val r = KullaniciRolleri.normalize(role)
        if (requestId != null && tip == "Reddedildi") {
            return if (r == KullaniciRolleri.YONETIM) "red-talepler"
            else "talep-detay?id=$requestId"
        }
        return when (tip) {
            "YonetimeGonderildi" -> "gelen-talepler"
            "TeklifIstendi" -> when {
                r == KullaniciRolleri.YONETIM -> "teklif-bekleyen"
                requestId != null -> "teklif-gir?id=$requestId"
                else -> "satinalma-teklif-istenen"
            }
            "TeklifDuzeltmeIstendi" -> if (requestId != null) "satinalma-teklif-duzeltme?id=$requestId"
                else "satinalma-teklif-duzeltme"
            "TeklifOnayda" -> if (requestId != null) "teklif-onay-detay?id=$requestId"
                else if (r == KullaniciRolleri.YONETIM) "yonetim-teklif-girilen"
                else "satinalma-teklif-girilen"
            "Onaylandi" -> when {
                requestId == null && r == KullaniciRolleri.YONETIM -> "onay-gecmisi"
                requestId == null && r == KullaniciRolleri.SATINALMA -> "satinalma-onaylanan"
                requestId == null -> "bildirimler"
                r == KullaniciRolleri.SATINALMA -> "talep-detay?id=$requestId&view=onaylanan"
                r == KullaniciRolleri.YONETIM -> "talep-detay?id=$requestId"
                else -> "talep-detay?id=$requestId"
            }
            "SiparisOlusturuldu" -> when {
                requestId != null && r in setOf(KullaniciRolleri.SATINALMA, KullaniciRolleri.ADMIN) ->
                    "talep-detay?id=$requestId&view=siparis"
                requestId != null -> "talep-detay?id=$requestId&view=siparis"
                r in setOf(KullaniciRolleri.SATINALMA, KullaniciRolleri.ADMIN) -> "satinalma-siparis"
                else -> "onaylanan-malzemeler?section=siparis"
            }
            "MalKabulEdildi" -> when {
                r == KullaniciRolleri.DEPO -> "stok-durum"
                requestId != null && r in setOf(KullaniciRolleri.SATINALMA, KullaniciRolleri.ADMIN) ->
                    "talep-detay?id=$requestId&view=malkabul"
                requestId != null -> "talep-detay?id=$requestId&view=malkabul"
                r in setOf(KullaniciRolleri.SATINALMA, KullaniciRolleri.ADMIN) -> "satinalma-mal-kabul"
                else -> "onaylanan-malzemeler?section=malkabul"
            }
            "Reddedildi" -> if (r == KullaniciRolleri.YONETIM) "red-talepler"
            else if (requestId != null) "talep-detay?id=$requestId" else "bildirimler"
            else -> if (requestId != null) "talep-detay?id=$requestId" else "bildirimler"
        }
    }

    fun safeRoute(route: String?, role: String?): String {
        if (route.isNullOrBlank()) return RolNavigasyon.defaultRoute(role)
        val aliased = IsAkisRotalari.normalize(route, role)
        val base = aliased.substringBefore('?')
        if (RolNavigasyon.canAccess(role, base)) return aliased
        val requestId = aliased.substringAfter("id=", "").substringBefore('&').takeIf { it.isNotBlank() }
        if (requestId != null && RolNavigasyon.canAccess(role, "teklif-onay-detay"))
            return "teklif-onay-detay?id=$requestId"
        if (requestId != null && RolNavigasyon.canAccess(role, "talep-detay"))
            return aliased
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

    fun filtrele(source: Collection<String>, query: String?, bosSorgudaGoster: Boolean = false): List<String> {
        if (query.isNullOrBlank()) {
            if (!bosSorgudaGoster) return emptyList()
            return source.asSequence()
                .map { it.trim() }
                .filter { it.isNotBlank() }
                .distinctBy { it.lowercase() }
                .sortedBy { it.lowercase() }
                .take(MAX)
                .toList()
        }
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
