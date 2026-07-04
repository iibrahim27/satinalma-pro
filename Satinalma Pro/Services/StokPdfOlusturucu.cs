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

public static class StokPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static StokPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void Indir(IEnumerable<StokKaydi> kayitlar, string? filtreBilgisi = null)
    {
        var liste = Hazirla(kayitlar);
        if (liste == null)
            return;

        var dosya = DosyaKaydetDialog($"StokYonetimi_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya == null)
            return;

        PdfOlustur(dosya, liste, filtreBilgisi);
        DosyaAc(dosya, "Stok raporu PDF olarak kaydedildi.");
    }

    public static void Yazdir(IEnumerable<StokKaydi> kayitlar, string? filtreBilgisi = null)
    {
        var liste = Hazirla(kayitlar);
        if (liste == null)
            return;

        var temp = Path.Combine(Path.GetTempPath(), $"StokYonetimi_{Guid.NewGuid():N}.pdf");
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

    private static List<StokKaydi>? Hazirla(IEnumerable<StokKaydi> kayitlar)
    {
        var liste = kayitlar.ToList();
        if (liste.Count == 0)
        {
            MessageBox.Show("Rapor için kayıt bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        foreach (var kayit in liste)
            kayit.ToplamDegerHesapla();

        return liste;
    }

    private static void PdfOlustur(string dosya, List<StokKaydi> liste, string? filtreBilgisi)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;
        var toplamDeger = liste.Sum(k => k.ToplamDeger);
        var kritik = liste.Count(k => k.DurumMetin == "Kritik");
        var tukenen = liste.Count(k => k.DurumMetin == "Tükendi");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "STOK YÖNETİMİ RAPORU"));

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}");
                        row.RelativeItem().AlignRight()
                            .Text($"{liste.Count} kalem · Toplam değer: {toplamDeger.ToString("C2", Tr)} · Kritik: {kritik} · Tükenen: {tukenen}")
                            .SemiBold();
                    });

                    if (!string.IsNullOrWhiteSpace(filtreBilgisi))
                    {
                        col.Item().PaddingTop(4).Text($"Filtre: {filtreBilgisi}")
                            .FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.3f);
                            c.ConstantColumn(58);
                            c.ConstantColumn(34);
                            c.ConstantColumn(48);
                            c.ConstantColumn(48);
                            c.RelativeColumn(0.9f);
                            c.ConstantColumn(52);
                            c.ConstantColumn(58);
                            c.ConstantColumn(46);
                            c.ConstantColumn(52);
                            c.RelativeColumn(1);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HucreBaslik).Text("Malzeme");
                            h.Cell().Element(HucreBaslik).Text("Kategori");
                            h.Cell().Element(HucreBaslik).Text("Birim");
                            h.Cell().Element(HucreBaslik).Text("Mevcut");
                            h.Cell().Element(HucreBaslik).Text("Min.");
                            h.Cell().Element(HucreBaslik).Text("Depo / Saha");
                            h.Cell().Element(HucreBaslik).Text("Birim Maliyet");
                            h.Cell().Element(HucreBaslik).Text("Toplam Değer");
                            h.Cell().Element(HucreBaslik).Text("Durum");
                            h.Cell().Element(HucreBaslik).Text("Son Günc.");
                            h.Cell().Element(HucreBaslik).Text("Açıklama");
                        });

                        foreach (var k in liste)
                        {
                            table.Cell().Element(HucreVeri).Text(k.MalzemeAdi);
                            table.Cell().Element(HucreVeri).Text(k.Kategori);
                            table.Cell().Element(HucreVeri).Text(k.Birim);
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.MevcutMiktar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.MinimumStok.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).Text(k.DepoSaha);
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.BirimMaliyet.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.ToplamDeger.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).Text(k.DurumMetin);
                            table.Cell().Element(HucreVeri).Text(k.SonGuncelleme);
                            table.Cell().Element(HucreVeri).Text(k.Aciklama);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(dosya);
    }

    private static void BaslikOlustur(IContainer container, UygulamaAyarlar ayarlar, string baslik)
    {
        const float logoGenislik = 96f;
        const float logoYukseklik = 60f;
        var logoYol = SatinalmaProLogoDeposu.TamYol(ayarlar.LogoDosyaYolu);
        var logoVar = !string.IsNullOrEmpty(logoYol);
        var firmaAdi = string.IsNullOrWhiteSpace(ayarlar.FirmaAdi) ? UygulamaBilgisi.Ad : ayarlar.FirmaAdi;

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (logoVar)
                {
                    row.ConstantItem(logoGenislik).Height(logoYukseklik)
                        .AlignLeft()
                        .Image(logoYol).FitArea();
                }
                else
                {
                    row.ConstantItem(logoGenislik);
                }

                row.RelativeItem().AlignMiddle().Column(c =>
                {
                    c.Item().AlignCenter().Text(firmaAdi).Bold().FontSize(12);
                    c.Item().AlignCenter().Text(baslik).Bold().FontSize(13).FontColor(Colors.Teal.Medium);
                });

                row.ConstantItem(logoGenislik);
            });
            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static IContainer HucreBaslik(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4)
            .Padding(3);

    private static IContainer HucreVeri(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(3);

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
