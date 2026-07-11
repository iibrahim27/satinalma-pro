using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

/// <summary>
/// Fiyat karşılaştırma PDF 2. sayfa: malzeme başına son 2 ay (yoksa en son 2) alım.
/// </summary>
public static class KarsilastirmaAlimGecmisiYardimcisi
{
    public sealed class AlimSatiri
    {
        public int KalemSiraNo { get; init; }
        public string Malzeme { get; init; } = "";
        public string Tarih { get; init; } = "";
        public double Miktar { get; init; }
        public string Birim { get; init; } = "";
        public decimal BirimFiyati { get; init; }
        public string Tedarikci { get; init; } = "";
        public bool KayitYok { get; init; }
        public bool SonIkiAlimYedegi { get; init; }
    }

    public static List<AlimSatiri> MalzemeBazliAlimlariTopla(
        IEnumerable<SatinalmaTalepKalemi> kalemler,
        IEnumerable<AlinanMalzemeKaydi>? alinanMalzemeler,
        DateTime? referansTarih = null)
    {
        var referans = (referansTarih ?? DateTime.Today).Date;
        var esik = referans.AddMonths(-2);
        var kaynak = (alinanMalzemeler ?? []).ToList();
        var sonuc = new List<AlimSatiri>();

        foreach (var kalem in kalemler.OrderBy(k => k.SiraNo))
        {
            var malzemeAdi = (kalem.Malzeme ?? "").Trim();
            if (string.IsNullOrWhiteSpace(malzemeAdi))
                continue;

            var eslesen = kaynak
                .Where(k => string.Equals(
                    (k.MalzemeHizmet ?? "").Trim(),
                    malzemeAdi,
                    StringComparison.OrdinalIgnoreCase))
                .Select(k => (kayit: k, tarih: TarihCoz(k.Tarih)))
                .OrderByDescending(x => x.tarih)
                .ThenByDescending(x => x.kayit.BirimFiyati)
                .ToList();

            var sonIkiAy = eslesen
                .Where(x => x.tarih >= esik && x.tarih <= referans.AddDays(1).AddTicks(-1))
                .ToList();

            if (sonIkiAy.Count > 0)
            {
                foreach (var (kayit, _) in sonIkiAy)
                    sonuc.Add(AlimdanSatir(kalem.SiraNo, malzemeAdi, kayit, sonIkiAlimYedegi: false));
                continue;
            }

            var yedek = eslesen.Take(2).ToList();
            if (yedek.Count == 0)
            {
                sonuc.Add(new AlimSatiri
                {
                    KalemSiraNo = kalem.SiraNo,
                    Malzeme = malzemeAdi,
                    KayitYok = true
                });
                continue;
            }

            foreach (var (kayit, _) in yedek)
                sonuc.Add(AlimdanSatir(kalem.SiraNo, malzemeAdi, kayit, sonIkiAlimYedegi: true));
        }

        return sonuc;
    }

    private static AlimSatiri AlimdanSatir(
        int siraNo, string malzeme, AlinanMalzemeKaydi kayit, bool sonIkiAlimYedegi) =>
        new()
        {
            KalemSiraNo = siraNo,
            Malzeme = malzeme,
            Tarih = string.IsNullOrWhiteSpace(kayit.Tarih) ? "—" : kayit.Tarih.Trim(),
            Miktar = kayit.Miktar,
            Birim = string.IsNullOrWhiteSpace(kayit.Birim) ? "—" : kayit.Birim.Trim(),
            BirimFiyati = kayit.BirimFiyati,
            Tedarikci = string.IsNullOrWhiteSpace(kayit.Tedarikci) ? "—" : kayit.Tedarikci.Trim(),
            SonIkiAlimYedegi = sonIkiAlimYedegi
        };

    private static DateTime TarihCoz(string? tarih)
    {
        if (string.IsNullOrWhiteSpace(tarih))
            return DateTime.MinValue;

        if (DateTime.TryParseExact(
                tarih.Trim(),
                "dd.MM.yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
            return dt;

        return DateTime.TryParse(tarih, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out dt)
            ? dt.Date
            : DateTime.MinValue;
    }
}
