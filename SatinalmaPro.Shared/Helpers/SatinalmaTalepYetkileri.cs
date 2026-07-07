using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Talep düzenleme / silme — satınalma+admin tam yetki; diğer roller yalnız kendi talebi.</summary>
public static class SatinalmaTalepYetkileri
{
    public static bool SatinalmaTamYetki(string? rol) =>
        KullaniciRolleri.AdminMi(rol)
        || KullaniciRolleri.Normalize(rol) == KullaniciRolleri.Satinalma;

    public static bool TalepSahibi(SatinalmaTalep talep, string? uid, string? ad) =>
        SatinalmaTalepKuyrugu.KullanicininTalebi(talep, uid, ad);

    public static bool TalepKilitli(SatinalmaTalep talep) =>
        talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu
        || talep.HerhangiKalemOnayli
        || talep.YonetimOnayKilitli;

    public static bool TalepSahibiFormDuzenlenebilir(SatinalmaTalep talep) =>
        !TalepKilitli(talep);

    public static bool TalepYonetilebilir(string? rol, SatinalmaTalep talep) =>
        SatinalmaTamYetki(rol)
        && SatinalmaTalepKuyrugu.KayitliTalep(talep);

    public static bool TalepDuzenleyebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return TalepYonetilebilir(rol, talep);

        if (!KullaniciRolleri.TalepOlusturabilir(rol))
            return false;

        if (!TalepSahibi(talep, uid, ad))
            return false;

        return TalepSahibiFormDuzenlenebilir(talep);
    }

    public static bool TalepKalemDuzenleyebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return TalepYonetilebilir(rol, talep);

        if (!KullaniciRolleri.TalepOlusturabilir(rol) || !TalepSahibi(talep, uid, ad))
            return false;

        return TalepSahibiFormDuzenlenebilir(talep);
    }

    public static bool TalepMetaDuzenleyebilir(string? rol) => SatinalmaTamYetki(rol);

    public static bool TalepSilebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return TalepYonetilebilir(rol, talep);

        if (!KullaniciRolleri.TalepOlusturabilir(rol))
            return false;

        if (!TalepSahibi(talep, uid, ad))
            return false;

        return !TalepKilitli(talep);
    }

    public static bool YonetimeYenidenGonderebilir(string? rol, SatinalmaTalep talep) =>
        SatinalmaTamYetki(rol) && SatinalmaIsAkisi.YonetimeYenidenGonderebilir(talep);
}
