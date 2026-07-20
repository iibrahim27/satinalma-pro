using SatinalmaPro.Helpers;
using System.Globalization;
using System.Text;
using System.Windows;
using ClosedXML.Excel;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class AlinanMalzemeExcelService
{
    private static readonly string[] SablonBasliklari =
    [
        "Tarih", "Fatura No", "Kategori", "Malzeme / Hizmet", "Miktar", "Birim",
        "Birim Fiyatı", "Tedarikçi", "İndirildiği Saha", "Teslim Alan", "Açıklama"
    ];

    private static readonly string[] DisaAktarBasliklari =
    [
        "Tarih", "Fatura No", "Kategori", "Malzeme / Hizmet", "Miktar", "Birim",
        "Birim Fiyatı", "Toplam Tutar", "Tedarikçi", "İndirildiği Saha", "Teslim Alan", "Açıklama"
    ];

    private static readonly string[] TedarikciBasliklari =
    [
        "Tedarikçi", "Tedarikci", "Tedarikçi Adı", "Tedarikci Adi",
        "Firma", "Firma Adı", "Firma Adi", "Satıcı", "Satici", "Ünvan", "Unvan"
    ];

    private static readonly string[] MalzemeBasliklari =
    [
        "Malzeme / Hizmet", "Malzeme/Hizmet", "Malzeme", "Malzeme Hizmet", "Hizmet"
    ];

    private static readonly string[] BirimFiyatBasliklari =
    [
        "Birim Fiyatı", "Birim Fiyati", "Birim Fiyat", "Fiyat"
    ];

    private static readonly string[] SahaBasliklari =
    [
        "İndirildiği Saha", "Indirildigi Saha", "Saha", "İndirildiği Yer"
    ];

    /// <summary>
    /// Excel satırlarını mevcut kayıtlarda eşleştirip boş tedarikçi alanlarını doldurur.
    /// Satınalma mal kabul kayıtlarına dokunur; sadece boş Tedarikçi güncellenir.
    /// </summary>
    public static int BosTedarikcileriGuncelle(
        IList<AlinanMalzemeKaydi> mevcutKayitlar,
        IEnumerable<AlinanMalzemeKaydi> excelKayitlari)
    {
        var guncellenen = 0;
        var excelListe = excelKayitlari
            .Where(e => !string.IsNullOrWhiteSpace(e.Tedarikci))
            .ToList();

        foreach (var excel in excelListe)
        {
            var adaylar = mevcutKayitlar.Where(m =>
                string.IsNullOrWhiteSpace(m.Tedarikci) &&
                KayitEslestir(m, excel)).ToList();

            foreach (var mevcut in adaylar)
            {
                mevcut.Tedarikci = excel.Tedarikci.Trim();
                guncellenen++;
            }
        }

        return guncellenen;
    }

    private static bool KayitEslestir(AlinanMalzemeKaydi a, AlinanMalzemeKaydi b)
    {
        if (!string.Equals(
                (a.MalzemeHizmet ?? "").Trim(),
                (b.MalzemeHizmet ?? "").Trim(),
                StringComparison.OrdinalIgnoreCase))
            return false;

        var tarihA = TarihYardimcisi.Normalize(a.Tarih);
        var tarihB = TarihYardimcisi.Normalize(b.Tarih);
        if (!string.IsNullOrWhiteSpace(tarihA) && !string.IsNullOrWhiteSpace(tarihB)
            && !string.Equals(tarihA, tarihB, StringComparison.Ordinal))
            return false;

        if (Math.Abs(a.Miktar - b.Miktar) > 0.0001)
            return false;

        if (Math.Abs(a.BirimFiyati - b.BirimFiyati) > 0.01m)
            return false;

        var faturaA = (a.FaturaNo ?? "").Trim();
        var faturaB = (b.FaturaNo ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(faturaA) && !string.IsNullOrWhiteSpace(faturaB)
            && !string.Equals(faturaA, faturaB, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public static void SablonKaydet()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Şablonu Kaydet",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = "AlinanMalzemeler_Sablon.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Alınan Malzemeler");
        for (var i = 0; i < SablonBasliklari.Length; i++)
            sayfa.Cell(1, i + 1).Value = SablonBasliklari[i];

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dialog.FileName);

        MessageBox.Show("Şablon başarıyla kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static List<AlinanMalzemeKaydi> DosyadanOku(string dosyaYolu)
    {
        var liste = new List<AlinanMalzemeKaydi>();

        using var kitap = new XLWorkbook(dosyaYolu);
        var sayfa = kitap.Worksheet(1);
        var satirlar = sayfa.RangeUsed()?.RowsUsed().Skip(1);
        if (satirlar == null)
            return liste;

        var kolonlar = BaslikKolonlari(sayfa);

        foreach (var satir in satirlar)
        {
            if (SatirAnlamsiz(satir, kolonlar))
                continue;

            var kayit = new AlinanMalzemeKaydi
            {
                Tarih = ExcelHucreYardimcisi.TarihOku(satir, kolonlar, "Tarih"),
                FaturaNo = FaturaNoOku(satir, kolonlar),
                Kategori = HucreMetin(satir, kolonlar, "Kategori"),
                MalzemeHizmet = HucreMetin(satir, kolonlar, MalzemeBasliklari),
                Miktar = HucreDouble(satir, kolonlar, "Miktar"),
                Birim = HucreMetin(satir, kolonlar, "Birim"),
                BirimFiyati = HucreDecimal(satir, kolonlar, BirimFiyatBasliklari),
                Tedarikci = HucreMetin(satir, kolonlar, TedarikciBasliklari),
                IndirildigiSaha = HucreMetin(satir, kolonlar, SahaBasliklari),
                TeslimAlan = HucreMetin(satir, kolonlar, "Teslim Alan", "Teslim Alanı", "TeslimAlan"),
                Aciklama = HucreMetin(satir, kolonlar, "Açıklama", "Aciklama", "Not")
            };

            kayit.ToplamTutariHesapla();
            liste.Add(kayit);
        }

        return liste;
    }

    /// <summary>
    /// Görünen kayıtları Excel'e yazar.
    /// Birden fazla kategori varsa (ör. filtre = Tümü) her kategori ayrı sayfaya yazılır.
    /// Tek kategoride tek sayfa kullanılır.
    /// </summary>
    public static void ListeyiKaydet(IEnumerable<AlinanMalzemeKaydi> kayitlar, string varsayilanAd)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Olarak Dışa Aktar",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = varsayilanAd
        };

        if (dialog.ShowDialog() != true)
            return;

        var liste = kayitlar?.ToList() ?? [];
        using var kitap = new XLWorkbook();

        var gruplar = liste
            .GroupBy(k => string.IsNullOrWhiteSpace(k.Kategori) ? "Kategorisiz" : k.Kategori.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (gruplar.Count <= 1)
        {
            var ad = gruplar.Count == 1 ? ExcelSayfaAdi(gruplar[0].Key, []) : "Alınan Malzemeler";
            SayfaYaz(kitap, ad, liste);
        }
        else
        {
            var kullanilan = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var grup in gruplar)
                SayfaYaz(kitap, ExcelSayfaAdi(grup.Key, kullanilan), grup.ToList());
        }

        kitap.SaveAs(dialog.FileName);

        var mesaj = gruplar.Count > 1
            ? $"Excel dosyası oluşturuldu.\n{gruplar.Count} kategori ayrı sayfalara yazıldı."
            : "Excel dosyası oluşturuldu.";
        MessageBox.Show(mesaj, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void SayfaYaz(XLWorkbook kitap, string sayfaAdi, IReadOnlyList<AlinanMalzemeKaydi> kayitlar)
    {
        var sayfa = kitap.Worksheets.Add(sayfaAdi);

        for (var i = 0; i < DisaAktarBasliklari.Length; i++)
            sayfa.Cell(1, i + 1).Value = DisaAktarBasliklari[i];

        var satirNo = 2;
        foreach (var k in kayitlar)
        {
            k.ToplamTutariHesapla();
            sayfa.Cell(satirNo, 1).Value = k.Tarih;
            sayfa.Cell(satirNo, 2).Value = k.FaturaNo;
            sayfa.Cell(satirNo, 3).Value = k.Kategori;
            sayfa.Cell(satirNo, 4).Value = k.MalzemeHizmet;
            sayfa.Cell(satirNo, 5).Value = k.Miktar;
            sayfa.Cell(satirNo, 6).Value = k.Birim;
            sayfa.Cell(satirNo, 7).Value = k.BirimFiyati;
            sayfa.Cell(satirNo, 8).Value = k.ToplamTutar;
            sayfa.Cell(satirNo, 9).Value = k.Tedarikci;
            sayfa.Cell(satirNo, 10).Value = k.IndirildigiSaha;
            sayfa.Cell(satirNo, 11).Value = k.TeslimAlan;
            sayfa.Cell(satirNo, 12).Value = k.Aciklama;
            satirNo++;
        }

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
        sayfa.Columns().AdjustToContents();
    }

    /// <summary>Excel sayfa adı kuralları: max 31 karakter, yasak karakter yok, benzersiz.</summary>
    private static string ExcelSayfaAdi(string? kategori, HashSet<string> kullanilan)
    {
        var ad = string.IsNullOrWhiteSpace(kategori) ? "Kategorisiz" : kategori.Trim();
        foreach (var c in new[] { '\\', '/', '?', '*', '[', ']', ':', '\'' })
            ad = ad.Replace(c, '_');

        if (ad.Length > 31)
            ad = ad[..31];
        if (string.IsNullOrWhiteSpace(ad))
            ad = "Kategorisiz";

        var sonuc = ad;
        var i = 2;
        while (!kullanilan.Add(sonuc))
        {
            var ek = $" ({i})";
            var taban = ad.Length + ek.Length > 31 ? ad[..(31 - ek.Length)] : ad;
            sonuc = taban + ek;
            i++;
        }

        return sonuc;
    }

    private static Dictionary<string, int> BaslikKolonlari(IXLWorksheet sayfa)
    {
        var kolonlar = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var kullanilan = sayfa.RangeUsed();
        if (kullanilan == null)
            return kolonlar;

        var sonKolon = kullanilan.LastColumn().ColumnNumber();
        for (var c = 1; c <= sonKolon; c++)
        {
            var baslik = HucreMetinOku(sayfa.Cell(1, c));
            if (string.IsNullOrEmpty(baslik))
                continue;

            kolonlar[baslik] = c;
            var norm = BaslikNormalize(baslik);
            if (!string.IsNullOrEmpty(norm) && !kolonlar.ContainsKey(norm))
                kolonlar[norm] = c;
        }

        return kolonlar;
    }

    private static string FaturaNoOku(IXLRangeRow satir, Dictionary<string, int> kolonlar)
    {
        foreach (var baslik in new[] { "Fatura No", "FaturaNo", "Fiş No", "Fis No", "İrsaliye No", "Irsaliye No" })
        {
            var deger = HucreMetin(satir, kolonlar, baslik);
            if (!string.IsNullOrWhiteSpace(deger))
                return deger.Trim();
        }

        return "";
    }

    private static bool SatirAnlamsiz(IXLRangeRow satir, Dictionary<string, int> kolonlar)
    {
        if (!string.IsNullOrWhiteSpace(ExcelHucreYardimcisi.TarihOku(satir, kolonlar, "Tarih")))
            return false;
        if (!string.IsNullOrWhiteSpace(FaturaNoOku(satir, kolonlar)))
            return false;
        if (!string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, MalzemeBasliklari)))
            return false;
        if (!string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, "Kategori")))
            return false;
        if (HucreDouble(satir, kolonlar, "Miktar") > 0)
            return false;
        if (!string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, TedarikciBasliklari)))
            return false;

        return true;
    }

    private static string HucreMetin(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!KolonBul(kolonlar, baslik, out var kolon))
                continue;

            var deger = HucreMetinOku(satir.Cell(kolon));
            if (!string.IsNullOrWhiteSpace(deger))
                return deger;
        }

        return "";
    }

    private static bool KolonBul(Dictionary<string, int> kolonlar, string baslik, out int kolon)
    {
        if (kolonlar.TryGetValue(baslik, out kolon))
            return true;

        var norm = BaslikNormalize(baslik);
        if (!string.IsNullOrEmpty(norm) && kolonlar.TryGetValue(norm, out kolon))
            return true;

        foreach (var kv in kolonlar)
        {
            if (BaslikNormalize(kv.Key) != norm)
                continue;
            kolon = kv.Value;
            return true;
        }

        kolon = 0;
        return false;
    }

    /// <summary>Türkçe karakter / boşluk / noktalama farklarını yok sayarak başlık eşleştirir.</summary>
    private static string BaslikNormalize(string? baslik)
    {
        if (string.IsNullOrWhiteSpace(baslik))
            return "";

        var sb = new StringBuilder(baslik.Length);
        foreach (var ch in baslik.Trim().Normalize(NormalizationForm.FormC))
        {
            var c = ch switch
            {
                'ı' or 'İ' or 'I' or 'i' => 'i',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ü' or 'Ü' => 'u',
                'ö' or 'Ö' => 'o',
                'ç' or 'Ç' => 'c',
                _ => char.ToLowerInvariant(ch)
            };

            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private static string HucreMetinOku(IXLCell hucre)
    {
        if (hucre.IsEmpty())
            return "";

        var metin = hucre.GetString()?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(metin))
            return metin;

        metin = hucre.GetFormattedString()?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(metin))
            return metin;

        try
        {
            var deger = hucre.Value;
            if (!deger.IsBlank)
            {
                metin = deger.ToString()?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(metin))
                    return metin;
            }
        }
        catch
        {
            // ClosedXML bazı hücre tiplerinde Value okuyamayabilir
        }

        return "";
    }

    private static double HucreDouble(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!KolonBul(kolonlar, baslik, out var kolon))
                continue;

            var hucre = satir.Cell(kolon);
            var metin = HucreMetinOku(hucre);
            if (double.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out var d))
                return d;
            if (double.TryParse(metin, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return d;
            if (hucre.TryGetValue(out double val))
                return val;
        }

        return 0;
    }

    private static decimal HucreDecimal(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!KolonBul(kolonlar, baslik, out var kolon))
                continue;

            var hucre = satir.Cell(kolon);
            var metin = HucreMetinOku(hucre);
            if (decimal.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out var d))
                return d;
            if (decimal.TryParse(metin, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
                return d;
            if (hucre.TryGetValue(out decimal val))
                return val;
        }

        return 0;
    }
}
