using System.Globalization;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

public static class FiloHesaplayici
{
    public const int MuayeneUyariGun = 15;

    public static void Hesapla(IEnumerable<FiloAracKaydi> araclar, IEnumerable<FiloGiderKaydi> giderler,
        IEnumerable<FiloZimmetKaydi>? zimmetler = null)
    {
        zimmetler ??= ModulVeriDeposu.FiloZimmetleri;
        var giderGruplari = giderler
            .Where(g => !string.IsNullOrWhiteSpace(g.Plaka))
            .GroupBy(g => g.Plaka.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Tutar), StringComparer.OrdinalIgnoreCase);

        var aktifZimmetler = zimmetler
            .Where(z => z.Aktif && !string.IsNullOrWhiteSpace(z.Plaka))
            .GroupBy(z => z.Plaka.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(x => x.SoforAdi).Where(s => !string.IsNullOrWhiteSpace(s))),
                StringComparer.OrdinalIgnoreCase);

        foreach (var arac in araclar)
        {
            var plaka = arac.Plaka.Trim();
            arac.ToplamGider = !string.IsNullOrEmpty(plaka) && giderGruplari.TryGetValue(plaka, out var toplam)
                ? toplam
                : 0;
            arac.MuayeneUyariMetin = UyariMetni(arac.MuayeneBitisTarihi, "Muayene");
            arac.SigortaUyariMetin = UyariMetni(arac.SigortaBitisTarihi, "Sigorta");
            arac.ZimmetMetin = !string.IsNullOrEmpty(plaka) && aktifZimmetler.TryGetValue(plaka, out var zimmet) &&
                               !string.IsNullOrWhiteSpace(zimmet)
                ? zimmet
                : "—";
        }
    }

    public static List<FiloAracKaydi> MuayenesiYaklasanAraclar(
        IEnumerable<FiloAracKaydi> araclar,
        int gunLimit = MuayeneUyariGun)
    {
        var bugun = DateTime.Today;
        return araclar
            .Where(a => a.Durum.Equals("Aktif", StringComparison.OrdinalIgnoreCase))
            .Select(a => (arac: a, bitis: TarihCoz(a.MuayeneBitisTarihi)))
            .Where(x => x.bitis != DateTime.MinValue)
            .Select(x => (x.arac, kalan: (x.bitis - bugun).Days))
            .Where(x => x.kalan <= gunLimit)
            .OrderBy(x => x.kalan)
            .Select(x => x.arac)
            .ToList();
    }

    public static int MuayenesiYaklasanSayisi(IEnumerable<FiloAracKaydi> araclar, int gunLimit = MuayeneUyariGun) =>
        MuayenesiYaklasanAraclar(araclar, gunLimit).Count;

    public static decimal ToplamGider(IEnumerable<FiloGiderKaydi> giderler) =>
        giderler.Sum(g => g.Tutar);

    private static string UyariMetni(string bitisTarihi, string etiket)
    {
        var bitis = TarihCoz(bitisTarihi);
        if (bitis == DateTime.MinValue)
            return "—";

        var kalan = (bitis - DateTime.Today).Days;
        return kalan switch
        {
            < 0 => $"{etiket}: {Math.Abs(kalan)} gün geçti",
            0 => $"{etiket}: Bugün",
            <= MuayeneUyariGun => $"{etiket}: {kalan} gün",
            _ => bitisTarihi
        };
    }

    public static DateTime TarihCoz(string tarih) =>
        DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MinValue;
}
