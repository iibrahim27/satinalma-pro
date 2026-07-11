package com.satinalmapro.android.services

import com.satinalmapro.android.core.model.AlinanMalzemeKaydi
import com.satinalmapro.android.core.model.TalepKalem
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale

/** Masaüstü KarsilastirmaAlimGecmisiYardimcisi ile aynı seçim kuralı. */
object KarsilastirmaAlimGecmisiYardimcisi {

    data class AlimSatiri(
        val kalemSiraNo: Int,
        val malzeme: String,
        val tarih: String = "",
        val miktar: Double = 0.0,
        val birim: String = "",
        val birimFiyati: Double = 0.0,
        val tedarikci: String = "",
        val kayitYok: Boolean = false,
        val sonIkiAlimYedegi: Boolean = false
    )

    fun malzemeBazliAlimlariTopla(
        kalemler: List<TalepKalem>,
        alinanMalzemeler: List<AlinanMalzemeKaydi>,
        referansTarih: Date = Date()
    ): List<AlimSatiri> {
        val cal = Calendar.getInstance().apply {
            time = referansTarih
            set(Calendar.HOUR_OF_DAY, 0)
            set(Calendar.MINUTE, 0)
            set(Calendar.SECOND, 0)
            set(Calendar.MILLISECOND, 0)
        }
        val referans = cal.time
        cal.add(Calendar.MONTH, -2)
        val esik = cal.time

        val sonuc = mutableListOf<AlimSatiri>()
        for (kalem in kalemler.sortedBy { it.siraNo }) {
            val malzemeAdi = kalem.malzeme.trim()
            if (malzemeAdi.isBlank()) continue

            val eslesen = alinanMalzemeler
                .filter { it.malzemeHizmet.trim().equals(malzemeAdi, ignoreCase = true) }
                .map { it to tarihCoz(it.tarih) }
                .sortedWith(compareByDescending<Pair<AlinanMalzemeKaydi, Date>> { it.second }
                    .thenByDescending { it.first.birimFiyati })

            val sonIkiAy = eslesen.filter { (_, t) -> !t.before(esik) && !t.after(referans) }
            if (sonIkiAy.isNotEmpty()) {
                sonIkiAy.forEach { (kayit, _) ->
                    sonuc += alimdanSatir(kalem.siraNo, malzemeAdi, kayit, sonIkiAlimYedegi = false)
                }
                continue
            }

            val yedek = eslesen.take(2)
            if (yedek.isEmpty()) {
                sonuc += AlimSatiri(kalemSiraNo = kalem.siraNo, malzeme = malzemeAdi, kayitYok = true)
                continue
            }
            yedek.forEach { (kayit, _) ->
                sonuc += alimdanSatir(kalem.siraNo, malzemeAdi, kayit, sonIkiAlimYedegi = true)
            }
        }
        return sonuc
    }

    private fun alimdanSatir(
        siraNo: Int,
        malzeme: String,
        kayit: AlinanMalzemeKaydi,
        sonIkiAlimYedegi: Boolean
    ) = AlimSatiri(
        kalemSiraNo = siraNo,
        malzeme = malzeme,
        tarih = kayit.tarih.ifBlank { "—" },
        miktar = kayit.miktar,
        birim = kayit.birim.ifBlank { "—" },
        birimFiyati = kayit.birimFiyati,
        tedarikci = kayit.tedarikci.ifBlank { "—" },
        sonIkiAlimYedegi = sonIkiAlimYedegi
    )

    private fun tarihCoz(tarih: String): Date {
        if (tarih.isBlank()) return Date(0)
        val tr = Locale("tr", "TR")
        val formats = listOf(
            SimpleDateFormat("dd.MM.yyyy", Locale.US),
            SimpleDateFormat("dd.MM.yyyy", tr),
            SimpleDateFormat("yyyy-MM-dd", Locale.US)
        )
        for (f in formats) {
            f.isLenient = false
            runCatching { f.parse(tarih.trim()) }.getOrNull()?.let { return it }
        }
        return Date(0)
    }
}
