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

public static class AkaryakitPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static AkaryakitPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void Indir(IEnumerable<AkaryakitKaydi> kayitlar, string baslik, string? filtreBilgisi = null)
    {
        var liste = Hazirla(kayitlar);
        if (liste == null)
            return;

        var dosya = DosyaKaydetDialog($"Akaryakit_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya == null)
            return;

        PdfOlustur(dosya, liste, baslik, filtreBilgisi);
        DosyaAc(dosya, "Akaryakıt raporu PDF olarak kaydedildi.");
    }

    public static void Yazdir(IEnumerable<AkaryakitKaydi> kayitlar, string baslik, string? filtreBilgisi = null)
    {
        var liste = Hazirla(kayitlar);
        if (liste == null)
            return;

        var temp = Path.Combine(Path.GetTempPath(), $"Akaryakit_{Guid.NewGuid():N}.pdf");
        PdfOlustur(temp, liste, baslik, filtreBilgisi);

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
                MessageBox.Show(
                    "PDF oluşturuldu. Yazdırmak için açılan dosyadan yazdırın.",
                    UygulamaBilgisi.Ad,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yazdırma başlatılamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static List<AkaryakitKaydi>? Hazirla(IEnumerable<AkaryakitKaydi> kayitlar)
    {
        var liste = kayitlar.ToList();
        if (liste.Count == 0)
        {
            MessageBox.Show("Yazdırılacak kayıt bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        foreach (var kayit in ModulVeriDeposu.Akaryakit)
            kayit.ToplamTutariHesapla();

        foreach (var kayit in liste)
            kayit.ToplamTutariHesapla();

        AkaryakitTuketimHesaplayici.Hesapla(ModulVeriDeposu.Akaryakit);
        return liste;
    }

    private static void PdfOlustur(string dosya, List<AkaryakitKaydi> liste, string baslik, string? filtreBilgisi = null)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;
        var dagitilanGruplar = DagitilanGruplari(liste);
        var alinanKayitlar = AlinanSirali(liste);
        var fiyatArtislari = FiyatArtislariHesapla();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, baslik.ToUpper(Tr)));

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}");
                        row.RelativeItem().AlignRight().Text(
                            $"{liste.Count} kayıt · {dagitilanGruplar.Count} plaka · {alinanKayitlar.Count} alım")
                            .SemiBold();
                    });

                    if (!string.IsNullOrWhiteSpace(filtreBilgisi))
                    {
                        col.Item().PaddingTop(4).Text($"Filtre: {filtreBilgisi}")
                            .FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                    }

                    if (dagitilanGruplar.Count > 0)
                    {
                        col.Item().PaddingTop(10).Text("DAĞITILAN YAKIT").Bold().FontSize(10)
                            .FontColor(Colors.Orange.Darken2);

                        foreach (var (plaka, kayitlar) in dagitilanGruplar)
                        {
                            var aracAdi = kayitlar.FirstOrDefault(k => !string.IsNullOrWhiteSpace(k.AracMakineAdi))?.AracMakineAdi ?? "";

                            col.Item().PaddingTop(8).Text(text =>
                            {
                                text.Span(plaka).Bold();
                                if (!string.IsNullOrWhiteSpace(aracAdi))
                                    text.Span($" — {aracAdi}").FontColor(Colors.Grey.Darken1);
                            });

                            col.Item().PaddingTop(3).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(62);
                                    c.ConstantColumn(72);
                                    c.ConstantColumn(68);
                                    c.ConstantColumn(58);
                                    c.ConstantColumn(58);
                                    c.RelativeColumn(1);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Element(HucreBaslik).Text("Tarih");
                                    h.Cell().Element(HucreBaslik).Text("Plaka");
                                    h.Cell().Element(HucreBaslik).Text("Verilen Yakıt");
                                    h.Cell().Element(HucreBaslik).Text("Km");
                                    h.Cell().Element(HucreBaslik).Text("Saat");
                                    h.Cell().Element(HucreBaslik).Text("Ortalama Tüketimi");
                                });

                                foreach (var k in kayitlar)
                                {
                                    table.Cell().Element(HucreVeri).Text(k.Tarih);
                                    table.Cell().Element(HucreVeri).Text(k.PlakaVeyaKod);
                                    table.Cell().Element(HucreVeri).AlignRight()
                                        .Text($"{k.Miktar.ToString("N1", Tr)} Lt");
                                    table.Cell().Element(HucreVeri).AlignRight()
                                        .Text(k.KmSayaci?.ToString("N0", Tr) ?? "—");
                                    table.Cell().Element(HucreVeri).AlignRight()
                                        .Text(k.SaatSayaci?.ToString("N1", Tr) ?? "—");
                                    table.Cell().Element(HucreVeri).Text(OrtalamaTuketimMetin(k));
                                }
                            });
                        }
                    }

                    if (alinanKayitlar.Count > 0)
                    {
                        col.Item().PaddingTop(dagitilanGruplar.Count > 0 ? 14 : 10).Text("ALINAN YAKIT").Bold()
                            .FontSize(10).FontColor(Colors.Green.Darken2);

                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(58);
                                c.ConstantColumn(54);
                                c.ConstantColumn(36);
                                c.ConstantColumn(64);
                                c.ConstantColumn(68);
                                c.ConstantColumn(58);
                                c.ConstantColumn(58);
                                c.RelativeColumn(1);
                                c.RelativeColumn(0.8f);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HucreBaslik).Text("Tarih");
                                h.Cell().Element(HucreBaslik).Text("Miktar");
                                h.Cell().Element(HucreBaslik).Text("Birim");
                                h.Cell().Element(HucreBaslik).Text("Birim Fiyatı");
                                h.Cell().Element(HucreBaslik).Text("Toplam Tutar");
                                h.Cell().Element(HucreBaslik).Text("Artış %");
                                h.Cell().Element(HucreBaslik).Text("Artış TL");
                                h.Cell().Element(HucreBaslik).Text("Tedarikçi");
                                h.Cell().Element(HucreBaslik).Text("Teslim Alan");
                            });

                            foreach (var k in alinanKayitlar)
                            {
                                fiyatArtislari.TryGetValue(k, out var artis);

                                table.Cell().Element(HucreVeri).Text(k.Tarih);
                                table.Cell().Element(HucreVeri).AlignRight()
                                    .Text(k.Miktar.ToString("N1", Tr));
                                table.Cell().Element(HucreVeri).Text(string.IsNullOrWhiteSpace(k.Birim) ? "Lt" : k.Birim);
                                table.Cell().Element(HucreVeri).AlignRight()
                                    .Text(ParaPdf(k.GosterilenBirimFiyati));
                                table.Cell().Element(HucreVeri).AlignRight()
                                    .Text(ParaPdf(k.ToplamTutar));
                                table.Cell().Element(HucreVeri).AlignRight()
                                    .Text(artis.YuzdeMetin);
                                table.Cell().Element(HucreVeri).AlignRight()
                                    .Text(artis.TlMetin);
                                table.Cell().Element(HucreVeri).Text(k.GosterilenTedarikci);
                                table.Cell().Element(HucreVeri).Text(
                                    string.IsNullOrWhiteSpace(k.TeslimAlan) ? "—" : k.TeslimAlan);
                            }
                        });
                    }
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

    private static List<(string Plaka, List<AkaryakitKaydi> Kayitlar)> DagitilanGruplari(List<AkaryakitKaydi> liste) =>
        liste
            .Where(k => !k.AlinanKayit)
            .GroupBy(k => string.IsNullOrWhiteSpace(k.PlakaVeyaKod) ? "—" : k.PlakaVeyaKod.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => (
                g.Key,
                g.OrderBy(k => TarihCoz(k.Tarih)).ThenBy(k => k.Miktar).ToList()))
            .ToList();

    private static List<AkaryakitKaydi> AlinanSirali(List<AkaryakitKaydi> liste) =>
        liste
            .Where(k => k.AlinanKayit)
            .OrderBy(k => TarihCoz(k.Tarih))
            .ThenBy(k => k.Miktar)
            .ToList();

    private static Dictionary<AkaryakitKaydi, FiyatArtisi> FiyatArtislariHesapla()
    {
        var sonuc = new Dictionary<AkaryakitKaydi, FiyatArtisi>();
        var sirali = ModulVeriDeposu.Akaryakit
            .Where(k => k.AlinanKayit && k.GosterilenBirimFiyati > 0)
            .OrderBy(k => TarihCoz(k.Tarih))
            .ThenBy(k => k.Miktar)
            .ToList();

        decimal? oncekiFiyat = null;
        foreach (var kayit in sirali)
        {
            var fiyat = kayit.GosterilenBirimFiyati;
            if (oncekiFiyat is > 0)
            {
                var fark = fiyat - oncekiFiyat.Value;
                sonuc[kayit] = new FiyatArtisi(
                    fark / oncekiFiyat.Value * 100m,
                    fark);
            }
            else
                sonuc[kayit] = FiyatArtisi.Bos;

            oncekiFiyat = fiyat;
        }

        return sonuc;
    }

    private static string OrtalamaTuketimMetin(AkaryakitKaydi kayit)
    {
        if (kayit.Tuketim100Km is not null)
            return $"{kayit.Tuketim100Km.Value.ToString("N1", Tr)} L/100km";

        if (kayit.TuketimSaat is not null)
            return $"{kayit.TuketimSaat.Value.ToString("N1", Tr)} Lt/saat";

        return "—";
    }

    private static DateTime TarihCoz(string tarih) =>
        DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.MinValue;

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
                    row.ConstantItem(logoGenislik);

                row.RelativeItem().AlignMiddle().Column(c =>
                {
                    c.Item().AlignCenter().Text(firmaAdi).Bold().FontSize(12);
                    c.Item().AlignCenter().Text(baslik).Bold().FontSize(13).FontColor(Colors.Red.Medium);
                });

                row.ConstantItem(logoGenislik);
            });
            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static string ParaPdf(decimal tutar) =>
        tutar > 0 ? tutar.ToString("N2", Tr) : "—";

    private static IContainer HucreBaslik(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4)
            .Padding(3);

    private static IContainer HucreVeri(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(3);

    private readonly record struct FiyatArtisi(decimal? Yuzde, decimal? Tl)
    {
        public static FiyatArtisi Bos => new(null, null);

        public string YuzdeMetin => Yuzde switch
        {
            null => "—",
            _ => $"{Yuzde.Value.ToString("N2", Tr)} %"
        };

        public string TlMetin => Tl switch
        {
            null => "—",
            _ => Tl.Value.ToString("N2", Tr)
        };
    }

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
