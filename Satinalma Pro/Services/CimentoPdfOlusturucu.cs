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

public static class CimentoPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static CimentoPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void Indir(IEnumerable<CimentoKaydi> kayitlar, string? filtreBilgisi = null)
    {
        var liste = Hazirla(kayitlar);
        if (liste is null)
            return;

        var dosya = DosyaKaydetDialog($"Cimento_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya is null)
            return;

        PdfOlustur(dosya, liste, filtreBilgisi);
        DosyaAc(dosya, "Çimento raporu PDF olarak kaydedildi.");
    }

    public static void Yazdir(IEnumerable<CimentoKaydi> kayitlar, string? filtreBilgisi = null)
    {
        var liste = Hazirla(kayitlar);
        if (liste is null)
            return;

        var temp = Path.Combine(Path.GetTempPath(), $"Cimento_{Guid.NewGuid():N}.pdf");
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

    private static void PdfOlustur(string dosya, List<CimentoKaydi> liste, string? filtreBilgisi)
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

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "ÇİMENTO GİRİŞ RAPORU"));

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
                            c.ConstantColumn(48);
                            c.RelativeColumn();
                            c.ConstantColumn(48);
                            c.ConstantColumn(36);
                            c.ConstantColumn(58);
                            c.ConstantColumn(58);
                            c.ConstantColumn(48);
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            foreach (var bas in new[] { "Tarih", "İrsaliye", "Sınıf", "Cins", "Miktar", "Birim", "B.Fiyat", "Toplam", "Tedarikçi", "Saha" })
                                h.Cell().Element(HucreBaslik).Text(bas);
                        });

                        foreach (var k in liste)
                        {
                            table.Cell().Element(HucreVeri).Text(k.Tarih);
                            table.Cell().Element(HucreVeri).Text(k.IrsaliyeNo);
                            table.Cell().Element(HucreVeri).Text(k.CimentoSinifi);
                            table.Cell().Element(HucreVeri).Text(k.CimentoCinsi);
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.Miktar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).Text(k.Birim);
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.BirimFiyati.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).AlignRight().Text(k.ToplamTutar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).Text(k.Tedarikci);
                            table.Cell().Element(HucreVeri).Text(k.IndirildigiSaha);
                        }

                        var birim = liste.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.Birim))?.Birim ?? "Ton";
                        table.Cell().ColumnSpan(4).Element(HucreToplam).AlignRight().Text("TOPLAM").SemiBold();
                        table.Cell().Element(HucreToplam).AlignRight().Text(toplamMiktar.ToString("N2", Tr)).SemiBold();
                        table.Cell().Element(HucreToplam).Text(birim);
                        table.Cell().Element(HucreToplam).Text("");
                        table.Cell().Element(HucreToplam).AlignRight().Text(toplamTutar.ToString("N2", Tr)).SemiBold();
                        table.Cell().ColumnSpan(2).Element(HucreToplam).Text("");
                    });

                    CinsOzetTablosuEkle(col, liste);
                });
            });
        }).GeneratePdf(dosya);
    }

    private static void CinsOzetTablosuEkle(ColumnDescriptor col, List<CimentoKaydi> liste)
    {
        var ozetler = liste
            .GroupBy(k => k.CimentoCinsi.Trim(), StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var miktar = g.Sum(k => k.Miktar);
                var tutar = g.Sum(k => k.ToplamTutar);
                var birim = g.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.Birim))?.Birim ?? "Ton";
                var ortBirim = miktar > 0 ? tutar / (decimal)miktar : 0m;
                return (Cins: string.IsNullOrWhiteSpace(g.Key) ? "—" : g.Key, Birim: birim, Miktar: miktar, Tutar: tutar, OrtBirim: ortBirim);
            })
            .ToList();

        if (ozetler.Count == 0)
            return;

        col.Item().PaddingTop(16).Text("Çimento Cinsi Bazlı Özet").Bold().FontSize(9);

        col.Item().PaddingTop(4).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2f);
                c.ConstantColumn(72);
                c.ConstantColumn(40);
                c.ConstantColumn(72);
                c.ConstantColumn(72);
            });

            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("Çimento Cinsi");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Toplam Miktar");
                h.Cell().Element(HucreBaslik).Text("Birim");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Toplam Tutar");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Ort. B.Fiyat");
            });

            foreach (var oz in ozetler)
            {
                table.Cell().Element(HucreVeri).Text(oz.Cins);
                table.Cell().Element(HucreVeri).AlignRight().Text(oz.Miktar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).Text(oz.Birim);
                table.Cell().Element(HucreVeri).AlignRight().Text(oz.Tutar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).AlignRight().Text(oz.OrtBirim.ToString("N2", Tr));
            }
        });
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

    private static IContainer HucreToplam(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten3).Padding(3);

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
