package com.satinalmapro.android.services

import android.content.Context
import android.content.Intent
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import android.os.Environment
import android.print.PrintAttributes
import android.print.PrintManager
import android.webkit.WebView
import android.webkit.WebViewClient
import androidx.core.content.FileProvider
import com.satinalmapro.android.core.roles.KullaniciRolleri
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

object StokTeslimFisiHelper {
    data class Satir(
        val malzeme: String,
        val miktar: String,
        val birim: String,
        val depoSaha: String = ""
    )

    data class Fis(
        val belgeNo: String,
        val tarih: String,
        val teslimEden: String,
        val teslimAlan: String,
        val satirlar: List<Satir>,
        val firmaAdi: String = "Satınalma Pro"
    )

    fun teslimEdenMetni(role: String?, fullName: String): String {
        val rol = KullaniciRolleri.normalize(role)
        val ad = fullName.trim()
        return if (ad.isBlank()) rol else "$rol - $ad"
    }

    fun miktarMetni(miktar: Double, birim: String): String {
        val b = birim.ifBlank { "Adet" }
        val m = if (miktar % 1.0 == 0.0) miktar.toLong().toString() else miktar.toString()
        return "$m $b"
    }

    fun bugunTarih(): String =
        SimpleDateFormat("dd.MM.yyyy", Locale("tr", "TR")).format(Date())

    fun yazdirA5(context: Context, fis: Fis) {
        val webView = WebView(context.applicationContext)
        webView.settings.apply {
            javaScriptEnabled = false
            defaultTextEncodingName = "UTF-8"
        }
        webView.webViewClient = object : WebViewClient() {
            override fun onPageFinished(view: WebView, url: String?) {
                val printManager = context.getSystemService(Context.PRINT_SERVICE) as PrintManager
                val jobName = "Depo Çıkış Fişi ${fis.belgeNo}"
                val attributes = PrintAttributes.Builder()
                    .setMediaSize(PrintAttributes.MediaSize.ISO_A5)
                    .setMinMargins(PrintAttributes.Margins.NO_MARGINS)
                    .build()
                printManager.print(
                    jobName,
                    webView.createPrintDocumentAdapter(jobName),
                    attributes
                )
            }
        }
        webView.loadDataWithBaseURL(null, htmlOlustur(fis), "text/html; charset=UTF-8", "UTF-8", null)
    }

    fun pdfKaydet(context: Context, fis: Fis): File {
        val dosyaAdi = "DepoCikisFisi_${fis.belgeNo.replace('/', '-')}.pdf"
        val hedef = File(context.getExternalFilesDir(Environment.DIRECTORY_DOCUMENTS), dosyaAdi)
        hedef.parentFile?.mkdirs()
        pdfOlustur(fis).writeTo(FileOutputStream(hedef))
        return hedef
    }

