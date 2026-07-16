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

    /// <summary>
    /// Malzeme bazında: en son alınan birim fiyat × teklifteki en düşük TL birim fiyat.
    /// </summary>
    public sealed class FiyatKarsilastirmaSatiri
    {
        public int KalemSiraNo { get; init; }
        public string Malzeme { get; init; } = "";
        public string Birim { get; init; } = "";
        public decimal? SonAlinanBirimFiyat { get; init; }
        public decimal? EnDusukTeklifBirimFiyat { get; init; }
        public string EnDusukTeklifFirma { get; init; } = "";
        public decimal? FarkTl { get; init; }
        public decimal? ArtisYuzde { get; init; }
        public bool SonAlimYok { get; init; }
        public bool TeklifYok { get; init; }
    }

    public static List<FiyatKarsilastirmaSatiri> MalzemeBazliFiyatKarsilastirmasiTopla(
        IEnumerable<SatinalmaTalepKalemi> kalemler,
        IEnumerable<SatinalmaTeklif> teklifler,
        IReadOnlyList<AlimSatiri> alimSatirlari)
    {
        var sonAlimlar = new Dictionary<string, AlimSatiri>(StringComparer.OrdinalIgnoreCase);
        foreach (var satir in alimSatirlari)
        {
            if (satir.KayitYok || string.IsNullOrWhiteSpace(satir.Malzeme))
                continue;
            if (!sonAlimlar.ContainsKey(satir.Malzeme))
                sonAlimlar[satir.Malzeme] = satir;
        }

        var teklifListesi = (teklifler ?? []).ToList();
        var sonuc = new List<FiyatKarsilastirmaSatiri>();

        foreach (var kalem in kalemler.OrderBy(k => k.SiraNo))
        {
            var malzemeAdi = (kalem.Malzeme ?? "").Trim();
            if (string.IsNullOrWhiteSpace(malzemeAdi))
                continue;

            var sonAlimYok = !sonAlimlar.TryGetValue(malzemeAdi, out var sonAlim);
            decimal? sonFiyat = sonAlimYok ? null : sonAlim!.BirimFiyati;

            decimal? enDusuk = null;
            var enDusukFirma = "";
            foreach (var teklif in teklifListesi)
            {
                var fiyat = (teklif.Fiyatlar ?? []).FirstOrDefault(f => f.KalemId == kalem.Id);
                if (fiyat is null || fiyat.BirimFiyat <= 0)
                    continue;

                var tl = fiyat.TlBirimFiyat(teklif.UsdKuru, teklif.EurKuru);
                if (tl <= 0)
                    continue;

                if (enDusuk is null || tl < enDusuk.Value)
                {
                    enDusuk = tl;
                    enDusukFirma = string.IsNullOrWhiteSpace(teklif.FirmaAdi) ? "—" : teklif.FirmaAdi.Trim();
                }
            }

            var teklifYok = enDusuk is null;
            decimal? fark = null;
            decimal? yuzde = null;
            if (sonFiyat is > 0 && enDusuk is not null)
            {
                fark = enDusuk.Value - sonFiyat.Value;
                yuzde = Math.Round(fark.Value / sonFiyat.Value * 100m, 2);
            }

            sonuc.Add(new FiyatKarsilastirmaSatiri
            {
                KalemSiraNo = kalem.SiraNo,
                Malzeme = malzemeAdi,
                Birim = string.IsNullOrWhiteSpace(kalem.Birim) ? "—" : kalem.Birim.Trim(),
                SonAlinanBirimFiyat = sonFiyat,
                EnDusukTeklifBirimFiyat = enDusuk,
                EnDusukTeklifFirma = enDusukFirma,
                FarkTl = fark,
                ArtisYuzde = yuzde,
                SonAlimYok = sonAlimYok || sonFiyat is null or <= 0,
                TeklifYok = teklifYok
            });
        }

        return sonuc;
    }

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
