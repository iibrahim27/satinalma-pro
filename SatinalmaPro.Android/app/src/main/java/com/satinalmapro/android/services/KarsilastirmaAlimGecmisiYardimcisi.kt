package com.satinalmapro.android.services

import com.satinalmapro.android.core.model.AlinanMalzemeKaydi
import com.satinalmapro.android.core.model.TalepKalem
import com.satinalmapro.android.core.model.TeklifItem
import java.text.SimpleDateFormat
import java.util.Calendar
import java.util.Date
import java.util.Locale
import kotlin.math.round

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

    data class FiyatKarsilastirmaSatiri(
        val kalemSiraNo: Int,
        val malzeme: String,
        val birim: String = "",
        val sonAlinanMiktar: Double? = null,
        val sonAlinanBirim: String = "",
        val sonAlinanBirimFiyat: Double? = null,
        val sonAlinanTarih: String = "",
        val sonAlinanTedarikci: String = "",
        val enDusukTeklifBirimFiyat: Double? = null,
        val enDusukTeklifFirma: String = "",
        val farkTl: Double? = null,
        val artisYuzde: Double? = null,
        val sonAlimYok: Boolean = false,
        val teklifYok: Boolean = false
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

    fun malzemeBazliFiyatKarsilastirmasiTopla(
        kalemler: List<TalepKalem>,
        teklifler: List<TeklifItem>,
        alimSatirlari: List<AlimSatiri>
    ): List<FiyatKarsilastirmaSatiri> {
        val sonAlimlar = linkedMapOf<String, AlimSatiri>()
        for (satir in alimSatirlari) {
            if (satir.kayitYok || satir.malzeme.isBlank()) continue
            val anahtar = satir.malzeme.lowercase(Locale("tr", "TR"))
            if (!sonAlimlar.containsKey(anahtar)) sonAlimlar[anahtar] = satir
        }

        val sonuc = mutableListOf<FiyatKarsilastirmaSatiri>()
        for (kalem in kalemler.sortedBy { it.siraNo }) {
            val malzemeAdi = kalem.malzeme.trim()
            if (malzemeAdi.isBlank()) continue

            val sonAlim = sonAlimlar[malzemeAdi.lowercase(Locale("tr", "TR"))]
            val sonFiyat = sonAlim?.birimFiyati?.takeIf { it > 0 }
            val sonAlimYok = sonFiyat == null

            var enDusuk: Double? = null
            var enDusukFirma = ""
            for (teklif in teklifler) {
                val fiyat = teklif.fiyatlar.firstOrNull { it.kalemId == kalem.id } ?: continue
                if (fiyat.birimFiyat <= 0) continue
                val tl = SatinalmaPdfFormats.tlCevir(
                    fiyat.birimFiyat,
                    fiyat.paraBirimi,
                    teklif.usdKuru,
                    teklif.eurKuru
                )
                if (tl <= 0) continue
                if (enDusuk == null || tl < enDusuk) {
                    enDusuk = tl
                    enDusukFirma = teklif.firmaAdi.trim().ifBlank { "—" }
                }
            }

            val teklifYok = enDusuk == null
            val fark = if (sonFiyat != null && enDusuk != null) enDusuk - sonFiyat else null
            val yuzde = if (fark != null && sonFiyat != null && sonFiyat > 0)
                round(fark / sonFiyat * 10000.0) / 100.0
            else null

            sonuc += FiyatKarsilastirmaSatiri(
                kalemSiraNo = kalem.siraNo,
                malzeme = malzemeAdi,
                birim = kalem.birim.trim().ifBlank { "—" },
                sonAlinanMiktar = sonAlim?.miktar?.takeIf { it > 0 },
                sonAlinanBirim = sonAlim?.birim?.takeIf { it.isNotBlank() && it != "—" }
                    ?: kalem.birim.trim().ifBlank { "—" },
                sonAlinanBirimFiyat = sonFiyat,
                sonAlinanTarih = sonAlim?.tarih.orEmpty(),
                sonAlinanTedarikci = sonAlim?.tedarikci.orEmpty(),
                enDusukTeklifBirimFiyat = enDusuk,
                enDusukTeklifFirma = enDusukFirma,
                farkTl = fark,
                artisYuzde = yuzde,
                sonAlimYok = sonAlimYok,
                teklifYok = teklifYok
            )
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
