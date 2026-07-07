package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TeklifFiyat
import com.satinalmapro.android.core.model.TeklifItem
import kotlin.math.abs

object MalKabulYardimcisi {

    fun kalemMiktariniGercekleseneGoreAyarla(talep: TalepItem, kalemId: String, yeniMiktar: Double): TalepItem {
        val kalem = talep.kalemler.firstOrNull { it.id.equals(kalemId, true) } ?: return talep
        if (abs(kalem.miktar - yeniMiktar) < 0.0001) return talep

        val kalemler = talep.kalemler.map { k ->
            if (k.id.equals(kalemId, true)) k.copy(miktar = yeniMiktar) else k
        }
        val guncelTalep = talep.copy(kalemler = kalemler)
        val teklifler = talep.teklifler.map { teklifFiyatlariHesapla(guncelTalep, it) }
        return guncelTalep.copy(teklifler = teklifler)
    }

    private fun teklifFiyatlariHesapla(talep: TalepItem, teklif: TeklifItem): TeklifItem {
        val fiyatlar = teklif.fiyatlar.map { fiyat ->
            val kalem = talep.kalemler.firstOrNull { it.id == fiyat.kalemId } ?: return@map fiyat
            hesaplaFiyat(fiyat, teklif, kalem.miktar)
        }
        return teklif.copy(fiyatlar = fiyatlar)
    }

    private fun hesaplaFiyat(fiyat: TeklifFiyat, teklif: TeklifItem, miktar: Double): TeklifFiyat {
        if (fiyat.birimFiyat <= 0) return fiyat
        val tlBirim = tlBirimFiyat(fiyat, teklif)
        val toplam = (tlBirim * miktar * 100).toLong() / 100.0
        val kdvOrani = when {
            fiyat.kdvOrani > 0 -> fiyat.kdvOrani
            teklif.kdvOrani > 0 -> teklif.kdvOrani
            else -> 20.0
        }
        val kdv = (toplam * kdvOrani / 100.0 * 100).toLong() / 100.0
        return fiyat.copy(toplamTutar = toplam, kdvTutari = kdv, toplamKdvDahil = toplam + kdv)
    }

    private fun tlBirimFiyat(fiyat: TeklifFiyat, teklif: TeklifItem): Double =
        when (fiyat.paraBirimi.uppercase()) {
            "USD" -> if (teklif.usdKuru > 0) fiyat.birimFiyat * teklif.usdKuru else fiyat.birimFiyat
            "EUR" -> if (teklif.eurKuru > 0) fiyat.birimFiyat * teklif.eurKuru else fiyat.birimFiyat
            else -> fiyat.birimFiyat
        }
}