    fun paylasPdf(context: Context, fis: Fis): File {
        val dosya = pdfKaydet(context, fis)
        val uri = FileProvider.getUriForFile(
            context,
            "${context.packageName}.fileprovider",
            dosya
        )
        val intent = Intent(Intent.ACTION_SEND).apply {
            type = "application/pdf"
            putExtra(Intent.EXTRA_STREAM, uri)
            putExtra(Intent.EXTRA_SUBJECT, "Depo Çıkış Fişi ${fis.belgeNo}")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        context.startActivity(Intent.createChooser(intent, "Fişi Paylaş"))
        return dosya
    }

    private fun pdfOlustur(fis: Fis): PdfDocument {
        val doc = PdfDocument()
        val pageInfo = PdfDocument.PageInfo.Builder(420, 595, 1).create()
        val page = doc.startPage(pageInfo)
        val canvas = page.canvas
        val paint = Paint(Paint.ANTI_ALIAS_FLAG)
        var y = 36f

        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        paint.textSize = 14f
        paint.textAlign = Paint.Align.CENTER
        canvas.drawText(fis.firmaAdi, 210f, y, paint)
        y += 20f
        paint.textSize = 13f
        canvas.drawText("DEPO ÇIKIŞ FİŞİ", 210f, y, paint)
        y += 24f

        paint.textAlign = Paint.Align.LEFT
        paint.textSize = 9f
        paint.typeface = Typeface.DEFAULT
        y = bilgiSatiri(canvas, paint, y, "Belge No", fis.belgeNo, "Tarih", fis.tarih)
        y = bilgiSatiri(canvas, paint, y, "Teslim Eden", fis.teslimEden, "Teslim Alan", fis.teslimAlan)
        y += 8f

        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        paint.textSize = 8.5f
        canvas.drawText("Malzeme", 28f, y, paint)
        canvas.drawText("Miktar", 250f, y, paint)
        canvas.drawText("Birim", 320f, y, paint)
        canvas.drawText("Depo", 370f, y, paint)
        y += 4f
        canvas.drawLine(24f, y, 396f, y, paint)
        y += 14f

        paint.typeface = Typeface.DEFAULT
        fis.satirlar.forEach { satir ->
            canvas.drawText(kisalt(satir.malzeme, 34), 28f, y, paint)
            canvas.drawText(satir.miktar, 250f, y, paint)
            canvas.drawText(satir.birim, 320f, y, paint)
            canvas.drawText(kisalt(satir.depoSaha, 12), 370f, y, paint)
            y += 16f
        }

        y += 28f
        imzaAlani(canvas, paint, y, "Teslim Eden", fis.teslimEden, 28f)
        imzaAlani(canvas, paint, y, "Teslim Alan", fis.teslimAlan, 220f)

        doc.finishPage(page)
        return doc
    }

    private fun bilgiSatiri(
        canvas: Canvas,
        paint: Paint,
        y: Float,
        etiket1: String,
        deger1: String,
        etiket2: String,
        deger2: String
    ): Float {
        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        canvas.drawText("$etiket1:", 28f, y, paint)
        paint.typeface = Typeface.DEFAULT
        canvas.drawText(kisalt(deger1, 28), 92f, y, paint)
        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        canvas.drawText("$etiket2:", 220f, y, paint)
        paint.typeface = Typeface.DEFAULT
        canvas.drawText(kisalt(deger2, 28), 286f, y, paint)
        return y + 16f
    }

    private fun imzaAlani(canvas: Canvas, paint: Paint, y: Float, baslik: String, isim: String, x: Float) {
        paint.typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        canvas.drawText(baslik, x, y, paint)
        paint.typeface = Typeface.DEFAULT
        canvas.drawText(kisalt(isim, 30), x, y + 16f, paint)
        canvas.drawLine(x, y + 44f, x + 168f, y + 44f, paint)
        paint.textSize = 7f
        canvas.drawText("Ad Soyad / İmza", x, y + 56f, paint)
        paint.textSize = 8.5f
    }

    private fun kisalt(metin: String, max: Int): String =
        if (metin.length <= max) metin else metin.take(max - 1) + "…"

    private fun htmlOlustur(fis: Fis): String {
        val satirlarHtml = fis.satirlar.joinToString("") { satir ->
            """
            <tr>
              <td>${escape(satir.malzeme)}</td>
              <td class="num">${escape(satir.miktar)}</td>
              <td>${escape(satir.birim)}</td>
              <td>${escape(satir.depoSaha)}</td>
            </tr>
            """.trimIndent()
        }
        return """
            <!DOCTYPE html>
            <html lang="tr">
            <head>
              <meta charset="UTF-8"/>
              <style>
                @page { size: A5 portrait; margin: 10mm; }
                body { font-family: 'Segoe UI', sans-serif; font-size: 10px; color: #111; margin: 0; }
                .page { width: 128mm; min-height: 190mm; margin: 0 auto; padding: 4mm 0; }
                h1 { text-align: center; font-size: 14px; margin: 0 0 2px; }
                .sub { text-align: center; color: #0d9488; font-size: 13px; font-weight: 700; margin-bottom: 12px; }
                .divider { border-top: 1px solid #d1d5db; margin: 8px 0 12px; }
                .info { width: 100%; border-collapse: collapse; margin-bottom: 10px; }
                .info td { padding: 4px 6px; vertical-align: top; font-size: 9px; }
                .info .label { width: 72px; color: #555; font-weight: 700; }
                table.items { width: 100%; border-collapse: collapse; }
                table.items th, table.items td { border: 1px solid #bbb; padding: 5px 6px; font-size: 9px; }
                table.items th { background: #f3f4f6; text-align: left; }
                .num { text-align: right; white-space: nowrap; }
                .signatures { width: 100%; margin-top: 28px; border-collapse: collapse; }
                .signatures td { width: 50%; vertical-align: top; padding: 0 8px; }
                .sign-title { font-weight: 700; font-size: 9px; margin-bottom: 4px; }
                .sign-name { min-height: 14px; font-size: 9px; margin-bottom: 22px; }
                .sign-line { border-bottom: 1px solid #333; height: 22px; }
                .sign-hint { font-size: 8px; color: #666; margin-top: 3px; }
              </style>
            </head>
            <body>
              <div class="page">
                <h1>${escape(fis.firmaAdi)}</h1>
                <div class="sub">DEPO ÇIKIŞ FİŞİ</div>
                <div class="divider"></div>
                <table class="info">
                  <tr><td class="label">Belge No</td><td>${escape(fis.belgeNo)}</td><td class="label">Tarih</td><td>${escape(fis.tarih)}</td></tr>
                  <tr><td class="label">Teslim Eden</td><td>${escape(fis.teslimEden)}</td><td class="label">Teslim Alan</td><td>${escape(fis.teslimAlan)}</td></tr>
                </table>
                <table class="items">
                  <thead>
                    <tr><th>Malzeme</th><th class="num">Miktar</th><th>Birim</th><th>Depo</th></tr>
                  </thead>
                  <tbody>
                    $satirlarHtml
                  </tbody>
                </table>
                <table class="signatures">
                  <tr>
                    <td>
                      <div class="sign-title">Teslim Eden</div>
                      <div class="sign-name">${escape(fis.teslimEden)}</div>
                      <div class="sign-line"></div>
                      <div class="sign-hint">Ad Soyad / İmza</div>
                    </td>
                    <td>
                      <div class="sign-title">Teslim Alan</div>
                      <div class="sign-name">${escape(fis.teslimAlan)}</div>
                      <div class="sign-line"></div>
                      <div class="sign-hint">Ad Soyad / İmza</div>
                    </td>
                  </tr>
                </table>
              </div>
            </body>
            </html>
        """.trimIndent()
    }

    private fun escape(text: String): String =
        text.replace("&", "&amp;")
            .replace("<", "&lt;")
            .replace(">", "&gt;")
            .replace("\"", "&quot;")
}
