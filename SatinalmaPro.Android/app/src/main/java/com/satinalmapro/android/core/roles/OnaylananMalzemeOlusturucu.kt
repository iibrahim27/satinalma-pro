package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.OnaylananMalzemeSatiri
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepKalem

object OnaylananMalzemeOlusturucu {

    fun malKabulTalep(talep: TalepItem): Boolean =
        talep.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS) &&
            (talep.herhangiKalemOnayli || talep.teklifsizYonetimOnayi)

    fun siparisVerBekleyen(satir: OnaylananMalzemeSatiri): Boolean =
        satir.durum == TalepDurumlari.ONAYLANDI &&
            !satir.siparisTamamlandi &&
            satir.kalanMiktar > 0.0001

    fun malKabulBekleyen(satir: OnaylananMalzemeSatiri): Boolean =
        !satir.siparisTamamlandi && satir.durum == TalepDurumlari.SIPARIS

    fun sevkiyatTamamlanabilir(satir: OnaylananMalzemeSatiri): Boolean =
        !satir.siparisTamamlandi &&
            satir.kabulEdilenMiktar > 0.0001 &&
            satir.kabulEdilenMiktar < satir.siparisMiktari - 0.0001 &&
            satir.durum == TalepDurumlari.SIPARIS

    fun olustur(talepler: List<TalepItem>): List<OnaylananMalzemeSatiri> {
        val liste = mutableListOf<OnaylananMalzemeSatiri>()
        for (talep in talepler.filter(::malKabulTalep)) {
            val teklifsiz = talep.teklifsizYonetimOnayi && !talep.herhangiKalemOnayli
            @Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
            val kalemler = ((talep.kalemler as List<TalepKalem>?) ?: emptyList())
                .filter { it.malzeme.isNotBlank() }
                .filter { teklifsiz || KalemFirmaAtamaYardimcisi.onayliMi(it) }
                .sortedBy { it.siraNo }
            for (kalem in kalemler) {
                if (teklifsiz && !KalemFirmaAtamaYardimcisi.onayliMi(kalem)) {
                    liste += teklifsizSatir(talep, kalem)
                    continue
                }
                for (atama in KalemFirmaAtamaYardimcisi.etkinAtamalar(kalem)) {
                    liste += atamaSatiri(talep, kalem, atama.teklifId, atama.miktar, atama.kabulEdilenMiktar, atama.siparisTamamlandi)
                }
            }
        }
        return liste
    }

    private fun teklifsizSatir(talep: TalepItem, kalem: TalepKalem): OnaylananMalzemeSatiri {
        val siparisNo = talep.siparisNo.ifBlank { talep.talepNo }
        return OnaylananMalzemeSatiri(
            talepId = talep.id,
            kalemId = kalem.id,
            talepNo = talep.talepNo,
            siparisNo = siparisNo,
            tarih = talep.tarih,
            durum = talep.durum,
            malzeme = kalem.malzeme,
            siparisMiktari = kalem.miktar,
            kabulEdilenMiktar = kalem.kabulEdilenMiktar,
            siparisTamamlandi = kalem.siparisTamamlandi,
            birim = kalem.birim
        )
    }

    private fun atamaSatiri(
        talep: TalepItem,
        kalem: TalepKalem,
        teklifId: String,
        siparisMiktari: Double,
        kabulEdilenMiktar: Double,
        siparisTamamlandi: Boolean
    ): OnaylananMalzemeSatiri {
        @Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
        val teklifler = (talep.teklifler as List<com.satinalmapro.android.core.model.TeklifItem>?) ?: emptyList()
        val teklif = teklifler.firstOrNull { it.id.equals(teklifId, true) }
        @Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
        val fiyatlar = (teklif?.fiyatlar as List<com.satinalmapro.android.core.model.TeklifFiyat>?) ?: emptyList()
        val fiyat = fiyatlar.firstOrNull { it.kalemId == kalem.id }
        @Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
        val siparisMap = (talep.firmaSiparisNolari as Map<String, String>?) ?: emptyMap()
        val siparisNo = siparisMap[teklifId] ?: talep.siparisNo.ifBlank { talep.talepNo }
        val birimFiyat = fiyat?.birimFiyat ?: 0.0
        return OnaylananMalzemeSatiri(
            talepId = talep.id,
            kalemId = kalem.id,
            teklifId = teklifId,
            talepNo = talep.talepNo,
            siparisNo = siparisNo,
            tarih = talep.tarih,
            durum = talep.durum,
            firma = teklif?.firmaAdi.orEmpty(),
            marka = fiyat?.marka?.ifBlank { teklif?.marka.orEmpty() }.orEmpty(),
            malzeme = kalem.malzeme,
            siparisMiktari = siparisMiktari,
            kabulEdilenMiktar = kabulEdilenMiktar,
            siparisTamamlandi = siparisTamamlandi,
            birim = kalem.birim,
            birimFiyati = birimFiyat
        )
    }
}
