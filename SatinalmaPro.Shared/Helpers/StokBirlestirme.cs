using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

public static class StokBirlestirme
{
    public static List<StokKaydi> Birlestir(IEnumerable<StokKaydi> yerel, IEnumerable<StokKaydi> bulut)
    {
        var sozluk = new Dictionary<string, StokKaydi>(StringComparer.OrdinalIgnoreCase);

        foreach (var kayit in bulut)
            EkleVeyaGuncelle(sozluk, kayit);

        foreach (var kayit in yerel)
            EkleVeyaGuncelle(sozluk, kayit);

        return sozluk.Values
            .OrderBy(s => s.MalzemeAdi, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.DepoSaha, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<StokHareketKaydi> HareketleriBirlestir(
        IEnumerable<StokHareketKaydi> yerel,
        IEnumerable<StokHareketKaydi> bulut)
    {
        var sozluk = new Dictionary<Guid, StokHareketKaydi>();

        foreach (var kayit in bulut)
            sozluk[kayit.Id] = kayit;

        foreach (var kayit in yerel)
            sozluk[kayit.Id] = kayit;

        return sozluk.Values
            .OrderByDescending(h => h.Tarih)
            .ThenBy(h => h.MalzemeAdi, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void EkleVeyaGuncelle(Dictionary<string, StokKaydi> sozluk, StokKaydi kayit)
    {
        var anahtar = Anahtar(kayit);
        if (!sozluk.TryGetValue(anahtar, out var mevcut))
        {
            sozluk[anahtar] = kayit;
            return;
        }

        if (DahaGuncel(kayit, mevcut))
            sozluk[anahtar] = kayit;
    }

    private static string Anahtar(StokKaydi kayit) =>
        $"{kayit.MalzemeAdi}|{kayit.DepoSaha}|{kayit.Kategori}";

    private static bool DahaGuncel(StokKaydi aday, StokKaydi mevcut)
    {
        var adayTarih = TarihOku(aday.SonGuncelleme);
        var mevcutTarih = TarihOku(mevcut.SonGuncelleme);

        if (adayTarih != mevcutTarih)
            return adayTarih > mevcutTarih;

        return aday.MevcutMiktar > mevcut.MevcutMiktar;
    }

    private static DateTime TarihOku(string? metin) =>
        DateTime.TryParse(metin, out var tarih) ? tarih : DateTime.MinValue;
}
