using SatinalmaPro.Models;
using SharedRol = SatinalmaPro.Shared.Models.KullaniciRolleri;

namespace SatinalmaPro.Helpers;

/// <summary>Talep düzenleme / silme / yeniden gönderme — masaüstü.</summary>
public static class SatinalmaTalepYetkileri
{
    public static bool SatinalmaTamYetki(string? rol) =>
        SharedRol.AdminMi(rol)
        || SharedRol.Normalize(rol) == SharedRol.Satinalma;

    public static bool TalepSahibi(SatinalmaTalep talep, string? uid, string? ad) =>
        SatinalmaTalepKuyrugu.KullanicininTalebi(talep, uid, ad);

    public static bool TalepDuzenleyebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return SatinalmaKalemDuzenlenebilir(talep) || SatinalmaTalepKuyrugu.KayitliTalep(talep);

        if (!SharedRol.TalepOlusturabilir(rol))
            return false;

        if (!TalepSahibi(talep, uid, ad))
            return false;

        return SatinalmaTalepYardimcisi.FormDuzenlenebilir(talep)
               || SatinalmaTalepYardimcisi.TalepKalemleriDuzenlenebilir(talep);
    }

    public static bool TalepKalemDuzenleyebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return SatinalmaKalemDuzenlenebilir(talep);

        if (!SharedRol.TalepOlusturabilir(rol) || !TalepSahibi(talep, uid, ad))
            return false;

        return SatinalmaTalepYardimcisi.TalepKalemleriDuzenlenebilir(talep);
    }

    public static bool TalepMetaDuzenleyebilir(string? rol) => SatinalmaTamYetki(rol);

    public static bool TalepSilebilir(string? rol, SatinalmaTalep talep, string? uid, string? ad)
    {
        if (SatinalmaTamYetki(rol))
            return true;

        if (!SharedRol.TalepOlusturabilir(rol))
            return false;

        return TalepSahibi(talep, uid, ad);
    }

    public static bool YonetimeYenidenGonderebilir(string? rol, SatinalmaTalep talep) =>
        SatinalmaTamYetki(rol) && SatinalmaYonetimGonderimi.YenidenGonderebilir(talep);

    private static bool SatinalmaKalemDuzenlenebilir(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            return false;

        if (talep.TeklifsizYonetimOnayi && talep.YonetimOnayKilitli)
            return false;

        return SatinalmaTalepYardimcisi.TalepKalemleriDuzenlenebilir(talep);
    }
}
