using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using ClosedXML.Excel;
using System.Globalization;
using System.Windows;

namespace SatinalmaPro.Services;

public static class RaporlamaExcelService
{
    public static void DisaAktar(
        string raporTuru,
        string filtreMetni,
        List<RaporModulOzeti> modulOzetleri,
        List<RaporDetaySatiri> detaySatirlari,
        List<RaporGrupOzeti> grupOzetleri)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Olarak Dışa Aktar",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = $"Rapor_{DateTime.Now:yyyyMMdd}.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var kitap = new XLWorkbook();

        var bilgi = kitap.Worksheets.Add("Bilgi");
        bilgi.Cell(1, 1).Value = "Rapor Türü";
        bilgi.Cell(1, 2).Value = raporTuru;
        bilgi.Cell(2, 1).Value = "Filtre";
        bilgi.Cell(2, 2).Value = filtreMetni;
        bilgi.Cell(3, 1).Value = "Oluşturma";
        bilgi.Cell(3, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        bilgi.Columns().AdjustToContents();

        if (raporTuru == RaporTurleri.GenelOzet)
            ModulOzetSayfasi(kitap, modulOzetleri);
        else if (raporTuru is RaporTurleri.TedarikciOzeti or RaporTurleri.SahaOzeti or RaporTurleri.KategoriOzeti)
            GrupOzetSayfasi(kitap, raporTuru, grupOzetleri);
        else
            DetaySayfasi(kitap, detaySatirlari);

        kitap.SaveAs(dialog.FileName);
        MessageBox.Show("Excel dosyası oluşturuldu.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void ModulOzetSayfasi(XLWorkbook kitap, List<RaporModulOzeti> ozetler)
    {
        var sayfa = kitap.Worksheets.Add("Modül Özeti");
        sayfa.Cell(1, 1).Value = "Modül";
        sayfa.Cell(1, 2).Value = "Kayıt Sayısı";
        sayfa.Cell(1, 3).Value = "Toplam Tutar";
        sayfa.Row(1).Style.Font.Bold = true;

        var satir = 2;
        foreach (var o in ozetler)
        {
            sayfa.Cell(satir, 1).Value = o.ModulAdi;
            sayfa.Cell(satir, 2).Value = o.KayitSayisi;
            sayfa.Cell(satir, 3).Value = o.ToplamTutar;
            satir++;
        }

        sayfa.Cell(satir, 1).Value = "GENEL TOPLAM";
        sayfa.Cell(satir, 2).Value = ozetler.Sum(o => o.KayitSayisi);
        sayfa.Cell(satir, 3).Value = ozetler.Sum(o => o.ToplamTutar);
        sayfa.Row(satir).Style.Font.Bold = true;
        sayfa.Columns().AdjustToContents();
    }

    private static void GrupOzetSayfasi(XLWorkbook kitap, string raporTuru, List<RaporGrupOzeti> gruplar)
    {
        var sayfa = kitap.Worksheets.Add(raporTuru);
        sayfa.Cell(1, 1).Value = "Grup";
        sayfa.Cell(1, 2).Value = "Kayıt";
        sayfa.Cell(1, 3).Value = "Toplam Tutar";
        sayfa.Cell(1, 4).Value = "Modül Dağılımı";
        sayfa.Row(1).Style.Font.Bold = true;

        var satir = 2;
        foreach (var g in gruplar)
        {
            sayfa.Cell(satir, 1).Value = g.GrupAdi;
            sayfa.Cell(satir, 2).Value = g.KayitSayisi;
            sayfa.Cell(satir, 3).Value = g.ToplamTutar;
            sayfa.Cell(satir, 4).Value = g.ModulDagilimi;
            satir++;
        }

        sayfa.Columns().AdjustToContents();
    }

    private static void DetaySayfasi(XLWorkbook kitap, List<RaporDetaySatiri> satirlar)
    {
        var sayfa = kitap.Worksheets.Add("Detay");
        var basliklar = new[]
        {
            "Modül", "Tarih", "Belge No", "Kategori", "Açıklama", "Miktar", "Birim",
            "Birim Fiyat", "Artış %", "Tedarikçi", "Saha", "Tutar"
        };
        for (var i = 0; i < basliklar.Length; i++)
            sayfa.Cell(1, i + 1).Value = basliklar[i];
        sayfa.Row(1).Style.Font.Bold = true;

        var satir = 2;
        foreach (var d in satirlar)
        {
            sayfa.Cell(satir, 1).Value = d.Modul;
            sayfa.Cell(satir, 2).Value = d.Tarih;
            sayfa.Cell(satir, 3).Value = d.BelgeNo;
            sayfa.Cell(satir, 4).Value = d.Kategori;
            sayfa.Cell(satir, 5).Value = d.Aciklama;
            sayfa.Cell(satir, 6).Value = d.Miktar;
            sayfa.Cell(satir, 7).Value = d.Birim;
            sayfa.Cell(satir, 8).Value = d.BirimFiyati;
            sayfa.Cell(satir, 9).Value = d.ArtisYuzdesiMetin;
            sayfa.Cell(satir, 10).Value = d.Tedarikci;
            sayfa.Cell(satir, 11).Value = d.Saha;
            sayfa.Cell(satir, 12).Value = d.Tutar;
            satir++;
        }

        sayfa.Columns().AdjustToContents();
    }
}
