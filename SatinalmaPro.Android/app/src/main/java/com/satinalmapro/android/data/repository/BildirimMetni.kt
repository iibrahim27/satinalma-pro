package com.satinalmapro.android.data.repository

import com.satinalmapro.android.core.model.BildirimTipleri
import com.satinalmapro.android.core.model.TalepItem

object BildirimMetni {
    fun olustur(
        tip: String,
        talep: TalepItem,
        firmaAdi: String? = null,
        ek: String? = null
    ): Pair<String, String> {
        val malzemeler = talep.kalemler.map { it.malzeme }
        return olustur(tip, talep.talepNo, talep.talepEden, talep.talepAciklamasi, malzemeler, firmaAdi, ek)
    }

    fun olustur(
        tip: String,
        talepNo: String,
        talepEden: String,
        aciklama: String?,
        malzemeler: List<String>,
        firmaAdi: String? = null,
        ek: String? = null
    ): Pair<String, String> {
        val satir = talepSatiri(talepNo, talepEden, aciklama, malzemeler)
        return when (tip) {
            BildirimTipleri.YONETIME_GONDERILDI -> "Yeni talep: $talepNo" to satir
            BildirimTipleri.TEKLIF_ISTENDI -> "Teklif istendi: $talepNo" to satir
            BildirimTipleri.TEKLIF_ONAYDA -> "Teklif onayda: $talepNo" to satir
            BildirimTipleri.TEKLIF_DUZELTME_ISTENDI -> "Teklif düzeltme: $talepNo" to
                if (ek.isNullOrBlank()) satir else talepSatiri(talepNo, talepEden, ek, emptyList())
            BildirimTipleri.REDDEDILDI -> "Talep reddedildi: $talepNo" to
                if (ek.isNullOrBlank()) satir else talepSatiri(talepNo, talepEden, ek, emptyList())
            BildirimTipleri.ONAYLANDI -> if (!firmaAdi.isNullOrBlank()) {
                "Firma onaylandı: $talepNo" to "Yönetim $firmaAdi firmasına onay verdi."
            } else {
                "Talep onaylandı: $talepNo" to satir
            }
            BildirimTipleri.SIPARIS_OLUSTURULDU -> "Sipariş verildi: $talepNo" to
                if (ek.isNullOrBlank()) satir else "$satir · $ek"
            BildirimTipleri.MAL_KABUL_EDILDI -> "Mal kabul: $talepNo" to
                if (ek.isNullOrBlank()) satir else "$satir · $ek"
            else -> "Talep: $talepNo" to satir
        }
    }

    private fun talepSatiri(talepNo: String, talepEden: String, aciklama: String?, malzemeler: List<String>): String {
        val ozet = ozet(aciklama, malzemeler)
        return buildList {
            if (talepNo.isNotBlank()) add(talepNo)
            if (talepEden.isNotBlank()) add(talepEden)
            if (ozet.isNotBlank()) add(ozet)
        }.joinToString(" · ").ifBlank { talepNo }
    }

    private fun ozet(aciklama: String?, malzemeler: List<String>, maxLen: Int = 100): String {
        if (!aciklama.isNullOrBlank()) return kisalt(aciklama.trim(), maxLen)
        val list = malzemeler.filter { it.isNotBlank() }.take(3)
        if (list.isNotEmpty()) return kisalt(list.joinToString(", "), maxLen)
        return ""
    }

    private fun kisalt(metin: String, maxLen: Int): String =
        if (metin.length <= maxLen) metin else metin.take(maxLen - 1).trimEnd() + "…"
}
