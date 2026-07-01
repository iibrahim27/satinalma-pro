using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>
/// Bulut ve yerel talep kayıtlarını birleştirir — hiçbir talep silinmez.
/// </summary>
public static class SatinalmaTalepBirlestirme
{
    public static List<SatinalmaTalep> Birlestir(
        IEnumerable<SatinalmaTalep> yerel,
        IEnumerable<SatinalmaTalep> bulut)
    {
        var sozluk = new Dictionary<Guid, SatinalmaTalep>();

        foreach (var talep in yerel)
            sozluk[talep.Id] = talep;

        foreach (var talep in bulut)
        {
            if (!sozluk.TryGetValue(talep.Id, out var mevcut))
            {
                sozluk[talep.Id] = talep;
                continue;
            }

            sozluk[talep.Id] = DahaDoluKayit(mevcut, talep);
        }

        return sozluk.Values.ToList();
    }

    private static SatinalmaTalep DahaDoluKayit(SatinalmaTalep a, SatinalmaTalep b)
    {
        var skorA = Skor(a);
        var skorB = Skor(b);
        if (skorB > skorA)
            return b;
        if (skorA > skorB)
            return a;

        return TarihSira(b.Tarih) >= TarihSira(a.Tarih) ? b : a;
    }

    private static int Skor(SatinalmaTalep talep)
    {
        var skor = 0;
        if (!string.IsNullOrWhiteSpace(talep.TalepNo))
            skor += 4;
        if (talep.Durum != SatinalmaTalepDurumlari.Taslak)
            skor += 8;
        skor += (talep.Kalemler?.Count ?? 0) * 3;
        skor += (talep.Teklifler?.Count ?? 0) * 5;
        if (talep.HerhangiKalemOnayli)
            skor += 10;
        if (!string.IsNullOrWhiteSpace(talep.YonetimOnaylayanUid))
            skor += 6;
        if (!string.IsNullOrWhiteSpace(talep.SiparisNo) || talep.FirmaSiparisNolari?.Count > 0)
            skor += 8;
        return skor;
    }

    private static DateTime TarihSira(string? tarih)
    {
        if (string.IsNullOrWhiteSpace(tarih))
            return DateTime.MinValue;

        if (DateTime.TryParse(tarih, out var dt))
            return dt;

        return DateTime.MinValue;
    }
}
