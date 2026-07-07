package com.satinalmapro.android.services

import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import com.satinalmapro.android.core.model.ImzaAyari

data class SatinalmaPdfBaglam(
    val firmaAdi: String = "Satınalma Pro",
    val sefImzalari: List<ImzaAyari> = emptyList(),
    val yonetimImzalari: List<ImzaAyari> = emptyList()
)

internal class PdfSayfaDuzeni(
    private val doc: PdfDocument,
    val genislik: Float,
    val yukseklik: Float,
    val margin: Float
) {
    private var sayfaNo = 0
    private lateinit var page: PdfDocument.Page
    lateinit var canvas: Canvas
        private set
    var y = margin

    val icerikGenisligi: Float get() = genislik - margin * 2
    val altSinir: Float get() = yukseklik - margin

    fun baslat() = yeniSayfa()

    fun yeniSayfa() {
        if (sayfaNo > 0) doc.finishPage(page)
        sayfaNo++
        val pageInfo = PdfDocument.PageInfo.Builder(genislik.toInt(), yukseklik.toInt(), sayfaNo).create()
        page = doc.startPage(pageInfo)
        canvas = page.canvas
        y = margin
    }

    fun alanGerekli(yukseklikGerekli: Float) {
        if (y + yukseklikGerekli > altSinir) yeniSayfa()
    }

    fun bitir() {
        if (sayfaNo > 0) doc.finishPage(page)
    }
}

internal object SatinalmaPdfCizim {
    private val kirmiziBaslik = Color.parseColor("#E53935")
    private val griCizgi = Color.parseColor("#BDBDBD")
    private val griBaslikZemin = Color.parseColor("#F5F5F5")
    private val maviOneriZemin = Color.parseColor("#E3F2FD")
    private val maviOneriCizgi = Color.parseColor("#90CAF9")
    private val yesilVurguZemin = Color.parseColor("#E8F5E9")

    fun baslikCiz(
        duzen: PdfSayfaDuzeni,
        baglam: SatinalmaPdfBaglam,
        baslik: String,
        kompakt: Boolean = false
    ) {
        val canvas = duzen.canvas
        val logoGenislik = if (kompakt) 84f else 108f
        val firmaBoyut = if (kompakt) 11f else 14f
        val baslikBoyut = if (kompakt) 11f else 15f
        val merkezX = duzen.genislik / 2f

        val paint = Paint(Paint.ANTI_ALIAS_FLAG)
        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        paint.textSize = firmaBoyut
        paint.textAlign = Paint.Align.CENTER
        paint.color = Color.BLACK
        canvas.drawText(baglam.firmaAdi, merkezX, duzen.y + firmaBoyut, paint)

        paint.textSize = baslikBoyut
        paint.color = kirmiziBaslik
        canvas.drawText(baslik, merkezX, duzen.y + firmaBoyut + baslikBoyut + 4f, paint)

        duzen.y += if (kompakt) 34f else 44f
        paint.color = griCizgi
        paint.strokeWidth = 1f
        canvas.drawLine(duzen.margin, duzen.y, duzen.genislik - duzen.margin, duzen.y, paint)
        duzen.y += if (kompakt) 10f else 14f
    }

