package com.satinalmapro.android.core.saas

import java.time.Instant
import kotlin.math.ceil

data class TenantLicense(
    val tip: String = "deneme",
    val baslangicUtc: String? = null,
    val bitisUtc: String? = null,
    val aktif: Boolean = true,
    val kalanGun: Int? = null,
    val suresiDoldu: Boolean = false
) {
    val kisaDurumMetni: String
        get() {
            val guncel = yenidenHesapla()
            if (guncel.suresiDoldu || !guncel.aktif || guncel.kalanGun == null || guncel.kalanGun <= 0) {
                return "Lisans süresi doldu"
            }
            return if (guncel.kalanGun <= 7) {
                "Lisans: ${guncel.kalanGun} gün kaldı"
            } else {
                "Lisans: ${guncel.kalanGun} gün"
            }
        }

    val tipGorunen: String
        get() = when (tip) {
            "yillik" -> "Yıllık lisans"
            else -> "30 günlük deneme"
        }

    /** bitisUtc üzerinden kalan günü yeniden hesaplar (oturum açıkken süre dolumu). */
    fun yenidenHesapla(): TenantLicense {
        val bitis = bitisUtc?.let { raw ->
            runCatching { Instant.parse(raw) }.getOrNull()
        } ?: return this

        val kalan = ceil((bitis.toEpochMilli() - Instant.now().toEpochMilli()) / 86_400_000.0).toInt()
        return copy(
            kalanGun = kalan,
            suresiDoldu = kalan <= 0,
            aktif = if (kalan <= 0) false else aktif
        )
    }
}
