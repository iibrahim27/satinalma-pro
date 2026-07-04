package com.satinalmapro.android.core

import com.satinalmapro.android.core.model.StokHareket
import java.util.Calendar

object StokBelgeNoUretici {
    fun sonrakiGirisBelgeNo(hareketler: List<StokHareket>): String = sonrakiBelgeNo("GR", hareketler)

    fun sonrakiCikisBelgeNo(hareketler: List<StokHareket>): String = sonrakiBelgeNo("CK", hareketler)

    private fun sonrakiBelgeNo(kod: String, hareketler: List<StokHareket>): String {
        val yil = Calendar.getInstance().get(Calendar.YEAR)
        val onEk = "$kod-$yil-"
        val sira = hareketler
            .map { it.belgeNo }
            .filter { it.startsWith(onEk, ignoreCase = true) }
            .mapNotNull { belge ->
                belge.substringAfterLast('-').toIntOrNull()
            }
            .maxOrNull() ?: 0
        return "$onEk${(sira + 1).toString().padStart(3, '0')}"
    }
}
