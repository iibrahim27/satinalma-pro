using SatinalmaPro.Models;
using SharedRol = SatinalmaPro.Shared.Models.KullaniciRolleri;

namespace SatinalmaPro.Helpers;

/// <summary>Talep düzenleme / silme — masaüstü.</summary>
public static class SatinalmaTalepYetkileri
{
    public static bool SatinalmaTamYetki(string? rol) =>
        SharedRol.AdminMi(rol)
        || SharedRol.Normalize(rol) == SharedRol.Satinalma;

    public static bool TalepSahibi(SatinalmaTalep talep, string? uid, string? ad) =>
        SatinalmaTalepKuyrugu.KullanicininTalebi(talep, uid, ad);

    /// <summary>Teklif onayı veya sipariş sonrası talep sahibi değiştiremez / silemez.</summary>
    public static bool TalepKilitli(SatinalmaTalep talep) =>
        talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu
        || talep.HerhangiKalemOnayli
        || talep.YonetimOnayKilitli;

    public static bool TalepSahibiFormDuzenlenebilir(SatinalmaTalep talep) =>
        !TalepKilitli(talep);

    /// <summary>Satınalma / admin — kayıtlı her talep (tam yetki).</summary>
    public static bool TalepYonetilebilir(string? rol, SatinalmaTalep talep) =>
        SatinalmaTamYetki(rol)
        && SatinalmaTalepKuyrugu.KayitliTalep(talep);

    public static bool TalepDuzenleyebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return TalepYonetilebilir(rol, talep);

        if (!SharedRol.TalepOlusturabilir(rol))
            return false;

        if (!TalepSahibi(talep, uid, ad))
            return false;

        return TalepSahibiFormDuzenlenebilir(talep);
    }

    public static bool TalepKalemDuzenleyebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return TalepYonetilebilir(rol, talep);

        if (!SharedRol.TalepOlusturabilir(rol) || !TalepSahibi(talep, uid, ad))
            return false;

        return TalepSahibiFormDuzenlenebilir(talep);
    }

    public static bool TalepMetaDuzenleyebilir(string? rol) => SatinalmaTamYetki(rol);

    public static bool TalepSilebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return TalepYonetilebilir(rol, talep);

        if (!SharedRol.TalepOlusturabilir(rol))
            return false;

        if (!TalepSahibi(talep, uid, ad))
            return false;

        return !TalepKilitli(talep);
    }

    public static bool DuzenlemeSonrasiYenidenGonder(SatinalmaTalep talep) =>
        talep.Durum is not SatinalmaTalepDurumlari.Taslak
            and not SatinalmaTalepDurumlari.Hazirlaniyor;

    public static bool DuzenlemeSonrasiYenidenGonder(string? rol, SatinalmaTalep talep, string? uid, string? ad) =>
        DuzenlemeSonrasiYenidenGonder(talep)
        && (TalepSahibi(talep, uid, ad) || SatinalmaTamYetki(rol));

    public static bool SahipDuzenlemeSonrasiYenidenGonder(SatinalmaTalep talep) =>
        DuzenlemeSonrasiYenidenGonder(talep);

    public static bool YonetimeYenidenGonderebilir(string? rol, SatinalmaTalep talep) =>
        SatinalmaTamYetki(rol) && SatinalmaYonetimGonderimi.YenidenGonderebilir(talep);
}
