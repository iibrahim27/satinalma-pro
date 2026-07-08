package com.satinalmapro.android.core.helpers

import com.satinalmapro.android.core.model.BildirimRecord
import com.satinalmapro.android.core.model.BildirimTipleri
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.roles.KullaniciRolleri

/** Yönetim yalnızca: saha talebi + teklif onayı bildirimlerini görür. */
object BildirimRolPolitikasi {
    fun islemYapanKendisiMi(record: BildirimRecord, user: UserProfile?): Boolean =
        user != null &&
            record.olusturanUid.isNotBlank() &&
            record.olusturanUid.equals(user.uid, ignoreCase = true)

    /** WhatsApp/Instagram kuralı: işlemi yapan kişiye kayıt/push oluşturulmaz. */
    fun kayitGonderilmeli(hedefRol: String?, hedefUid: String?, islemYapanUid: String?): Boolean {
        if (islemYapanUid.isNullOrBlank())
            return !hedefRol.isNullOrBlank() || !hedefUid.isNullOrBlank()
        if (!hedefUid.isNullOrBlank() && hedefUid.equals(islemYapanUid, ignoreCase = true))
            return false
        return !hedefRol.isNullOrBlank() || !hedefUid.isNullOrBlank()
    }

    fun kayitGonderilmeli(record: BildirimRecord): Boolean =
        kayitGonderilmeli(record.hedefRol, record.hedefUid, record.olusturanUid)

    fun rolTipGorebilirMi(rol: String?, tip: String): Boolean {
        if (KullaniciRolleri.isAdmin(rol)) return true
        val r = KullaniciRolleri.normalize(rol)
        val t = normalizeTip(tip)
        return when (t) {
            BildirimTipleri.YONETIME_GONDERILDI ->
                r == KullaniciRolleri.YONETIM
            BildirimTipleri.TEKLIF_ONAYDA -> r == KullaniciRolleri.YONETIM
            BildirimTipleri.TEKLIF_ISTENDI, BildirimTipleri.TEKLIF_DUZELTME_ISTENDI ->
                r == KullaniciRolleri.SATINALMA
            BildirimTipleri.ONAYLANDI -> r == KullaniciRolleri.SATINALMA
            BildirimTipleri.REDDEDILDI -> r == KullaniciRolleri.SATINALMA
            BildirimTipleri.SIPARIS_OLUSTURULDU ->
                r == KullaniciRolleri.SATINALMA || r == KullaniciRolleri.DEPO || r == KullaniciRolleri.ATOLYE
            BildirimTipleri.MAL_KABUL_EDILDI -> r == KullaniciRolleri.SATINALMA
            else -> false
        }
    }

    fun kullaniciyaMi(record: BildirimRecord, user: UserProfile?): Boolean {
        if (user == null) return false
        if (islemYapanKendisiMi(record, user)) return false

        if (!record.hedefUid.isNullOrBlank()) {
            return record.hedefUid.equals(user.uid, ignoreCase = true)
        }

        if (!record.inboxDocId.isNullOrBlank()) {
            return rolTipGorebilirMi(user.role, record.tip)
        }

        if (KullaniciRolleri.isAdmin(user.role)) return true

        if (!record.hedefRol.isNullOrBlank()) {
            val rol = KullaniciRolleri.normalize(user.role)
            return KullaniciRolleri.normalize(record.hedefRol) == rol &&
                rolTipGorebilirMi(rol, record.tip)
        }

        return false
    }

    fun yonetimeGonderildiHedefleri(): List<Pair<String?, String?>> =
        listOf(KullaniciRolleri.YONETIM to null)

    fun reddedildiHedefleri(talepOlusturanUid: String?, islemYapanUid: String?): List<Pair<String?, String?>> {
        val hedefler = mutableListOf<Pair<String?, String?>>()
        hedefler += KullaniciRolleri.SATINALMA to null
        if (!talepOlusturanUid.isNullOrBlank() &&
            !talepOlusturanUid.equals(islemYapanUid, ignoreCase = true)
        ) {
            hedefler += null to talepOlusturanUid
        }
        return hedefler
    }

    fun siparisOlusturulduHedefleri(talepOlusturanUid: String?, islemYapanUid: String?): List<Pair<String?, String?>> {
        val hedefler = mutableListOf<Pair<String?, String?>>()
        hedefler += KullaniciRolleri.SATINALMA to null
        hedefler += KullaniciRolleri.DEPO to null
        hedefler += KullaniciRolleri.ATOLYE to null
        if (!talepOlusturanUid.isNullOrBlank() &&
            !talepOlusturanUid.equals(islemYapanUid, ignoreCase = true)
        ) {
            hedefler += null to talepOlusturanUid
        }
        return hedefler
    }

    fun malKabulEdildiHedefleri(talepOlusturanUid: String?, islemYapanUid: String?): List<Pair<String?, String?>> {
        val hedefler = mutableListOf<Pair<String?, String?>>()
        hedefler += KullaniciRolleri.SATINALMA to null
        if (!talepOlusturanUid.isNullOrBlank() &&
            !talepOlusturanUid.equals(islemYapanUid, ignoreCase = true)
        ) {
            hedefler += null to talepOlusturanUid
        }
        return hedefler
    }

    private fun normalizeTip(tip: String): String = when (tip.lowercase()) {
        "yonetimegonderildi" -> BildirimTipleri.YONETIME_GONDERILDI
        "teklifistendi" -> BildirimTipleri.TEKLIF_ISTENDI
        "teklifonayda" -> BildirimTipleri.TEKLIF_ONAYDA
        "teklifduzeltmeistendi" -> BildirimTipleri.TEKLIF_DUZELTME_ISTENDI
        "onaylandi" -> BildirimTipleri.ONAYLANDI
        "reddedildi" -> BildirimTipleri.REDDEDILDI
        "siparisolusturuldu" -> BildirimTipleri.SIPARIS_OLUSTURULDU
        "malkabuledildi" -> BildirimTipleri.MAL_KABUL_EDILDI
        else -> tip.lowercase()
    }
}
