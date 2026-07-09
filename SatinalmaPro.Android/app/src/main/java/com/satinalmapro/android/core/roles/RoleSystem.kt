package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.MenuItem
import com.satinalmapro.android.core.roles.OnaylananMalzemeOlusturucu
import com.satinalmapro.android.core.roles.TalepKuyrugu

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
    fun isDepoOnly(role: String?) = normalize(role) == DEPO
    /** Stok durumu / hareketleri görüntüleme. */
    fun canStockView(role: String?) = normalize(role) in setOf(
        ADMIN, YONETIM, SATINALMA, SEF, SAHA, ATOLYE, DEPO
    )
    /** Stok giriş / çıkış / sayım yazma. */
    fun canStockWrite(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA, DEPO)
    fun canModulKayitView(role: String?) = normalize(role) in setOf(ADMIN, YONETIM, SEF, SAHA, SATINALMA)
    fun canModulKayitWrite(role: String?) = normalize(role) in setOf(ADMIN, SATINALMA)
}

/** Rol menüleri — Atölye/Depo satınalma dışı (stok); Şef/Saha talep + stok. */
object RolNavigasyon {
    private val dashboard = MenuItem("Özet", "dashboard", "Genel")
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
    private val onayGecmisi = MenuItem("Onay Geçmişi", "onay-gecmisi", "Talep")
    private val onaylananMalzemeler = MenuItem("Sipariş & Mal Kabul", "onaylanan-malzemeler", "Malzeme")
    private val gecmisTalepler = MenuItem("Geçmiş Talepler", "gecmis-talepler", "Talep")
    private val gecmisTeklifli = MenuItem("Geçmiş Teklifli", "gecmis-teklifli-onaylar", "Talep")
    private val redTalepler = MenuItem("Red Talepler", "red-talepler", "Talep")
    private val yonetimTeklifGirilen = MenuItem("Teklif Girilen", "yonetim-teklif-girilen", "Talep")
    private val yonetimDirekOnaylanan = MenuItem("Direk Onaylanan", "yonetim-direk-onaylanan", "Talep")
    private val satinalmaTeklifIstenen = MenuItem("Teklif İstenen", "satinalma-teklif-istenen", "Teklif")
    private val satinalmaTeklifGirilen = MenuItem("Yönetime Gönderilen", "satinalma-teklif-girilen", "Teklif")
    private val satinalmaTeklifDuzeltme = MenuItem("Düzeltme Bekleyen", "satinalma-teklif-duzeltme", "Teklif")
    private val satinalmaOnaylanan = MenuItem("Sipariş Bekleyen", "satinalma-onaylanan", "Malzeme")
    private val satinalmaSiparis = MenuItem("Sipariş Verilen", "satinalma-siparis", "Malzeme")
    private val satinalmaMalKabul = MenuItem("Mal Kabul", "satinalma-mal-kabul", "Malzeme")
    private val ayarlar = MenuItem("Ayarlar", "ayarlar", "Yönetim")
    private val stokDurum = MenuItem("Stok Durumu", "stok-durum", "Stok")
    private val stokHareket = MenuItem("Stok Hareketleri", "stok-hareket", "Stok")
    private val stokGiris = MenuItem("Stok Girişi", "stok-giris", "Stok")
    private val stokCikis = MenuItem("Stok Çıkışı", "stok-cikis", "Stok")

