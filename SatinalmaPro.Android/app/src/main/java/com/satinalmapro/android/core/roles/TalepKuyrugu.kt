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
    fun kayitli(t: TalepItem): Boolean =
        t.durum != TalepDurumlari.TASLAK || t.kalemler.any { it.malzeme.isNotBlank() } || t.talepAciklamasi.isNotBlank()

    fun onayBekleyen(t: TalepItem): Boolean =
        t.durum in setOf(TalepDurumlari.HAZIRLANIYOR, TalepDurumlari.IMZA, TalepDurumlari.YONETIM_ONAY)

    fun reddedildi(t: TalepItem): Boolean = t.durum == TalepDurumlari.REDDEDILDI

    fun onaylanmis(t: TalepItem): Boolean =
        t.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) &&
            (t.herhangiKalemOnayli || t.teklifsizYonetimOnayi || t.yonetimOnayKilitli)

    fun teklifsizFirmaFiyatBekliyor(t: TalepItem): Boolean =
        t.teklifsizYonetimOnayi && !t.herhangiKalemOnayli

    private fun teklifsizYonetim(t: TalepItem): Boolean = t.teklifler.isEmpty()

    fun yonetimTalepler(t: TalepItem): Boolean =
        teklifsizYonetim(t) && t.durum in setOf(TalepDurumlari.IMZA, TalepDurumlari.YONETIM_ONAY)

    fun yonetimTeklifBekleyen(t: TalepItem): Boolean =
        t.durum == TalepDurumlari.TEKLIF_GIRISI && t.teklifler.isEmpty()

    fun teklifGirisi(t: TalepItem): Boolean =
        (t.durum == TalepDurumlari.TEKLIF_GIRISI && t.teklifler.isEmpty() && !t.yonetimOnayKilitli) ||
            (t.durum == TalepDurumlari.IMZA && t.teklifler.isEmpty() && !t.yonetimOnayKilitli && t.talepTuru != "Acil")

    fun karsilastirma(t: TalepItem): Boolean =
        (t.durum == TalepDurumlari.KARSILASTIRMA ||
            (t.durum == TalepDurumlari.TEKLIF_GIRISI && t.teklifler.isNotEmpty())) &&
            !t.yonetimOnayKilitli

    fun yonetimOnayinda(t: TalepItem): Boolean = t.durum == TalepDurumlari.YONETIM_ONAY

    fun onaylananMalzeme(t: TalepItem): Boolean = t.herhangiKalemOnayli

    fun gecmisTalep(t: TalepItem): Boolean =
        t.teklifsizYonetimOnayi && t.yonetimOnayKilitli &&
            t.durum !in setOf(TalepDurumlari.REDDEDILDI, TalepDurumlari.IMZA, TalepDurumlari.TEKLIF_GIRISI, TalepDurumlari.KARSILASTIRMA)

    fun gecmisTeklifli(t: TalepItem): Boolean =
        !t.teklifsizYonetimOnayi && t.herhangiKalemOnayli &&
            t.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) &&
            !yonetimOnayinda(t)

    fun onaylananTeklif(t: TalepItem): Boolean =
        !t.teklifsizYonetimOnayi &&
            t.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) &&
            t.herhangiKalemOnayli

    fun onayGecmisi(t: TalepItem): Boolean = gecmisTalep(t) || gecmisTeklifli(t)

    fun taleplerim(t: TalepItem): Boolean = kayitli(t)

    fun filtre(queue: com.satinalmapro.android.core.model.TalepQueue, list: List<TalepItem>, uid: String, ad: String, rol: String?): List<TalepItem> {
        val normalized = KullaniciRolleri.normalize(rol)
        return when (queue) {
            com.satinalmapro.android.core.model.TalepQueue.TALEPLERIM ->
                list.filter { taleplerim(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.ONAY_BEKLEYEN ->
                list.filter { onayBekleyen(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.ONAYLANAN_TALEPLER ->
                list.filter { onaylanmis(it) && !onaylananMalzeme(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.GELEN_TALEPLER ->
                list.filter { yonetimTalepler(it) || yonetimTeklifBekleyen(it) || karsilastirma(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIF_BEKLEYEN ->
                list.filter { yonetimTeklifBekleyen(it) || teklifGirisi(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.GECMIS_TALEPLER ->
                list.filter { gecmisTalep(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.GECMIS_TEKLIFLI ->
                list.filter { gecmisTeklifli(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.ONAY_GECMISI ->
                list.filter { onayGecmisi(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.RED_TALEPLER ->
                list.filter { reddedildi(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIF_GIR ->
                list.filter { teklifGirisi(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIF_KARSILASTIRMA ->
                list.filter { karsilastirma(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIF_ONAY ->
                list.filter { yonetimOnayinda(it) || karsilastirma(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.ONAYLANAN_TEKLIFLER ->
                list.filter { onaylananTeklif(it) }.sortedByDescending { it.guncellemeUtc }
            com.satinalmapro.android.core.model.TalepQueue.TEKLIFSIZ_FIRMA_FIYAT ->
                list.filter { teklifsizFirmaFiyatBekliyor(it) }.sortedByDescending { it.guncellemeUtc }
        }
    }
}
