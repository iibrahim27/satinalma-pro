package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepQueue

/** Masaüstü SatinalmaPart1DurumEtiketi ile uyumlu görünen durum metinleri. */
object TalepDurumEtiketi {
    fun talepDurumu(talep: TalepItem): String {
        if (talep.durum == TalepDurumlari.REDDEDILDI) return "Red verildi"
        if (talep.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) &&
            (talep.teklifsizYonetimOnayi || talep.yonetimOnayKilitli || talep.herhangiKalemOnayli)
        ) return "Onaylandı"
        if (talep.durum == TalepDurumlari.TEKLIF_GIRISI && !gercekTeklifVar(talep))
            return "Satınalmadan teklif bekleniyor"
        if (talep.durum in setOf(
                TalepDurumlari.IMZA,
                TalepDurumlari.HAZIRLANIYOR,
                TalepDurumlari.YONETIM_ONAY,
                TalepDurumlari.TASLAK
            )
        ) return "Yönetim onayı bekliyor"
        if (talep.durum == TalepDurumlari.KARSILASTIRMA)
            return "Satınalmadan teklif bekleniyor"
        return "Yönetim onayı bekliyor"
    }

    fun teklifDurumu(talep: TalepItem): String {
        if (talep.durum == TalepDurumlari.SIPARIS) {
            val tamam = talep.kalemler.any { it.kabulEdilenMiktar > 0 } &&
                talep.kalemler.all { it.siparisTamamlandi || it.kabulEdilenMiktar >= it.miktar - 0.0001 }
            return if (tamam) "Mal kabul yapıldı" else "Siparişte"
        }
        if (!gercekTeklifVar(talep)) return "—"
        if (talep.durum == TalepDurumlari.REDDEDILDI) return "Red edildi"
        if (talep.teklifDuzeltmeNotu.isNotBlank() && talep.durum == TalepDurumlari.KARSILASTIRMA)
            return "Yeniden teklif istendi"
        if (TalepKuyrugu.teklifYonetimOnayiBekliyor(talep))
            return "Yönetim teklif değerlendirmede"
        if (talep.durum == TalepDurumlari.KARSILASTIRMA)
            return "Karşılaştırma inceleniyor"
        if (talep.herhangiKalemOnayli || talep.onaylananTeklifId != null)
            return "Teklif onaylandı"
        if (talep.durum == TalepDurumlari.ONAYLANDI) return "Onaylandı"
        return "Karşılaştırma inceleniyor"
    }

    fun onayTipi(talep: TalepItem): String = when {
        talep.durum == TalepDurumlari.REDDEDILDI -> "Reddedildi"
        talep.teklifsizYonetimOnayi && !talep.herhangiKalemOnayli ->
            if (talep.talepTuru == TalepTurleri.ACIL) "Acil teklifsiz onay" else "Teklifsiz yönetim onayı"
        talep.herhangiKalemOnayli -> "Teklifli onay"
        TalepKuyrugu.teklifYonetimOnayiBekliyor(talep) -> "Teklif onay bekliyor"
        talep.teklifDuzeltmeNotu.isNotBlank() -> "Düzeltme istendi"
        else -> talepDurumu(talep)
    }

    fun listeAltMetin(talep: TalepItem, queue: TalepQueue): String = when (queue) {
        TalepQueue.GECMIS_TALEPLER,
        TalepQueue.GECMIS_TEKLIFLI,
        TalepQueue.ONAY_GECMISI,
        TalepQueue.YONETIM_DIREK_ONAYLANAN,
        TalepQueue.RED_TALEPLER -> ozetSatir(talep)
        TalepQueue.TEKLIF_ONAY,
        TalepQueue.SATINALMA_TEKLIF_GIRILEN -> teklifDurumu(talep)
        TalepQueue.SATINALMA_TEKLIF_DUZELTME -> {
            val adet = talep.teklifler.count { it.firmaAdi.isNotBlank() }
            val not = talep.teklifDuzeltmeNotu.takeIf { it.isNotBlank() } ?: "Düzeltme bekleniyor"
            if (adet > 0) "$adet teklif · $not" else not
        }
        else -> "${talep.talepEden} · ${talep.tarih}"
    }

    private fun ozetSatir(talep: TalepItem): String {
        val islem = when {
            talep.durum == TalepDurumlari.REDDEDILDI -> "Reddedildi"
            talep.teklifDuzeltmeNotu.isNotBlank() && talep.durum == TalepDurumlari.KARSILASTIRMA ->
                "Satınalmaya geri gönderildi"
            talep.herhangiKalemOnayli -> "Teklif onaylandı"
            talep.teklifsizYonetimOnayi && talep.yonetimOnayKilitli -> "Teklifsiz onaylandı"
            else -> teklifDurumu(talep).takeIf { it != "—" } ?: talepDurumu(talep)
        }
        val onaylayan = talep.yonetimOnaylayanAd.takeIf { it.isNotBlank() }
        val tarih = talep.yonetimOnayTarihi.takeIf { it.isNotBlank() }
        return buildString {
            append(islem)
            if (onaylayan != null) append(" · $onaylayan")
            if (tarih != null) append(" · $tarih")
        }
    }

    fun islemSatirlari(talep: TalepItem): List<Pair<String, String>> {
        val satirlar = mutableListOf<Pair<String, String>>()
        satirlar += "Talep" to "${talep.talepNo} · ${talep.talepEden} · ${talep.tarih}"
        satirlar += "Durum" to talepDurumu(talep)
        if (gercekTeklifVar(talep)) {
            satirlar += "Teklif süreci" to teklifDurumu(talep)
        }
        if (talep.teklifDuzeltmeNotu.isNotBlank()) {
            satirlar += "Düzeltme notu" to talep.teklifDuzeltmeNotu
        }
        if (talep.yonetimOnaylayanAd.isNotBlank() || talep.yonetimOnayTarihi.isNotBlank()) {
            val onay = buildString {
                if (talep.yonetimOnaylayanAd.isNotBlank()) append(talep.yonetimOnaylayanAd)
                if (talep.yonetimOnayTarihi.isNotBlank()) {
                    if (isNotEmpty()) append(" · ")
                    append(talep.yonetimOnayTarihi)
                }
            }
            satirlar += "Onaylayan" to onay.ifBlank { "—" }
        }
        if (talep.redGerekcesi.isNotBlank()) {
            satirlar += "Red gerekçesi" to talep.redGerekcesi
        }
        if (talep.siparisNo.isNotBlank()) {
            satirlar += "Sipariş no" to talep.siparisNo
        }
        satirlar += "Onay tipi" to onayTipi(talep)
        return satirlar
    }

    private fun gercekTeklifVar(talep: TalepItem): Boolean =
        talep.teklifler.any { teklif ->
            teklif.firmaAdi.isNotBlank() || teklif.fiyatlar.any { it.birimFiyat > 0 }
        }
}