    fun menus(role: String?): List<MenuItem> {
        val normalized = KullaniciRolleri.normalize(role)
        val items = when (normalized) {
            KullaniciRolleri.ADMIN -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler, gelenTalepler, teklifBekleyen,
                yonetimTeklifGirilen, yonetimDirekOnaylanan, satinalmaTeklifIstenen, teklifGir, teklifKarsilastirma,
                teklifsizFirmaFiyat, satinalmaTeklifGirilen, satinalmaTeklifDuzeltme, teklifOnay, onaylananTeklifler, onayGecmisi,
                satinalmaOnaylanan, onaylananMalzemeler, satinalmaSiparis, satinalmaMalKabul,
                stokDurum, stokHareket, stokGiris, stokCikis,
                redTalepler, gecmisTalepler, gecmisTeklifli, bildirimler
            )
            KullaniciRolleri.YONETIM -> listOf(
                gelenTalepler, teklifBekleyen, yonetimTeklifGirilen, yonetimDirekOnaylanan,
                onayGecmisi, onaylananTeklifler, gecmisTalepler, redTalepler, stokDurum, bildirimler
            )
            KullaniciRolleri.SATINALMA -> listOf(
                yeniTalep, taleplerim, gelenTalepler,
                satinalmaTeklifIstenen, satinalmaTeklifGirilen, satinalmaTeklifDuzeltme, teklifKarsilastirma, teklifsizFirmaFiyat,
                satinalmaOnaylanan, satinalmaSiparis, satinalmaMalKabul, onaylananMalzemeler,
                stokDurum, stokHareket, stokGiris, stokCikis,
                bildirimler, ayarlar
            )
            KullaniciRolleri.SEF -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler,
                stokDurum, stokHareket, bildirimler
            )
            KullaniciRolleri.SAHA -> listOf(
                yeniTalep, taleplerim, onayBekleyen, onaylananTalepler,
                stokDurum, stokHareket, bildirimler
            )
            // Atölye: satınalma yok — yalnızca stok durumu
            KullaniciRolleri.ATOLYE -> listOf(stokDurum, bildirimler)
            // Depo: satınalma yok — stok giriş/çıkış/hareket/durum
            KullaniciRolleri.DEPO -> listOf(stokDurum, stokGiris, stokCikis, stokHareket, bildirimler)
            else -> listOf(yeniTalep, taleplerim, bildirimler)
        }
        return listOf(dashboard) + items + profil
    }

    fun queueMenus(role: String?): List<MenuItem> =
        menus(role).filter { it.route !in setOf("dashboard", "profil", "bildirimler", "ayarlar") }

    fun accessibleRoutes(role: String?): Set<String> =
        menus(role).map { it.route }.toSet()

    fun defaultRoute(role: String?): String =
        when (KullaniciRolleri.normalize(role)) {
            KullaniciRolleri.ATOLYE, KullaniciRolleri.DEPO -> "stok-durum"
            else -> "dashboard"
        }

    private val talepDetayKaynakRotalar = setOf(
        "yeni-talep", "taleplerim", "onay-bekleyen", "onaylanan-talepler", "gelen-talepler",
        "teklif-bekleyen", "gecmis-talepler", "gecmis-teklifli-onaylar", "onay-gecmisi",
        "red-talepler", "onaylanan-teklifler", "teklif-onay", "yonetim-teklif-girilen",
        "yonetim-direk-onaylanan", "teklif-gir", "satinalma-teklif-istenen", "satinalma-teklif-girilen",
        "satinalma-teklif-duzeltme", "teklif-karsilastirma", "satinalma-karsilastirma", "teklifsiz-firma-fiyat",
        "satinalma-onaylanan", "satinalma-siparis", "satinalma-mal-kabul", "onaylanan-malzemeler"
    )

    fun menuBadgeCounts(
        role: String?,
        list: List<com.satinalmapro.android.core.model.TalepItem>,
        uid: String,
        ad: String
    ): Map<String, Int> =
        queueItemCounts(role, list, uid, ad).filterKeys { isActionQueue(it) }

    /** Tüm kuyruklardaki kayıt sayısı (geçmiş dahil) — İşler listesi için. */
    fun queueItemCounts(
        role: String?,
        list: List<com.satinalmapro.android.core.model.TalepItem>,
        uid: String,
        ad: String
    ): Map<String, Int> {
        val malzemeler = runCatching { OnaylananMalzemeOlusturucu.olustur(list) }.getOrDefault(emptyList())
        return menus(role)
            .mapNotNull { item ->
                val count = TalepKuyrugu.menuSayac(item.route, list, uid, ad, role, malzemeler)
                if (count > 0) item.route to count else null
            }
            .toMap()
    }

    /**
     * Kullanıcı aksiyonu bekleyen kuyruklar — alt rozet / "bekliyor" metni yalnızca bunlar.
     * Onay geçmişi, onaylanan teklif, red, geçmiş vb. arşivdir.
     */
    fun isActionQueue(route: String): Boolean {
        val r = route.substringBefore('?')
        return when (r) {
            "gelen-talepler",
            "yonetim-gelen-talepler",
            "teklif-bekleyen",
            "yonetim-teklif-bekleyen",
            "yonetim-teklif-girilen",
            "teklif-onay",
            "onay-bekleyen",
            "satinalma-teklif-istenen",
            "satinalma-teklif-duzeltme",
            "teklif-duzeltme",
            "satinalma-teklif-girilen",
            "teklif-gir",
            "teklif-karsilastirma",
            "satinalma-karsilastirma",
            "teklifsiz-firma-fiyat",
            "satinalma-onaylanan",
            "satinalma-siparis",
            "satinalma-mal-kabul",
            "onaylanan-malzemeler" -> true
            else -> false
        }
    }

    fun canAccess(role: String?, route: String): Boolean {
        val base = route.substringBefore('?')
        val menus = accessibleRoutes(role)
        if (menus.contains(base)) return true
        return when (base) {
            "isler", "dashboard", "bildirimler", "profil" -> true
            "talep-duzenle" -> menus.contains("yeni-talep") || menus.contains("taleplerim") ||
                KullaniciRolleri.isAdmin(role) ||
                KullaniciRolleri.normalize(role) == KullaniciRolleri.SATINALMA
            // Bildirim derin linkleri Atölye/Depo dahil tüm roller için talep detayına inebilir.
            "talep-detay" -> true
            "teklif-gir" -> (menus.contains("teklif-gir") || menus.contains("satinalma-teklif-istenen")) &&
                KullaniciRolleri.canEnterQuotes(role)
            "teklif-karsilastirma", "satinalma-karsilastirma", "satinalma-teklif-duzeltme" ->
                KullaniciRolleri.canEnterQuotes(role) && (
                    menus.contains("teklif-karsilastirma") ||
                        menus.contains("satinalma-teklif-duzeltme") ||
                        menus.contains("satinalma-teklif-istenen") ||
                        menus.contains("satinalma-teklif-girilen")
                    )
            "teklifsiz-firma-fiyat" ->
                KullaniciRolleri.canEnterQuotes(role) && menus.contains("teklifsiz-firma-fiyat")
            "teklif-onay-detay" ->
                menus.contains("teklif-onay") ||
                    menus.contains("yonetim-teklif-girilen") ||
                    menus.contains("satinalma-teklif-girilen")
            "onaylanan-malzemeler" ->
                KullaniciRolleri.canMalKabul(role) ||
                    menus.contains("onaylanan-malzemeler") ||
                    menus.contains("satinalma-siparis") ||
                    menus.contains("satinalma-onaylanan")
            "stok-durum" -> KullaniciRolleri.canStockView(role) || menus.contains("stok-durum")
            "stok-hareket" -> menus.contains("stok-hareket") ||
                KullaniciRolleri.normalize(role) in setOf(
                    KullaniciRolleri.ADMIN, KullaniciRolleri.SATINALMA, KullaniciRolleri.DEPO,
                    KullaniciRolleri.SEF, KullaniciRolleri.SAHA
                )
            "stok-giris", "stok-cikis" ->
                KullaniciRolleri.canStockWrite(role) && (
                    menus.contains(base) ||
                        KullaniciRolleri.normalize(role) in setOf(
                            KullaniciRolleri.ADMIN, KullaniciRolleri.SATINALMA, KullaniciRolleri.DEPO
                        )
                    )
            else -> false
        }
    }
}

