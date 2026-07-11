package com.satinalmapro.android.core.model

import com.google.gson.annotations.SerializedName

/** Gson reflection Kotlin non-null alanlara null yazabiliyor; runtime NPE önlemi. */
@Suppress("UNCHECKED_CAST", "USELESS_ELVIS", "UNNECESSARY_SAFE_CALL")
internal object GsonNulls {
    fun s(value: String?): String = value ?: ""
    fun <T> list(value: List<T>?): List<T> = value ?: emptyList()
    fun <K, V> map(value: Map<K, V>?): Map<K, V> = value ?: emptyMap()
}

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

    fun normalized(): TalepKalem = copy(
        id = GsonNulls.s(id as String?),
        malzeme = GsonNulls.s(malzeme as String?),
        birim = GsonNulls.s(birim as String?).ifBlank { "Adet" },
        aciklama = GsonNulls.s(aciklama as String?),
        onaylananTeklifId = onaylananTeklifId?.takeIf { it.isNotBlank() }
    )
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
) {
    fun normalized(): TeklifFiyat = copy(
        kalemId = GsonNulls.s(kalemId as String?),
        marka = GsonNulls.s(marka as String?),
        paraBirimi = GsonNulls.s(paraBirimi as String?).ifBlank { "TRY" }
    )
}

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
    private val safeFiyatlar: List<TeklifFiyat>
        get() = GsonNulls.list(fiyatlar as List<TeklifFiyat>?)

    val araToplam: Double get() = safeFiyatlar.sumOf { it.toplamTutar }
    val kdvTutari: Double get() = safeFiyatlar.sumOf { it.kdvTutari }
    val genelToplam: Double get() = safeFiyatlar.sumOf { it.toplamKdvDahil }

    fun normalized(): TeklifItem = copy(
        id = GsonNulls.s(id as String?),
        firmaAdi = GsonNulls.s(firmaAdi as String?),
        marka = GsonNulls.s(marka as String?),
        teslimSuresi = GsonNulls.s(teslimSuresi as String?),
        odemeSekli = GsonNulls.s(odemeSekli as String?),
        aciklama = GsonNulls.s(aciklama as String?),
        fiyatlar = safeFiyatlar.map { it.normalized() }
    )
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
    val status: String = "",
    val priority: String = "",
    val guncellemeUtc: Long = 0,
    val teklifsizYonetimOnayi: Boolean = false,
    val yonetimOnayKilitli: Boolean = false,
    val yonetimOnaylayanUid: String = "",
    val yonetimOnaylayanAd: String = "",
    val yonetimOnaylayanEposta: String = "",
    val yonetimOnayTarihi: String = "",
    val onaylananTeklifId: String? = null,
    val teklifDuzeltmeNotu: String = "",
    val yonetimOnerilenTeklifId: String? = null,
    val satinalmaOnerisiElleSecildi: Boolean = false,
    val siparisNo: String = "",
    val firmaSiparisNolari: Map<String, String> = emptyMap(),
    @SerializedName(value = "hasReturnFlag", alternate = ["HasReturnFlag"])
    val hasReturnFlag: Boolean = false,
    val kalemler: List<TalepKalem> = emptyList(),
    val teklifler: List<TeklifItem> = emptyList()
) {
    private val safeKalemler: List<TalepKalem>
        get() = GsonNulls.list(kalemler as List<TalepKalem>?)

    private val safeTeklifler: List<TeklifItem>
        get() = GsonNulls.list(teklifler as List<TeklifItem>?)

    val malzemeOzeti: String
        get() {
            val aciklama = GsonNulls.s(talepAciklamasi as String?)
            return safeKalemler.firstOrNull()?.let { GsonNulls.s(it.malzeme as String?) }
                ?.ifBlank { aciklama }
                ?: aciklama
        }

    val miktarOzeti: String
        get() = safeKalemler.firstOrNull()?.let {
            "${it.miktar} ${GsonNulls.s(it.birim as String?)}"
        } ?: ""

    val herhangiKalemOnayli: Boolean
        get() = safeKalemler.any { !it.onaylananTeklifId.isNullOrBlank() }

    val teklifGirilmis: Boolean
        get() = safeTeklifler.any { teklif ->
            GsonNulls.s(teklif.firmaAdi as String?).isNotBlank() ||
                GsonNulls.list(teklif.fiyatlar as List<TeklifFiyat>?).any { f -> f.birimFiyat > 0 }
        }

    fun onerilenTeklif(): TeklifItem? {
        val gecerli = safeTeklifler.filter {
            GsonNulls.s(it.firmaAdi as String?).isNotBlank() && it.genelToplam > 0
        }
        if (satinalmaOnerisiElleSecildi && !yonetimOnerilenTeklifId.isNullOrBlank()) {
            return gecerli.firstOrNull { it.id.equals(yonetimOnerilenTeklifId, true) }
        }
        return gecerli.minWithOrNull(
            compareBy<TeklifItem> { it.genelToplam }
                .thenBy { GsonNulls.s(it.firmaAdi as String?).lowercase() }
        )
    }

    fun normalized(): TalepItem = copy(
        id = GsonNulls.s(id as String?),
        talepNo = GsonNulls.s(talepNo as String?),
        tarih = GsonNulls.s(tarih as String?),
        talepEden = GsonNulls.s(talepEden as String?),
        santiyeAdi = GsonNulls.s(santiyeAdi as String?),
        talepAciklamasi = GsonNulls.s(talepAciklamasi as String?),
        talepTuru = GsonNulls.s(talepTuru as String?).ifBlank { "Normal" },
        olusturanUid = GsonNulls.s(olusturanUid as String?),
        olusturanRol = GsonNulls.s(olusturanRol as String?),
        redGerekcesi = GsonNulls.s(redGerekcesi as String?),
        durum = GsonNulls.s(durum as String?).ifBlank { "Taslak" },
        status = GsonNulls.s(status as String?),
        priority = GsonNulls.s(priority as String?),
        yonetimOnaylayanUid = GsonNulls.s(yonetimOnaylayanUid as String?),
        yonetimOnaylayanAd = GsonNulls.s(yonetimOnaylayanAd as String?),
        yonetimOnaylayanEposta = GsonNulls.s(yonetimOnaylayanEposta as String?),
        yonetimOnayTarihi = GsonNulls.s(yonetimOnayTarihi as String?),
        onaylananTeklifId = onaylananTeklifId?.takeIf { it.isNotBlank() },
        teklifDuzeltmeNotu = GsonNulls.s(teklifDuzeltmeNotu as String?),
        yonetimOnerilenTeklifId = yonetimOnerilenTeklifId?.takeIf { it.isNotBlank() },
        siparisNo = GsonNulls.s(siparisNo as String?),
        firmaSiparisNolari = GsonNulls.map(firmaSiparisNolari as Map<String, String>?),
        kalemler = safeKalemler.map { it.normalized() },
        teklifler = safeTeklifler.map { it.normalized() }
    )
}

