using System.Globalization;

namespace SatinalmaPro.Helpers;

public static class TarihYardimcisi
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static readonly string[] Formatlar =
    [
        "dd.MM.yyyy", "d.M.yyyy", "d.MM.yyyy", "dd.M.yyyy",
        "dd.MM.yyyy HH:mm", "d.M.yyyy H:mm", "dd.MM.yyyy HH:mm:ss",
        "dd/MM/yyyy", "d/M/yyyy", "d/MM/yyyy", "dd/M/yyyy",
        "dd/MM/yyyy HH:mm", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss",
        "yyyy.MM.dd", "M/d/yyyy", "MM/dd/yyyy"
    ];

    public static bool TryParse(string? tarih, out DateTime sonuc)
    {
        sonuc = default;
        if (string.IsNullOrWhiteSpace(tarih))
            return false;

        var temiz = tarih.Trim();

        if (DateTime.TryParseExact(temiz, Formatlar, Tr, DateTimeStyles.None, out sonuc))
            return true;

        if (DateTime.TryParseExact(temiz, Formatlar, CultureInfo.InvariantCulture, DateTimeStyles.None, out sonuc))
            return true;

        if (DateTime.TryParse(temiz, Tr, DateTimeStyles.None, out sonuc))
            return true;

        if (DateTime.TryParse(temiz, CultureInfo.InvariantCulture, DateTimeStyles.None, out sonuc))
            return true;

        if (OaTarihDene(temiz, out sonuc))
            return true;

        var tarihKismi = temiz.Split(' ', 'T')[0];
        if (tarihKismi != temiz && TryParse(tarihKismi, out sonuc))
            return true;

        return false;
    }

    public static string Normalize(string? tarih)
    {
        if (!TryParse(tarih, out var dt))
            return tarih?.Trim() ?? "";

        return dt.ToString("dd.MM.yyyy", Tr);
    }

    public static bool Aralikta(string? tarih, DateTime? baslangic, DateTime? bitis)
    {
        if (baslangic is null && bitis is null)
            return true;

        if (!TryParse(tarih, out var dt))
            return false;

        if (baslangic is DateTime b && dt.Date < b.Date)
            return false;

        if (bitis is DateTime son && dt.Date > son.Date)
            return false;

        return true;
    }

    /// <summary>Liste sıralaması için tarih değeri (yeni → eski).</summary>
    public static DateTime SiralamaDegeri(string? tarih) =>
        TryParse(tarih, out var dt) ? dt : DateTime.MinValue;

    private static bool OaTarihDene(string temiz, out DateTime sonuc)
    {
        sonuc = default;
        var sayisal = temiz.Replace(',', '.');
        if (!double.TryParse(sayisal, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
            return false;

        if (oa is <= 1 or >= 1000000)
            return false;

        try
        {
            sonuc = DateTime.FromOADate(oa);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
