using System.Globalization;

namespace SatinalmaPro.Helpers;

public static class SayiMetniYardimcisi
{
    public static bool OndalikOku(string? metin, out decimal sonuc)
    {
        sonuc = 0;
        if (string.IsNullOrWhiteSpace(metin))
            return true;

        if (decimal.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out sonuc))
            return true;

        return decimal.TryParse(metin.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out sonuc);
    }

    public static bool CiftOku(string? metin, out double sonuc)
    {
        sonuc = 0;
        if (string.IsNullOrWhiteSpace(metin))
            return true;

        if (double.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out sonuc))
            return true;

        return double.TryParse(metin.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out sonuc);
    }

    public static string OndalikGoster(decimal deger) =>
        deger.ToString("G", CultureInfo.CurrentCulture);

    public static string CiftGoster(double deger) =>
        deger.ToString("G", CultureInfo.CurrentCulture);
}
