package com.satinalmapro.android.core.model

data class TalepKalem(
    val id: String = "",
    val siraNo: Int = 1,
    val malzeme: String = "",
    val miktar: Double = 0.0,
    val birim: String = "Adet",
    val aciklama: String = "",
    val onaylananTeklifId: String? = null,
    val kabulEdilenMiktar: Double = 0.0,
    val siparisTamamlandi: Boolean = false
) {
    val kalanMiktar: Double get() = (miktar - kabulEdilenMiktar).coerceAtLeast(0.0)
}

data class TeklifFiyat(
    val kalemId: String = "",
    val marka: String = "",
    val paraBirimi: String = "TRY",
    val birimFiyat: Double = 0.0,
    val kdvOrani: Double = 20.0,
    val toplamTutar: Double = 0.0,
    val kdvTutari: Double = 0.0,
    val toplamKdvDahil: Double = 0.0
)

data class TeklifItem(
    val id: String = "",
    val firmaAdi: String = "",
    val marka: String = "",
    val vadeGunu: Int = 0,
    val teslimSuresi: String = "",
    val odemeSekli: String = "",
    val kdvOrani: Double = 20.0,
    val aciklama: String = "",
    val usdKuru: Double = 0.0,
    val eurKuru: Double = 0.0,
    val onaylandi: Boolean = false,
    val fiyatlar: List<TeklifFiyat> = emptyList()
) {
    val genelToplam: Double get() = fiyatlar.sumOf { it.toplamKdvDahil }
}

data class TalepItem(
    val id: String = "",
    val talepNo: String = "",
    val tarih: String = "",
    val talepEden: String = "",
    val santiyeAdi: String = "",
    val talepAciklamasi: String = "",
    val talepTuru: String = "Normal",
    val olusturanUid: String = "",
    val olusturanRol: String = "",
    val redGerekcesi: String = "",
    val durum: String = "Taslak",
    val guncellemeUtc: Long = 0,
    val teklifsizYonetimOnayi: Boolean = false,
    val yonetimOnayKilitli: Boolean = false,
    val yonetimOnaylayanAd: String = "",
    val siparisNo: String = "",
    val kalemler: List<TalepKalem> = emptyList(),
    val teklifler: List<TeklifItem> = emptyList()
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
    var sonTalepSira: Int = 0,
    var sonSiparisSira: Int = 0,
    val silinenTalepIdleri: List<String> = emptyList()
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
