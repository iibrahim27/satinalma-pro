package com.satinalmapro.android.data.local

import android.content.Context
import com.satinalmapro.android.core.helpers.BildirimMantikAnahtari
import com.satinalmapro.android.core.model.BildirimRecord

/** Yerel bildirim hatırlatmalarını saatlik aralıkla sınırlar (masaüstü ile aynı mantık). */
class BildirimHatirlatmaDeposu(context: Context) {
    private val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)

    fun gosterilebilirMi(bildirim: BildirimRecord, @Suppress("UNUSED_PARAMETER") ilkGosterim: Boolean = false): Boolean {
        if (bildirim.okundu) return false
        val anahtar = BildirimMantikAnahtari.olustur(bildirim)
        val sonUtc = prefs.getLong(anahtar, 0L)
        if (sonUtc == 0L) return true
        return System.currentTimeMillis() - sonUtc >= HATIRLATMA_ARALIGI_MS
    }

    fun dahaOnceGosterildiMi(bildirim: BildirimRecord): Boolean {
        val anahtar = BildirimMantikAnahtari.olustur(bildirim)
        return prefs.getLong(anahtar, 0L) > 0L
    }

    fun gosterildi(bildirim: BildirimRecord) {
        prefs.edit()
            .putLong(BildirimMantikAnahtari.olustur(bildirim), System.currentTimeMillis())
            .apply()
    }

    fun temizle(bildirim: BildirimRecord) {
        prefs.edit().remove(BildirimMantikAnahtari.olustur(bildirim)).apply()
    }

    companion object {
        private const val PREFS = "bildirim_hatirlatma"
        const val HATIRLATMA_ARALIGI_MS = 3_600_000L
    }
}
