using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class AkaryakitExcelTamamlayici
{
    public static void DagitilanKaydiniTamamla(
        AkaryakitKaydi kayit,
        IEnumerable<AkaryakitKaydi> tumKayitlar,
        bool excelKmSaatVar = false)
    {
        kayit.KayitTipi = "Dağıtılan";
        kayit.Birim = "Lt";
        kayit.PlakaVeyaKod = kayit.PlakaVeyaKod.Trim();

        var onceki = SonDagitilanKayit(tumKayitlar, kayit.PlakaVeyaKod, kayit.Tarih);

        if (string.IsNullOrWhiteSpace(kayit.YakitTuru))
            kayit.YakitTuru = onceki?.YakitTuru ?? "Motorin";

        var arac = FiloPlakaServisi.AracBul(kayit.PlakaVeyaKod);
        if (arac is not null)
        {
            kayit.AracTipi = FiloPlakaServisi.AkaryakitAracTipi(arac.AracTipi);
            kayit.AracMakineAdi = arac.MarkaModel;
            if (string.IsNullOrWhiteSpace(kayit.Saha))
                kayit.Saha = arac.Saha;
        }
        else if (string.IsNullOrWhiteSpace(kayit.AracTipi))
        {
            kayit.AracTipi = onceki?.AracTipi ?? "Araç";
            kayit.AracMakineAdi = onceki?.AracMakineAdi ?? "";
            if (string.IsNullOrWhiteSpace(kayit.Saha))
                kayit.Saha = onceki?.Saha ?? "";
        }

        if (string.IsNullOrWhiteSpace(kayit.SoforOperator))
        {
            var zimmetliSofor = FiloPlakaServisi.AktifZimmetliSofor(kayit.PlakaVeyaKod);
            kayit.SoforOperator = !string.IsNullOrWhiteSpace(zimmetliSofor)
                ? zimmetliSofor
                : onceki?.SoforOperator ?? "";
        }

        if (!excelKmSaatVar)
        {
            if (kayit.KmSayaci is null && onceki?.KmSayaci is not null)
                kayit.KmSayaci = onceki.KmSayaci;

            if (kayit.SaatSayaci is null && onceki?.SaatSayaci is not null)
                kayit.SaatSayaci = onceki.SaatSayaci;
        }

        kayit.ToplamTutariHesapla();
    }

    private static AkaryakitKaydi? SonDagitilanKayit(
        IEnumerable<AkaryakitKaydi> kayitlar,
        string plaka,
        string? tarih = null)
    {
        if (string.IsNullOrWhiteSpace(plaka))
            return null;

        var hedefTarih = TarihCoz(tarih);
        return kayitlar
            .Where(k => !k.AlinanKayit &&
                        k.PlakaVeyaKod.Equals(plaka.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(k => (kayit: k, tarih: TarihCoz(k.Tarih)))
            .Where(x => hedefTarih == DateTime.MinValue || x.tarih <= hedefTarih)
            .OrderByDescending(x => x.tarih)
            .ThenByDescending(x => x.kayit.Miktar)
            .Select(x => x.kayit)
            .FirstOrDefault();
    }

    private static DateTime TarihCoz(string? tarih) =>
        DateTime.TryParseExact(tarih?.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MinValue;
}
