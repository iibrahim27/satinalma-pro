using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.IO;
using System.Windows;

namespace SatinalmaPro.Services;

public static class AgregaPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static AgregaPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void Indir(IEnumerable<AgregaKaydi> kayitlar, string? filtreBilgisi = null)
    {
        var liste = Hazirla(kayitlar);
        if (liste == null)
            return;

        var dosya = DosyaKaydetDialog($"Agrega_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya == null)
            return;

        PdfOlustur(dosya, liste, filtreBilgisi);
        DosyaAc(dosya, "Agrega raporu PDF olarak kaydedildi.");
    }

    public static void Yazdir(IEnumerable<AgregaKaydi> kayitlar, string? filtreBilgisi = null)
    {
        var liste = Hazirla(kayitlar);
        if (liste == null)
            return;

        var temp = Path.Combine(Path.GetTempPath(), $"Agrega_{Guid.NewGuid():N}.pdf");
        PdfOlustur(temp, liste, filtreBilgisi);

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = temp,
                Verb = "print",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = temp,
                    UseShellExecute = true
                });
                MessageBox.Show("PDF oluşturuldu. Yazdırmak için açılan dosyadan yazdırın.",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yazdırma başlatılamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static List<AgregaKaydi>? Hazirla(IEnumerable<AgregaKaydi> kayitlar)
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

    private static void PdfOlustur(string dosya, List<AgregaKaydi> liste, string? filtreBilgisi)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;
        var toplamMiktar = liste.Sum(k => k.Miktar);
        var toplamTutar = liste.Sum(k => k.ToplamTutar);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "AGREGA GİRİŞ RAPORU"));

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}");
                        row.RelativeItem().AlignRight()
                            .Text($"{liste.Count} kayıt · {toplamMiktar:N1} Ton · {toplamTutar:N2} ₺")
                            .SemiBold();
                    });

                    if (!string.IsNullOrWhiteSpace(filtreBilgisi))
                    {
                        col.Item().PaddingTop(4).Text($"Filtre: {filtreBilgisi}")
                            .FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(52);
                            c.ConstantColumn(62);
                            c.ConstantColumn(54);
                            c.RelativeColumn(1.1f);
                            c.ConstantColumn(48);
                            c.ConstantColumn(36);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(62);
                            c.ConstantColumn(48);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(0.8f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HucreBaslik).Text("Tarih");
                            h.Cell().Element(HucreBaslik).Text("İrsaliye");
                            h.Cell().Element(HucreBaslik).Text("Tür");
                            h.Cell().Element(HucreBaslik).Text("Cins");
                            h.Cell().Element(HucreBaslik).Text("Miktar");
                            h.Cell().Element(HucreBaslik).Text("Birim");
                            h.Cell().Element(HucreBaslik).Text("B.Fiyat");
                            h.Cell().Element(HucreBaslik).Text("Toplam");
                            h.Cell().Element(HucreBaslik).Text("Fatura");
                            h.Cell().Element(HucreBaslik).Text("Artış");
                            h.Cell().Element(HucreBaslik).Text("Tedarikçi");
                            h.Cell().Element(HucreBaslik).Text("Saha");
                        });

                        foreach (var k in liste)
                        {
                            table.Cell().Element(HucreVeri).Text(k.Tarih);
                            table.Cell().Element(HucreVeri).Text(k.IrsaliyeNo);
                            table.Cell().Element(HucreVeri).Text(k.AgregaTuru);
                            table.Cell().Element(HucreVeri).Text(k.AgregaCinsi);
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.Miktar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).Text(k.Birim);
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.BirimFiyati.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.ToplamTutar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).Text(k.FaturaDurumuMetin);
                            table.Cell().Element(HucreVeri).Text(k.ArtisYuzdesiMetin);
                            table.Cell().Element(HucreVeri).Text(k.Tedarikci);
                            table.Cell().Element(HucreVeri).Text(k.IndirildigiSaha);
                        }
                    });
                });
            });
        }).GeneratePdf(dosya);
    }

    private static void BaslikOlustur(IContainer container, UygulamaAyarlar ayarlar, string baslik)
    {
        var logoYol = SatinalmaProLogoDeposu.TamYol(ayarlar.LogoDosyaYolu);
        var logoVar = !string.IsNullOrEmpty(logoYol);

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (logoVar)
                    row.ConstantItem(84).Height(54).AlignLeft().Image(logoYol).FitArea();
                else
                    row.ConstantItem(84);

                row.RelativeItem().AlignMiddle().Column(c =>
                {
                    c.Item().AlignCenter().Text(ayarlar.FirmaAdi).Bold().FontSize(11);
                    c.Item().AlignCenter().Text(baslik).Bold().FontSize(12).FontColor(Colors.Green.Darken2);
                });

                row.ConstantItem(84);
            });
            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
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
        catch { /* ignore */ }
    }
}
