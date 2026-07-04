package com.satinalmapro.android.core.model

import com.google.gson.annotations.SerializedName

data class StokKaydi(
    @SerializedName("MalzemeAdi") val malzemeAdi: String = "",
    @SerializedName("Kategori") val kategori: String = "",
    @SerializedName("Birim") val birim: String = "Adet",
    @SerializedName("MevcutMiktar") val mevcutMiktar: Double = 0.0,
    @SerializedName("MinimumStok") val minimumStok: Double = 0.0,
    @SerializedName("DepoSaha") val depoSaha: String = "",
    @SerializedName("BirimMaliyet") val birimMaliyet: Double = 0.0,
    @SerializedName("ToplamDeger") val toplamDeger: Double = 0.0,
    @SerializedName("SonGuncelleme") val sonGuncelleme: String = "",
    @SerializedName("Aciklama") val aciklama: String = ""
) {
    val durumMetin: String
        get() = when {
            mevcutMiktar <= 0 -> "Tükendi"
            minimumStok > 0 && mevcutMiktar <= minimumStok -> "Kritik"
            else -> "Normal"
        }
}

data class StokHareket(
    @SerializedName("Id") val id: String = "",
    @SerializedName("Tarih") val tarih: String = "",
    @SerializedName("HareketTipi") val hareketTipi: String = "",
    @SerializedName("MalzemeAdi") val malzemeAdi: String = "",
    @SerializedName("Kategori") val kategori: String = "",
    @SerializedName("Birim") val birim: String = "",
    @SerializedName("Miktar") val miktar: Double = 0.0,
    @SerializedName("DepoSaha") val depoSaha: String = "",
    @SerializedName("BirimMaliyet") val birimMaliyet: Double = 0.0,
    @SerializedName("BelgeNo") val belgeNo: String = "",
    @SerializedName("IslemYapan") val islemYapan: String = "",
    @SerializedName("TeslimEdilen") val teslimEdilen: String = "",
    @SerializedName("Aciklama") val aciklama: String = ""
)

object StokHareketTipi {
    const val GIRIS = "Giriş"
    const val CIKIS = "Çıkış"
    const val SAYIM = "Sayım"
}