    fun metinCiz(
        duzen: PdfSayfaDuzeni,
        metin: String,
        x: Float = duzen.margin,
        kalin: Boolean = false,
        boyut: Float = 10f,
        renk: Int = Color.BLACK,
        genislik: Float? = null
    ): Float {
        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            textSize = boyut
            typeface = Typeface.create(Typeface.DEFAULT, if (kalin) Typeface.BOLD else Typeface.NORMAL)
            color = renk
            textAlign = Paint.Align.LEFT
        }
        val maxGenislik = genislik ?: duzen.icerikGenisligi
        val satirYuksekligi = boyut + 4f
        var currentY = duzen.y
        metin.split('\n').forEach { paragraf ->
            var satir = ""
            paragraf.split(' ').forEach { kelime ->
                val deneme = if (satir.isEmpty()) kelime else "$satir $kelime"
                if (paint.measureText(deneme) > maxGenislik) {
                    duzen.alanGerekli(satirYuksekligi)
                    currentY = duzen.y
                    duzen.canvas.drawText(satir, x, currentY, paint)
                    currentY += satirYuksekligi
                    duzen.y = currentY
                    satir = kelime
                } else {
                    satir = deneme
                }
            }
            if (satir.isNotEmpty()) {
                duzen.alanGerekli(satirYuksekligi)
                currentY = duzen.y
                duzen.canvas.drawText(satir, x, currentY, paint)
                currentY += satirYuksekligi
                duzen.y = currentY
            }
        }
        return duzen.y
    }

    fun cerceveliMetin(
        duzen: PdfSayfaDuzeni,
        metin: String,
        zemin: Int = Color.WHITE,
        cerceve: Int = griCizgi,
        padding: Float = 8f,
        boyut: Float = 10f
    ) {
        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
            textSize = boyut
            typeface = Typeface.DEFAULT
            color = Color.BLACK
            textAlign = Paint.Align.LEFT
        }
        val satirYuksekligi = boyut + 4f
        val satirlar = mutableListOf<String>()
        var satir = ""
        metin.split(' ').forEach { kelime ->
            val deneme = if (satir.isEmpty()) kelime else "$satir $kelime"
            if (paint.measureText(deneme) > duzen.icerikGenisligi - padding * 2) {
                if (satir.isNotEmpty()) satirlar.add(satir)
                satir = kelime
            } else {
                satir = deneme
            }
        }
        if (satir.isNotEmpty()) satirlar.add(satir)
        val kutuYuksekligi = padding * 2 + satirlar.size * satirYuksekligi
        duzen.alanGerekli(kutuYuksekligi + 4f)
        val rect = RectF(duzen.margin, duzen.y, duzen.genislik - duzen.margin, duzen.y + kutuYuksekligi)
        val fill = Paint().apply { color = zemin; style = Paint.Style.FILL }
        val stroke = Paint().apply { color = cerceve; style = Paint.Style.STROKE; strokeWidth = 1f }
        duzen.canvas.drawRect(rect, fill)
        duzen.canvas.drawRect(rect, stroke)
        var textY = duzen.y + padding + boyut
        satirlar.forEach { line ->
            duzen.canvas.drawText(line, duzen.margin + padding, textY, paint)
            textY += satirYuksekligi
        }
        duzen.y += kutuYuksekligi + 8f
    }

    fun tabloSatirCiz(
        duzen: PdfSayfaDuzeni,
        kolonGenislikleri: List<Float>,
        satir: List<String>,
        vurgula: Boolean = false,
        kompakt: Boolean = false,
        fontBoyut: Float = if (kompakt) 7.5f else 9f,
        satirYuksekligi: Float = if (kompakt) 16f else 20f
    ) {
        tabloCiz(
            duzen,
            kolonGenislikleri,
            basliklar = emptyList(),
            satirlar = listOf(satir),
            satirVurgulari = listOf(vurgula),
            kompakt = kompakt,
            fontBoyut = fontBoyut,
            satirYuksekligi = satirYuksekligi,
            baslikCiz = false
        )
    }

    fun tabloCiz(
        duzen: PdfSayfaDuzeni,
        kolonGenislikleri: List<Float>,
        basliklar: List<String>,
        satirlar: List<List<String>>,
        satirVurgulari: List<Boolean> = emptyList(),
        baslikVurgulari: List<Boolean> = emptyList(),
        kompakt: Boolean = false,
        hucrePadding: Float = if (kompakt) 2f else 4f,
        fontBoyut: Float = if (kompakt) 7.5f else 9f,
        satirYuksekligi: Float = if (kompakt) 16f else 20f,
        baslikCiz: Boolean = true
    ) {
        val toplamGenislik = kolonGenislikleri.sum()
        val x0 = duzen.margin

        fun hucreZemin(vurgula: Boolean, baslik: Boolean) = when {
            vurgula && baslik -> Color.parseColor("#BBDEFB")
            vurgula -> yesilVurguZemin
            baslik -> griBaslikZemin
            else -> Color.WHITE
        }

        fun satirCiz(satir: List<String>, baslik: Boolean, vurgular: List<Boolean>) {
            duzen.alanGerekli(satirYuksekligi + 2f)
            var cx = x0
            satir.forEachIndexed { index, hucre ->
                val gen = kolonGenislikleri.getOrElse(index) { 0f }
                val vurgula = vurgular.getOrElse(index) { false }
                val rect = RectF(cx, duzen.y, cx + gen, duzen.y + satirYuksekligi)
                val fill = Paint().apply { color = hucreZemin(vurgula, baslik); style = Paint.Style.FILL }
                val stroke = Paint().apply { color = griCizgi; style = Paint.Style.STROKE; strokeWidth = 1f }
                duzen.canvas.drawRect(rect, fill)
                duzen.canvas.drawRect(rect, stroke)
                val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
                    textSize = fontBoyut
                    typeface = Typeface.create(Typeface.DEFAULT, if (baslik) Typeface.BOLD else Typeface.NORMAL)
                    color = Color.BLACK
                    textAlign = if (index >= 2 && !baslik) Paint.Align.RIGHT else Paint.Align.LEFT
                }
                val metin = kisalt(hucre, if (gen < 60f) 10 else 28)
                val textX = if (paint.textAlign == Paint.Align.RIGHT) cx + gen - hucrePadding else cx + hucrePadding
                val textY = duzen.y + satirYuksekligi - hucrePadding - 2f
                duzen.canvas.drawText(metin, textX, textY, paint)
                cx += gen
            }
            duzen.y += satirYuksekligi
        }

        // İki satırlı başlık desteği: basliklar tek satır ise doğrudan çiz
        if (baslikCiz && basliklar.isNotEmpty()) {
            satirCiz(basliklar, baslik = true, vurgular = baslikVurgulari)
        }
        satirlar.forEachIndexed { index, satir ->
            val vurgu = satirVurgulari.getOrElse(index) { false }
            val vurguList = List(satir.size) { vurgu }
            satirCiz(satir, baslik = false, vurgular = vurguList)
        }
    }

    fun imzaAlanlariCiz(duzen: PdfSayfaDuzeni, imzalar: List<ImzaAyari>, kompakt: Boolean = false) {
        if (imzalar.isEmpty()) return
        val hucreGenisligi = (duzen.icerikGenisligi - 12f * (imzalar.size - 1)) / imzalar.size
        val yukseklik = if (kompakt) 52f else 72f
        duzen.alanGerekli(yukseklik + 8f)
        imzalar.forEachIndexed { index, imza ->
            val x = duzen.margin + index * (hucreGenisligi + 12f)
            imzaHucreCiz(duzen.canvas, x, duzen.y, hucreGenisligi, imza, kompakt)
        }
        duzen.y += yukseklik + 8f
    }

    fun yonetimImzaCiz(duzen: PdfSayfaDuzeni, imzalar: List<ImzaAyari>, kompakt: Boolean = false) {
        if (imzalar.isEmpty()) {
            metinCiz(duzen, "Yönetim İmzası", kalin = true, boyut = if (kompakt) 7.5f else 9f)
            duzen.y += if (kompakt) 20f else 28f
            val paint = Paint().apply { color = griCizgi; strokeWidth = 0.5f }
            duzen.canvas.drawLine(duzen.margin, duzen.y, duzen.genislik - duzen.margin, duzen.y, paint)
            duzen.y += 12f
            return
        }
        imzalar.forEach { imza ->
            val genislik = minOf(200f, duzen.icerikGenisligi)
            val x = duzen.margin + (duzen.icerikGenisligi - genislik) / 2f
            duzen.alanGerekli(if (kompakt) 52f else 72f)
            imzaHucreCiz(duzen.canvas, x, duzen.y, genislik, imza, kompakt)
            duzen.y += if (kompakt) 52f else 72f
        }
    }

    private fun imzaHucreCiz(canvas: Canvas, x: Float, y: Float, genislik: Float, imza: ImzaAyari, kompakt: Boolean) {
        val unvanBoyut = if (kompakt) 7.5f else 9f
        val adBoyut = if (kompakt) 7.5f else 9f
        val paint = Paint(Paint.ANTI_ALIAS_FLAG)
        paint.textAlign = Paint.Align.CENTER
        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.ITALIC)
        paint.textSize = unvanBoyut
        paint.color = Color.BLACK
        canvas.drawText(imza.unvan.ifBlank { " " }, x + genislik / 2f, y + unvanBoyut, paint)
        val lineY = y + unvanBoyut + 10f
        canvas.drawLine(x + 4f, lineY, x + genislik - 4f, lineY, Paint().apply { color = Color.BLACK; strokeWidth = 0.5f })
        paint.typeface = Typeface.DEFAULT
        paint.textSize = adBoyut
        if (imza.adSoyad.isNotBlank()) {
            canvas.drawText(imza.adSoyad, x + genislik / 2f, lineY + adBoyut + 4f, paint)
        }
        paint.textSize = if (kompakt) 7f else 8f
        paint.color = Color.parseColor("#757575")
        canvas.drawText("....../....../202.....", x + genislik / 2f, lineY + adBoyut + 18f, paint)
    }

    fun oneriKutusu(duzen: PdfSayfaDuzeni, metin: String, kompakt: Boolean = false) {
        cerceveliMetin(
            duzen,
            metin,
            zemin = maviOneriZemin,
            cerceve = maviOneriCizgi,
            padding = if (kompakt) 4f else 8f,
            boyut = if (kompakt) 7.5f else 9f
        )
    }

    private fun kisalt(metin: String, max: Int): String =
        if (metin.length <= max) metin else metin.take(max - 1) + "…"
}
