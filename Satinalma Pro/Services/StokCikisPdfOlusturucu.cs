using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.IO;

namespace SatinalmaPro.Services;

public static class StokCikisPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static StokCikisPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void OnizleVeYazdir(StokCikisFisVerisi veri)
    {
        var dosyaAdi = $"TeslimFisi_{veri.BelgeNo.Replace('/', '-')}.pdf";
        PdfOnizlemeServisi.Goster(
            dosya => PdfOlustur(dosya, veri),
            dosyaAdi,
            "Stok Teslim Fişi");
    }

    public static string TeslimEdenMetni()
    {
        var k = OturumYoneticisi.AktifKullanici;
        if (k is null)
            return "";

        var ad = string.IsNullOrWhiteSpace(k.AdSoyad) ? "" : k.AdSoyad.Trim();
        var rol = KullaniciRolleri.Normalize(k.Rol);
        return string.IsNullOrWhiteSpace(ad) ? rol : $"{rol} - {ad}";
    }

    private static void PdfOlustur(string dosya, StokCikisFisVerisi veri)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(32);
                page.MarginVertical(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Element(c => BaslikOlustur(c, ayarlar, veri));
                    col.Item().Element(c => BilgiAlani(c, veri));
                    col.Item().PaddingTop(4).Element(c => MalzemeTablosu(c, veri));
                    col.Item().PaddingTop(24).Element(c => ImzaAlani(c, veri));
                });
            });
        }).GeneratePdf(dosya);
    }

    private static void BaslikOlustur(IContainer container, UygulamaAyarlar ayarlar, StokCikisFisVerisi veri)
    {
        const float logoGenislik = 96f;
        const float logoYukseklik = 60f;
        var logoYol = SatinalmaProLogoDeposu.TamYol(ayarlar.LogoDosyaYolu);
        var logoVar = !string.IsNullOrEmpty(logoYol) && File.Exists(logoYol);
        var firmaAdi = string.IsNullOrWhiteSpace(ayarlar.FirmaAdi) ? UygulamaBilgisi.Ad : ayarlar.FirmaAdi;
        var baslik = string.IsNullOrWhiteSpace(veri.IndigiSaha)
            ? "STOK TESLİM / DEPO ÇIKIŞ FİŞİ"
            : "DEPO ÇIKIŞ FİŞİ (SAHAYA İNDİRME)";

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
                    c.Item().AlignCenter().Text(baslik).Bold().FontSize(12).FontColor(Colors.Teal.Medium);
                });

                row.ConstantItem(logoGenislik);
            });
            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void BilgiAlani(IContainer container, StokCikisFisVerisi veri)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.ConstantColumn(72);
                c.RelativeColumn();
                c.ConstantColumn(72);
                c.RelativeColumn();
            });

            BilgiSatiri(t, "Belge No", veri.BelgeNo, "Tarih", veri.Tarih);
            BilgiSatiri(t, "Teslim Eden", veri.TeslimEden, "Teslim Alan", veri.TeslimEdilen);
            if (!string.IsNullOrWhiteSpace(veri.IndigiSaha))
                BilgiSatiri(t, "İndiği Saha", veri.IndigiSaha!, "", "");
        });
    }

    private static void BilgiSatiri(TableDescriptor t, string etiket1, string deger1, string etiket2, string deger2)
    {
        Hucre(t, etiket1, true);
        Hucre(t, deger1, false);
        if (string.IsNullOrEmpty(etiket2) && string.IsNullOrEmpty(deger2))
        {
            Hucre(t, "", true);
            Hucre(t, "", false);
            return;
        }

        Hucre(t, etiket2, true);
        Hucre(t, deger2, false);
    }

    private static void MalzemeTablosu(IContainer container, StokCikisFisVerisi veri)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.2f);
                c.ConstantColumn(72);
                c.ConstantColumn(56);
                c.ConstantColumn(72);
            });

            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("Malzeme").SemiBold();
                h.Cell().Element(HucreBaslik).AlignRight().Text("Miktar").SemiBold();
                h.Cell().Element(HucreBaslik).Text("Birim").SemiBold();
                h.Cell().Element(HucreBaslik).Text("Depo").SemiBold();
            });

            foreach (var satir in veri.Satirlar)
            {
                table.Cell().Element(HucreVeri).Text(satir.Malzeme);
                table.Cell().Element(HucreVeri).AlignRight().Text(satir.MiktarGosterim);
                table.Cell().Element(HucreVeri).Text(satir.Birim);
                table.Cell().Element(HucreVeri).Text(satir.DepoSaha ?? "");
            }
        });
    }

    private static void ImzaAlani(IContainer container, StokCikisFisVerisi veri)
    {
        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.ConstantColumn(16);
                c.RelativeColumn();
            });

            t.Cell().Column(col =>
            {
                col.Item().Text("Teslim Eden").SemiBold().FontSize(9);
                col.Item().PaddingTop(4).Text(veri.TeslimEden).FontSize(9);
                col.Item().PaddingTop(18).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium);
                col.Item().PaddingTop(2).Text("Ad Soyad / İmza").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });

            t.Cell();

            t.Cell().Column(col =>
            {
                col.Item().Text("Teslim Alan").SemiBold().FontSize(9);
                col.Item().PaddingTop(4).Text(veri.TeslimEdilen).FontSize(9);
                col.Item().PaddingTop(18).BorderBottom(0.5f).BorderColor(Colors.Grey.Medium);
                col.Item().PaddingTop(2).Text("Ad Soyad / İmza").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void Hucre(TableDescriptor t, string metin, bool baslik)
    {
        t.Cell().Element(c => baslik ? HucreBaslik(c) : HucreVeri(c)).Text(metin);
    }

    private static IContainer HucreBaslik(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten4)
            .Padding(4);

    private static IContainer HucreVeri(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.White)
            .Padding(4);
}

public sealed record StokCikisFisSatir(string Malzeme, string MiktarGosterim, string Birim, string? DepoSaha);

public sealed record StokCikisFisVerisi(
    string BelgeNo,
    string Tarih,
    string TeslimEden,
    string TeslimEdilen,
    IReadOnlyList<StokCikisFisSatir> Satirlar,
    string? IndigiSaha = null);
