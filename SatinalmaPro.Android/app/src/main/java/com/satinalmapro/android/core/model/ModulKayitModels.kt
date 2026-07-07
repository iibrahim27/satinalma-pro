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
    val tarih: String = "",
    val faturaNo: String = "",
    val kategori: String = "",
    val malzemeHizmet: String = "",
    val miktar: Double = 0.0,
    val birim: String = "",
    val birimFiyati: Double = 0.0,
    val toplamTutar: Double = 0.0,
    val artisYuzdesi: Double? = null,
    val tedarikci: String = "",
    val indirildigiSaha: String = "",
    val teslimAlan: String = "",
    val aciklama: String = "",
    val satinalmaTalepId: String? = null,
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
