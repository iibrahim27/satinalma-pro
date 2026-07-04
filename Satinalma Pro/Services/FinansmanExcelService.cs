using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using ClosedXML.Excel;
using System.Globalization;
using System.Windows;

namespace SatinalmaPro.Services;

public static class FinansmanExcelService
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static void DisaAktar(
        string raporTuru,
        string filtreMetni,
        FinansmanGenelOzet ozet,
        List<FinansmanModulOzeti> modulOzetleri,
        List<FinansmanHareketSatiri> hareketler,
        List<FinansmanAylikOzet> aylikOzetler,
        List<FinansmanVadeSatiri> vadeler,
        List<FinansmanGrupOzeti> grupOzetleri)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Olarak Dışa Aktar",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = $"Finansman_{DateTime.Now:yyyyMMdd}.xlsx"
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
        bilgi.Cell(3, 2).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm", Tr);
        bilgi.Cell(5, 1).Value = "Toplam Gider";
        bilgi.Cell(5, 2).Value = ozet.ToplamGider;
        bilgi.Cell(6, 1).Value = "Toplam Gelir";
        bilgi.Cell(6, 2).Value = ozet.ToplamGelir;
        bilgi.Cell(7, 1).Value = "Net Nakit";
        bilgi.Cell(7, 2).Value = ozet.NetNakit;
        bilgi.Cell(8, 1).Value = "Bekleyen Ödeme";
        bilgi.Cell(8, 2).Value = ozet.BekleyenOdeme;
        bilgi.Columns().AdjustToContents();

        switch (raporTuru)
        {
            case FinansmanTurleri.FinansalOzet:
                ModulSayfasi(kitap, modulOzetleri);
                AylikSayfasi(kitap, aylikOzetler);
                if (vadeler.Count > 0)
                    VadeSayfasi(kitap, vadeler, "Vadeler");
                break;
            case FinansmanTurleri.NakitAkisi:
                AylikSayfasi(kitap, aylikOzetler);
                break;
            case FinansmanTurleri.VadeTakvimi or FinansmanTurleri.BekleyenOdemeler:
                VadeSayfasi(kitap, vadeler, raporTuru);
                break;
            case FinansmanTurleri.SahaOzeti or FinansmanTurleri.TedarikciOzeti:
                GrupSayfasi(kitap, grupOzetleri, raporTuru);
                break;
            case FinansmanTurleri.ModulDagilimi:
                ModulSayfasi(kitap, modulOzetleri);
                break;
            default:
                HareketSayfasi(kitap, hareketler, raporTuru);
                break;
        }

        kitap.SaveAs(dialog.FileName);
        MessageBox.Show("Excel dosyası oluşturuldu.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static void GelirSablonKaydet()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Gelir Şablonu Kaydet",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = "Finansman_Gelir_Sablonu.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Gelir");
        var basliklar = new[]
        {
            "Tarih", "Belge No", "Kategori", "Açıklama", "Kaynak",
            "Saha", "Tutar", "Ödeme Şekli", "Notlar"
        };

        for (var i = 0; i < basliklar.Length; i++)
            sayfa.Cell(1, i + 1).Value = basliklar[i];

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EDE9FE");
        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dialog.FileName);

        MessageBox.Show("Gelir şablonu kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static List<FinansmanGelirKaydi> GelirDosyadanOku(string dosyaYolu)
    {
        using var kitap = new XLWorkbook(dosyaYolu);
        var sayfa = kitap.Worksheet(1);
        var kolonlar = BaslikHaritasi(sayfa);
        var liste = new List<FinansmanGelirKaydi>();

        foreach (var satir in sayfa.RowsUsed().Skip(1))
        {
            var tutar = HucreDecimal(satir, kolonlar, "Tutar");
            if (tutar <= 0)
                continue;

            liste.Add(new FinansmanGelirKaydi
            {
                Tarih = HucreMetin(satir, kolonlar, "Tarih"),
                BelgeNo = HucreMetin(satir, kolonlar, "Belge No"),
                Kategori = HucreMetin(satir, kolonlar, "Kategori"),
                Aciklama = HucreMetin(satir, kolonlar, "Açıklama"),
                Kaynak = HucreMetin(satir, kolonlar, "Kaynak"),
                Saha = HucreMetin(satir, kolonlar, "Saha"),
                Tutar = tutar,
                OdemeSekli = HucreMetin(satir, kolonlar, "Ödeme Şekli"),
                Notlar = HucreMetin(satir, kolonlar, "Notlar")
            });
        }

        return liste;
    }

    private static void ModulSayfasi(XLWorkbook kitap, List<FinansmanModulOzeti> ozetler)
    {
        var sayfa = kitap.Worksheets.Add("Modül Dağılımı");
        sayfa.Cell(1, 1).Value = "Modül";
        sayfa.Cell(1, 2).Value = "Tip";
        sayfa.Cell(1, 3).Value = "Kayıt";
        sayfa.Cell(1, 4).Value = "Toplam Tutar";
        sayfa.Row(1).Style.Font.Bold = true;

        var satir = 2;
        foreach (var o in ozetler)
        {
            sayfa.Cell(satir, 1).Value = o.ModulAdi;
            sayfa.Cell(satir, 2).Value = o.Tip;
            sayfa.Cell(satir, 3).Value = o.KayitSayisi;
            sayfa.Cell(satir, 4).Value = o.ToplamTutar;
            satir++;
        }

        sayfa.Columns().AdjustToContents();
    }

    private static void AylikSayfasi(XLWorkbook kitap, List<FinansmanAylikOzet> aylar)
    {
        var sayfa = kitap.Worksheets.Add("Nakit Akışı");
        sayfa.Cell(1, 1).Value = "Ay";
        sayfa.Cell(1, 2).Value = "Gider";
        sayfa.Cell(1, 3).Value = "Gelir";
        sayfa.Cell(1, 4).Value = "Net";
        sayfa.Cell(1, 5).Value = "Hareket";
        sayfa.Row(1).Style.Font.Bold = true;

        var satir = 2;
        foreach (var a in aylar)
        {
            sayfa.Cell(satir, 1).Value = a.Ay;
            sayfa.Cell(satir, 2).Value = a.Gider;
            sayfa.Cell(satir, 3).Value = a.Gelir;
            sayfa.Cell(satir, 4).Value = a.Net;
            sayfa.Cell(satir, 5).Value = a.HareketSayisi;
            satir++;
        }

        sayfa.Columns().AdjustToContents();
    }

    private static void VadeSayfasi(XLWorkbook kitap, List<FinansmanVadeSatiri> vadeler, string ad)
    {
        var sayfa = kitap.Worksheets.Add(ad.Length > 31 ? ad[..31] : ad);
        var basliklar = new[] { "Vade Tarihi", "İşlem Tarihi", "Firma", "Belge No", "Açıklama", "Vade Gün", "Tutar", "KDV Dahil", "Durum", "Kalan Gün" };
        for (var i = 0; i < basliklar.Length; i++)
            sayfa.Cell(1, i + 1).Value = basliklar[i];
        sayfa.Row(1).Style.Font.Bold = true;

        var satir = 2;
        foreach (var v in vadeler)
        {
            sayfa.Cell(satir, 1).Value = v.VadeTarihi;
            sayfa.Cell(satir, 2).Value = v.IslemTarihi;
            sayfa.Cell(satir, 3).Value = v.Firma;
            sayfa.Cell(satir, 4).Value = v.BelgeNo;
            sayfa.Cell(satir, 5).Value = v.Aciklama;
            sayfa.Cell(satir, 6).Value = v.VadeGunu;
            sayfa.Cell(satir, 7).Value = v.Tutar;
            sayfa.Cell(satir, 8).Value = v.KdvDahilTutar;
            sayfa.Cell(satir, 9).Value = v.DurumMetin;
            sayfa.Cell(satir, 10).Value = v.KalanGun;
            satir++;
        }

        sayfa.Columns().AdjustToContents();
    }

    private static void GrupSayfasi(XLWorkbook kitap, List<FinansmanGrupOzeti> gruplar, string ad)
    {
        var sayfa = kitap.Worksheets.Add(ad.Length > 31 ? ad[..31] : ad);
        sayfa.Cell(1, 1).Value = "Grup";
        sayfa.Cell(1, 2).Value = "Kayıt";
        sayfa.Cell(1, 3).Value = "Gider";
        sayfa.Cell(1, 4).Value = "Gelir";
        sayfa.Cell(1, 5).Value = "Modül Dağılımı";
        sayfa.Row(1).Style.Font.Bold = true;

        var satir = 2;
        foreach (var g in gruplar)
        {
            sayfa.Cell(satir, 1).Value = g.GrupAdi;
            sayfa.Cell(satir, 2).Value = g.KayitSayisi;
            sayfa.Cell(satir, 3).Value = g.GiderTutar;
            sayfa.Cell(satir, 4).Value = g.GelirTutar;
            sayfa.Cell(satir, 5).Value = g.ModulDagilimi;
            satir++;
        }

        sayfa.Columns().AdjustToContents();
    }

    private static void HareketSayfasi(XLWorkbook kitap, List<FinansmanHareketSatiri> hareketler, string ad)
    {
        var sayfa = kitap.Worksheets.Add(ad.Length > 31 ? ad[..31] : ad);
        var basliklar = new[] { "Tip", "Tarih", "Modül", "Belge No", "Kategori", "Açıklama", "Tedarikçi/Kaynak", "Saha", "Tutar", "Ödeme Şekli" };
        for (var i = 0; i < basliklar.Length; i++)
            sayfa.Cell(1, i + 1).Value = basliklar[i];
        sayfa.Row(1).Style.Font.Bold = true;

        var satir = 2;
        foreach (var h in hareketler)
        {
            sayfa.Cell(satir, 1).Value = h.Tip;
            sayfa.Cell(satir, 2).Value = h.Tarih;
            sayfa.Cell(satir, 3).Value = h.Modul;
            sayfa.Cell(satir, 4).Value = h.BelgeNo;
            sayfa.Cell(satir, 5).Value = h.Kategori;
            sayfa.Cell(satir, 6).Value = h.Aciklama;
            sayfa.Cell(satir, 7).Value = h.Tedarikci;
            sayfa.Cell(satir, 8).Value = h.Saha;
            sayfa.Cell(satir, 9).Value = h.Tutar;
            sayfa.Cell(satir, 10).Value = h.OdemeSekli;
            satir++;
        }

        sayfa.Columns().AdjustToContents();
    }

    private static Dictionary<string, int> BaslikHaritasi(IXLWorksheet sayfa)
    {
        var harita = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var satir = sayfa.Row(1);
        foreach (var hucre in satir.CellsUsed())
        {
            var baslik = hucre.GetString().Trim();
            if (!string.IsNullOrWhiteSpace(baslik))
                harita[baslik] = hucre.Address.ColumnNumber;
        }
        return harita;
    }

    private static string HucreMetin(IXLRow satir, Dictionary<string, int> kolonlar, string baslik) =>
        kolonlar.TryGetValue(baslik, out var kolon)
            ? satir.Cell(kolon).GetString().Trim()
            : "";

    private static decimal HucreDecimal(IXLRow satir, Dictionary<string, int> kolonlar, string baslik) =>
        kolonlar.TryGetValue(baslik, out var kolon) && satir.Cell(kolon).TryGetValue(out decimal val)
            ? val
            : 0;
}
