using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class AlinanMalzemeArtisHesaplayici
{
    public static void Hesapla(IEnumerable<AlinanMalzemeKaydi> kayitlar)
    {
        var sirali = kayitlar
            .Select((kayit, sira) => (kayit, sira, tarih: TarihCoz(kayit.Tarih)))
            .OrderBy(x => x.tarih)
            .ThenBy(x => x.sira)
            .ToList();

        var sonBirimFiyati = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var (kayit, _, _) in sirali)
        {
            var malzeme = kayit.MalzemeHizmet.Trim();
            if (string.IsNullOrEmpty(malzeme) || kayit.BirimFiyati <= 0)
            {
                kayit.ArtisYuzdesi = null;
                continue;
            }

            if (sonBirimFiyati.TryGetValue(malzeme, out var onceki) && onceki > 0)
                kayit.ArtisYuzdesi = (double)((kayit.BirimFiyati - onceki) / onceki * 100m);
            else
                kayit.ArtisYuzdesi = null;

            sonBirimFiyati[malzeme] = kayit.BirimFiyati;
        }
    }

    private static DateTime TarihCoz(string tarih) =>
        DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MinValue;
}
