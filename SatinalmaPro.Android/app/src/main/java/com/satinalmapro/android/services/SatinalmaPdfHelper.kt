package com.satinalmapro.android.services

import android.content.Context
import android.content.Intent
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import android.os.Environment
import androidx.core.content.FileProvider
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TeklifItem
import java.io.File
import java.io.FileOutputStream

object SatinalmaPdfHelper {

    fun talepFormuPaylas(context: Context, talep: TalepItem, baglam: SatinalmaPdfBaglam = SatinalmaPdfBaglam()) {
        val dosya = talepFormuKaydet(context, talep, baglam)
        pdfPaylas(context, dosya, "Talep ${talep.talepNo}")
    }

    fun karsilastirmaPaylas(context: Context, talep: TalepItem, baglam: SatinalmaPdfBaglam = SatinalmaPdfBaglam()) {
        if (talep.teklifler.none { it.firmaAdi.isNotBlank() }) return
        val ad = "Karsilastirma_${talep.talepNo.replace('/', '-')}.pdf"
        val hedef = File(context.getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS), ad)
        hedef.parentFile?.mkdirs()
        karsilastirmaPdf(talep, baglam).writeTo(FileOutputStream(hedef))
        pdfPaylas(context, hedef, "Teklif Karşılaştırma ${talep.talepNo}")
    }

    fun tedarikciTeklifTalebiPaylas(context: Context, talep: TalepItem, baglam: SatinalmaPdfBaglam = SatinalmaPdfBaglam()) {
        val ad = "Tedarikci_Teklif_Talebi_${talep.talepNo.replace('/', '-')}.pdf"
        val hedef = File(context.getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS), ad)
        hedef.parentFile?.mkdirs()
        tedarikciTeklifTalebiPdf(talep, baglam).writeTo(FileOutputStream(hedef))
        pdfPaylas(context, hedef, "Tedarikçi Teklif Talebi ${talep.talepNo}")
    }

    fun yonetimOnayBelgesiPaylas(context: Context, talep: TalepItem, baglam: SatinalmaPdfBaglam = SatinalmaPdfBaglam()) {
        val ad = "Yonetim_Onay_${talep.talepNo.replace('/', '-')}.pdf"
        val hedef = File(context.getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS), ad)
        hedef.parentFile?.mkdirs()
        yonetimOnayPdf(talep, baglam).writeTo(FileOutputStream(hedef))
        pdfPaylas(context, hedef, "Yönetim Onay Belgesi ${talep.talepNo}")
    }

    fun siparisFormuPaylas(context: Context, talep: TalepItem, baglam: SatinalmaPdfBaglam = SatinalmaPdfBaglam()) {
        val ad = "Siparis_${talep.talepNo.replace('/', '-')}.pdf"
        val hedef = File(context.getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS), ad)
        hedef.parentFile?.mkdirs()
        siparisFormuPdf(talep, baglam).writeTo(FileOutputStream(hedef))
        pdfPaylas(context, hedef, "Sipariş Formu ${talep.talepNo}")
    }

    private fun pdfPaylas(context: Context, dosya: File, konu: String) {
        val uri = FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", dosya)
        val intent = Intent(Intent.ACTION_SEND).apply {
            type = "application/pdf"
            putExtra(Intent.EXTRA_STREAM, uri)
            putExtra(Intent.EXTRA_SUBJECT, konu)
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        context.startActivity(Intent.createChooser(intent, "PDF Paylaş"))
    }

    fun talepFormuKaydet(context: Context, talep: TalepItem, baglam: SatinalmaPdfBaglam = SatinalmaPdfBaglam()): File {
        val ad = "Talep_${talep.talepNo.replace('/', '-')}.pdf"
        val hedef = File(context.getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS), ad)
        hedef.parentFile?.mkdirs()
        talepFormuPdf(talep, baglam).writeTo(FileOutputStream(hedef))
        return hedef
    }

    private fun talepFormuPdf(talep: TalepItem, baglam: SatinalmaPdfBaglam): PdfDocument {
        val doc = PdfDocument()
        val duzen = PdfSayfaDuzeni(doc, 595f, 842f, 36f)
        duzen.baslat()

        SatinalmaPdfCizim.baslikCiz(duzen, baglam, "SATIN ALMA TALEP FORMU")

        val paint = Paint(Paint.ANTI_ALIAS_FLAG)
        paint.textSize = 10f
        paint.color = Color.BLACK

        duzen.alanGerekli(18f)
        paint.typeface = Typeface.DEFAULT
        duzen.canvas.drawText("Tarih: ${talep.tarih}", duzen.margin, duzen.y, paint)
        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        val talepNoMetni = "Talep No: ${talep.talepNo}"
        paint.textAlign = Paint.Align.RIGHT
        duzen.canvas.drawText(talepNoMetni, duzen.genislik - duzen.margin, duzen.y, paint)
        paint.textAlign = Paint.Align.LEFT
        duzen.y += 16f

        if (talep.talepEden.isNotBlank()) {
            SatinalmaPdfCizim.metinCiz(duzen, "Talep Eden: ${talep.talepEden}")
            duzen.y += 4f
        }

        if (talep.talepAciklamasi.isNotBlank()) {
            SatinalmaPdfCizim.metinCiz(duzen, "Talep Açıklaması", kalin = true, boyut = 11f)
            SatinalmaPdfCizim.cerceveliMetin(duzen, talep.talepAciklamasi)
        }

        duzen.y += 4f
        val kolonlarAciklama = listOf(24f, duzen.icerikGenisligi - 194f, 60f, 50f, duzen.icerikGenisligi - 134f)
        val basliklar = listOf("No", "Malzeme", "Miktar", "Birim", "Açıklama")
        val satirlar = talep.kalemler.sortedBy { it.siraNo }.map { k ->
            listOf(
                k.siraNo.toString(),
                k.malzeme,
                SatinalmaPdfFormats.miktar(k.miktar),
                k.birim,
                k.aciklama
            )
        }
        SatinalmaPdfCizim.tabloCiz(duzen, kolonlarAciklama, basliklar, satirlar)

        val sefler = baglam.sefImzalari.filter { it.aktif }
        val yonetim = baglam.yonetimImzalari.filter { it.aktif }
        if (sefler.isNotEmpty() || yonetim.isNotEmpty()) {
            duzen.y += 10f
            if (sefler.isNotEmpty()) {
                SatinalmaPdfCizim.imzaAlanlariCiz(duzen, sefler)
            }
            if (yonetim.isNotEmpty()) {
                duzen.y += 24f
                SatinalmaPdfCizim.yonetimImzaCiz(duzen, yonetim)
            }
        }

        duzen.bitir()
        return doc
    }

    private fun karsilastirmaPdf(talep: TalepItem, baglam: SatinalmaPdfBaglam): PdfDocument {
        SatinalmaPdfFormats.teklifVerisiniHazirla(talep)
        val teklifler = talep.teklifler.filter { it.firmaAdi.isNotBlank() }
        val kalemler = talep.kalemler.sortedBy { it.siraNo }
        val onerilen = talep.onerilenTeklif()
        val markaGoster = SatinalmaPdfFormats.markaSutunuGoster(teklifler)
        val grupSutun = if (markaGoster) 3 else 2

        val doc = PdfDocument()
        val duzen = PdfSayfaDuzeni(doc, 842f, 595f, 28f)
        duzen.baslat()

        SatinalmaPdfCizim.baslikCiz(duzen, baglam, "FİYAT KARŞILAŞTIRMA TABLOSU", kompakt = false)

        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            textSize = 9f
            color = Color.BLACK
        }
        duzen.alanGerekli(16f)
        duzen.canvas.drawText("Talep No: ${talep.talepNo}", duzen.margin, duzen.y, paint)
        paint.textAlign = Paint.Align.RIGHT
        duzen.canvas.drawText("Tarih: ${talep.tarih}", duzen.genislik - duzen.margin, duzen.y, paint)
        paint.textAlign = Paint.Align.LEFT
        duzen.y += 14f

        if (talep.talepEden.isNotBlank()) {
            SatinalmaPdfCizim.metinCiz(duzen, "Talep Eden: ${talep.talepEden}", boyut = 9f)
            duzen.y += 2f
        }

        if (onerilen != null) {
            val oneriMetni = buildString {
                append("Satınalma Önerisi: ${onerilen.firmaAdi}")
                append(" — KDV Hariç: ${SatinalmaPdfFormats.tl(onerilen.araToplam)}")
                append(" | KDV: ${SatinalmaPdfFormats.tl(onerilen.kdvTutari)}")
                append(" | KDV Dahil: ${SatinalmaPdfFormats.tl(onerilen.genelToplam)}")
            }
            SatinalmaPdfCizim.oneriKutusu(duzen, oneriMetni)
        }

        duzen.y += 4f
        karsilastirmaTablosuCiz(duzen, talep, kalemler, teklifler, onerilen, markaGoster, grupSutun)

        duzen.y += 8f
        SatinalmaPdfCizim.metinCiz(duzen, "Onaylanacak firmayı işaretleyiniz:", boyut = 9f)
        duzen.y += 4f
        firmaOnayTablosuCiz(duzen, teklifler, onerilen)

        duzen.y += 8f
        SatinalmaPdfCizim.metinCiz(duzen, "Onaylanan Firma:", kalin = true, boyut = 9f)
        duzen.y += 16f
        val linePaint = Paint().apply { color = Color.parseColor("#9E9E9E"); strokeWidth = 0.5f }
        duzen.canvas.drawLine(duzen.margin, duzen.y, duzen.margin + 200f, duzen.y, linePaint)
        duzen.y += 20f
        SatinalmaPdfCizim.yonetimImzaCiz(duzen, baglam.yonetimImzalari.filter { it.aktif })

        // Sayfa 2+: son alımlar (karşılaştırma referansı)
        karsilastirmaAlimGecmisiCiz(duzen, talep, kalemler, baglam)

        duzen.bitir()
        return doc
    }

    private fun karsilastirmaAlimGecmisiCiz(
        duzen: PdfSayfaDuzeni,
        talep: TalepItem,
        kalemler: List<com.satinalmapro.android.core.model.TalepKalem>,
        baglam: SatinalmaPdfBaglam
    ) {
        val satirlari = KarsilastirmaAlimGecmisiYardimcisi.malzemeBazliAlimlariTopla(
            kalemler,
            baglam.alinanMalzemeler
        )

        duzen.yeniSayfa()
        SatinalmaPdfCizim.baslikCiz(duzen, baglam, "SON ALIMLAR (KARŞILAŞTIRMA REFERANSI)", kompakt = false)

        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            textSize = 9f
            color = Color.BLACK
        }
        duzen.alanGerekli(16f)
        duzen.canvas.drawText("Talep No: ${talep.talepNo}", duzen.margin, duzen.y, paint)
        paint.textAlign = Paint.Align.RIGHT
        duzen.canvas.drawText("Tarih: ${talep.tarih}", duzen.genislik - duzen.margin, duzen.y, paint)
        paint.textAlign = Paint.Align.LEFT
        duzen.y += 14f

        SatinalmaPdfCizim.metinCiz(
            duzen,
            "Son 2 aydaki alımlar listelenir; kayıt yoksa en son 2 alım gösterilir.",
            boyut = 8f
        )
        duzen.y += 4f

        val kolonGenislikleri = listOf(28f, 180f, 68f, 60f, 48f, 84f, duzen.icerikGenisligi - 28f - 180f - 68f - 60f - 48f - 84f)
        val basliklar = listOf("No", "Malzeme", "Tarih", "Miktar", "Birim", "Birim Fiyat", "Tedarikçi")
        SatinalmaPdfCizim.tabloCiz(
            duzen,
            kolonGenislikleri,
            basliklar = basliklar,
            satirlar = emptyList(),
            baslikCiz = true
        )

        if (satirlari.isEmpty()) {
            SatinalmaPdfCizim.tabloSatirCiz(
                duzen,
                kolonGenislikleri,
                listOf("—", "Karşılaştırma kalemleri için alım kaydı bulunamadı.", "", "", "", "", "")
            )
            return
        }

        var oncekiMalzeme = ""
        for (satir in satirlari) {
            val malzemeDegisti = !oncekiMalzeme.equals(satir.malzeme, ignoreCase = true)
            oncekiMalzeme = satir.malzeme

            if (satir.kayitYok) {
                SatinalmaPdfCizim.tabloSatirCiz(
                    duzen,
                    kolonGenislikleri,
                    listOf(
                        if (malzemeDegisti) satir.kalemSiraNo.toString() else "",
                        if (malzemeDegisti) satir.malzeme else "",
                        "Alım kaydı yok",
                        "",
                        "",
                        "",
                        ""
                    )
                )
                continue
            }

            SatinalmaPdfCizim.tabloSatirCiz(
                duzen,
                kolonGenislikleri,
                listOf(
                    if (malzemeDegisti) satir.kalemSiraNo.toString() else "",
                    if (malzemeDegisti) satir.malzeme else "",
                    satir.tarih,
                    SatinalmaPdfFormats.miktar(satir.miktar),
                    satir.birim,
                    SatinalmaPdfFormats.tl(satir.birimFiyati),
                    if (satir.sonIkiAlimYedegi) "${satir.tedarikci} *" else satir.tedarikci
                )
            )
        }

        if (satirlari.any { it.sonIkiAlimYedegi }) {
            duzen.y += 6f
            SatinalmaPdfCizim.metinCiz(
                duzen,
                "* Son 2 ayda alım yok; en son 2 alım gösterildi.",
                boyut = 7.5f
            )
        }
    }

    private fun karsilastirmaTablosuCiz(
        duzen: PdfSayfaDuzeni,
        talep: TalepItem,
        kalemler: List<com.satinalmapro.android.core.model.TalepKalem>,
        teklifler: List<TeklifItem>,
        onerilen: TeklifItem?,
        markaGoster: Boolean,
        grupSutun: Int
    ) {
        val noGen = 24f
        val miktarGen = 52f
        val birimGen = 42f
        val teklifAlanGen = (duzen.icerikGenisligi - noGen - miktarGen - birimGen - 120f) / (teklifler.size * grupSutun).coerceAtLeast(1)
        val malzemeGen = duzen.icerikGenisligi - noGen - miktarGen - birimGen - teklifler.size * grupSutun * teklifAlanGen

        val kolonGenislikleri = buildList {
            add(noGen)
            add(malzemeGen)
            add(miktarGen)
            add(birimGen)
            teklifler.forEach { _ ->
                repeat(grupSutun) { add(teklifAlanGen) }
            }
        }

        karsilastirmaBaslikCiz(duzen, kolonGenislikleri, teklifler, onerilen, markaGoster, grupSutun)

        kalemler.forEach { kalem ->
            val fiyatlar = teklifler.map { teklif ->
                SatinalmaPdfFormats.teklifFiyati(talep, teklif, kalem.id)
            }
            val enDusuk = fiyatlar.filterNotNull().filter { it.toplamTutar > 0 }.minOfOrNull { it.toplamTutar }

            val satir = mutableListOf(
                kalem.siraNo.toString(),
                kalem.malzeme,
                SatinalmaPdfFormats.miktar(kalem.miktar),
                kalem.birim
            )
            teklifler.forEachIndexed { index, teklif ->
                val fiyat = fiyatlar[index]
                satir.add(
                    fiyat?.let {
                        SatinalmaPdfFormats.birimFiyatGosterim(it.birimFiyat, it.paraBirimi, teklif.usdKuru, teklif.eurKuru)
                    } ?: "—"
                )
                if (markaGoster) {
                    satir.add(fiyat?.marka?.ifBlank { "—" } ?: "—")
                }
                satir.add(fiyat?.let { SatinalmaPdfFormats.tl(it.toplamTutar) } ?: "—")
            }

            val vurgula = teklifler.any { teklif ->
                val fiyat = SatinalmaPdfFormats.teklifFiyati(talep, teklif, kalem.id)
                val dusuk = fiyat != null && fiyat.toplamTutar > 0 && fiyat.toplamTutar == enDusuk
                val oneri = onerilen != null && teklif.id == onerilen.id
                dusuk || oneri
            }
            SatinalmaPdfCizim.tabloSatirCiz(duzen, kolonGenislikleri, satir, vurgula = vurgula)
        }

        val toplamSatir = buildList {
            add("ARA TOPLAM (KDV Hariç)")
            add("")
            add("")
            add("")
            teklifler.forEach {
                repeat(grupSutun - 1) { add("") }
                add(SatinalmaPdfFormats.tl(it.araToplam))
            }
        }
        SatinalmaPdfCizim.tabloSatirCiz(duzen, kolonGenislikleri, toplamSatir, vurgula = false)
    }

    private fun karsilastirmaBaslikCiz(
        duzen: PdfSayfaDuzeni,
        kolonGenislikleri: List<Float>,
        teklifler: List<TeklifItem>,
        onerilen: TeklifItem?,
        markaGoster: Boolean,
        grupSutun: Int
    ) {
        val satirYuksekligi = 20f
        duzen.alanGerekli(satirYuksekligi * 2 + 2f)
        val x0 = duzen.margin
        val griCizgi = Color.parseColor("#BDBDBD")
        val griZemin = Color.parseColor("#F5F5F5")
        val maviZemin = Color.parseColor("#BBDEFB")
        val y0 = duzen.y

        fun hucre(x: Float, y: Float, gen: Float, yuk: Float, metin: String, vurgula: Boolean) {
            val rect = RectF(x, y, x + gen, y + yuk)
            duzen.canvas.drawRect(rect, Paint().apply { color = if (vurgula) maviZemin else griZemin; style = Paint.Style.FILL })
            duzen.canvas.drawRect(rect, Paint().apply { color = griCizgi; style = Paint.Style.STROKE; strokeWidth = 1f })
            if (metin.isNotBlank()) {
                val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
                    textSize = 9f
                    typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
                    color = Color.BLACK
                    textAlign = Paint.Align.CENTER
                }
                duzen.canvas.drawText(
                    if (metin.length > 18) metin.take(17) + "…" else metin,
                    x + gen / 2f,
                    y + satirYuksekligi - 6f,
                    paint
                )
            }
        }

        var cx = x0
        val sabitBasliklar = listOf("No", "Malzeme", "Miktar", "Birim")
        sabitBasliklar.forEachIndexed { index, baslik ->
            val gen = kolonGenislikleri[index]
            hucre(cx, y0, gen, satirYuksekligi * 2, baslik, false)
            cx += gen
        }

        teklifler.forEachIndexed { index, teklif ->
            val gen = kolonGenislikleri.subList(4 + index * grupSutun, 4 + (index + 1) * grupSutun).sum()
            val oneri = onerilen != null && teklif.id == onerilen.id
            hucre(cx, y0, gen, satirYuksekligi, teklif.firmaAdi, oneri)
            cx += gen
        }

        cx = x0 + kolonGenislikleri.take(4).sum()
        val y1 = y0 + satirYuksekligi
        teklifler.forEachIndexed { index, teklif ->
            val oneri = onerilen != null && teklif.id == onerilen.id
            val altBasliklar = buildList {
                add("Birim Fiyatı")
                if (markaGoster) add("Marka")
                add("Toplam KDV Hariç")
            }
            altBasliklar.forEachIndexed { altIndex, baslik ->
                val col = 4 + index * grupSutun + altIndex
                val gen = kolonGenislikleri[col]
                hucre(cx, y1, gen, satirYuksekligi, baslik, oneri)
                cx += gen
            }
        }

        duzen.y = y0 + satirYuksekligi * 2
    }

    private fun firmaOnayTablosuCiz(duzen: PdfSayfaDuzeni, teklifler: List<TeklifItem>, onerilen: TeklifItem?) {
        val kolonlar = listOf(28f, duzen.icerikGenisligi - 28f - 70f - 80f - 80f - 70f - 90f, 70f, 80f, 80f, 70f, 90f)
        val basliklar = listOf("Seç", "Firma", "Vade", "Teslim", "KDV Hariç", "KDV", "KDV Dahil")
        val satirlar = teklifler.map { teklif ->
            listOf(
                "☐",
                teklif.firmaAdi,
                if (teklif.vadeGunu > 0) "${teklif.vadeGunu} gün" else "—",
                teklif.teslimSuresi.ifBlank { "—" },
                SatinalmaPdfFormats.tl(teklif.araToplam),
                SatinalmaPdfFormats.tl(teklif.kdvTutari),
                SatinalmaPdfFormats.tl(teklif.genelToplam)
            )
        }
        val vurgular = teklifler.map { onerilen != null && it.id == onerilen.id }
        SatinalmaPdfCizim.tabloCiz(duzen, kolonlar, basliklar, satirlar, satirVurgulari = vurgular, fontBoyut = 8.5f)
    }

    private fun tedarikciTeklifTalebiPdf(talep: TalepItem, baglam: SatinalmaPdfBaglam): PdfDocument {
        val doc = PdfDocument()
        val duzen = PdfSayfaDuzeni(doc, 595f, 842f, 36f)
        duzen.baslat()
        SatinalmaPdfCizim.baslikCiz(duzen, baglam, "TEDARİKÇİ TEKLİF TALEP FORMU")
        SatinalmaPdfCizim.metinCiz(
            duzen,
            "Aşağıdaki malzemeler için birim fiyat teklifinizi doldurarak iade ediniz.",
            boyut = 10f
        )
        duzen.y += 4f
        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply { textSize = 10f; color = Color.BLACK }
        duzen.alanGerekli(16f)
        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        duzen.canvas.drawText("Talep No: ${talep.talepNo}", duzen.margin, duzen.y, paint)
        paint.typeface = Typeface.DEFAULT
        paint.textAlign = Paint.Align.RIGHT
        duzen.canvas.drawText("Tarih: ${talep.tarih}", duzen.genislik - duzen.margin, duzen.y, paint)
        paint.textAlign = Paint.Align.LEFT
        duzen.y += 16f

        val kolonlar = listOf(24f, duzen.icerikGenisligi - 248f, 52f, 44f, 72f, 64f, 72f)
        val basliklar = listOf("No", "Malzeme", "Miktar", "Birim", "Birim Fiyatı", "Marka", "Toplam")
        val satirlar = talep.kalemler.sortedBy { it.siraNo }.map { k ->
            listOf(k.siraNo.toString(), k.malzeme, SatinalmaPdfFormats.miktar(k.miktar), k.birim, "", "", "")
        }
        SatinalmaPdfCizim.tabloCiz(duzen, kolonlar, basliklar, satirlar)
        duzen.bitir()
        return doc
    }

    private fun yonetimOnayPdf(talep: TalepItem, baglam: SatinalmaPdfBaglam): PdfDocument {
        val doc = PdfDocument()
        val duzen = PdfSayfaDuzeni(doc, 595f, 842f, 36f)
        duzen.baslat()
        SatinalmaPdfCizim.baslikCiz(duzen, baglam, "YÖNETİM ONAY BELGESİ", kompakt = true)
        SatinalmaPdfCizim.metinCiz(duzen, "Talep No: ${talep.talepNo}", kalin = true, boyut = 9f)
        SatinalmaPdfCizim.metinCiz(duzen, "Onaylayan: ${talep.yonetimOnaylayanAd.ifBlank { "—" }}", boyut = 9f)
        SatinalmaPdfCizim.metinCiz(duzen, "Durum: ${talep.durum}", boyut = 9f)
        duzen.y += 4f
        val kolonlar = listOf(duzen.icerikGenisligi - 220f, 70f, 90f, 70f)
        val basliklar = listOf("Malzeme", "Miktar", "Firma", "Toplam")
        val satirlar = talep.kalemler.sortedBy { it.siraNo }.map { kalem ->
            val firma = kalem.onaylananTeklifId?.let { tid ->
                talep.teklifler.firstOrNull { it.id == tid }?.firmaAdi
            }.orEmpty().ifBlank { "Teklifsiz" }
            listOf(kalem.malzeme, "${SatinalmaPdfFormats.miktar(kalem.miktar)} ${kalem.birim}", firma, "")
        }
        SatinalmaPdfCizim.tabloCiz(duzen, kolonlar, basliklar, satirlar, kompakt = true)
        SatinalmaPdfCizim.yonetimImzaCiz(duzen, baglam.yonetimImzalari.filter { it.aktif }, kompakt = true)
        duzen.bitir()
        return doc
    }

    private fun siparisFormuPdf(talep: TalepItem, baglam: SatinalmaPdfBaglam): PdfDocument {
        val doc = PdfDocument()
        val duzen = PdfSayfaDuzeni(doc, 595f, 842f, 36f)
        duzen.baslat()
        SatinalmaPdfCizim.baslikCiz(duzen, baglam, "SİPARİŞ FORMU")
        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply { textSize = 10f }
        duzen.alanGerekli(16f)
        duzen.canvas.drawText("Talep No: ${talep.talepNo}", duzen.margin, duzen.y, paint)
        paint.textAlign = Paint.Align.RIGHT
        duzen.canvas.drawText("Sipariş No: ${talep.siparisNo.ifBlank { "—" }}", duzen.genislik - duzen.margin, duzen.y, paint)
        paint.textAlign = Paint.Align.LEFT
        duzen.y += 16f
        SatinalmaPdfCizim.metinCiz(duzen, "Şantiye: ${talep.santiyeAdi}")
        duzen.y += 4f
        val kolonlar = listOf(duzen.icerikGenisligi - 220f, 70f, 90f, 70f)
        val basliklar = listOf("Malzeme", "Miktar", "Firma", "Toplam (KDV Hariç)")
        val satirlar = talep.kalemler.sortedBy { it.siraNo }.map { kalem ->
            val teklif = kalem.onaylananTeklifId?.let { tid -> talep.teklifler.firstOrNull { it.id == tid } }
            val fiyat = teklif?.fiyatlar?.firstOrNull { it.kalemId == kalem.id }
            val firma = teklif?.firmaAdi.orEmpty().ifBlank { "—" }
            val tutar = fiyat?.toplamTutar?.let { SatinalmaPdfFormats.tl(it) } ?: "—"
            listOf(kalem.malzeme, "${SatinalmaPdfFormats.miktar(kalem.miktar)} ${kalem.birim}", firma, tutar)
        }
        SatinalmaPdfCizim.tabloCiz(duzen, kolonlar, basliklar, satirlar)
        duzen.bitir()
        return doc
    }
}
