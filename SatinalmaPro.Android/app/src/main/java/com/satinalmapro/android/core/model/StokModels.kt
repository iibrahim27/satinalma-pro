package com.satinalmapro.android.core.model

data class StokKaydi(
    val malzemeAdi: String = "",
    val kategori: String = "",
    val birim: String = "Adet",
    val mevcutMiktar: Double = 0.0,
    val minimumStok: Double = 0.0,
    val depoSaha: String = "",
    val birimMaliyet: Double = 0.0,
    val toplamDeger: Double = 0.0,
    val sonGuncelleme: String = "",
    val aciklama: String = ""
) {
    val durumMetin: String
        get() = when {
            mevcutMiktar <= 0 -> "Tükendi"
            minimumStok > 0 && mevcutMiktar <= minimumStok -> "Kritik"
            else -> "Normal"
        }
}

data class StokHareket(
    val id: String = "",
    val tarih: String = "",
    val hareketTipi: String = "",
    val malzemeAdi: String = "",
    val kategori: String = "",
    val birim: String = "",
    val miktar: Double = 0.0,
    val depoSaha: String = "",
    val birimMaliyet: Double = 0.0,
    val belgeNo: String = "",
    val islemYapan: String = "",
    val teslimEdilen: String = "",
    val aciklama: String = ""
)

object StokHareketTipi {
    const val GIRIS = "Giriş"
    const val CIKIS = "Çıkış"
    const val SAYIM = "Sayım"
}
