package com.satinalmapro.android.core.model

import com.google.gson.annotations.SerializedName
import java.util.UUID

/** Masaüstü agrega.json ile uyumlu kayıt. */
data class AgregaKaydi(
    val id: String = UUID.randomUUID().toString(),
    val tarih: String = "",
    @SerializedName(value = "faturaNo", alternate = ["FaturaNo"])
    val irsaliyeNo: String = "",
    val agregaTuru: String = "",
    val agregaCinsi: String = "",
    val miktar: Double = 0.0,
    val birim: String = "Ton",
    val birimFiyati: Double = 0.0,
    val toplamTutar: Double = 0.0,
    val artisYuzdesi: Double? = null,
    val tedarikci: String = "",
    val indirildigiSaha: String = "",
    val teslimAlan: String = "",
    val aciklama: String = "",
    val faturasiKesildi: Boolean = false
) {
    fun hesaplaToplam() = copy(toplamTutar = (miktar * birimFiyati * 100).toLong() / 100.0)

    val artisMetin: String
        get() = when {
            artisYuzdesi == null -> "—"
            artisYuzdesi > 0 -> "+${"%.1f".format(artisYuzdesi)}%"
            else -> "${"%.1f".format(artisYuzdesi)}%"
        }

    val faturaDurumu: String get() = if (faturasiKesildi) "Kesildi" else "Kesilmedi"
}

/** Masaüstü cimento.json ile uyumlu kayıt. */
data class CimentoKaydi(
    val id: String = UUID.randomUUID().toString(),
    val tarih: String = "",
    @SerializedName(value = "faturaNo", alternate = ["FaturaNo"])
    val irsaliyeNo: String = "",
    val cimentoSinifi: String = "",
    val cimentoCinsi: String = "",
    val miktar: Double = 0.0,
    val birim: String = "Torba",
    val birimFiyati: Double = 0.0,
    val toplamTutar: Double = 0.0,
    val artisYuzdesi: Double? = null,
    val tedarikci: String = "",
    val indirildigiSaha: String = "",
    val teslimAlan: String = "",
    val aciklama: String = "",
    val faturasiKesildi: Boolean = false
) {
    fun hesaplaToplam() = copy(toplamTutar = (miktar * birimFiyati * 100).toLong() / 100.0)

    val artisMetin: String
        get() = when {
            artisYuzdesi == null -> "—"
            artisYuzdesi > 0 -> "+${"%.1f".format(artisYuzdesi)}%"
            else -> "${"%.1f".format(artisYuzdesi)}%"
        }

    val faturaDurumu: String get() = if (faturasiKesildi) "Kesildi" else "Kesilmedi"
}

/** Masaüstü alinan_malzemeler.json ile uyumlu bağımsız kayıt. */
data class AlinanMalzemeKaydi(
    val id: String = UUID.randomUUID().toString(),
    @SerializedName(value = "tarih", alternate = ["Tarih"])
    val tarih: String = "",
    @SerializedName(value = "faturaNo", alternate = ["FaturaNo"])
    val faturaNo: String = "",
    @SerializedName(value = "kategori", alternate = ["Kategori"])
    val kategori: String = "",
    @SerializedName(value = "malzemeHizmet", alternate = ["MalzemeHizmet"])
    val malzemeHizmet: String = "",
    @SerializedName(value = "miktar", alternate = ["Miktar"])
    val miktar: Double = 0.0,
    @SerializedName(value = "birim", alternate = ["Birim"])
    val birim: String = "",
    @SerializedName(value = "birimFiyati", alternate = ["BirimFiyati"])
    val birimFiyati: Double = 0.0,
    @SerializedName(value = "toplamTutar", alternate = ["ToplamTutar"])
    val toplamTutar: Double = 0.0,
    @SerializedName(value = "artisYuzdesi", alternate = ["ArtisYuzdesi"])
    val artisYuzdesi: Double? = null,
    @SerializedName(value = "tedarikci", alternate = ["Tedarikci"])
    val tedarikci: String = "",
    @SerializedName(value = "indirildigiSaha", alternate = ["IndirildigiSaha"])
    val indirildigiSaha: String = "",
    @SerializedName(value = "teslimAlan", alternate = ["TeslimAlan"])
    val teslimAlan: String = "",
    @SerializedName(value = "aciklama", alternate = ["Aciklama"])
    val aciklama: String = "",
    @SerializedName(value = "satinalmaTalepId", alternate = ["SatinalmaTalepId"])
    val satinalmaTalepId: String? = null,
    @SerializedName(value = "satinalmaKalemId", alternate = ["SatinalmaKalemId"])
    val satinalmaKalemId: String? = null
) {
    fun hesaplaToplam() = copy(toplamTutar = (miktar * birimFiyati * 100).toLong() / 100.0)

    val artisMetin: String
        get() = when {
            artisYuzdesi == null -> "—"
            artisYuzdesi > 0 -> "+${"%.1f".format(artisYuzdesi)}%"
            else -> "${"%.1f".format(artisYuzdesi)}%"
        }
}

enum class ModulKayitTipi(val firestorePath: String, val baslik: String) {
    AGREGA("veri/agrega", "Agrega"),
    CIMENTO("veri/cimento", "Çimento"),
    ALINAN_MALZEME("veri/alinan_malzemeler", "Alınan Malzemeler")
}
