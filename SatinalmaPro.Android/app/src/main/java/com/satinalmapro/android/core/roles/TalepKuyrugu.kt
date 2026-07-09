package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.TalepItem

object TalepDurumlari {
    const val TASLAK = "Taslak"
    const val HAZIRLANIYOR = "Hazırlanıyor"
    const val IMZA = "İmza Sürecinde"
    const val YONETIM_ONAY = "Yönetim Onayında"
    const val TEKLIF_GIRISI = "Teklif Girişi"
    const val KARSILASTIRMA = "Karşılaştırma"
    const val ONAYLANDI = "Onaylandı"
    const val REDDEDILDI = "Reddedildi"
    const val SIPARIS = "Sipariş Oluşturuldu"
}

object TalepKuyrugu {
    @Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
    private fun tekliflerOf(t: TalepItem): List<com.satinalmapro.android.core.model.TeklifItem> =
        (t.teklifler as List<com.satinalmapro.android.core.model.TeklifItem>?) ?: emptyList()

    @Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
    private fun kalemlerOf(t: TalepItem): List<com.satinalmapro.android.core.model.TalepKalem> =
        (t.kalemler as List<com.satinalmapro.android.core.model.TalepKalem>?) ?: emptyList()

    @Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
    private fun fiyatlarOf(teklif: com.satinalmapro.android.core.model.TeklifItem): List<com.satinalmapro.android.core.model.TeklifFiyat> =
        (teklif.fiyatlar as List<com.satinalmapro.android.core.model.TeklifFiyat>?) ?: emptyList()

    fun kayitli(t: TalepItem): Boolean =
        t.durum != TalepDurumlari.TASLAK || kalemlerOf(t).any { it.malzeme.isNotBlank() } || t.talepAciklamasi.isNotBlank()

    fun onayBekleyen(t: TalepItem): Boolean =
        t.durum in setOf(TalepDurumlari.HAZIRLANIYOR, TalepDurumlari.IMZA, TalepDurumlari.YONETIM_ONAY)

    fun onayBekleyenListede(t: TalepItem, talepSahibiModu: Boolean): Boolean =
        onayBekleyen(t) && (talepSahibiModu || !satinalmaTeklifGirisiAktif(t))

    private fun talepSahibi(t: TalepItem, uid: String, ad: String): Boolean {
        if (uid.isNotBlank() && t.olusturanUid.equals(uid, ignoreCase = true)) return true
        if (ad.isNotBlank() && t.talepEden.equals(ad, ignoreCase = true)) return true
        return false
    }

    private fun sahaModu(rol: String?): Boolean {
        val normalized = KullaniciRolleri.normalize(rol)
        return !KullaniciRolleri.isAdmin(rol) &&
            normalized !in setOf(KullaniciRolleri.SATINALMA, KullaniciRolleri.YONETIM, KullaniciRolleri.ADMIN)
    }

    fun reddedildi(t: TalepItem): Boolean = t.durum == TalepDurumlari.REDDEDILDI

