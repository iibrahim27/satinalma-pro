package com.satinalmapro.android.core.helpers

import android.content.Context
import android.util.Log
import org.json.JSONArray
import org.json.JSONObject
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

/** Bildirim/FCM tanılama günlüğü — Logcat + yerel halka tampon. */
object BildirimLog {
    private const val TAG = "SatinalmaBildirim"
    private const val PREFS = "bildirim_log"
    private const val KEY_ENTRIES = "entries"
    private const val MAX_ENTRIES = 80

    fun d(kaynak: String, mesaj: String) {
        Log.d(TAG, "[$kaynak] $mesaj")
        kaydet("DEBUG", kaynak, mesaj)
    }

    fun i(kaynak: String, mesaj: String) {
        Log.i(TAG, "[$kaynak] $mesaj")
        kaydet("INFO", kaynak, mesaj)
    }

    fun w(kaynak: String, mesaj: String, hata: Throwable? = null) {
        if (hata != null) Log.w(TAG, "[$kaynak] $mesaj", hata)
        else Log.w(TAG, "[$kaynak] $mesaj")
        kaydet("WARN", kaynak, mesaj + (hata?.message?.let { " · $it" } ?: ""))
    }

    fun e(kaynak: String, mesaj: String, hata: Throwable? = null) {
        if (hata != null) Log.e(TAG, "[$kaynak] $mesaj", hata)
        else Log.e(TAG, "[$kaynak] $mesaj")
        kaydet("ERROR", kaynak, mesaj + (hata?.message?.let { " · $it" } ?: ""))
    }

    fun pushBasladi(tip: String, talepId: String?, hedefRol: String?, hedefUid: String?) {
        i(
            "FCM_PUSH",
            "Başladı tip=$tip talep=${talepId.orEmpty()} hedefRol=${hedefRol.orEmpty()} hedefUid=${hedefUid.orEmpty()}"
        )
    }

    fun pushSonuc(sonuc: com.satinalmapro.android.services.FcmPushService.PushResult) {
        val seviye = if (sonuc.basarili) "INFO" else "ERROR"
        val mesaj = buildString {
            append("Push sonuc: basarili=${sonuc.basarili}")
            append(" hedef=${sonuc.hedefSayisi}")
            append(" inbox=${sonuc.inboxYazilan}")
            append(" fcm=${sonuc.fcmGonderilen}")
            if (sonuc.uyarilar.isNotEmpty()) append(" uyarilar=${sonuc.uyarilar.joinToString("; ")}")
            if (sonuc.hatalar.isNotEmpty()) append(" hatalar=${sonuc.hatalar.joinToString("; ")}")
        }
        if (sonuc.basarili) i("FCM_PUSH", mesaj) else e("FCM_PUSH", mesaj)
        if (seviye == "ERROR") kaydet("ERROR", "FCM_PUSH", mesaj)
    }

    fun sonKayitlar(context: Context, limit: Int = 30): List<String> {
        val arr = JSONArray(context.getSharedPreferences(PREFS, Context.MODE_PRIVATE).getString(KEY_ENTRIES, "[]"))
        return buildList {
            for (i in (arr.length() - 1) downTo 0) {
                if (size >= limit) break
                val o = arr.optJSONObject(i) ?: continue
                add("${o.optString("zaman")} [${o.optString("seviye")}] ${o.optString("kaynak")}: ${o.optString("mesaj")}")
            }
        }
    }

    private fun kaydet(seviye: String, kaynak: String, mesaj: String) {
        runCatching {
            // SharedPreferences yazımı isteğe bağlı; Logcat her zaman aktif.
        }
    }

    fun kaydetContext(context: Context, seviye: String, kaynak: String, mesaj: String) {
        runCatching {
            val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            val arr = JSONArray(prefs.getString(KEY_ENTRIES, "[]"))
            val entry = JSONObject()
                .put("zaman", SimpleDateFormat("HH:mm:ss", Locale("tr", "TR")).format(Date()))
                .put("seviye", seviye)
                .put("kaynak", kaynak)
                .put("mesaj", mesaj.take(500))
            val yeni = JSONArray()
            yeni.put(entry)
            val baslangic = (arr.length() - (MAX_ENTRIES - 1)).coerceAtLeast(0)
            for (i in baslangic until arr.length()) yeni.put(arr.get(i))
            prefs.edit().putString(KEY_ENTRIES, yeni.toString()).apply()
        }
    }
}