data class OnaylananMalzemeSatiri(
    val talepId: String,
    val kalemId: String,
    val teklifId: String = "",
    val talepNo: String = "",
    val siparisNo: String = "",
    val tarih: String = "",
    val durum: String = "",
    val firma: String = "",
    val marka: String = "",
    val malzeme: String = "",
    val siparisMiktari: Double = 0.0,
    val kabulEdilenMiktar: Double = 0.0,
    val siparisTamamlandi: Boolean = false,
    val birim: String = "",
    val birimFiyati: Double = 0.0
) {
    val kalanMiktar: Double get() = (siparisMiktari - kabulEdilenMiktar).coerceAtLeast(0.0)

    val kabulDurumu: String
        get() = when {
            siparisTamamlandi || kabulEdilenMiktar >= siparisMiktari -> "Tamamlandı"
            kabulEdilenMiktar > 0 -> "Kısmi"
            else -> "Bekliyor"
        }
}

data class ImzaAyari(
    val unvan: String = "",
    val adSoyad: String = "",
    val aktif: Boolean = true
)

data class SatinalmaAyarlar(
    val firmaAdi: String = "",
    val sartnameMetni: String = "",
    val teklifIstemeSartnameleri: String = "",
    val sefImzalari: List<ImzaAyari> = emptyList(),
    val yonetimImzalari: List<ImzaAyari> = emptyList(),
    var sonTalepSira: Int = 0,
    var sonSiparisSira: Int = 0,
    val silinenTalepIdleri: List<String> = emptyList(),
    val varsayilanUsdKuru: Double = 0.0,
    val varsayilanEurKuru: Double = 0.0
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
    TEKLIFSIZ_FIRMA_FIYAT,
    YONETIM_DIREK_ONAYLANAN,
    SATINALMA_TEKLIF_ISTENEN,
    SATINALMA_TEKLIF_GIRILEN,
    SATINALMA_TEKLIF_DUZELTME,
    SATINALMA_ONAYLANAN,
    SATINALMA_SIPARIS,
    SATINALMA_MAL_KABUL
}
