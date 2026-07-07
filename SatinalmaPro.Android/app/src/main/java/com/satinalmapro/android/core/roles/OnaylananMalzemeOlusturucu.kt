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
            val kalemler = talep.kalemler
                .filter { it.malzeme.isNotBlank() }
                .filter { teklifsiz || !it.onaylananTeklifId.isNullOrBlank() }
                .sortedBy { it.siraNo }
            for (kalem in kalemler) {
                liste += satirOlustur(talep, kalem, teklifsiz)
            }
        }
        return liste
    }

    private fun satirOlustur(talep: TalepItem, kalem: TalepKalem, teklifsiz: Boolean): OnaylananMalzemeSatiri {
        val teklifId = kalem.onaylananTeklifId
        if (!teklifsiz && !teklifId.isNullOrBlank()) {
            val teklif = talep.teklifler.firstOrNull { it.id == teklifId }
            val fiyat = teklif?.fiyatlar?.firstOrNull { it.kalemId == kalem.id }
            val siparisNo = talep.firmaSiparisNolari[teklifId]
                ?: talep.siparisNo.ifBlank { talep.talepNo }
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
                siparisMiktari = kalem.miktar,
                kabulEdilenMiktar = kalem.kabulEdilenMiktar,
                siparisTamamlandi = kalem.siparisTamamlandi,
                birim = kalem.birim,
                birimFiyati = fiyat?.birimFiyat ?: 0.0
            )
        }
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
}
