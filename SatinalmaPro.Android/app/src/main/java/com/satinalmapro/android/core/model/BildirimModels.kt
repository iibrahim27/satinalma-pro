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
)

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
