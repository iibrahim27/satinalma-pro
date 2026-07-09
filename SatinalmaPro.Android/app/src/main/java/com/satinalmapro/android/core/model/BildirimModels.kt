package com.satinalmapro.android.core.model

data class BildirimRecord(
    val id: String = "",
    val baslik: String = "",
    val mesaj: String = "",
    val tip: String = "",
    val talepId: String? = null,
    val hedefRol: String? = null,
    val hedefUid: String? = null,
    val olusturanUid: String = "",
    val olusturanAd: String = "",
    val olusturmaTarihi: String = "",
    val okundu: Boolean = false,
    val guncellemeUtc: Long = 0,
    val inboxDocId: String? = null
) {
    @Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
    fun normalized(): BildirimRecord = copy(
        id = (id as String?) ?: "",
        baslik = (baslik as String?) ?: "",
        mesaj = (mesaj as String?) ?: "",
        tip = (tip as String?) ?: "",
        talepId = talepId?.takeIf { it.isNotBlank() },
        hedefRol = hedefRol?.takeIf { it.isNotBlank() },
        hedefUid = hedefUid?.takeIf { it.isNotBlank() },
        olusturanUid = (olusturanUid as String?) ?: "",
        olusturanAd = (olusturanAd as String?) ?: "",
        olusturmaTarihi = (olusturmaTarihi as String?) ?: "",
        inboxDocId = inboxDocId?.takeIf { it.isNotBlank() }
    )
}

object BildirimTipleri {
    const val YONETIME_GONDERILDI = "yonetime_gonderildi"
    const val TEKLIF_ISTENDI = "teklif_istendi"
    const val TEKLIF_ONAYDA = "teklif_onayda"
    const val TEKLIF_DUZELTME_ISTENDI = "teklif_duzeltme_istendi"
    const val ONAYLANDI = "onaylandi"
    const val REDDEDILDI = "reddedildi"
    const val SIPARIS_OLUSTURULDU = "siparis_olusturuldu"
    const val MAL_KABUL_EDILDI = "mal_kabul_edildi"
}
