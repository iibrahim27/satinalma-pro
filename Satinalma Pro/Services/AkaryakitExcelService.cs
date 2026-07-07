using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using ClosedXML.Excel;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class AkaryakitExcelService
{
    private static readonly string[] SablonBasliklari =
        ["Tarih", "Plaka", "Km", "Saat", "Verilen Yakıt Miktarı"];

    private static readonly string[] DisaAktarBasliklari =
    [
        "Kayıt Tipi", "Tarih", "Araç Tipi", "Plaka / Kod", "Araç / Makine Adı", "Yakıt Türü",
        "Miktar", "Birim", "Birim Fiyatı", "Toplam Tutar", "Tedarikçi", "Teslim Alan",
        "Km Sayacı", "Saat Sayacı", "L/100km", "Lt/Saat", "Şoför / Operatör", "Saha", "Açıklama"
    ];

    public static void SablonKaydet()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Şablonu Kaydet",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = "Akaryakit_Gecmis_Veri_Sablonu.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Geçmiş Veriler");
        for (var i = 0; i < SablonBasliklari.Length; i++)
            sayfa.Cell(1, i + 1).Value = SablonBasliklari[i];

        sayfa.Cell(2, 1).Value = "15.06.2026";
        sayfa.Cell(2, 2).Value = "34 ABC 123";
        sayfa.Cell(2, 3).Value = 125400;
        sayfa.Cell(2, 4).Value = 3420;
        sayfa.Cell(2, 5).Value = 85;

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dialog.FileName);

        MessageBox.Show(
            "Geçmiş veri şablonu kaydedildi.\n\n" +
            "Sütunlar: Tarih, Plaka, Km, Saat, Verilen Yakıt Miktarı\n\n" +
            "Her satır dağıtılan yakıt kaydı olarak eklenir. Araç bilgileri filo kayıtlarından otomatik tamamlanır.",
            UygulamaBilgisi.Ad,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public static List<AkaryakitKaydi> DosyadanOku(string dosyaYolu)
    {
        var liste = new List<AkaryakitKaydi>();
        var mevcutKayitlar = ModulVeriDeposu.Akaryakit.ToList();

        using var kitap = new XLWorkbook(dosyaYolu);
        var sayfa = kitap.Worksheet(1);
        var satirlar = sayfa.RangeUsed()?.RowsUsed().Skip(1);
        if (satirlar == null)
            return liste;

        var kolonlar = BaslikKolonlari(sayfa);
        var basitSablon = BasitSablonMu(kolonlar);
        var gecmisSablon = GecmisSablonMu(kolonlar);

        foreach (var satir in satirlar)
        {
            var kayit = basitSablon || gecmisSablon
                ? BasitSatirdanOku(satir, kolonlar)
                : TamSatirdanOku(satir, kolonlar);

            if (string.IsNullOrWhiteSpace(kayit.Tarih) &&
                string.IsNullOrWhiteSpace(kayit.PlakaVeyaKod) &&
                kayit.Miktar <= 0)
                continue;

            if (basitSablon || gecmisSablon || !kayit.AlinanKayit)
            {
                kayit.KayitTipi = "Dağıtılan";
                AkaryakitExcelTamamlayici.DagitilanKaydiniTamamla(
                    kayit,
                    mevcutKayitlar.Concat(liste),
                    excelKmSaatVar: gecmisSablon || kayit.KmSayaci is not null || kayit.SaatSayaci is not null);
            }
            else
                kayit.ToplamTutariHesapla();

            liste.Add(kayit);
        }

        return liste;
    }

    private static bool BasitSablonMu(Dictionary<string, int> kolonlar) =>
        KolonVar(kolonlar, "Tarih") &&
        (KolonVar(kolonlar, "Plaka") || KolonVar(kolonlar, "Plaka / Kod")) &&
        MiktarKolonuVar(kolonlar) &&
        !GecmisSablonMu(kolonlar);

    private static bool GecmisSablonMu(Dictionary<string, int> kolonlar) =>
        KolonVar(kolonlar, "Tarih") &&
        (KolonVar(kolonlar, "Plaka") || KolonVar(kolonlar, "Plaka / Kod")) &&
        (KolonVar(kolonlar, "Km") || KolonVar(kolonlar, "KM") || KolonVar(kolonlar, "Km Sayacı")) &&
        (KolonVar(kolonlar, "Saat") || KolonVar(kolonlar, "SAAT") || KolonVar(kolonlar, "Saat Sayacı")) &&
        MiktarKolonuVar(kolonlar);

    private static bool MiktarKolonuVar(Dictionary<string, int> kolonlar) =>
        KolonVar(kolonlar, "Verilen Yakıt Miktarı") ||
        KolonVar(kolonlar, "Verilen Miktar") ||
        KolonVar(kolonlar, "Miktar") ||
        KolonVar(kolonlar, "Verilen Yakıt");

    private static AkaryakitKaydi BasitSatirdanOku(IXLRangeRow satir, Dictionary<string, int> kolonlar) =>
        new()
        {
            KayitTipi = "Dağıtılan",
            Tarih = HucreTarih(satir, kolonlar, "Tarih"),
            PlakaVeyaKod = HucreMetin(satir, kolonlar, "Plaka", "Plaka / Kod"),
            KmSayaci = HucreNullableDouble(satir, kolonlar, "Km", "KM", "Km Sayacı"),
            SaatSayaci = HucreNullableDouble(satir, kolonlar, "Saat", "SAAT", "Saat Sayacı"),
            Miktar = HucreDouble(satir, kolonlar, "Verilen Yakıt Miktarı", "Verilen Miktar", "Miktar", "Verilen Yakıt"),
            Birim = "Lt"
        };

    private static AkaryakitKaydi TamSatirdanOku(IXLRangeRow satir, Dictionary<string, int> kolonlar)
    {
        var kayitTipi = HucreMetin(satir, kolonlar, "Kayıt Tipi");
        if (string.IsNullOrWhiteSpace(kayitTipi))
            kayitTipi = HucreDecimal(satir, kolonlar, "Birim Fiyatı") > 0 ? "Alınan" : "Dağıtılan";

        var kayit = new AkaryakitKaydi
        {
            KayitTipi = kayitTipi,
            Tarih = HucreTarih(satir, kolonlar, "Tarih"),
            AracTipi = HucreMetin(satir, kolonlar, "Araç Tipi"),
            PlakaVeyaKod = HucreMetin(satir, kolonlar, "Plaka / Kod", "Plaka"),
            AracMakineAdi = HucreMetin(satir, kolonlar, "Araç / Makine Adı"),
            YakitTuru = HucreMetin(satir, kolonlar, "Yakıt Türü"),
            Miktar = HucreDouble(satir, kolonlar, "Miktar", "Verilen Miktar", "Verilen Yakıt"),
            Birim = string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, "Birim")) ? "Lt" : HucreMetin(satir, kolonlar, "Birim"),
            BirimFiyati = HucreDecimal(satir, kolonlar, "Birim Fiyatı"),
            Tedarikci = HucreMetin(satir, kolonlar, "Tedarikçi"),
            TeslimAlan = HucreMetin(satir, kolonlar, "Teslim Alan"),
            KmSayaci = HucreNullableDouble(satir, kolonlar, "Km Sayacı"),
            SaatSayaci = HucreNullableDouble(satir, kolonlar, "Saat Sayacı"),
            SoforOperator = HucreMetin(satir, kolonlar, "Şoför / Operatör"),
            Saha = HucreMetin(satir, kolonlar, "Saha"),
            Aciklama = HucreMetin(satir, kolonlar, "Açıklama")
        };

        if (string.IsNullOrWhiteSpace(kayit.Tedarikci))
            kayit.Istasyon = HucreMetin(satir, kolonlar, "İstasyon");

        return kayit;
    }

    public static void ListeyiKaydet(IEnumerable<AkaryakitKaydi> kayitlar, string varsayilanAd)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Olarak Dışa Aktar",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = varsayilanAd
        };

        if (dialog.ShowDialog() != true)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Akaryakıt");

        for (var i = 0; i < DisaAktarBasliklari.Length; i++)
            sayfa.Cell(1, i + 1).Value = DisaAktarBasliklari[i];

        var satirNo = 2;
        foreach (var k in kayitlar)
        {
            k.ToplamTutariHesapla();
            sayfa.Cell(satirNo, 1).Value = k.KayitTipi;
            sayfa.Cell(satirNo, 2).Value = k.Tarih;
            sayfa.Cell(satirNo, 3).Value = k.AracTipi;
            sayfa.Cell(satirNo, 4).Value = k.PlakaVeyaKod;
            sayfa.Cell(satirNo, 5).Value = k.AracMakineAdi;
            sayfa.Cell(satirNo, 6).Value = k.YakitTuru;
            sayfa.Cell(satirNo, 7).Value = k.Miktar;
            sayfa.Cell(satirNo, 8).Value = k.Birim;
            sayfa.Cell(satirNo, 9).Value = k.AlinanKayit ? k.BirimFiyati : "";
            sayfa.Cell(satirNo, 10).Value = k.AlinanKayit ? k.ToplamTutar : "";
            sayfa.Cell(satirNo, 11).Value = k.AlinanKayit ? k.GosterilenTedarikci : "";
            sayfa.Cell(satirNo, 12).Value = k.AlinanKayit ? k.TeslimAlan : "";
            sayfa.Cell(satirNo, 13).Value = k.KmSayaci;
            sayfa.Cell(satirNo, 14).Value = k.SaatSayaci;
            sayfa.Cell(satirNo, 15).Value = k.Tuketim100KmMetin;
            sayfa.Cell(satirNo, 16).Value = k.TuketimSaatMetin;
            sayfa.Cell(satirNo, 17).Value = k.SoforOperator;
            sayfa.Cell(satirNo, 18).Value = k.Saha;
            sayfa.Cell(satirNo, 19).Value = k.Aciklama;
            satirNo++;
        }

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dialog.FileName);

        MessageBox.Show("Excel dosyası oluşturuldu.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
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
            var baslik = sayfa.Cell(1, c).GetString().Trim();
            if (!string.IsNullOrEmpty(baslik))
                kolonlar[baslik] = c;
        }

        return kolonlar;
    }

    private static bool KolonVar(Dictionary<string, int> kolonlar, string baslik) =>
        kolonlar.ContainsKey(baslik);

    private static string HucreMetin(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!kolonlar.TryGetValue(baslik, out var kolon))
                continue;

            return satir.Cell(kolon).GetString().Trim();
        }

        return "";
    }

    private static string HucreTarih(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!kolonlar.TryGetValue(baslik, out var kolon))
                continue;

            var hucre = satir.Cell(kolon);
            if (hucre.TryGetValue(out DateTime tarih))
                return tarih.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

            var metin = hucre.GetString().Trim();
            if (string.IsNullOrEmpty(metin))
                return "";

            if (DateTime.TryParseExact(metin, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out tarih))
                return tarih.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

            if (DateTime.TryParse(metin, CultureInfo.CurrentCulture, DateTimeStyles.None, out tarih))
                return tarih.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

            return metin;
        }

        return "";
    }

    private static double HucreDouble(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!kolonlar.TryGetValue(baslik, out var kolon))
                continue;

            var hucre = satir.Cell(kolon);
            return double.TryParse(hucre.GetString(), NumberStyles.Any, CultureInfo.CurrentCulture, out var d)
                ? d
                : hucre.TryGetValue(out double val) ? val : 0;
        }

        return 0;
    }

    private static double? HucreNullableDouble(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!kolonlar.TryGetValue(baslik, out var kolon))
                continue;

            var hucre = satir.Cell(kolon);
            var metin = hucre.GetString().Trim();
            if (string.IsNullOrEmpty(metin))
            {
                if (hucre.TryGetValue(out double sayi))
                    return sayi;

                return null;
            }

            return double.TryParse(metin, NumberStyles.Any, CultureInfo.CurrentCulture, out var d)
                ? d
                : hucre.TryGetValue(out double val) ? val : null;
        }

        return null;
    }

    private static decimal HucreDecimal(IXLRangeRow satir, Dictionary<string, int> kolonlar, params string[] basliklar)
    {
        foreach (var baslik in basliklar)
        {
            if (!kolonlar.TryGetValue(baslik, out var kolon))
                continue;

            var hucre = satir.Cell(kolon);
            return decimal.TryParse(hucre.GetString(), NumberStyles.Any, CultureInfo.CurrentCulture, out var d)
                ? d
                : hucre.TryGetValue(out decimal val) ? val : 0;
        }

        return 0;
    }
}
