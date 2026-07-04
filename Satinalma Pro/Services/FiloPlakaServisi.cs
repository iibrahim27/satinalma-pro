using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class FiloPlakaServisi
{
    public static List<FiloAracKaydi> AktifAraclar() =>
        ModulVeriDeposu.FiloAraclari
            .Where(a => a.Durum.Equals("Aktif", StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Plaka)
            .ToList();

    public static List<string> AktifPlakalar() =>
        AktifAraclar()
            .Select(a => a.Plaka.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p)
            .ToList();

    public static FiloAracKaydi? AracBul(string? plaka)
    {
        if (string.IsNullOrWhiteSpace(plaka))
            return null;

        return ModulVeriDeposu.FiloAraclari
            .FirstOrDefault(a => a.Plaka.Equals(plaka.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static string AktifZimmetliSofor(string? plaka)
    {
        if (string.IsNullOrWhiteSpace(plaka))
            return "";

        var soforler = ModulVeriDeposu.FiloZimmetleri
            .Where(z => z.Aktif && z.Plaka.Equals(plaka.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(z => z.SoforAdi.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return soforler.Count == 0 ? "" : string.Join(", ", soforler);
    }

    public static string AkaryakitAracTipi(string? filoAracTipi) =>
        filoAracTipi?.Equals("İş Makinası", StringComparison.OrdinalIgnoreCase) == true
            ? "İş Makinası"
            : "Araç";
}