object BildirimRota {
    fun hedefRoute(type: String, requestId: String?, role: String?): String {
        val tip = normalizeTip(type)
        val r = KullaniciRolleri.normalize(role)
        val sahaLike = r in setOf(KullaniciRolleri.SAHA, KullaniciRolleri.SEF)
        val stokOnly = r in setOf(KullaniciRolleri.ATOLYE, KullaniciRolleri.DEPO)
        if (stokOnly) {
            return when {
                tip == "MalKabulEdildi" && r == KullaniciRolleri.DEPO -> "stok-giris"
                tip in setOf("MalKabulEdildi", "SiparisOlusturuldu", "Onaylandi") -> "stok-durum"
                else -> "stok-durum"
            }
        }
        if (requestId != null && tip == "Reddedildi") {
            return if (r == KullaniciRolleri.YONETIM) "red-talepler"
            else "talep-detay?id=$requestId"
        }
        return when (tip) {
            "YonetimeGonderildi" -> when {
                r == KullaniciRolleri.YONETIM || r == KullaniciRolleri.ADMIN || r == KullaniciRolleri.SATINALMA ->
                    "gelen-talepler"
                requestId != null -> "talep-detay?id=$requestId"
                sahaLike -> "onay-bekleyen"
                else -> "bildirimler"
            }
            "TeklifIstendi" -> when {
                r == KullaniciRolleri.YONETIM -> "teklif-bekleyen"
                r == KullaniciRolleri.SATINALMA || r == KullaniciRolleri.ADMIN ->
                    if (requestId != null) "teklif-gir?id=$requestId" else "satinalma-teklif-istenen"
                requestId != null -> "talep-detay?id=$requestId"
                sahaLike -> "onay-bekleyen"
                else -> "bildirimler"
            }
            "TeklifDuzeltmeIstendi" -> when {
                r == KullaniciRolleri.SATINALMA || r == KullaniciRolleri.ADMIN ->
                    if (requestId != null) "satinalma-teklif-duzeltme?id=$requestId"
                    else "satinalma-teklif-duzeltme"
                requestId != null -> "talep-detay?id=$requestId"
                else -> "bildirimler"
            }
            "TeklifOnayda" -> when {
                requestId != null && (r == KullaniciRolleri.YONETIM || r == KullaniciRolleri.SATINALMA || r == KullaniciRolleri.ADMIN) ->
                    "teklif-onay-detay?id=$requestId"
                r == KullaniciRolleri.YONETIM -> "yonetim-teklif-girilen"
                r == KullaniciRolleri.SATINALMA || r == KullaniciRolleri.ADMIN -> "satinalma-teklif-girilen"
                requestId != null -> "talep-detay?id=$requestId"
                else -> "bildirimler"
            }
            "Onaylandi" -> when {
                requestId == null && r == KullaniciRolleri.YONETIM -> "onay-gecmisi"
                requestId == null && r == KullaniciRolleri.SATINALMA -> "satinalma-onaylanan"
                requestId == null && sahaLike -> "onaylanan-talepler"
                requestId == null -> "bildirimler"
                r == KullaniciRolleri.SATINALMA -> "talep-detay?id=$requestId&view=onaylanan"
                else -> "talep-detay?id=$requestId"
            }
            "SiparisOlusturuldu" -> when {
                requestId != null -> "talep-detay?id=$requestId&view=siparis"
                r in setOf(KullaniciRolleri.SATINALMA, KullaniciRolleri.ADMIN) -> "satinalma-siparis"
                r == KullaniciRolleri.YONETIM -> "onay-gecmisi"
                sahaLike -> "onaylanan-talepler"
                else -> "bildirimler"
            }
            "MalKabulEdildi" -> when {
                r == KullaniciRolleri.DEPO -> "stok-durum"
                requestId != null -> "talep-detay?id=$requestId&view=malkabul"
                r in setOf(KullaniciRolleri.SATINALMA, KullaniciRolleri.ADMIN) -> "satinalma-mal-kabul"
                r == KullaniciRolleri.YONETIM -> "onay-gecmisi"
                sahaLike -> "onaylanan-talepler"
                else -> "bildirimler"
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
        // Bildirim derin linki erişilemezse dashboard yerine bildirim listesine düş.
        if (RolNavigasyon.canAccess(role, "bildirimler")) return "bildirimler"
        return RolNavigasyon.defaultRoute(role)
    }

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
