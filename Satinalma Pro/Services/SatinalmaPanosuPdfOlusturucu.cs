using SatinalmaPro.Helpers;
using SatinalmaPro.Models.SatinalmaMerkezi;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Windows;

namespace SatinalmaPro.Services;

public static class SatinalmaPanosuPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static SatinalmaPanosuPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void TalepListesiIndir(IEnumerable<SatinalmaPanosuTalepSatir> satirlar, string baslik = "Satınalma Panosu")
    {
        var liste = satirlar.ToList();
        if (!Hazir(liste.Count, "PDF için talep kaydı bulunamadı."))
            return;

        var dosya = KaydetDialog($"Satinalma_Panosu_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya is null)
            return;

        TalepPdfOlustur(dosya, liste, baslik);
        DosyaAc(dosya);
    }

    public static void TalepListesiYazdir(IEnumerable<SatinalmaPanosuTalepSatir> satirlar, string baslik = "Satınalma Panosu")
    {
        var liste = satirlar.ToList();
        if (!Hazir(liste.Count, "Yazdırılacak talep kaydı bulunamadı."))
            return;

        PdfOnizlemeServisi.Goster(
            dosya => TalepPdfOlustur(dosya, liste, baslik),
            $"Satinalma_Panosu_{DateTime.Now:yyyyMMdd}.pdf",
            baslik);
    }

    public static void IadeListesiIndir(IEnumerable<IadeSatirModel> satirlar)
    {
        var liste = satirlar.ToList();
        if (!Hazir(liste.Count, "PDF için iade kaydı bulunamadı."))
            return;

        var dosya = KaydetDialog($"Satinalma_Iade_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya is null)
            return;

        IadePdfOlustur(dosya, liste);
        DosyaAc(dosya);
    }

    public static void IadeListesiYazdir(IEnumerable<IadeSatirModel> satirlar) =>
        PdfOnizlemeServisi.Goster(
            dosya => IadePdfOlustur(dosya, satirlar.ToList()),
            $"Satinalma_Iade_{DateTime.Now:yyyyMMdd}.pdf",
            "İade Kayıtları");

    public static void TedarikciListesiIndir(IEnumerable<TedarikciPerformansModel> satirlar)
    {
        var liste = satirlar.ToList();
        if (!Hazir(liste.Count, "PDF için tedarikçi kaydı bulunamadı."))
            return;

        var dosya = KaydetDialog($"Satinalma_Tedarikciler_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya is null)
            return;

        TedarikciPdfOlustur(dosya, liste);
        DosyaAc(dosya);
    }

    public static void TedarikciListesiYazdir(IEnumerable<TedarikciPerformansModel> satirlar) =>
        PdfOnizlemeServisi.Goster(
            dosya => TedarikciPdfOlustur(dosya, satirlar.ToList()),
            $"Satinalma_Tedarikciler_{DateTime.Now:yyyyMMdd}.pdf",
            "Tedarikçi Performans");

    private static bool Hazir(int adet, string mesaj)
    {
        if (adet > 0)
            return true;

        MessageBox.Show(mesaj, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private static string? KaydetDialog(string varsayilanAd)
    {
        var dialog = new SaveFileDialog
        {
            Title = "PDF Kaydet",
            Filter = "PDF Dosyası (*.pdf)|*.pdf",
            FileName = varsayilanAd
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void DosyaAc(string dosya) =>
        MessageBox.Show("PDF kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);

    private static void TalepPdfOlustur(string dosya, List<SatinalmaPanosuTalepSatir> liste, string baslik)
    {
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
                    col.Item().PaddingTop(4).Text($"{liste.Count} kayıt · {DateTime.Now:dd.MM.yyyy HH:mm}");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(62);
                            c.ConstantColumn(72);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.ConstantColumn(58);
                            c.ConstantColumn(52);
                            c.ConstantColumn(38);
                            c.ConstantColumn(62);
                            c.ConstantColumn(58);
                        });

                        table.Header(h =>
                        {
                            HucreBaslik(h.Cell(), "Talep No");
                            HucreBaslik(h.Cell(), "Talep Eden");
                            HucreBaslik(h.Cell(), "Şantiye");
                            HucreBaslik(h.Cell(), "Malzeme");
                            HucreBaslik(h.Cell(), "Kategori");
                            HucreBaslik(h.Cell(), "Öncelik");
                            HucreBaslik(h.Cell(), "Teklif");
                            HucreBaslik(h.Cell(), "Durum");
                            HucreBaslik(h.Cell(), "Son İşlem");
                        });

                        foreach (var s in liste)
                        {
                            HucreVeri(table.Cell(), s.TalepNo);
                            HucreVeri(table.Cell(), s.TalepEden);
                            HucreVeri(table.Cell(), s.Santiye);
                            HucreVeri(table.Cell(), s.Malzeme);
                            HucreVeri(table.Cell(), s.Kategori);
                            HucreVeri(table.Cell(), s.Oncelik);
                            HucreVeri(table.Cell(), s.TeklifSayisi.ToString(Tr));
                            HucreVeri(table.Cell(), s.Durum);
                            HucreVeri(table.Cell(), s.SonIslem);
                        }
                    });
                });
            });
        }).GeneratePdf(dosya);
    }

    private static void IadePdfOlustur(string dosya, List<IadeSatirModel> liste)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Segoe UI"));

                page.Content().Column(col =>
                {
                    col.Item().Text("İade Kayıtları").Bold().FontSize(14);
                    col.Item().PaddingTop(4).Text($"{liste.Count} kayıt");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(72);
                            c.ConstantColumn(72);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.ConstantColumn(64);
                            c.RelativeColumn();
                            c.ConstantColumn(72);
                            c.ConstantColumn(58);
                        });

                        table.Header(h =>
                        {
                            HucreBaslik(h.Cell(), "İade No");
                            HucreBaslik(h.Cell(), "Sipariş No");
                            HucreBaslik(h.Cell(), "Firma");
                            HucreBaslik(h.Cell(), "Malzeme");
                            HucreBaslik(h.Cell(), "Miktar");
                            HucreBaslik(h.Cell(), "Neden");
                            HucreBaslik(h.Cell(), "Durum");
                            HucreBaslik(h.Cell(), "Tarih");
                        });

                        foreach (var s in liste)
                        {
                            HucreVeri(table.Cell(), s.IadeNo);
                            HucreVeri(table.Cell(), s.SiparisNo);
                            HucreVeri(table.Cell(), s.Firma);
                            HucreVeri(table.Cell(), s.Malzeme);
                            HucreVeri(table.Cell(), s.Miktar);
                            HucreVeri(table.Cell(), s.Neden);
                            HucreVeri(table.Cell(), s.Durum);
                            HucreVeri(table.Cell(), s.Tarih);
                        }
                    });
                });
            });
        }).GeneratePdf(dosya);
    }

    private static void TedarikciPdfOlustur(string dosya, List<TedarikciPerformansModel> liste)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Segoe UI"));

                page.Content().Column(col =>
                {
                    col.Item().Text("Tedarikçi Performans").Bold().FontSize(14);
                    col.Item().PaddingTop(4).Text($"{liste.Count} firma");
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(52);
                            c.ConstantColumn(72);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(42);
                            c.ConstantColumn(42);
                            c.ConstantColumn(58);
                            c.ConstantColumn(52);
                        });

                        table.Header(h =>
                        {
                            HucreBaslik(h.Cell(), "Firma");
                            HucreBaslik(h.Cell(), "Sipariş");
                            HucreBaslik(h.Cell(), "Toplam Tutar");
                            HucreBaslik(h.Cell(), "Zamanında");
                            HucreBaslik(h.Cell(), "Eksik");
                            HucreBaslik(h.Cell(), "İade");
                            HucreBaslik(h.Cell(), "Kalite");
                            HucreBaslik(h.Cell(), "Ort. Teslim");
                            HucreBaslik(h.Cell(), "Puan");
                        });

                        foreach (var s in liste)
                        {
                            HucreVeri(table.Cell(), s.Firma);
                            HucreVeri(table.Cell(), s.ToplamSiparis.ToString(Tr));
                            HucreVeri(table.Cell(), s.ToplamTutar.ToString("N0", Tr) + " ₺");
                            HucreVeri(table.Cell(), s.ZamanindaTeslim.ToString(Tr));
                            HucreVeri(table.Cell(), s.EksikTeslim.ToString(Tr));
                            HucreVeri(table.Cell(), s.Iade.ToString(Tr));
                            HucreVeri(table.Cell(), s.Kalite.ToString(Tr));
                            HucreVeri(table.Cell(), s.OrtTeslimSuresi);
                            HucreVeri(table.Cell(), s.PerformansPuani.ToString(Tr));
                        }
                    });
                });
            });
        }).GeneratePdf(dosya);
    }

    private static void HucreBaslik(IContainer cell, string metin) =>
        cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
            .Text(metin).SemiBold().FontSize(8f);

    private static void HucreVeri(IContainer cell, string metin) =>
        cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(metin);
}
