using ClosedXML.Excel;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models.SatinalmaMerkezi;
using System.Windows;

namespace SatinalmaPro.Services;

public static class SatinalmaPanosuExcelService
{
    private static readonly string[] TalepBasliklari =
    [
        "Talep No", "Talep Eden", "Şantiye", "Malzeme", "Kategori", "Öncelik",
        "Teklif Sayısı", "Durum", "Son İşlem"
    ];

    private static readonly string[] IadeBasliklari =
    [
        "İade No", "Sipariş No", "Firma", "Malzeme", "Miktar", "Neden", "Durum", "Tarih"
    ];

    private static readonly string[] TedarikciBasliklari =
    [
        "Firma", "Toplam Sipariş", "Toplam Tutar", "Zamanında Teslim", "Eksik Teslim",
        "İade", "Kalite", "Ort. Teslim", "Performans"
    ];

    public static void TalepListesiKaydet(IEnumerable<SatinalmaPanosuTalepSatir> satirlar, string varsayilanAd = "Satinalma_Panosu.xlsx")
    {
        var liste = satirlar.ToList();
        if (liste.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak kayıt bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dosya = KaydetDialog("Satınalma Panosu Excel", varsayilanAd);
        if (dosya is null)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Talepler");
        BaslikYaz(sayfa, TalepBasliklari);

        var satirNo = 2;
        foreach (var s in liste)
        {
            sayfa.Cell(satirNo, 1).Value = s.TalepNo;
            sayfa.Cell(satirNo, 2).Value = s.TalepEden;
            sayfa.Cell(satirNo, 3).Value = s.Santiye;
            sayfa.Cell(satirNo, 4).Value = s.Malzeme;
            sayfa.Cell(satirNo, 5).Value = s.Kategori;
            sayfa.Cell(satirNo, 6).Value = s.Oncelik;
            sayfa.Cell(satirNo, 7).Value = s.TeklifSayisi;
            sayfa.Cell(satirNo, 8).Value = s.Durum;
            sayfa.Cell(satirNo, 9).Value = s.SonIslem;
            satirNo++;
        }

        Sonlandir(sayfa, kitap, dosya);
    }

    public static void IadeListesiKaydet(IEnumerable<IadeSatirModel> satirlar, string varsayilanAd = "Satinalma_Iade.xlsx")
    {
        var liste = satirlar.ToList();
        if (liste.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak iade kaydı bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dosya = KaydetDialog("İade Listesi Excel", varsayilanAd);
        if (dosya is null)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("İade");
        BaslikYaz(sayfa, IadeBasliklari);

        var satirNo = 2;
        foreach (var s in liste)
        {
            sayfa.Cell(satirNo, 1).Value = s.IadeNo;
            sayfa.Cell(satirNo, 2).Value = s.SiparisNo;
            sayfa.Cell(satirNo, 3).Value = s.Firma;
            sayfa.Cell(satirNo, 4).Value = s.Malzeme;
            sayfa.Cell(satirNo, 5).Value = s.Miktar;
            sayfa.Cell(satirNo, 6).Value = s.Neden;
            sayfa.Cell(satirNo, 7).Value = s.Durum;
            sayfa.Cell(satirNo, 8).Value = s.Tarih;
            satirNo++;
        }

        Sonlandir(sayfa, kitap, dosya);
    }

    public static void TedarikciListesiKaydet(IEnumerable<TedarikciPerformansModel> satirlar, string varsayilanAd = "Satinalma_Tedarikciler.xlsx")
    {
        var liste = satirlar.ToList();
        if (liste.Count == 0)
        {
            MessageBox.Show("Dışa aktarılacak tedarikçi kaydı bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dosya = KaydetDialog("Tedarikçi Performans Excel", varsayilanAd);
        if (dosya is null)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Tedarikçiler");
        BaslikYaz(sayfa, TedarikciBasliklari);

        var satirNo = 2;
        foreach (var s in liste)
        {
            sayfa.Cell(satirNo, 1).Value = s.Firma;
            sayfa.Cell(satirNo, 2).Value = s.ToplamSiparis;
            sayfa.Cell(satirNo, 3).Value = s.ToplamTutar;
            sayfa.Cell(satirNo, 4).Value = s.ZamanindaTeslim;
            sayfa.Cell(satirNo, 5).Value = s.EksikTeslim;
            sayfa.Cell(satirNo, 6).Value = s.Iade;
            sayfa.Cell(satirNo, 7).Value = s.Kalite;
            sayfa.Cell(satirNo, 8).Value = s.OrtTeslimSuresi;
            sayfa.Cell(satirNo, 9).Value = s.PerformansPuani;
            satirNo++;
        }

        Sonlandir(sayfa, kitap, dosya);
    }

    private static string? KaydetDialog(string baslik, string varsayilanAd)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = baslik,
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = varsayilanAd
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void BaslikYaz(IXLWorksheet sayfa, string[] basliklar)
    {
        for (var i = 0; i < basliklar.Length; i++)
            sayfa.Cell(1, i + 1).Value = basliklar[i];

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
    }

    private static void Sonlandir(IXLWorksheet sayfa, XLWorkbook kitap, string dosya)
    {
        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dosya);
        MessageBox.Show("Excel dosyası oluşturuldu.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
