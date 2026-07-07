using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ClosedXML.Excel;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class CimentoExcelService
{
    private static readonly string[] SablonBasliklari =
    [
        "Tarih", "İrsaliye No", "Çimento Sınıfı", "Çimento Cinsi", "Miktar", "Birim",
        "Birim Fiyatı", "Tedarikçi", "İndirildiği Saha", "Teslim Alan", "Açıklama"
    ];

    private static readonly string[] DisaAktarBasliklari =
    [
        "Tarih", "İrsaliye No", "Çimento Sınıfı", "Çimento Cinsi", "Miktar", "Birim",
        "Birim Fiyatı", "Toplam Tutar", "Tedarikçi", "İndirildiği Saha", "Teslim Alan", "Açıklama"
    ];

    public static void SablonKaydet()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Şablonu Kaydet",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = "Cimento_Sablon.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Çimento");
        for (var i = 0; i < SablonBasliklari.Length; i++)
            sayfa.Cell(1, i + 1).Value = SablonBasliklari[i];

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dialog.FileName);

        MessageBox.Show("Şablon başarıyla kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static List<CimentoKaydi> DosyadanOku(string dosyaYolu)
    {
        var liste = new List<CimentoKaydi>();

        using var kitap = new XLWorkbook(dosyaYolu);
        var sayfa = kitap.Worksheet(1);
        var satirlar = sayfa.RangeUsed()?.RowsUsed().Skip(1);
        if (satirlar == null)
            return liste;

        var kolonlar = BaslikKolonlari(sayfa);

        foreach (var satir in satirlar)
        {
            if (satir.Cell(1).IsEmpty() && satir.Cell(2).IsEmpty())
                continue;

            var kayit = new CimentoKaydi
            {
                Tarih = ExcelHucreYardimcisi.TarihOku(satir, kolonlar, "Tarih"),
                IrsaliyeNo = IrsaliyeNoOku(satir, kolonlar),
                CimentoSinifi = HucreMetin(satir, kolonlar, "Çimento Sınıfı"),
                CimentoCinsi = HucreMetin(satir, kolonlar, "Çimento Cinsi"),
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

    public static void ListeyiKaydet(IEnumerable<CimentoKaydi> kayitlar, string varsayilanAd)
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
        var sayfa = kitap.Worksheets.Add("Çimento");

        for (var i = 0; i < DisaAktarBasliklari.Length; i++)
            sayfa.Cell(1, i + 1).Value = DisaAktarBasliklari[i];

        var satirNo = 2;
        foreach (var k in kayitlar)
        {
            k.ToplamTutariHesapla();
            sayfa.Cell(satirNo, 1).Value = k.Tarih;
            sayfa.Cell(satirNo, 2).Value = k.IrsaliyeNo;
            sayfa.Cell(satirNo, 3).Value = k.CimentoSinifi;
            sayfa.Cell(satirNo, 4).Value = k.CimentoCinsi;
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

    private static string HucreMetin(IXLRangeRow satir, Dictionary<string, int> kolonlar, string baslik) =>
        kolonlar.TryGetValue(baslik, out var kolon) ? satir.Cell(kolon).GetString().Trim() : "";

    private static string IrsaliyeNoOku(IXLRangeRow satir, Dictionary<string, int> kolonlar)
    {
        var irsaliye = HucreMetin(satir, kolonlar, "İrsaliye No");
        if (!string.IsNullOrWhiteSpace(irsaliye))
            return irsaliye;

        return HucreMetin(satir, kolonlar, "Fatura No");
    }

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

public static class CimentoPdfService
{
    public static void Yazdir(IEnumerable<CimentoKaydi> kayitlar, string baslik)
    {
        var liste = kayitlar.ToList();
        if (liste.Count == 0)
        {
            MessageBox.Show("Yazdırılacak kayıt bulunamadı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var doc = new FlowDocument
        {
            PagePadding = new Thickness(40),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11
        };

        doc.Blocks.Add(new Paragraph(new Run(baslik))
        {
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        });

        var tablo = new Table { CellSpacing = 0 };
        for (var i = 0; i < 9; i++)
            tablo.Columns.Add(new TableColumn());

        var baslikGrup = new TableRowGroup();
        var baslikSatir = new TableRow { Background = Brushes.Gainsboro };
        foreach (var h in new[] { "Tarih", "İrsaliye", "Sınıf", "Cins", "Miktar", "Birim Fiyat", "Tutar", "Tedarikçi", "Saha" })
            baslikSatir.Cells.Add(Hucre(h, true));
        baslikGrup.Rows.Add(baslikSatir);
        tablo.RowGroups.Add(baslikGrup);

        var veriGrup = new TableRowGroup();
        foreach (var k in liste)
        {
            k.ToplamTutariHesapla();
            var satir = new TableRow();
            satir.Cells.Add(Hucre(k.Tarih));
            satir.Cells.Add(Hucre(k.IrsaliyeNo));
            satir.Cells.Add(Hucre(k.CimentoSinifi));
            satir.Cells.Add(Hucre(k.CimentoCinsi));
            satir.Cells.Add(Hucre(k.Miktar.ToString("N2", CultureInfo.CurrentCulture)));
            satir.Cells.Add(Hucre(k.BirimFiyati.ToString("C2", CultureInfo.GetCultureInfo("tr-TR"))));
            satir.Cells.Add(Hucre(k.ToplamTutar.ToString("C2", CultureInfo.GetCultureInfo("tr-TR"))));
            satir.Cells.Add(Hucre(k.Tedarikci));
            satir.Cells.Add(Hucre(k.IndirildigiSaha));
            veriGrup.Rows.Add(satir);
        }
        tablo.RowGroups.Add(veriGrup);
        doc.Blocks.Add(tablo);

        var yazici = new PrintDialog();
        if (yazici.ShowDialog() != true)
            return;

        doc.PageHeight = yazici.PrintableAreaHeight;
        doc.PageWidth = yazici.PrintableAreaWidth;
        yazici.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, baslik);
    }

    private static TableCell Hucre(string metin, bool kalin = false)
    {
        var paragraf = new Paragraph(new Run(metin)) { Margin = new Thickness(4) };
        if (kalin) paragraf.FontWeight = FontWeights.SemiBold;
        return new TableCell(paragraf)
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0.5)
        };
    }
}
