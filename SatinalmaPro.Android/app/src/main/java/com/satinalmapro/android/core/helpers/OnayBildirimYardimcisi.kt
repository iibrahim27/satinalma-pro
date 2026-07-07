package com.satinalmapro.android.core.helpers

import com.satinalmapro.android.core.roles.KullaniciRolleri

/** Masaüstü OnayBildirimYardimcisi ile aynı hedefleme mantığı. */
object OnayBildirimYardimcisi {
    fun satinalmaOnayladi(onaylayanRol: String?): Boolean =
        KullaniciRolleri.normalize(onaylayanRol) == KullaniciRolleri.SATINALMA

    fun onaylandiHedefleri(
        olusturanUid: String?,
        onaylayanRol: String?
    ): List<Pair<String?, String?>> {
        val hedefler = mutableListOf<Pair<String?, String?>>()
        if (satinalmaOnayladi(onaylayanRol)) {
            if (!olusturanUid.isNullOrBlank()) hedefler.add(null to olusturanUid)
            hedefler.add(KullaniciRolleri.YONETIM to null)
            return hedefler
        }
        if (!olusturanUid.isNullOrBlank()) hedefler.add(null to olusturanUid)
        hedefler.add(KullaniciRolleri.SATINALMA to null)
        return hedefler
    }

    fun teklifIstemeBildirimEk(onaylayanRol: String?): String =
        if (satinalmaOnayladi(onaylayanRol)) {
            "Satınalma birimi teklifiniz için teklif girişi talep etti."
        } else {
            "Yönetim teklifiniz için satınalmadan teklif talep etti."
        }
}
