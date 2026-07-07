package com.satinalmapro.android.core.helpers

import com.satinalmapro.android.core.model.BildirimRecord

/** Aynı iş akışı bildirimini tekilleştirmek için mantıksal anahtar (talep + tip + hedef). */
object BildirimMantikAnahtari {
    fun olustur(bildirim: BildirimRecord): String {
        val talepId = normalizeTalepId(bildirim.talepId)
        if (talepId.isNullOrBlank()) return "id:${bildirim.id}"

        val hedef = if (!bildirim.hedefUid.isNullOrBlank()) {
            "u:${bildirim.hedefUid.trim().lowercase()}"
        } else {
            "r:${bildirim.hedefRol.orEmpty().trim()}"
        }
        return "$talepId:${bildirim.tip}:$hedef"
    }

    /** Masaüstü ile uyumlu GUID normalizasyonu ({talepId:N}). */
    fun normalizeTalepId(talepId: String?): String? =
        talepId?.trim()?.replace("-", "")?.lowercase()?.takeIf { it.isNotBlank() }
}
