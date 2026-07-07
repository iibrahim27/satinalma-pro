package com.satinalmapro.android.core.roles

import com.satinalmapro.android.core.model.TalepItem

/** Masaüstü SatinalmaTalepYetkileri ile uyumlu talep düzenleme / silme kuralları. */
object TalepYetkileri {
    fun satinalmaTamYetki(rol: String?): Boolean =
        KullaniciRolleri.isAdmin(rol) || KullaniciRolleri.normalize(rol) == KullaniciRolleri.SATINALMA

    fun talepSahibi(talep: TalepItem, uid: String?, ad: String?): Boolean {
        if (!uid.isNullOrBlank() && talep.olusturanUid.equals(uid, true)) return true
        if (!ad.isNullOrBlank() && talep.talepEden.equals(ad, true)) return true
        return false
    }

    /** Teklif onayı veya sipariş sonrası değiştirme / silme kilitlenir. */
    fun talepKilitli(talep: TalepItem): Boolean =
        talep.durum == TalepDurumlari.SIPARIS ||
            talep.herhangiKalemOnayli ||
            talep.yonetimOnayKilitli

    fun formDuzenlenebilir(talep: TalepItem): Boolean =
        talep.durum == TalepDurumlari.TASLAK ||
            talep.durum in setOf(TalepDurumlari.HAZIRLANIYOR, TalepDurumlari.REDDEDILDI)

    fun talepSahibiFormDuzenlenebilir(talep: TalepItem): Boolean =
        !talepKilitli(talep)

    fun talepKalemleriDuzenlenebilir(talep: TalepItem): Boolean =
        formDuzenlenebilir(talep) || TalepKuyrugu.teklifDuzenlemeDevamEdiyor(talep)

    /** Satınalma / admin — kayıtlı her talep (kilit yok). */
    fun talepYonetilebilir(rol: String?, talep: TalepItem): Boolean =
        satinalmaTamYetki(rol) && TalepKuyrugu.kayitli(talep)

    fun talepDuzenleyebilir(rol: String?, talep: TalepItem, uid: String?, ad: String?): Boolean {
        if (satinalmaTamYetki(rol))
            return talepYonetilebilir(rol, talep)
        if (!KullaniciRolleri.canCreateRequest(rol)) return false
        if (!talepSahibi(talep, uid, ad)) return false
        return talepSahibiFormDuzenlenebilir(talep)
    }

    fun talepSilebilir(rol: String?, talep: TalepItem, uid: String?, ad: String?): Boolean {
        if (satinalmaTamYetki(rol))
            return talepYonetilebilir(rol, talep)
        if (!KullaniciRolleri.canCreateRequest(rol)) return false
        if (!talepSahibi(talep, uid, ad)) return false
        return !talepKilitli(talep)
    }

    fun duzenlemeSonrasiYenidenGonder(talep: TalepItem): Boolean =
        talep.durum !in setOf(TalepDurumlari.TASLAK, TalepDurumlari.HAZIRLANIYOR)

    fun duzenlemeSonrasiYenidenGonder(
        rol: String?,
        talep: TalepItem,
        uid: String?,
        ad: String?
    ): Boolean =
        duzenlemeSonrasiYenidenGonder(talep) &&
            (talepSahibi(talep, uid, ad) || satinalmaTamYetki(rol))
}
