package com.satinalmapro.android.core.helpers

import com.satinalmapro.android.core.model.BildirimRecord
import com.satinalmapro.android.core.model.UserProfile

/** Aynı talep+tip+hedef için yinelenen bildirimleri birleştirir. */
object BildirimTekillestirme {
    fun tekille(kaynak: Collection<BildirimRecord>): List<BildirimRecord> =
        kaynak
            .groupBy { BildirimMantikAnahtari.olustur(it) }
            .values
            .map { birlestirGrup(it) }
            .sortedWith(
                compareByDescending<BildirimRecord> { it.guncellemeUtc }
                    .thenByDescending { it.olusturmaTarihi }
            )

    fun inboxIleBirlestir(
        legacy: List<BildirimRecord>,
        inbox: List<BildirimRecord>,
        user: UserProfile?,
        kullaniciyaMi: (BildirimRecord, UserProfile?) -> Boolean
    ): List<BildirimRecord> {
        val sonuc = inbox.toMutableList()
        for (l in legacy) {
            if (inbox.any { BildirimMantikAnahtari.olustur(it) == BildirimMantikAnahtari.olustur(l) }) continue
            if (user != null && !kullaniciyaMi(l, user)) continue
            sonuc.add(l)
        }
        return tekille(sonuc)
    }

    private fun birlestirGrup(grup: Collection<BildirimRecord>): BildirimRecord {
        val sirali = grup.sortedWith(
            compareByDescending<BildirimRecord> { it.guncellemeUtc }
                .thenByDescending { it.olusturmaTarihi }
        )
        var birincil = sirali.first()
        sirali.drop(1).forEach { diger ->
            birincil = birincil.copy(
                okundu = birincil.okundu || diger.okundu,
                guncellemeUtc = maxOf(birincil.guncellemeUtc, diger.guncellemeUtc),
                inboxDocId = birincil.inboxDocId?.takeIf { it.isNotBlank() } ?: diger.inboxDocId,
                baslik = birincil.baslik.ifBlank { diger.baslik },
                mesaj = birincil.mesaj.ifBlank { diger.mesaj }
            )
        }
        return birincil
    }
}
