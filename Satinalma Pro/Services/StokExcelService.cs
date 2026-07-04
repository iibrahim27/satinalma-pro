using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using ClosedXML.Excel;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class StokExcelService
{
    private static readonly string[] Basliklar =
    [
        "Malzeme Adı", "Kategori", "Birim", "Mevcut Miktar", "Minimum Stok",
        "Depo / Saha", "Birim Maliyet", "Son Güncelleme", "Açıklama"
    ];

    public static void SablonKaydet()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Şablonu Kaydet",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = "StokYonetimi_Sablon.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Stok Yönetimi");
        for (var i = 0; i < Basliklar.Length; i++)
            sayfa.Cell(1, i + 1).Value = Basliklar[i];

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");
        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dialog.FileName);

        MessageBox.Show("Şablon başarıyla kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static List<StokKaydi> DosyadanOku(string dosyaYolu)
    {
        var liste = new List<StokKaydi>();

        using var kitap = new XLWorkbook(dosyaYolu);
        var sayfa = kitap.Worksheet(1);
        var satirlar = sayfa.RangeUsed()?.RowsUsed().Skip(1);
        if (satirlar == null)
            return liste;

        foreach (var satir in satirlar)
        {
            var malzeme = satir.Cell(1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(malzeme))
                continue;

            var kayit = new StokKaydi
            {
                MalzemeAdi = malzeme,
                Kategori = satir.Cell(2).GetString().Trim(),
                Birim = satir.Cell(3).GetString().Trim(),
                MevcutMiktar = HucreDouble(satir.Cell(4)),
                MinimumStok = HucreDouble(satir.Cell(5)),
                DepoSaha = satir.Cell(6).GetString().Trim(),
                BirimMaliyet = HucreDecimal(satir.Cell(7)),
                SonGuncelleme = satir.Cell(8).GetString().Trim(),
                Aciklama = satir.Cell(9).GetString().Trim()
            };

            if (string.IsNullOrWhiteSpace(kayit.SonGuncelleme))
                kayit.SonGuncelleme = DateTime.Now.ToString("dd.MM.yyyy");

            kayit.ToplamDegerHesapla();
            liste.Add(kayit);
        }

        return liste;
    }

    public static void ListeyiKaydet(IEnumerable<StokKaydi> kayitlar, string varsayilanAd)
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
        var sayfa = kitap.Worksheets.Add("Stok Yönetimi");

        for (var i = 0; i < Basliklar.Length; i++)
            sayfa.Cell(1, i + 1).Value = Basliklar[i];

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");

        var satirNo = 2;
        foreach (var k in kayitlar)
        {
            k.ToplamDegerHesapla();
            sayfa.Cell(satirNo, 1).Value = k.MalzemeAdi;
            sayfa.Cell(satirNo, 2).Value = k.Kategori;
            sayfa.Cell(satirNo, 3).Value = k.Birim;
            sayfa.Cell(satirNo, 4).Value = k.MevcutMiktar;
            sayfa.Cell(satirNo, 5).Value = k.MinimumStok;
            sayfa.Cell(satirNo, 6).Value = k.DepoSaha;
            sayfa.Cell(satirNo, 7).Value = k.BirimMaliyet;
            sayfa.Cell(satirNo, 8).Value = k.SonGuncelleme;
            sayfa.Cell(satirNo, 9).Value = k.Aciklama;
            satirNo++;
        }

        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dialog.FileName);

        MessageBox.Show("Liste Excel olarak kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static double HucreDouble(IXLCell hucre)
    {
        if (hucre.TryGetValue(out double d))
            return d;
        return double.TryParse(hucre.GetString(), NumberStyles.Any, CultureInfo.CurrentCulture, out d) ? d : 0;
    }

    private static decimal HucreDecimal(IXLCell hucre)
    {
        if (hucre.TryGetValue(out decimal d))
            return d;
        return decimal.TryParse(hucre.GetString(), NumberStyles.Any, CultureInfo.CurrentCulture, out d) ? d : 0;
    }
}
