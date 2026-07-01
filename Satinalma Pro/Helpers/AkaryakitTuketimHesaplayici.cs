using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class AkaryakitTuketimHesaplayici
{
    public static void Hesapla(IEnumerable<AkaryakitKaydi> kayitlar)
    {
        foreach (var kayit in kayitlar)
        {
            kayit.Tuketim100Km = null;
            kayit.TuketimSaat = null;
        }

        var gruplar = kayitlar
            .Where(k => !k.AlinanKayit && !string.IsNullOrWhiteSpace(k.PlakaVeyaKod))
            .GroupBy(k => k.PlakaVeyaKod.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var grup in gruplar)
        {
            var sirali = grup
                .Select((kayit, sira) => (kayit, sira, tarih: TarihCoz(kayit.Tarih)))
                .OrderBy(x => x.tarih)
                .ThenBy(x => x.sira)
                .ToList();

            AkaryakitKaydi? onceki = null;

            foreach (var (kayit, _, _) in sirali)
            {
                if (onceki is not null)
                {
                    if (kayit.KmSayaci is > 0 && onceki.KmSayaci is > 0)
                    {
                        var kmFark = kayit.KmSayaci.Value - onceki.KmSayaci.Value;
                        if (kmFark > 0)
                            kayit.Tuketim100Km = kayit.Miktar / kmFark * 100;
                    }

                    if (kayit.SaatSayaci is > 0 && onceki.SaatSayaci is > 0)
                    {
                        var saatFark = kayit.SaatSayaci.Value - onceki.SaatSayaci.Value;
                        if (saatFark > 0)
                            kayit.TuketimSaat = kayit.Miktar / saatFark;
                    }
                }

                onceki = kayit;
            }
        }
    }

    private static DateTime TarihCoz(string tarih) =>
        DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MinValue;
}
