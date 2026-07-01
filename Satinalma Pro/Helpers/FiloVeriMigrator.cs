using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class FiloVeriMigrator
{
    public static FiloVeriPaketi EskiKayittanOlustur(IEnumerable<FiloKaydi> eskiKayitlar)
    {
        var paket = new FiloVeriPaketi();
        var araclar = new Dictionary<string, FiloAracKaydi>(StringComparer.OrdinalIgnoreCase);

        foreach (var kayit in eskiKayitlar)
        {
            if (kayit.VarlikKaydi)
            {
                var plaka = kayit.PlakaKod.Trim();
                if (string.IsNullOrWhiteSpace(plaka))
                    continue;

                araclar[plaka] = new FiloAracKaydi
                {
                    Plaka = plaka,
                    AracTipi = AracTipiDonustur(kayit.AracTipi),
                    MarkaModel = kayit.MarkaModel,
                    ModelYili = kayit.ModelYili,
                    Saha = kayit.Saha,
                    Durum = kayit.Durum,
                    Aciklama = kayit.Aciklama,
                    KayitTarihi = kayit.Tarih,
                    SahiplikTipi = "Bizim"
                };
                continue;
            }

            if (kayit.MasrafKaydi && kayit.Tutar > 0)
            {
                paket.Giderler.Add(new FiloGiderKaydi
                {
                    Plaka = kayit.PlakaKod.Trim(),
                    Tarih = kayit.Tarih,
                    GiderTipi = kayit.KayitTipi,
                    Tutar = kayit.Tutar,
                    BelgeNo = kayit.BelgeNo,
                    Aciklama = kayit.Aciklama
                });
            }

            if (!kayit.SurecKaydi || string.IsNullOrWhiteSpace(kayit.PlakaKod))
                continue;

            var p = kayit.PlakaKod.Trim();
            if (!araclar.TryGetValue(p, out var arac))
            {
                arac = new FiloAracKaydi
                {
                    Plaka = p,
                    AracTipi = "Binek",
                    KayitTarihi = kayit.Tarih,
                    SahiplikTipi = "Bizim",
                    Durum = "Aktif"
                };
                araclar[p] = arac;
            }

            if (kayit.KayitTipi.Equals("Muayene", StringComparison.OrdinalIgnoreCase))
                arac.MuayeneBitisTarihi = kayit.BitisTarihi;
            else if (kayit.KayitTipi.Equals("Sigorta", StringComparison.OrdinalIgnoreCase))
                arac.SigortaBitisTarihi = kayit.BitisTarihi;
        }

        paket.Araclar.AddRange(araclar.Values.OrderBy(a => a.Plaka));
        return paket;
    }

    private static string AracTipiDonustur(string? tip) =>
        tip?.Equals("İş Makinası", StringComparison.OrdinalIgnoreCase) == true
            ? "İş Makinası"
            : "Binek";
}
