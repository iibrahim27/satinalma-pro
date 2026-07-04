using System.Globalization;

using SatinalmaPro.Models;



namespace SatinalmaPro.Helpers;



public static class RaporArtisHesaplayici

{

    public static void Hesapla(IList<RaporDetaySatiri> satirlar)

    {

        var sirali = satirlar

            .Select((satir, sira) => (satir, sira, tarih: TarihCoz(satir.Tarih)))

            .OrderBy(x => x.tarih)

            .ThenBy(x => x.sira)

            .ToList();



        var sonBirimFiyati = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);



        foreach (var (satir, _, _) in sirali)

        {

            var anahtar = AnahtarOlustur(satir);

            if (string.IsNullOrEmpty(anahtar) || satir.BirimFiyati <= 0)

            {

                satir.ArtisYuzdesi = null;

                continue;

            }



            if (sonBirimFiyati.TryGetValue(anahtar, out var onceki) && onceki > 0)

                satir.ArtisYuzdesi = (double)((satir.BirimFiyati - onceki) / onceki * 100m);

            else

                satir.ArtisYuzdesi = null;



            sonBirimFiyati[anahtar] = satir.BirimFiyati;

        }

    }



    private static string AnahtarOlustur(RaporDetaySatiri satir) =>

        $"{satir.Modul}|{satir.Aciklama.Trim()}";



    private static DateTime TarihCoz(string tarih) =>

        DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)

            ? dt

            : DateTime.MinValue;

}


