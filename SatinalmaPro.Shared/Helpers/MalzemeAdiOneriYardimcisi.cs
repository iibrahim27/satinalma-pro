namespace SatinalmaPro.Shared.Helpers;

/// <summary>Alınan malzeme / stok / geçmiş talep adlarından yazım önerisi.</summary>
public static class MalzemeAdiOneriYardimcisi
{
    public const int MaxOneri = 12;

    public static IEnumerable<string> Filtrele(IEnumerable<string> kaynaklar, string? arama)
    {
        var benzersiz = kaynaklar
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (string.IsNullOrWhiteSpace(arama))
            return [];

        var metin = arama.Trim();
        return benzersiz
            .Where(s => s.Contains(metin, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(s => Skor(s, metin))
            .ThenBy(s => s, StringComparer.CurrentCultureIgnoreCase)
            .Take(MaxOneri);
    }

    private static int Skor(string ad, string arama)
    {
        if (ad.Equals(arama, StringComparison.CurrentCultureIgnoreCase))
            return 0;
        if (ad.StartsWith(arama, StringComparison.CurrentCultureIgnoreCase))
            return 1;
        return 2;
    }
}
