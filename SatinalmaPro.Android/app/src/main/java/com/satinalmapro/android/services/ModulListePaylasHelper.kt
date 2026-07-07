package com.satinalmapro.android.services

import android.content.Context
import android.content.Intent
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.Typeface
import android.graphics.pdf.PdfDocument
import android.os.Environment
import androidx.core.content.FileProvider
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/** Modül listeleri ve stok durumu için basit PDF/CSV paylaşım. */
object ModulListePaylasHelper {

    fun csvPaylas(context: Context, baslik: String, satirlar: List<List<String>>) {
        val basliklar = satirlar.firstOrNull() ?: return
        val csv = buildString {
            appendLine(basliklar.joinToString(";") { escapeCsv(it) })
            satirlar.drop(1).forEach { appendLine(it.joinToString(";") { v -> escapeCsv(v) }) }
        }
        val dosya = File(context.cacheDir, "${baslik.replace(' ', '_')}_${tarihDamga()}.csv")
        dosya.writeText("\uFEFF$csv", Charsets.UTF_8)
        paylasDosya(context, dosya, "text/csv", baslik)
    }

    fun pdfTabloPaylas(context: Context, baslik: String, basliklar: List<String>, satirlar: List<List<String>>) {
        val dosya = pdfOlustur(context, baslik, basliklar, satirlar)
        paylasDosya(context, dosya, "application/pdf", baslik)
    }

    private fun pdfOlustur(context: Context, baslik: String, basliklar: List<String>, satirlar: List<List<String>>): File {
        val doc = PdfDocument()
        val paint = Paint(Paint.ANTI_ALIAS_FLAG).apply { textSize = 10f }
        val bold = Paint(paint).apply { typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD) }
        val pageInfo = PdfDocument.PageInfo.Builder(595, 842, 1).create()
        var page = doc.startPage(pageInfo)
        var canvas: Canvas = page.canvas
        var y = 40f
        canvas.drawText(baslik, 40f, y, bold.apply { textSize = 14f })
        y += 24f
        canvas.drawText("Tarih: ${bugun()}", 40f, y, paint)
        y += 20f
        basliklar.forEachIndexed { i, h ->
            canvas.drawText(h.take(18), 40f + i * 72f, y, bold)
        }
        y += 16f
        satirlar.forEach { satir ->
            if (y > 800f) {
                doc.finishPage(page)
                page = doc.startPage(pageInfo)
                canvas = page.canvas
                y = 40f
            }
            satir.forEachIndexed { i, v ->
                canvas.drawText(v.take(20), 40f + i * 72f, y, paint)
            }
            y += 14f
        }
        doc.finishPage(page)
        val dosya = File(context.cacheDir, "${baslik.replace(' ', '_')}_${tarihDamga()}.pdf")
        FileOutputStream(dosya).use { doc.writeTo(it) }
        doc.close()
        return dosya
    }

    private fun paylasDosya(context: Context, dosya: File, mime: String, baslik: String) {
        val uri = FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", dosya)
        val intent = Intent(Intent.ACTION_SEND).apply {
            type = mime
            putExtra(Intent.EXTRA_STREAM, uri)
            putExtra(Intent.EXTRA_SUBJECT, baslik)
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        context.startActivity(Intent.createChooser(intent, "$baslik paylaş"))
    }

    private fun escapeCsv(value: String): String {
        val v = value.replace("\"", "\"\"")
        return if (v.contains(';') || v.contains('"') || v.contains('\n')) "\"$v\"" else v
    }

    private fun bugun() = SimpleDateFormat("dd.MM.yyyy HH:mm", Locale("tr", "TR")).format(Date())
    private fun tarihDamga() = SimpleDateFormat("yyyyMMdd_HHmm", Locale.US).format(Date())
}
