package com.satinalmapro.android.core.model

import com.google.gson.annotations.SerializedName

data class TalepKalem(
    @SerializedName("Id") val id: String = "",
    @SerializedName("SiraNo") val siraNo: Int = 1,
    @SerializedName("Malzeme") val malzeme: String = "",
    @SerializedName("Miktar") val miktar: Double = 0.0,
    @SerializedName("Birim") val birim: String = "Adet",
    @SerializedName("Aciklama") val aciklama: String = "",
    @SerializedName("OnaylananTeklifId") val onaylananTeklifId: String? = null,
    @SerializedName("KabulEdilenMiktar") val kabulEdilenMiktar: Double = 0.0,
    @SerializedName("SiparisTamamlandi") val siparisTamamlandi: Boolean = false
) {
    val kalanMiktar: Double get() = (miktar - kabulEdilenMiktar).coerceAtLeast(0.0)
}

data class TeklifFiyat(
    @SerializedName("KalemId") val kalemId: String = "",
    @SerializedName("Marka") val marka: String = "",
    @SerializedName("ParaBirimi") val paraBirimi: String = "TRY",
    @SerializedName("BirimFiyat") val birimFiyat: Double = 0.0,
    @SerializedName("KdvOrani") val kdvOrani: Double = 20.0,
    @SerializedName("ToplamTutar") val toplamTutar: Double = 0.0,
    @SerializedName("KdvTutari") val kdvTutari: Double = 0.0,
    @SerializedName("ToplamKdvDahil") val toplamKdvDahil: Double = 0.0
)

data class TeklifItem(
    @SerializedName("Id") val id: String = "",
    @SerializedName("FirmaAdi") val firmaAdi: String = "",
    @SerializedName("Marka") val marka: String = "",
    @SerializedName("VadeGunu") val vadeGunu: Int = 0,
    @SerializedName("TeslimSuresi") val teslimSuresi: String = "",
    @SerializedName("OdemeSekli") val odemeSekli: String = "",
    @SerializedName("KdvOrani") val kdvOrani: Double = 20.0,
    @SerializedName("Aciklama") val aciklama: String = "",
    @SerializedName("UsdKuru") val usdKuru: Double = 0.0,
    @SerializedName("EurKuru") val eurKuru: Double = 0.0,
    @SerializedName("Onaylandi") val onaylandi: Boolean = false,
    @SerializedName("Fiyatlar") val fiyatlar: List<TeklifFiyat> = emptyList()
) {
    val genelToplam: Double get() = fiyatlar.sumOf { it.toplamKdvDahil }
}

data class TalepItem(
    @SerializedName("Id") val id: String = "",
    @SerializedName("TalepNo") val talepNo: String = "",
    @SerializedName("Tarih") val tarih: String = "",
    @SerializedName("TalepEden") val talepEden: String = "",
    @SerializedName("SantiyeAdi") val santiyeAdi: String = "",
    @SerializedName("TalepAciklamasi") val talepAciklamasi: String = "",
    @SerializedName("TalepTuru") val talepTuru: String = "Normal",
    @SerializedName("OlusturanUid") val olusturanUid: String = "",
    @SerializedName("OlusturanRol") val olusturanRol: String = "",
    @SerializedName("RedGerekcesi") val redGerekcesi: String = "",
    @SerializedName("Durum") val durum: String = "Taslak",
    @SerializedName("GuncellemeUtc") val guncellemeUtc: Long = 0,
    @SerializedName("TeklifsizYonetimOnayi") val teklifsizYonetimOnayi: Boolean = false,
    @SerializedName("YonetimOnayKilitli") val yonetimOnayKilitli: Boolean = false,
    @SerializedName("YonetimOnaylayanAd") val yonetimOnaylayanAd: String = "",
    @SerializedName("SiparisNo") val siparisNo: String = "",
    @SerializedName("Kalemler") val kalemler: List<TalepKalem> = emptyList(),
    @SerializedName("Teklifler") val teklifler: List<TeklifItem> = emptyList()
) {
    val malzemeOzeti: String
        get() = kalemler.firstOrNull()?.malzeme?.ifBlank { talepAciklamasi } ?: talepAciklamasi

    val miktarOzeti: String
        get() = kalemler.firstOrNull()?.let { "${it.miktar} ${it.birim}" } ?: ""

    val herhangiKalemOnayli: Boolean
        get() = kalemler.any { !it.onaylananTeklifId.isNullOrBlank() }

    val teklifGirilmis: Boolean
        get() = teklifler.any { it.firmaAdi.isNotBlank() || it.fiyatlar.any { f -> f.birimFiyat > 0 } }
}

data class SatinalmaAyarlar(
    @SerializedName("SonTalepSira") var sonTalepSira: Int = 0,
    @SerializedName("SonSiparisSira") var sonSiparisSira: Int = 0,
    @SerializedName("SilinenTalepIdleri") val silinenTalepIdleri: List<String> = emptyList()
)

data class DashboardCard(
    val title: String,
    val value: String,
    val subtitle: String,
    val route: String
)

data class DashboardActivity(
    val title: String,
    val subtitle: String,
    val status: String,
    val route: String,
    val talepId: String?
)

enum class TalepQueue {
    TALEPLERIM,
    ONAY_BEKLEYEN,
    ONAYLANAN_TALEPLER,
    GELEN_TALEPLER,
    TEKLIF_BEKLEYEN,
    GECMIS_TALEPLER,
    GECMIS_TEKLIFLI,
    ONAY_GECMISI,
    RED_TALEPLER,
    ONAYLANAN_TEKLIFLER,
    TEKLIF_GIR,
    TEKLIF_KARSILASTIRMA,
    TEKLIF_ONAY,
    TEKLIFSIZ_FIRMA_FIYAT
}
