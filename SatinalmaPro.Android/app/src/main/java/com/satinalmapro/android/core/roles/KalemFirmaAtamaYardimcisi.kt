package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.KalemFirmaAtamasi
import com.satinalmapro.android.core.model.TalepKalem
import com.satinalmapro.android.core.model.TeklifItem
import kotlin.math.abs

/** Kalem miktarını firmalara bölme — masaüstü/Shared ile aynı kurallar. */
object KalemFirmaAtamaYardimcisi {
    const val TOLERANS = 0.0001

    fun onayliMi(kalem: TalepKalem): Boolean = etkinAtamalar(kalem).isNotEmpty()

    fun etkinAtamalar(kalem: TalepKalem): List<KalemFirmaAtamasi> {
        val liste = kalem.firmaAtamalari
            .filter { it.teklifId.isNotBlank() && it.miktar > TOLERANS }
            .sortedByDescending { it.miktar }
        if (liste.isNotEmpty()) return liste
        val tid = kalem.onaylananTeklifId?.takeIf { it.isNotBlank() } ?: return emptyList()
        return listOf(
            KalemFirmaAtamasi(
                teklifId = tid,
                miktar = kalem.miktar,
                kabulEdilenMiktar = kalem.kabulEdilenMiktar,
                siparisTamamlandi = kalem.siparisTamamlandi
            )
        )
    }

    fun dogrula(kalem: TalepKalem, atamalar: List<KalemFirmaAtamasi>) {
        require(atamalar.isNotEmpty()) { "«${kalem.malzeme}» için en az bir firma ataması gerekli." }
        require(atamalar.all { it.miktar > TOLERANS }) {
            "«${kalem.malzeme}» atama miktarları sıfırdan büyük olmalı."
        }
        require(atamalar.map { it.teklifId.lowercase() }.distinct().size == atamalar.size) {
            "«${kalem.malzeme}» için aynı firma birden fazla kez seçilemez."
        }
        val toplam = atamalar.sumOf { it.miktar }
        require(abs(toplam - kalem.miktar) <= TOLERANS) {
            "«${kalem.malzeme}» atama toplamı (${"%.2f".format(toplam)}) talep miktarına (${"%.2f".format(kalem.miktar)}) eşit olmalı."
        }
    }

    fun uygula(kalem: TalepKalem, atamalar: List<KalemFirmaAtamasi>): TalepKalem {
        val liste = atamalar
            .filter { it.teklifId.isNotBlank() && it.miktar > TOLERANS }
            .map { it.copy() }
        dogrula(kalem, liste)
        val ana = liste.maxByOrNull { it.miktar }!!.teklifId
        return kabulOzetiniSenkronla(
            kalem.copy(firmaAtamalari = liste, onaylananTeklifId = ana)
        )
    }

    fun tekFirmayaAta(kalem: TalepKalem, teklifId: String): TalepKalem =
        uygula(kalem, listOf(KalemFirmaAtamasi(teklifId = teklifId, miktar = kalem.miktar)))

    fun temizle(kalem: TalepKalem): TalepKalem = kalem.copy(
        firmaAtamalari = emptyList(),
        onaylananTeklifId = null,
        kabulEdilenMiktar = 0.0,
        siparisTamamlandi = false
    )

    fun kabulEkle(kalem: TalepKalem, teklifId: String, miktar: Double): TalepKalem {
        val atamalar = etkinAtamalar(kalem).map { it.copy() }.toMutableList()
        val idx = atamalar.indexOfFirst { it.teklifId.equals(teklifId, true) }
        require(idx >= 0) { "Firma ataması bulunamadı." }
        var atama = atamalar[idx]
        var yeniKabul = atama.kabulEdilenMiktar + miktar
        var yeniMiktar = atama.miktar
        if (yeniKabul > yeniMiktar + TOLERANS) yeniMiktar = yeniKabul
        val tamam = yeniKabul >= yeniMiktar - TOLERANS
        atamalar[idx] = atama.copy(
            miktar = yeniMiktar,
            kabulEdilenMiktar = yeniKabul,
            siparisTamamlandi = tamam
        )
        val ana = kalem.onaylananTeklifId?.takeIf { it.isNotBlank() }
            ?: atamalar.maxByOrNull { it.miktar }!!.teklifId
        return kabulOzetiniSenkronla(kalem.copy(firmaAtamalari = atamalar, onaylananTeklifId = ana))
    }

