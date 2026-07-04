using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Talep düzenleme / silme — satınalma+admin tümü; diğer roller yalnız kendi talebi.</summary>
public static class SatinalmaTalepYetkileri
{
    public static bool SatinalmaTamYetki(string? rol) =>
        KullaniciRolleri.AdminMi(rol)
        || KullaniciRolleri.Normalize(rol) == KullaniciRolleri.Satinalma;

    public static bool TalepSahibi(SatinalmaTalep talep, string? uid, string? ad) =>
        SatinalmaTalepKuyrugu.KullanicininTalebi(talep, uid, ad);

    /// <summary>Talep düzenleme formu açılabilir mi?</summary>
    public static bool TalepDuzenleyebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return SatinalmaKalemDuzenlenebilir(talep) || SatinalmaTalepKuyrugu.KayitliTalep(talep);

        if (!KullaniciRolleri.TalepOlusturabilir(rol))
            return false;

        if (!TalepSahibi(talep, uid, ad))
            return false;

        return SatinalmaTalepYardimcisi.FormDuzenlenebilir(talep)
               || SatinalmaTalepYardimcisi.TalepKalemleriDuzenlenebilir(talep);
    }

    /// <summary>Kalem, açıklama ve tür düzenlenebilir mi?</summary>
    public static bool TalepKalemDuzenleyebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return SatinalmaKalemDuzenlenebilir(talep);

        if (!KullaniciRolleri.TalepOlusturabilir(rol) || !TalepSahibi(talep, uid, ad))
            return false;

        return SatinalmaTalepYardimcisi.TalepKalemleriDuzenlenebilir(talep);
    }

    /// <summary>Satınalma — talep eden adı ve tarih.</summary>
    public static bool TalepMetaDuzenleyebilir(string? rol) => SatinalmaTamYetki(rol);

    public static bool TalepSilebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return true;

        if (!KullaniciRolleri.TalepOlusturabilir(rol))
            return false;

        return TalepSahibi(talep, uid, ad);
    }

    /// <summary>Yönetime yeniden gönder — yalnızca satınalma / admin.</summary>
    public static bool YonetimeYenidenGonderebilir(string? rol, SatinalmaTalep talep) =>
        SatinalmaTamYetki(rol) && SatinalmaIsAkisi.YonetimeYenidenGonderebilir(talep);

    private static bool SatinalmaKalemDuzenlenebilir(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return false;

        if (talep.TeklifsizYonetimOnayi && talep.YonetimOnayKilitli)
            return false;

        return SatinalmaTalepYardimcisi.TalepKalemleriDuzenlenebilir(talep);
    }
}
