using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Windows;

namespace SatinalmaPro.Services;

public static class CimentoPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static CimentoPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void Indir(IEnumerable<CimentoKaydi> kayitlar, string baslik)
    {
        var liste = Hazirla(kayitlar);
        if (liste is null)
            return;

        var dosya = DosyaKaydetDialog($"Cimento_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya is null)
            return;

        PdfOlustur(dosya, liste, baslik);
        DosyaAc(dosya, "Çimento raporu PDF olarak kaydedildi.");
    }

    public static void Yazdir(IEnumerable<CimentoKaydi> kayitlar, string baslik) =>
        CimentoPdfService.Yazdir(kayitlar, baslik);

    private static List<CimentoKaydi>? Hazirla(IEnumerable<CimentoKaydi> kayitlar)
    {
        var liste = kayitlar.ToList();
        if (liste.Count == 0)
        {
            MessageBox.Show("PDF için kayıt bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        foreach (var kayit in liste)
            kayit.ToplamTutariHesapla();

        return liste;
    }

    private static void PdfOlustur(string dosya, List<CimentoKaydi> liste, string baslik)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;
        var toplamTutar = liste.Sum(k => k.ToplamTutar);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI"));

                page.Content().Column(col =>
                {
                    col.Item().Text(baslik).Bold().FontSize(14);
                    col.Item().PaddingTop(4).Text($"{liste.Count} kayıt · Toplam: {toplamTutar:N2} ₺");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(52);
                            c.ConstantColumn(62);
                            c.ConstantColumn(48);
                            c.RelativeColumn();
                            c.ConstantColumn(48);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(48);
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            foreach (var bas in new[] { "Tarih", "İrsaliye", "Sınıf", "Cins", "Miktar", "B.Fiyat", "Tutar", "Fatura", "Tedarikçi", "Saha" })
                                h.Cell().Element(HucreBaslik).Text(bas);
                        });

                        foreach (var k in liste)
                        {
                            table.Cell().Element(HucreVeri).Text(k.Tarih);
                            table.Cell().Element(HucreVeri).Text(k.IrsaliyeNo);
                            table.Cell().Element(HucreVeri).Text(k.CimentoSinifi);
                            table.Cell().Element(HucreVeri).Text(k.CimentoCinsi);
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.Miktar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.BirimFiyati.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.ToplamTutar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).Text(k.FaturaDurumuMetin);
                            table.Cell().Element(HucreVeri).Text(k.Tedarikci);
                            table.Cell().Element(HucreVeri).Text(k.IndirildigiSaha);
                        }
                    });

                    col.Item().PaddingTop(8).Text(ayarlar.FirmaAdi).FontSize(7).Italic();
                });
            });
        }).GeneratePdf(dosya);
    }

    private static IContainer HucreBaslik(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(3);

    private static IContainer HucreVeri(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(3);

    private static string? DosyaKaydetDialog(string varsayilanAd)
    {
        var dialog = new SaveFileDialog
        {
            Title = "PDF Kaydet",
            Filter = "PDF Dosyası (*.pdf)|*.pdf",
            FileName = varsayilanAd
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void DosyaAc(string dosya, string mesaj)
    {
        MessageBox.Show(mesaj, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dosya,
                UseShellExecute = true
            });
        }
        catch { /* yoksay */ }
    }
}
