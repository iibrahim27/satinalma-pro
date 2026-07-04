using System.Globalization;
using ClosedXML.Excel;

namespace SatinalmaPro.Helpers;

public static class ExcelHucreYardimcisi
{
    public static string TarihOku(IXLCell hucre)
    {
        if (hucre.IsEmpty())
            return "";

        if (hucre.DataType == XLDataType.DateTime && hucre.TryGetValue(out DateTime dt))
            return dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

        if (hucre.TryGetValue(out double seri) && seri is > 1 and < 1000000)
        {
            try
            {
                return DateTime.FromOADate(seri).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            }
            catch
            {
                // geçersiz seri
            }
        }

        var metin = hucre.GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(metin))
            metin = hucre.GetString().Trim();

        return string.IsNullOrWhiteSpace(metin) ? "" : TarihYardimcisi.Normalize(metin);
    }

    public static string TarihOku(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!kolonlar.TryGetValue(baslik, out var kolon))
                continue;

            return TarihOku(satir.Cell(kolon));
        }

        return "";
    }
}
