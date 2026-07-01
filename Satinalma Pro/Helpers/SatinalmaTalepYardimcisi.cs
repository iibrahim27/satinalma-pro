using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class SatinalmaTalepYardimcisi
{
    public static bool TeklifYonetimOnayiBekliyor(SatinalmaTalep talep) =>
        (talep.Durum is SatinalmaTalepDurumlari.YonetimOnayinda or SatinalmaTalepDurumlari.Karsilastirma)
        && (talep.Teklifler?.Count ?? 0) > 0
        && !talep.HerhangiKalemOnayli
        && !talep.YonetimOnayKilitli;

    public static bool YonetimTeklifKarariBekliyor(SatinalmaTalep talep) =>
        (talep.Teklifler?.Count ?? 0) > 0
        && !talep.YonetimOnayKilitli
        && !talep.TeklifsizYonetimOnayi
        && talep.Durum is SatinalmaTalepDurumlari.YonetimOnayinda
            or SatinalmaTalepDurumlari.Karsilastirma
            or SatinalmaTalepDurumlari.TeklifGirisi;

    public static bool IcerikVar(SatinalmaTalep talep) =>
        !string.IsNullOrWhiteSpace(talep.TalepAciklamasi)
        || talep.Kalemler?.Any(k => !string.IsNullOrWhiteSpace(k.Malzeme)) == true;

    public static bool GonderimOncesiDuzenlenebilir(SatinalmaTalep talep) =>
        talep.Durum is SatinalmaTalepDurumlari.Hazirlaniyor or SatinalmaTalepDurumlari.Reddedildi;

    public static bool FormDuzenlenebilir(SatinalmaTalep talep) =>
        talep.Durum == SatinalmaTalepDurumlari.Taslak || GonderimOncesiDuzenlenebilir(talep);

    public static void KayitOncesiHazirla(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Taslak)
            talep.Durum = SatinalmaTalepDurumlari.Hazirlaniyor;
    }

    public static bool TaslaklariNormalizeEt(IList<SatinalmaTalep> talepler)
    {
        var degisti = false;
        for (var i = talepler.Count - 1; i >= 0; i--)
        {
            var talep = talepler[i];
            if (talep.Durum != SatinalmaTalepDurumlari.Taslak)
                continue;

            if (!IcerikVar(talep))
            {
                talepler.RemoveAt(i);
                degisti = true;
                continue;
            }

            talep.Durum = SatinalmaTalepDurumlari.Hazirlaniyor;
            degisti = true;
        }

        return degisti;
    }

    public static bool YonetimOnayMirasiniGuncelle(SatinalmaTalep talep)
    {
        if (talep.YonetimOnayKilitli)
            return false;

        if (talep.Durum is not (SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu))
            return false;

        if (!string.IsNullOrWhiteSpace(talep.YonetimOnaylayanUid)
            || !string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi)
            || talep.TeklifsizYonetimOnayi)
        {
            talep.YonetimOnayKilitli = true;
            return true;
        }

        return false;
    }

    public static bool YonetimOnayMiraslariniGuncelle(IList<SatinalmaTalep> talepler)
    {
        var degisti = false;
        foreach (var talep in talepler)
        {
            if (YonetimOnayMirasiniGuncelle(talep))
                degisti = true;
        }

        return degisti;
    }
}
