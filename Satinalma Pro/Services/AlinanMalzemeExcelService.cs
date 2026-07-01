using SatinalmaPro.Helpers;
using System.Globalization;
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

                MalzemeHizmet = HucreMetin(satir, kolonlar, "Malzeme / Hizmet"),

                Miktar = HucreDouble(satir, kolonlar, "Miktar"),

                Birim = HucreMetin(satir, kolonlar, "Birim"),

                BirimFiyati = HucreDecimal(satir, kolonlar, "Birim Fiyatı"),

                Tedarikci = HucreMetin(satir, kolonlar, "Tedarikçi"),

                IndirildigiSaha = HucreMetin(satir, kolonlar, "İndirildiği Saha"),

                TeslimAlan = HucreMetin(satir, kolonlar, "Teslim Alan"),

                Aciklama = HucreMetin(satir, kolonlar, "Açıklama")

            };



            kayit.ToplamTutariHesapla();

            liste.Add(kayit);

        }



        return liste;

    }



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



        using var kitap = new XLWorkbook();

        var sayfa = kitap.Worksheets.Add("Alınan Malzemeler");



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



    private static string FaturaNoOku(IXLRangeRow satir, Dictionary<string, int> kolonlar)
    {
        foreach (var baslik in new[] { "Fatura No", "Fiş No", "Fis No" })
        {
            if (!kolonlar.ContainsKey(baslik))
                continue;

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
        if (!string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, "Malzeme / Hizmet")))
            return false;
        if (!string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, "Kategori")))
            return false;
        if (HucreDouble(satir, kolonlar, "Miktar") > 0)
            return false;
        if (!string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, "Tedarikçi")))
            return false;

        return true;
    }

    private static string HucreMetin(IXLRangeRow satir, Dictionary<string, int> kolonlar, string baslik) =>

        kolonlar.TryGetValue(baslik, out var kolon) ? satir.Cell(kolon).GetString().Trim() : "";



    private static double HucreDouble(IXLRangeRow satir, Dictionary<string, int> kolonlar, string baslik)

    {

        if (!kolonlar.TryGetValue(baslik, out var kolon))

            return 0;



        return double.TryParse(satir.Cell(kolon).GetString(), NumberStyles.Any, CultureInfo.CurrentCulture, out var d)

            ? d

            : satir.Cell(kolon).TryGetValue(out double val) ? val : 0;

    }



    private static decimal HucreDecimal(IXLRangeRow satir, Dictionary<string, int> kolonlar, string baslik)

    {

        if (!kolonlar.TryGetValue(baslik, out var kolon))

            return 0;



        return decimal.TryParse(satir.Cell(kolon).GetString(), NumberStyles.Any, CultureInfo.CurrentCulture, out var d)

            ? d

            : satir.Cell(kolon).TryGetValue(out decimal val) ? val : 0;
    }
}