    fun sevkiyatiTamamla(kalem: TalepKalem, teklifId: String): TalepKalem {
        val atamalar = etkinAtamalar(kalem).map { it.copy() }.toMutableList()
        val idx = atamalar.indexOfFirst { it.teklifId.equals(teklifId, true) }
        require(idx >= 0) { "Firma ataması bulunamadı." }
        var atama = atamalar[idx]
        if (atama.kabulEdilenMiktar < atama.miktar - TOLERANS) {
            atama = atama.copy(miktar = atama.kabulEdilenMiktar)
        }
        atamalar[idx] = atama.copy(siparisTamamlandi = true)
        return kabulOzetiniSenkronla(kalem.copy(firmaAtamalari = atamalar))
    }

    fun kabulOzetiniSenkronla(kalem: TalepKalem): TalepKalem {
        val atamalar = kalem.firmaAtamalari.filter { it.teklifId.isNotBlank() && it.miktar > TOLERANS }
        if (atamalar.isEmpty()) return kalem
        val kabul = atamalar.sumOf { it.kabulEdilenMiktar }
        val tamam = atamalar.all {
            it.siparisTamamlandi || it.kabulEdilenMiktar >= it.miktar - TOLERANS
        }
        val atamaToplam = atamalar.sumOf { it.miktar }
        return kalem.copy(
            kabulEdilenMiktar = kabul,
            siparisTamamlandi = tamam,
            miktar = if (atamaToplam > TOLERANS && abs(atamaToplam - kalem.miktar) > TOLERANS)
                atamaToplam else kalem.miktar
        )
    }

    fun ozetMetni(kalem: TalepKalem, teklifler: List<TeklifItem>): String {
        val atamalar = etkinAtamalar(kalem)
        if (atamalar.isEmpty()) return "Firma seçilmedi"
        return atamalar.joinToString(" + ") { a ->
            val firma = teklifler.firstOrNull { it.id.equals(a.teklifId, true) }?.firmaAdi ?: "?"
            "${"%.2f".format(a.miktar)} $firma"
        }
    }

    /** Firma miktarını ayarlar; diğer firmaların payını kalan miktara oransal ölçekler. */
    fun firmaMiktariniAyarla(kalem: TalepKalem, teklifId: String, miktar: Double): TalepKalem {
        require(miktar > TOLERANS) { "Miktar sıfırdan büyük olmalı." }
        require(miktar <= kalem.miktar + TOLERANS) { "Miktar talep miktarını aşamaz." }
        val diger = etkinAtamalar(kalem)
            .filter { !it.teklifId.equals(teklifId, true) }
            .map { it.copy() }
        val kalan = kalem.miktar - miktar
        if (diger.isEmpty()) {
            require(abs(miktar - kalem.miktar) <= TOLERANS) {
                "Tek firmada miktar talep toplamına eşit olmalı. Başka firma ekleyin veya tüm miktarı seçin."
            }
            return uygula(kalem, listOf(KalemFirmaAtamasi(teklifId = teklifId, miktar = miktar)))
        }
        val sonuc = mutableListOf<KalemFirmaAtamasi>()
        if (kalan > TOLERANS) {
            val digerToplam = diger.sumOf { it.miktar }
            if (digerToplam <= TOLERANS) {
                sonuc += diger.first().copy(miktar = kalan)
            } else {
                for (a in diger) {
                    val m = a.miktar / digerToplam * kalan
                    if (m > TOLERANS) sonuc += a.copy(miktar = m)
                }
                val fark = kalan - sonuc.sumOf { it.miktar }
                if (sonuc.isNotEmpty()) {
                    sonuc[0] = sonuc[0].copy(miktar = sonuc[0].miktar + fark)
                }
            }
        }
        sonuc += KalemFirmaAtamasi(teklifId = teklifId, miktar = miktar)
        return uygula(kalem, sonuc)
    }
}
