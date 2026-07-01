using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

public static class SatinalmaTalepYardimcisi
{
    /// <summary>Teklifler girildi, yönetim onayı bekleniyor (eski kayıtlarda Karşılaştırma da sayılır).</summary>
    public static bool TeklifYonetimOnayiBekliyor(SatinalmaTalep talep) =>
        (talep.Durum is SatinalmaTalepDurumlari.YonetimOnayinda or SatinalmaTalepDurumlari.Karsilastirma)
        && (talep.Teklifler?.Count ?? 0) > 0
        && !talep.HerhangiKalemOnayli
        && !talep.YonetimOnayKilitli;

    /// <summary>Teklif girilmiş — yönetim teklif kararı verecek (kısmi kalem onayı dahil).</summary>
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

    /// <summary>Kaydedilmiş, yönetime henüz gönderilmemiş veya reddedilmiş — düzenlenebilir talepler.</summary>
    public static bool GonderimOncesiDuzenlenebilir(SatinalmaTalep talep) =>
        talep.Durum is SatinalmaTalepDurumlari.Hazirlaniyor or SatinalmaTalepDurumlari.Reddedildi;

    public static bool FormDuzenlenebilir(SatinalmaTalep talep) =>
        talep.Durum == SatinalmaTalepDurumlari.Taslak || GonderimOncesiDuzenlenebilir(talep);

    public static void KayitOncesiHazirla(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Taslak)
            talep.Durum = SatinalmaTalepDurumlari.Hazirlaniyor;
    }

    /// <summary>Kalıcı kayıtlarda Taslak kalmamalı — boş olanları at, dolu olanları Hazırlanıyor yap.</summary>
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

    /// <summary>Eski kayıtlarda yönetim onayı var ama kilit bayrağı eksikse düzeltir.</summary>
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
