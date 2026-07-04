using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ClosedXML.Excel;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class FiloExcelService
{
    private static readonly string[] SablonBasliklari =
    [
        "Plaka", "Şasi No", "Araç Tipi", "Marka / Model", "Model Yılı", "Sahiplik", "Şirket", "Saha",
        "Muayene Bitiş", "Sigorta Bitiş", "Durum", "Açıklama"
    ];

    public static void SablonKaydet()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Şablonu Kaydet",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = "AracFilo_Sablon.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Araç Filo");
        for (var i = 0; i < SablonBasliklari.Length; i++)
            sayfa.Cell(1, i + 1).Value = SablonBasliklari[i];

        sayfa.Row(1).Style.Font.Bold = true;
        sayfa.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
        sayfa.Columns().AdjustToContents();
        kitap.SaveAs(dialog.FileName);

        MessageBox.Show("Şablon başarıyla kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static List<FiloAracKaydi> DosyadanOku(string dosyaYolu)
    {
        var liste = new List<FiloAracKaydi>();

        using var kitap = new XLWorkbook(dosyaYolu);
        var sayfa = kitap.Worksheet(1);
        var satirlar = sayfa.RangeUsed()?.RowsUsed().Skip(1);
        if (satirlar == null)
            return liste;

        var kolonlar = BaslikKolonlari(sayfa);

        foreach (var satir in satirlar)
        {
            var plaka = HucreMetin(satir, kolonlar, "Plaka");
            if (string.IsNullOrWhiteSpace(plaka))
                plaka = HucreMetin(satir, kolonlar, "Plaka / Kod");
            if (string.IsNullOrWhiteSpace(plaka))
                continue;

            var tip = HucreMetin(satir, kolonlar, "Araç Tipi");
            liste.Add(new FiloAracKaydi
            {
                Plaka = plaka,
                SasiNo = HucreMetin(satir, kolonlar, "Şasi No"),
                AracTipi = tip.Equals("İş Makinası", StringComparison.OrdinalIgnoreCase) ? "İş Makinası" : "Binek",
                MarkaModel = HucreMetin(satir, kolonlar, "Marka / Model"),
                ModelYili = HucreMetin(satir, kolonlar, "Model Yılı"),
                SahiplikTipi = string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, "Sahiplik")) ? "Bizim" : HucreMetin(satir, kolonlar, "Sahiplik"),
                Sirket = HucreMetin(satir, kolonlar, "Şirket"),
                Saha = HucreMetin(satir, kolonlar, "Saha"),
                MuayeneBitisTarihi = HucreMetin(satir, kolonlar, "Muayene Bitiş"),
                SigortaBitisTarihi = HucreMetin(satir, kolonlar, "Sigorta Bitiş"),
                Durum = string.IsNullOrWhiteSpace(HucreMetin(satir, kolonlar, "Durum")) ? "Aktif" : HucreMetin(satir, kolonlar, "Durum"),
                Aciklama = HucreMetin(satir, kolonlar, "Açıklama"),
                KayitTarihi = DateTime.Now.ToString("dd.MM.yyyy")
            });
        }

        return liste;
    }

    public static void ListeyiKaydet(IEnumerable<FiloAracKaydi> araclar, string varsayilanAd)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Excel Olarak Dışa Aktar",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx",
            FileName = varsayilanAd
        };

        if (dialog.ShowDialog() != true)
            return;

        FiloHesaplayici.Hesapla(araclar, ModulVeriDeposu.FiloGiderleri);

        using var kitap = new XLWorkbook();
        var sayfa = kitap.Worksheets.Add("Araç Filo");

        for (var i = 0; i < SablonBasliklari.Length; i++)
            sayfa.Cell(1, i + 1).Value = SablonBasliklari[i];
        sayfa.Cell(1, 13).Value = "Toplam Gider";

        var satirNo = 2;
        foreach (var a in araclar)
        {
            sayfa.Cell(satirNo, 1).Value = a.Plaka;
            sayfa.Cell(satirNo, 2).Value = a.SasiNo;
            sayfa.Cell(satirNo, 3).Value = a.AracTipi;
            sayfa.Cell(satirNo, 4).Value = a.MarkaModel;
            sayfa.Cell(satirNo, 5).Value = a.ModelYili;
            sayfa.Cell(satirNo, 6).Value = a.SahiplikTipi;
            sayfa.Cell(satirNo, 7).Value = a.Sirket;
            sayfa.Cell(satirNo, 8).Value = a.Saha;
            sayfa.Cell(satirNo, 9).Value = a.MuayeneBitisTarihi;
            sayfa.Cell(satirNo, 10).Value = a.SigortaBitisTarihi;
            sayfa.Cell(satirNo, 11).Value = a.Durum;
            sayfa.Cell(satirNo, 12).Value = a.Aciklama;
            sayfa.Cell(satirNo, 13).Value = a.ToplamGider;
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
}

public static class FiloPdfService
{
    public static void Yazdir(IEnumerable<FiloAracKaydi> araclar, string baslik)
    {
        var liste = araclar.ToList();
        if (liste.Count == 0)
        {
            MessageBox.Show("Yazdırılacak araç bulunamadı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        FiloHesaplayici.Hesapla(liste, ModulVeriDeposu.FiloGiderleri);

        var doc = new FlowDocument
        {
            PagePadding = new Thickness(32),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 10
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
        foreach (var h in new[] { "Plaka", "Tip", "Marka", "Sahiplik", "Şirket", "Saha", "Muayene", "Sigorta", "Gider" })
            baslikSatir.Cells.Add(Hucre(h, true));
        baslikGrup.Rows.Add(baslikSatir);
        tablo.RowGroups.Add(baslikGrup);

        var veriGrup = new TableRowGroup();
        foreach (var a in liste)
        {
            var satir = new TableRow();
            satir.Cells.Add(Hucre(a.Plaka));
            satir.Cells.Add(Hucre(a.AracTipi));
            satir.Cells.Add(Hucre(a.MarkaModel));
            satir.Cells.Add(Hucre(a.SahiplikTipi));
            satir.Cells.Add(Hucre(a.Sirket));
            satir.Cells.Add(Hucre(a.Saha));
            satir.Cells.Add(Hucre(a.MuayeneUyariMetin));
            satir.Cells.Add(Hucre(a.SigortaUyariMetin));
            satir.Cells.Add(Hucre(a.ToplamGiderMetin));
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
        var paragraf = new Paragraph(new Run(metin)) { Margin = new Thickness(3) };
        if (kalin) paragraf.FontWeight = FontWeights.SemiBold;
        return new TableCell(paragraf)
        {
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(0.5)
        };
    }
}