    fun onaylanmis(t: TalepItem): Boolean =
        t.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) &&
            (t.herhangiKalemOnayli || t.teklifsizYonetimOnayi || t.yonetimOnayKilitli)

    fun teklifsizFirmaFiyatBekliyor(t: TalepItem): Boolean =
        t.teklifsizYonetimOnayi && !t.herhangiKalemOnayli

    private fun gercekTeklifVar(t: TalepItem): Boolean =
        tekliflerOf(t).any { teklif ->
            teklif.firmaAdi.isNotBlank() || fiyatlarOf(teklif).any { it.birimFiyat > 0 }
        }

    private fun teklifsizYonetim(t: TalepItem): Boolean = !gercekTeklifVar(t)

    fun yonetimTalepler(t: TalepItem): Boolean =
        teklifsizYonetim(t) && t.durum in setOf(TalepDurumlari.IMZA, TalepDurumlari.YONETIM_ONAY)

    fun yonetimTeklifBekleyen(t: TalepItem): Boolean =
        t.durum == TalepDurumlari.TEKLIF_GIRISI && !gercekTeklifVar(t) && !t.yonetimOnayKilitli

    fun teklifGirisi(t: TalepItem): Boolean =
        yonetimTeklifBekleyen(t) ||
            (t.durum == TalepDurumlari.TEKLIF_GIRISI && tekliflerOf(t).isNotEmpty() && !t.yonetimOnayKilitli) ||
            (t.durum == TalepDurumlari.KARSILASTIRMA && !t.yonetimOnayKilitli) ||
            (t.durum == TalepDurumlari.IMZA && tekliflerOf(t).isEmpty() && !t.yonetimOnayKilitli && t.talepTuru != "Acil")

    fun teklifDuzeltmeBekliyor(t: TalepItem): Boolean =
        t.teklifDuzeltmeNotu.isNotBlank() &&
            t.durum == TalepDurumlari.KARSILASTIRMA &&
            gercekTeklifVar(t) &&
            !t.yonetimOnayKilitli

    fun karsilastirma(t: TalepItem): Boolean =
        ((t.durum == TalepDurumlari.KARSILASTIRMA ||
            (t.durum == TalepDurumlari.TEKLIF_GIRISI && tekliflerOf(t).isNotEmpty())) &&
            !t.yonetimOnayKilitli &&
            !teklifYonetimOnayiBekliyor(t)) &&
            !teklifDuzeltmeBekliyor(t)

    fun yonetimOnayinda(t: TalepItem): Boolean = t.durum == TalepDurumlari.YONETIM_ONAY

    fun teklifYonetimOnayiBekliyor(t: TalepItem): Boolean =
        t.durum == TalepDurumlari.YONETIM_ONAY &&
            tekliflerOf(t).isNotEmpty() &&
            !t.herhangiKalemOnayli &&
            !t.yonetimOnayKilitli

    /** Teklif onayı kanıtı — kalem, ana teklif veya teklif bayrağı. */
    fun teklifOnayKanitiVar(t: TalepItem): Boolean =
        t.herhangiKalemOnayli ||
            !t.onaylananTeklifId.isNullOrBlank() ||
            tekliflerOf(t).any { it.onaylandi }

    /** Yönetim teklif kararı bekliyor (masaüstü YonetimTeklifler). */
    fun yonetimTeklifKarariBekliyor(t: TalepItem): Boolean =
        teklifYonetimOnayiBekliyor(t) && !t.teklifsizYonetimOnayi

    fun teklifDuzenlemeDevamEdiyor(t: TalepItem): Boolean =
        !t.yonetimOnayKilitli &&
            !t.herhangiKalemOnayli &&
            tekliflerOf(t).isNotEmpty() &&
            t.durum in setOf(
                TalepDurumlari.KARSILASTIRMA,
                TalepDurumlari.TEKLIF_GIRISI,
                TalepDurumlari.IMZA,
                TalepDurumlari.HAZIRLANIYOR,
                TalepDurumlari.YONETIM_ONAY
            )

    fun satinalmaTeklifGirisiAktif(t: TalepItem): Boolean =
        teklifGirisi(t) || karsilastirma(t) || teklifDuzenlemeDevamEdiyor(t)

    fun onaylananMalzeme(t: TalepItem): Boolean = t.herhangiKalemOnayli

    fun gecmisTalep(t: TalepItem): Boolean =
        t.teklifsizYonetimOnayi && t.yonetimOnayKilitli &&
            t.durum !in setOf(TalepDurumlari.REDDEDILDI, TalepDurumlari.IMZA, TalepDurumlari.TEKLIF_GIRISI, TalepDurumlari.KARSILASTIRMA)

    fun gecmisTeklifli(t: TalepItem): Boolean =
        !t.teklifsizYonetimOnayi && teklifOnayKanitiVar(t) &&
            t.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) &&
            !yonetimOnayinda(t)

    fun onaylananTeklif(t: TalepItem): Boolean =
        !t.teklifsizYonetimOnayi &&
            t.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) &&
            teklifOnayKanitiVar(t)

    /** Tüm yönetim onayları — teklifsiz/teklifli, sipariş sonrası dahil (masaüstü YonetimOnayGecmisinde). */
    fun yonetimOnayGecmisinde(t: TalepItem): Boolean =
        !reddedildi(t) && (
            onaylanmis(t) || yonetimDirekOnaylanan(t) ||
                gecmisTalep(t) || gecmisTeklifli(t)
            )

    /** Yönetim teklifsiz / acil onaylanmış talepler. */
    fun yonetimDirekOnaylanan(t: TalepItem): Boolean =
        t.teklifsizYonetimOnayi &&
            (t.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) || teklifsizFirmaFiyatBekliyor(t))

    /** Onaylandı — sipariş henüz verilmedi. */
    fun satinalmaOnaylanan(t: TalepItem): Boolean =
        t.durum == TalepDurumlari.ONAYLANDI &&
            (t.herhangiKalemOnayli || t.teklifsizYonetimOnayi)

    fun malKabulTamam(t: TalepItem, malzemeler: List<com.satinalmapro.android.core.model.OnaylananMalzemeSatiri>): Boolean {
        val kalemler = malzemeler.filter { it.talepId == t.id }
        return kalemler.isNotEmpty() && kalemler.all { it.kalanMiktar <= 0.0001 }
    }

    /** Sipariş verilmiş talepler (mal kabul tamamlanmış olanlar dahil). */
    fun satinalmaSiparisVerilen(t: TalepItem): Boolean =
        t.durum == TalepDurumlari.SIPARIS &&
            (t.herhangiKalemOnayli || t.teklifsizYonetimOnayi)

    /** Sipariş verilmiş — mal kabul bekleyen (badge sayacı). */
    fun satinalmaSiparisBekleyen(t: TalepItem, malzemeler: List<com.satinalmapro.android.core.model.OnaylananMalzemeSatiri>): Boolean =
        satinalmaSiparisVerilen(t) && !malKabulTamam(t, malzemeler)

    /** Tüm kalemlerin mal kabulü tamamlandı. */
    fun satinalmaMalKabulEdilmis(t: TalepItem, malzemeler: List<com.satinalmapro.android.core.model.OnaylananMalzemeSatiri>): Boolean {
        if (t.durum != TalepDurumlari.SIPARIS) return false
        if (!(t.herhangiKalemOnayli || t.teklifsizYonetimOnayi)) return false
        return malKabulTamam(t, malzemeler)
    }

    /** Satınalma — teklif girilmesi istenen (yönetime gönderilmemiş). */
    fun satinalmaTeklifIstenen(t: TalepItem): Boolean =
        teklifGirisi(t) && !teklifYonetimOnayiBekliyor(t)

    /** Satınalma — yönetim onayı bekleyen (gönderilmiş teklifler). */
    fun satinalmaTeklifGirilen(t: TalepItem): Boolean = teklifYonetimOnayiBekliyor(t)

    fun onayGecmisi(t: TalepItem): Boolean = yonetimOnayGecmisinde(t)

    fun taleplerim(t: TalepItem): Boolean = kayitli(t)

    fun filtre(
        queue: com.satinalmapro.android.core.model.TalepQueue,
        list: List<TalepItem>,
        uid: String,
        ad: String,
        rol: String?,
        malzemelerOnbellek: List<com.satinalmapro.android.core.model.OnaylananMalzemeSatiri>? = null
    ): List<TalepItem> {
        val normalized = KullaniciRolleri.normalize(rol)
        val malzemeler = malzemelerOnbellek ?: OnaylananMalzemeOlusturucu.olustur(list)
        return when (queue) {
            com.satinalmapro.android.core.model.TalepQueue.TALEPLERIM ->
                list.filter { kayitli(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.ONAY_BEKLEYEN ->
                list.filter { onayBekleyenListede(it, talepSahibiModu = true) }
                    .sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.ONAYLANAN_TALEPLER ->
                list.filter { onaylanmis(it) }
                    .sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.GELEN_TALEPLER ->
                list.filter { yonetimTalepler(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIF_BEKLEYEN ->
                list.filter { yonetimTeklifBekleyen(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.GECMIS_TALEPLER ->
                list.filter { gecmisTalep(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.GECMIS_TEKLIFLI ->
                list.filter { gecmisTeklifli(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.ONAY_GECMISI ->
                list.filter { yonetimOnayGecmisinde(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.RED_TALEPLER ->
                list.filter { reddedildi(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIF_GIR ->
                list.filter { teklifGirisi(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIF_KARSILASTIRMA ->
                list.filter { karsilastirma(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIF_ONAY ->
                list.filter { yonetimTeklifKarariBekliyor(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.ONAYLANAN_TEKLIFLER ->
                list.filter { onaylananTeklif(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIFSIZ_FIRMA_FIYAT ->
                list.filter { teklifsizFirmaFiyatBekliyor(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.YONETIM_DIREK_ONAYLANAN ->
                list.filter { yonetimDirekOnaylanan(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.SATINALMA_TEKLIF_ISTENEN ->
                list.filter { satinalmaTeklifIstenen(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.SATINALMA_TEKLIF_GIRILEN ->
                list.filter { satinalmaTeklifGirilen(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.SATINALMA_TEKLIF_DUZELTME ->
                list.filter { teklifDuzeltmeBekliyor(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.SATINALMA_ONAYLANAN ->
                list.filter { satinalmaOnaylanan(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.SATINALMA_SIPARIS ->
                list.filter { satinalmaSiparisBekleyen(it, malzemeler) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.SATINALMA_MAL_KABUL ->
                list.filter { satinalmaMalKabulEdilmis(it, malzemeler) }.sortedByDescending { it.guncellemeUtc }
        }
    }

    fun menuSayac(
        route: String,
        list: List<TalepItem>,
        uid: String,
        ad: String,
        rol: String?,
        malzemelerOnbellek: List<com.satinalmapro.android.core.model.OnaylananMalzemeSatiri>? = null
    ): Int {
        return runCatching {
            val malzemeler = malzemelerOnbellek
                ?: OnaylananMalzemeOlusturucu.olustur(list)
            when (route) {
                "onaylanan-malzemeler" -> malzemeler.count {
                    OnaylananMalzemeOlusturucu.siparisVerBekleyen(it) || OnaylananMalzemeOlusturucu.malKabulBekleyen(it)
                }
                "satinalma-siparis" -> list.count { satinalmaSiparisBekleyen(it, malzemeler) }
                else -> routeToQueue(route)?.let { filtre(it, list, uid, ad, rol, malzemeler).size } ?: 0
            }
        }.getOrDefault(0)
    }

    private fun routeToQueue(route: String): com.satinalmapro.android.core.model.TalepQueue? = when (route) {
        "taleplerim" -> com.satinalmapro.android.core.model.TalepQueue.TALEPLERIM
        "onay-bekleyen" -> com.satinalmapro.android.core.model.TalepQueue.ONAY_BEKLEYEN
        "onaylanan-talepler" -> com.satinalmapro.android.core.model.TalepQueue.ONAYLANAN_TALEPLER
        "gelen-talepler" -> com.satinalmapro.android.core.model.TalepQueue.GELEN_TALEPLER
        "teklif-bekleyen" -> com.satinalmapro.android.core.model.TalepQueue.TEKLIF_BEKLEYEN
        "teklif-gir" -> com.satinalmapro.android.core.model.TalepQueue.TEKLIF_GIR
        "satinalma-teklif-istenen" -> com.satinalmapro.android.core.model.TalepQueue.SATINALMA_TEKLIF_ISTENEN
        "teklif-karsilastirma", "satinalma-karsilastirma" -> com.satinalmapro.android.core.model.TalepQueue.TEKLIF_KARSILASTIRMA
        "teklif-onay", "yonetim-teklif-girilen" -> com.satinalmapro.android.core.model.TalepQueue.TEKLIF_ONAY
        "satinalma-teklif-girilen" -> com.satinalmapro.android.core.model.TalepQueue.SATINALMA_TEKLIF_GIRILEN
        "satinalma-teklif-duzeltme", "teklif-duzeltme" -> com.satinalmapro.android.core.model.TalepQueue.SATINALMA_TEKLIF_DUZELTME
        "onaylanan-teklifler" -> com.satinalmapro.android.core.model.TalepQueue.ONAYLANAN_TEKLIFLER
        "satinalma-onaylanan" -> com.satinalmapro.android.core.model.TalepQueue.SATINALMA_ONAYLANAN
        "satinalma-siparis" -> com.satinalmapro.android.core.model.TalepQueue.SATINALMA_SIPARIS
        "satinalma-mal-kabul" -> com.satinalmapro.android.core.model.TalepQueue.SATINALMA_MAL_KABUL
        "red-talepler" -> com.satinalmapro.android.core.model.TalepQueue.RED_TALEPLER
        "gecmis-talepler" -> com.satinalmapro.android.core.model.TalepQueue.GECMIS_TALEPLER
        "gecmis-teklifli-onaylar" -> com.satinalmapro.android.core.model.TalepQueue.GECMIS_TEKLIFLI
        "yonetim-direk-onaylanan" -> com.satinalmapro.android.core.model.TalepQueue.YONETIM_DIREK_ONAYLANAN
        "onay-gecmisi", "yonetim-onay-gecmisi", "yonetim-gecmis" ->
            com.satinalmapro.android.core.model.TalepQueue.ONAY_GECMISI
        "yonetim-gelen-talepler" -> com.satinalmapro.android.core.model.TalepQueue.GELEN_TALEPLER
        "yonetim-teklif-bekleyen" -> com.satinalmapro.android.core.model.TalepQueue.TEKLIF_BEKLEYEN
        "yonetim-onaylanan-teklifler" -> com.satinalmapro.android.core.model.TalepQueue.GECMIS_TEKLIFLI
        "yonetim-red-verilen" -> com.satinalmapro.android.core.model.TalepQueue.RED_TALEPLER
        else -> null
    }
}
