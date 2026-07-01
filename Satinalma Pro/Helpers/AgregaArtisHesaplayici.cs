using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class AgregaArtisHesaplayici
{
    public static void Hesapla(IEnumerable<AgregaKaydi> kayitlar)
    {
        var sirali = kayitlar
            .Select((kayit, sira) => (kayit, sira, tarih: TarihCoz(kayit.Tarih)))
            .OrderBy(x => x.tarih)
            .ThenBy(x => x.sira)
            .ToList();

        var sonBirimFiyati = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var (kayit, _, _) in sirali)
        {
            var cins = kayit.AgregaCinsi.Trim();
            if (string.IsNullOrEmpty(cins) || kayit.BirimFiyati <= 0)
            {
                kayit.ArtisYuzdesi = null;
                continue;
            }

            if (sonBirimFiyati.TryGetValue(cins, out var onceki) && onceki > 0)
                kayit.ArtisYuzdesi = (double)((kayit.BirimFiyati - onceki) / onceki * 100m);
            else
                kayit.ArtisYuzdesi = null;

            sonBirimFiyati[cins] = kayit.BirimFiyati;
        }
    }

        private static DateTime TarihCoz(string tarih) =>
        TarihYardimcisi.TryParse(tarih, out var dt) ? dt : DateTime.MinValue;
}
