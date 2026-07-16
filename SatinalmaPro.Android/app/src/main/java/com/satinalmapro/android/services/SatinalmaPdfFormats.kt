package com.satinalmapro.android.services

import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TeklifFiyat
import com.satinalmapro.android.core.model.TeklifItem
import java.text.DecimalFormat
import java.text.DecimalFormatSymbols
import java.util.Locale
import kotlin.math.round

object SatinalmaPdfFormats {
    private val tr = Locale("tr", "TR")
    private val sayiSembolleri = DecimalFormatSymbols(tr)

    fun sayi(tutar: Double): String =
        DecimalFormat("#,##0.00", sayiSembolleri).format(tutar)

    fun tl(tutar: Double): String = "${sayi(tutar)} ₺"

    fun miktar(deger: Double): String = sayi(deger)

    fun birimFiyatGosterim(
        birimFiyat: Double,
        paraBirimi: String?,
        usdKuru: Double,
        eurKuru: Double
    ): String {
        val pb = (paraBirimi?.trim()?.uppercase(Locale.ROOT)).orEmpty().ifBlank { "TRY" }
        val sembol = when (pb) {
            "USD" -> "$"
            "EUR" -> "€"
            else -> "₺"
        }
        if (pb == "TRY" || birimFiyat == 0.0) return "${sayi(birimFiyat)} $sembol"
        val tlDeger = tlCevir(birimFiyat, pb, usdKuru, eurKuru)
        return "${sayi(birimFiyat)} $sembol (${sayi(tlDeger)} ₺)"
    }

    fun tlCevir(birimFiyat: Double, paraBirimi: String?, usdKuru: Double, eurKuru: Double): Double {
        val pb = (paraBirimi?.trim()?.uppercase(Locale.ROOT)).orEmpty().ifBlank { "TRY" }
        return when (pb) {
            "USD" -> if (usdKuru > 0) round(birimFiyat * usdKuru * 10000.0) / 10000.0 else birimFiyat
            "EUR" -> if (eurKuru > 0) round(birimFiyat * eurKuru * 10000.0) / 10000.0 else birimFiyat
            else -> birimFiyat
        }
    }

    fun markaSutunuGoster(teklifler: List<TeklifItem>): Boolean {
        teklifler.forEach { teklif ->
            if (teklif.marka.isNotBlank()) return true
            teklif.fiyatlar.forEach { fiyat ->
                if (fiyat.marka.isNotBlank()) return true
            }
        }
        return false
    }

    fun teklifFiyati(talep: TalepItem, teklif: TeklifItem, kalemId: String): TeklifFiyat? {
        val kalem = talep.kalemler.firstOrNull { it.id == kalemId } ?: return null
        val fiyat = teklif.fiyatlar.firstOrNull { it.kalemId == kalemId } ?: return null
        if (fiyat.birimFiyat > 0 && fiyat.toplamTutar <= 0) {
            val toplam = fiyat.birimFiyat * kalem.miktar
            val kdvOrani = if (fiyat.kdvOrani > 0) fiyat.kdvOrani else if (teklif.kdvOrani > 0) teklif.kdvOrani else 20.0
            val kdv = toplam * kdvOrani / 100.0
            return fiyat.copy(toplamTutar = toplam, kdvTutari = kdv, toplamKdvDahil = toplam + kdv)
        }
        return fiyat
    }

    fun teklifVerisiniHazirla(talep: TalepItem) {
        // Veri Firebase'den geldiğinde toplamlar zaten hesaplıdır; eksikse teklifFiyati ile okunur.
    }
}
