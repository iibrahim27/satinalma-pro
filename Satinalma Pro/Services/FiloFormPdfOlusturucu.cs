using SatinalmaPro.Helpers;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class FiloFormPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static FiloFormPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static string? ZimmetFormuKaydet(FiloAracKaydi arac, string soforAdi, bool otomatikAc = true)
    {
        var tamYol = FiloZimmetPdfDeposu.YeniPdfTamYolu(arac.Plaka, soforAdi);
        ZimmetPdfUret(tamYol, arac, soforAdi);
        var goreli = FiloZimmetPdfDeposu.GoreliYol(tamYol);

        if (otomatikAc)
            PdfAc(tamYol, "Zimmet formu kaydedildi.");

        return goreli;
    }

    public static bool ZimmetPdfAc(string? goreliYol)
    {
        var tam = FiloZimmetPdfDeposu.TamYol(goreliYol);
        if (string.IsNullOrEmpty(tam) || !File.Exists(tam))
        {
            System.Windows.MessageBox.Show("Zimmet PDF dosyası bulunamadı.", UygulamaBilgisi.Ad,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return false;
        }

        PdfAc(tam);
        return true;
    }

    public static bool ZimmetPdfYazdir(string? goreliYol)
    {
        var tam = FiloZimmetPdfDeposu.TamYol(goreliYol);
        if (string.IsNullOrEmpty(tam) || !File.Exists(tam))
        {
            System.Windows.MessageBox.Show("Yazdırılacak zimmet PDF dosyası bulunamadı.", UygulamaBilgisi.Ad,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return false;
        }

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = tam,
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true
            });
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Yazdırma başlatılamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return false;
        }
    }

    private static void ZimmetPdfUret(string dosyaYolu, FiloAracKaydi arac, string soforAdi)
    {
        var hamListe = UygulamaAyarDeposu.Ayarlar.FiloZimmetFormMaddeleri;
        var maddeler = ZimmetMaddeYardimcisi.Ayikla(string.Join("\n", hamListe))
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();

        var (maddeFont, maddePadding) = ZimmetMaddeOlcekleri(maddeler);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(22);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Element(ZimmetBaslik);

                    col.Item().Element(c => ZimmetAracBilgileri(c, arac, soforAdi));

                    if (maddeler.Count > 0)
                    {
                        col.Item().PaddingTop(2).Text("Teslim Edilen / Kontrol Maddeleri")
                            .SemiBold().FontSize(9);
                        col.Item().Element(c => ZimmetMaddeBolgesi(c, maddeler, maddeFont, maddePadding));
                    }

                    col.Item().PaddingTop(6).Text(
                            "Yukarıda belirtilen aracı ve aksesuarları eksiksiz teslim aldığımı, kullanım " +
                            "kurallarına uyacağımı ve sorumluluğumu kabul ettiğimi beyan ederim.")
                        .FontSize(7.5f).Italic().FontColor(Colors.Grey.Darken1);

                    col.Item().PaddingTop(8).Element(c => ZimmetImzaAlani(c, soforAdi));
                });
            });
        }).GeneratePdf(dosyaYolu);
    }

    private static void ZimmetBaslik(IContainer container)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;
        var logoYol = SatinalmaProLogoDeposu.TamYol(ayarlar.LogoDosyaYolu);
        var logoVar = !string.IsNullOrEmpty(logoYol) && File.Exists(logoYol);

        container.Column(col =>
        {
            col.Item().Height(logoVar ? 76 : 56).Layers(layers =>
            {
                layers.PrimaryLayer().AlignMiddle().Column(baslik =>
                {
                    if (!string.IsNullOrWhiteSpace(ayarlar.FirmaAdi))
                        baslik.Item().AlignCenter().Text(ayarlar.FirmaAdi).Bold().FontSize(11);

                    baslik.Item().AlignCenter().Text("ARAÇ ZİMMET FORMU").Bold().FontSize(14);
                    baslik.Item().AlignCenter().PaddingTop(2)
                        .Text($"Düzenleme: {DateTime.Now:dd.MM.yyyy}").FontSize(8)
                        .FontColor(Colors.Grey.Darken1);
                });

                if (logoVar)
                {
                    layers.Layer().AlignLeft().AlignMiddle()
                        .Width(96).Height(72)
                        .Image(logoYol).FitArea();
                }
            });

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void ZimmetAracBilgileri(IContainer container, FiloAracKaydi arac, string soforAdi)
    {
        var zimmetTarihi = DateTime.Now.ToString("dd.MM.yyyy", Tr);
        var firmaAdi = UygulamaAyarDeposu.Ayarlar.FirmaAdi;
        var sirket = !string.IsNullOrWhiteSpace(firmaAdi) ? firmaAdi : arac.Sirket;

        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(72);
                c.RelativeColumn();
                c.ConstantColumn(72);
                c.RelativeColumn();
            });

            ZimmetCiftSatiri(t, "Plaka", arac.Plaka, "Marka / Model", arac.MarkaModel);
            ZimmetCiftSatiri(t, "Şasi No", arac.SasiNo, "Araç Tipi", arac.AracTipi);
            ZimmetCiftSatiri(t, "Şirket", sirket, "Saha", arac.Saha);
            ZimmetCiftSatiri(t, "Muayene", arac.MuayeneBitisTarihi, "Sigorta", arac.SigortaBitisTarihi);
            ZimmetCiftSatiri(t, "Zimmet Tarihi", zimmetTarihi, "Şoför / Operatör", soforAdi);
        });
    }

    private static void ZimmetCiftSatiri(TableDescriptor t, string etiket1, string deger1, string etiket2, string deger2)
    {
        ZimmetHucre(t, etiket1, true);
        ZimmetHucre(t, deger1, false);
        ZimmetHucre(t, etiket2, true);
        ZimmetHucre(t, deger2, false);
    }

    private static void ZimmetHucre(TableDescriptor t, string metin, bool etiket)
    {
        var cell = t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2).PaddingHorizontal(4);
        var text = cell.Text(string.IsNullOrWhiteSpace(metin) ? "—" : metin).FontSize(8.5f);
        if (etiket)
            text.SemiBold();
    }

    private static (float font, float padding) ZimmetMaddeOlcekleri(List<string> maddeler)
    {
        if (maddeler.Count == 0)
            return (9f, 3f);

        var ortalamaUzunluk = maddeler.Average(m => m.Length);
        var uzunMaddeVar = maddeler.Any(m => m.Length > 90);

        if (uzunMaddeVar || ortalamaUzunluk > 55)
        {
            return maddeler.Count switch
            {
                <= 8 => (8f, 3f),
                <= 14 => (7.5f, 2.5f),
                <= 20 => (7f, 2f),
                <= 28 => (6.5f, 2f),
                _ => (6f, 1.5f)
            };
        }

        return maddeler.Count switch
        {
            <= 12 => (9f, 3f),
            <= 20 => (8f, 2.5f),
            _ => (7.5f, 2f)
        };
    }

    private static void ZimmetMaddeBolgesi(IContainer container, List<string> maddeler, float fontSize, float padding)
    {
        var ortalamaUzunluk = maddeler.Average(m => m.Length);
        var ikiSutunUygun = maddeler.Count >= 8
                            && maddeler.All(m => m.Length <= 85)
                            && ortalamaUzunluk <= 50;

        if (ikiSutunUygun)
            ZimmetMaddeleriParalel(container, maddeler, fontSize, padding);
        else
            ZimmetMaddeListesi(container, maddeler, fontSize, padding);
    }

    private static void ZimmetMaddeleriParalel(IContainer container, List<string> maddeler, float fontSize, float padding)
    {
        var solAdet = (maddeler.Count + 1) / 2;
        var sol = maddeler.Take(solAdet).ToList();
        var sag = maddeler.Skip(solAdet).ToList();

        container.Row(row =>
        {
            row.RelativeItem().Element(c => ZimmetMaddeListesi(c, sol, fontSize, padding));
            row.ConstantItem(10);
            row.RelativeItem().Element(c => ZimmetMaddeListesi(c, sag, fontSize, padding));
        });
    }

    private static void ZimmetMaddeListesi(IContainer container, List<string> maddeler, float fontSize, float padding)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(22);
                c.RelativeColumn();
            });

            foreach (var madde in maddeler)
                ZimmetMaddeSatiri(t, madde, fontSize, padding);
        });
    }

    private static void ZimmetMaddeSatiri(TableDescriptor t, string madde, float fontSize, float padding)
    {
        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(padding)
            .AlignMiddle().AlignCenter().Text("☐").FontSize(fontSize);
        t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(padding)
            .AlignMiddle().Text(madde).FontSize(fontSize).LineHeight(1.15f);
    }

    private static void ZimmetImzaAlani(IContainer container, string soforAdi)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.ConstantColumn(12);
                c.RelativeColumn();
            });

            t.Cell().Column(col =>
            {
                col.Item().Text("Teslim Eden").SemiBold().FontSize(8.5f);
                col.Item().PaddingTop(22).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium);
                col.Item().PaddingTop(2).Text("Ad Soyad / İmza").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });

            t.Cell();

            t.Cell().Column(col =>
            {
                col.Item().Text("Teslim Alan (Şoför)").SemiBold().FontSize(8.5f);
                col.Item().PaddingTop(6).Text(soforAdi).FontSize(9);
                col.Item().PaddingTop(10).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium);
                col.Item().PaddingTop(2).Text("İmza").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    public static void SevkFormuOlustur(FiloAracKaydi arac, string kaynakSaha, string hedefSaha, string aciklama)
    {
        var dosya = DosyaKaydetDialog($"Sevk_{arac.Plaka.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya is null)
            return;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Element(c => Baslik(c, "ARAÇ SEVK FORMU"));

                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(120);
                            c.RelativeColumn();
                        });
                        BilgiSatiri(t, "Sevk Tarihi", DateTime.Now.ToString("dd.MM.yyyy", Tr));
                        BilgiSatiri(t, "Plaka", arac.Plaka);
                        BilgiSatiri(t, "Marka / Model", arac.MarkaModel);
                        BilgiSatiri(t, "Şasi No", arac.SasiNo);
                        BilgiSatiri(t, "Araç Tipi", arac.AracTipi);
                        BilgiSatiri(t, "Şirket", arac.Sirket);
                        BilgiSatiri(t, "Kaynak Saha", kaynakSaha);
                        BilgiSatiri(t, "Hedef Şantiye", hedefSaha);
                    });

                    if (!string.IsNullOrWhiteSpace(aciklama))
                    {
                        col.Item().PaddingTop(8).Text("Açıklama").SemiBold();
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text(aciklama);
                    }

                    col.Item().PaddingTop(8).Text(
                        "Bu sevk işlemi sonrası araç filo parkında pasif duruma alınmıştır.")
                        .FontSize(9).Italic();

                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Sevk Eden").SemiBold();
                            c.Item().PaddingTop(30).BorderTop(1).BorderColor(Colors.Grey.Medium).Text("İmza");
                        });
                        row.ConstantItem(24);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Teslim Alan Şantiye").SemiBold();
                            c.Item().PaddingTop(30).BorderTop(1).BorderColor(Colors.Grey.Medium).Text("İmza");
                        });
                    });
                });
            });
        }).GeneratePdf(dosya);

        DosyaAc(dosya, "Sevk formu oluşturuldu.");
    }

    private static void Baslik(IContainer container, string baslik)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;
        var logoYol = SatinalmaProLogoDeposu.TamYol(ayarlar.LogoDosyaYolu);

        container.Column(col =>
        {
            if (!string.IsNullOrEmpty(logoYol) && File.Exists(logoYol))
                col.Item().AlignCenter().Height(50).Image(logoYol);

            if (!string.IsNullOrWhiteSpace(ayarlar.FirmaAdi))
                col.Item().AlignCenter().Text(ayarlar.FirmaAdi).Bold().FontSize(12);

            col.Item().AlignCenter().Text(baslik).Bold().FontSize(14);
            col.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void BilgiSatiri(TableDescriptor t, string etiket, string deger)
    {
        t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(etiket).SemiBold();
        t.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
            .Text(string.IsNullOrWhiteSpace(deger) ? "—" : deger);
    }

    private static string? DosyaKaydetDialog(string dosyaAdi)
    {
        var dialog = new SaveFileDialog
        {
            Title = "PDF Kaydet",
            Filter = "PDF Dosyası (*.pdf)|*.pdf",
            FileName = dosyaAdi
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void PdfAc(string dosya, string? mesaj = null)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dosya,
                UseShellExecute = true
            });
        }
        catch
        {
            // yoksay
        }

        if (!string.IsNullOrWhiteSpace(mesaj))
        {
            System.Windows.MessageBox.Show(mesaj, UygulamaBilgisi.Ad,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }

    private static void DosyaAc(string dosya, string mesaj) => PdfAc(dosya, mesaj);
}
