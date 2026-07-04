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

public static class FinansmanPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static FinansmanPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void Indir(
        FinansmanFiltreleri filtre,
        FinansmanGenelOzet ozet,
        List<FinansmanModulOzeti> modulOzetleri,
        List<FinansmanHareketSatiri> hareketler,
        List<FinansmanAylikOzet> aylikOzetler,
        List<FinansmanVadeSatiri> vadeler,
        List<FinansmanGrupOzeti> grupOzetleri,
        string filtreMetni)
    {
        if (!VeriVarMi(filtre, ozet, hareketler, aylikOzetler, vadeler, grupOzetleri))
            return;

        var dosya = DosyaKaydetDialog($"Finansman_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya == null) return;

        PdfOlustur(dosya, filtre, ozet, modulOzetleri, hareketler, aylikOzetler, vadeler, grupOzetleri, filtreMetni);
        DosyaAc(dosya, "Finansman raporu PDF olarak kaydedildi.");
    }

    public static void Yazdir(
        FinansmanFiltreleri filtre,
        FinansmanGenelOzet ozet,
        List<FinansmanModulOzeti> modulOzetleri,
        List<FinansmanHareketSatiri> hareketler,
        List<FinansmanAylikOzet> aylikOzetler,
        List<FinansmanVadeSatiri> vadeler,
        List<FinansmanGrupOzeti> grupOzetleri,
        string filtreMetni)
    {
        if (!VeriVarMi(filtre, ozet, hareketler, aylikOzetler, vadeler, grupOzetleri))
            return;

        var temp = Path.Combine(Path.GetTempPath(), $"Finansman_{Guid.NewGuid():N}.pdf");
        PdfOlustur(temp, filtre, ozet, modulOzetleri, hareketler, aylikOzetler, vadeler, grupOzetleri, filtreMetni);

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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = temp,
                UseShellExecute = true
            });
            MessageBox.Show("PDF oluşturuldu. Yazdırmak için açılan dosyadan yazdırın.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static bool VeriVarMi(
        FinansmanFiltreleri filtre,
        FinansmanGenelOzet ozet,
        List<FinansmanHareketSatiri> hareketler,
        List<FinansmanAylikOzet> aylikOzetler,
        List<FinansmanVadeSatiri> vadeler,
        List<FinansmanGrupOzeti> grupOzetleri)
    {
        var varMi = filtre.RaporTuru switch
        {
            FinansmanTurleri.FinansalOzet => ozet.GiderKayitSayisi + ozet.GelirKayitSayisi > 0,
            FinansmanTurleri.NakitAkisi => aylikOzetler.Count > 0,
            FinansmanTurleri.VadeTakvimi or FinansmanTurleri.BekleyenOdemeler => vadeler.Count > 0,
            FinansmanTurleri.SahaOzeti or FinansmanTurleri.TedarikciOzeti => grupOzetleri.Count > 0,
            _ => hareketler.Count > 0
        };

        if (!varMi)
            MessageBox.Show("Seçilen filtrelere uygun finansman verisi bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);

        return varMi;
    }

    private static void PdfOlustur(
        string dosya,
        FinansmanFiltreleri filtre,
        FinansmanGenelOzet ozet,
        List<FinansmanModulOzeti> modulOzetleri,
        List<FinansmanHareketSatiri> hareketler,
        List<FinansmanAylikOzet> aylikOzetler,
        List<FinansmanVadeSatiri> vadeler,
        List<FinansmanGrupOzeti> grupOzetleri,
        string filtreMetni)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "FİNANSMAN RAPORU — " + filtre.RaporTuru.ToUpper(Tr)));

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}");
                        row.RelativeItem().AlignRight().Text(filtreMetni).SemiBold();
                    });

                    if (filtre.RaporTuru == FinansmanTurleri.FinansalOzet)
                    {
                        col.Item().PaddingTop(8).Element(c => OzetKartlari(c, ozet));
                        col.Item().PaddingTop(10).Element(c => ModulTablosu(c, modulOzetleri));
                        if (aylikOzetler.Count > 0)
                            col.Item().PaddingTop(10).Element(c => AylikTablosu(c, aylikOzetler));
                        if (vadeler.Count > 0)
                            col.Item().PaddingTop(10).Element(c => VadeTablosu(c, vadeler.Take(15).ToList(), "YAKLAŞAN / BEKLEYEN ÖDEMELER"));
                    }
                    else if (filtre.RaporTuru == FinansmanTurleri.NakitAkisi)
                    {
                        col.Item().PaddingTop(8).Element(c => OzetKartlari(c, ozet));
                        col.Item().PaddingTop(10).Element(c => AylikTablosu(c, aylikOzetler));
                    }
                    else if (filtre.RaporTuru is FinansmanTurleri.VadeTakvimi or FinansmanTurleri.BekleyenOdemeler)
                    {
                        col.Item().PaddingTop(8).Element(c => VadeTablosu(c, vadeler, filtre.RaporTuru.ToUpper(Tr)));
                    }
                    else if (filtre.RaporTuru is FinansmanTurleri.SahaOzeti or FinansmanTurleri.TedarikciOzeti)
                    {
                        col.Item().PaddingTop(8).Element(c => GrupTablosu(c, grupOzetleri, filtre.RaporTuru));
                    }
                    else if (filtre.RaporTuru == FinansmanTurleri.ModulDagilimi)
                    {
                        col.Item().PaddingTop(8).Element(c => ModulTablosu(c, modulOzetleri));
                    }
                    else
                    {
                        col.Item().PaddingTop(8).Element(c => HareketTablosu(c, hareketler, filtre.RaporTuru));
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

    private static void OzetKartlari(IContainer container, FinansmanGenelOzet ozet)
    {
        container.Row(row =>
        {
            OzetKutu(row, "TOPLAM GİDER", ozet.ToplamGider, Colors.Red.Medium);
            OzetKutu(row, "TOPLAM GELİR", ozet.ToplamGelir, Colors.Green.Medium);
            OzetKutu(row, "NET NAKİT", ozet.NetNakit, ozet.NetNakit >= 0 ? Colors.Teal.Medium : Colors.Red.Medium);
            OzetKutu(row, "BEKLEYEN ÖDEME", ozet.BekleyenOdeme, Colors.Orange.Medium);
            OzetKutu(row, "GECİKEN ÖDEME", ozet.GecikenOdeme, Colors.Red.Darken1);
        });
    }

    private static void OzetKutu(RowDescriptor row, string baslik, decimal tutar, Color renk)
    {
        row.RelativeItem().Padding(3).Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(Colors.Grey.Lighten5).Padding(6).Column(c =>
            {
                c.Item().Text(baslik).FontSize(7).SemiBold().FontColor(Colors.Grey.Darken1);
                c.Item().Text($"₺{tutar:N0}").FontSize(12).Bold().FontColor(renk);
            });
    }

    private static void ModulTablosu(IContainer container, List<FinansmanModulOzeti> ozetler)
    {
        container.Column(col =>
        {
            col.Item().Text("MODÜL DAĞILIMI").Bold().FontSize(9).FontColor(Colors.Purple.Medium);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.ConstantColumn(50);
                    c.ConstantColumn(55);
                    c.ConstantColumn(90);
                    c.ConstantColumn(55);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HucreBaslik).Text("Modül");
                    h.Cell().Element(HucreBaslik).Text("Tip");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Kayıt");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Tutar");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Pay %");
                });

                var toplam = ozetler.Sum(o => o.ToplamTutar);
                foreach (var o in ozetler)
                {
                    var pay = toplam > 0 ? o.ToplamTutar / toplam * 100m : 0;
                    table.Cell().Element(HucreVeri).Text(o.ModulAdi);
                    table.Cell().Element(HucreVeri).Text(o.Tip);
                    table.Cell().Element(HucreVeri).AlignRight().Text(o.KayitSayisi.ToString());
                    table.Cell().Element(HucreVeri).AlignRight().Text($"₺{o.ToplamTutar:N2}");
                    table.Cell().Element(HucreVeri).AlignRight().Text($"{pay:N1}");
                }
            });
        });
    }

    private static void AylikTablosu(IContainer container, List<FinansmanAylikOzet> aylar)
    {
        container.Column(col =>
        {
            col.Item().Text("AYLIK NAKİT AKIŞI").Bold().FontSize(9).FontColor(Colors.Purple.Medium);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.5f);
                    c.ConstantColumn(80);
                    c.ConstantColumn(80);
                    c.ConstantColumn(80);
                    c.ConstantColumn(55);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HucreBaslik).Text("Ay");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Gider");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Gelir");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Net");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Hareket");
                });

                foreach (var a in aylar)
                {
                    table.Cell().Element(HucreVeri).Text(a.Ay);
                    table.Cell().Element(HucreVeri).AlignRight().Text($"₺{a.Gider:N2}");
                    table.Cell().Element(HucreVeri).AlignRight().Text($"₺{a.Gelir:N2}");
                    table.Cell().Element(HucreVeri).AlignRight()
                        .Text($"₺{a.Net:N2}")
                        .FontColor(a.Net >= 0 ? Colors.Green.Medium : Colors.Red.Medium);
                    table.Cell().Element(HucreVeri).AlignRight().Text(a.HareketSayisi.ToString());
                }
            });
        });
    }

    private static void VadeTablosu(IContainer container, List<FinansmanVadeSatiri> vadeler, string baslik)
    {
        container.Column(col =>
        {
            col.Item().Text(baslik).Bold().FontSize(9).FontColor(Colors.Purple.Medium);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(62);
                    c.ConstantColumn(62);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(1);
                    c.ConstantColumn(42);
                    c.ConstantColumn(72);
                    c.ConstantColumn(72);
                    c.ConstantColumn(62);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HucreBaslik).Text("Vade");
                    h.Cell().Element(HucreBaslik).Text("İşlem");
                    h.Cell().Element(HucreBaslik).Text("Firma");
                    h.Cell().Element(HucreBaslik).Text("Açıklama");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Vade Gün");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Tutar");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("KDV Dahil");
                    h.Cell().Element(HucreBaslik).Text("Durum");
                });

                foreach (var v in vadeler)
                {
                    table.Cell().Element(HucreVeri).Text(v.VadeTarihi);
                    table.Cell().Element(HucreVeri).Text(v.IslemTarihi);
                    table.Cell().Element(HucreVeri).Text(v.Firma);
                    table.Cell().Element(HucreVeri).Text(v.Aciklama);
                    table.Cell().Element(HucreVeri).AlignRight().Text(v.VadeGunu.ToString());
                    table.Cell().Element(HucreVeri).AlignRight().Text($"₺{v.Tutar:N2}");
                    table.Cell().Element(HucreVeri).AlignRight().Text($"₺{v.KdvDahilTutar:N2}");
                    table.Cell().Element(HucreVeri).Text(v.DurumMetin);
                }
            });
        });
    }

    private static void GrupTablosu(IContainer container, List<FinansmanGrupOzeti> gruplar, string baslik)
    {
        container.Column(col =>
        {
            col.Item().Text(baslik.ToUpper(Tr)).Bold().FontSize(9).FontColor(Colors.Purple.Medium);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.5f);
                    c.ConstantColumn(50);
                    c.ConstantColumn(80);
                    c.ConstantColumn(80);
                    c.RelativeColumn(1.5f);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HucreBaslik).Text("Grup");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Kayıt");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Gider");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Gelir");
                    h.Cell().Element(HucreBaslik).Text("Modül Dağılımı");
                });

                foreach (var g in gruplar)
                {
                    table.Cell().Element(HucreVeri).Text(g.GrupAdi);
                    table.Cell().Element(HucreVeri).AlignRight().Text(g.KayitSayisi.ToString());
                    table.Cell().Element(HucreVeri).AlignRight().Text($"₺{g.GiderTutar:N2}");
                    table.Cell().Element(HucreVeri).AlignRight().Text($"₺{g.GelirTutar:N2}");
                    table.Cell().Element(HucreVeri).Text(g.ModulDagilimi);
                }
            });
        });
    }

    private static void HareketTablosu(IContainer container, List<FinansmanHareketSatiri> hareketler, string baslik)
    {
        container.Column(col =>
        {
            col.Item().Text(baslik.ToUpper(Tr)).Bold().FontSize(9).FontColor(Colors.Purple.Medium);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(42);
                    c.ConstantColumn(62);
                    c.ConstantColumn(95);
                    c.ConstantColumn(62);
                    c.RelativeColumn(1);
                    c.RelativeColumn(1);
                    c.RelativeColumn(0.8f);
                    c.ConstantColumn(72);
                });

                table.Header(h =>
                {
                    h.Cell().Element(HucreBaslik).Text("Tip");
                    h.Cell().Element(HucreBaslik).Text("Tarih");
                    h.Cell().Element(HucreBaslik).Text("Modül");
                    h.Cell().Element(HucreBaslik).Text("Belge No");
                    h.Cell().Element(HucreBaslik).Text("Açıklama");
                    h.Cell().Element(HucreBaslik).Text("Tedarikçi/Kaynak");
                    h.Cell().Element(HucreBaslik).Text("Saha");
                    h.Cell().Element(HucreBaslik).AlignRight().Text("Tutar");
                });

                foreach (var h in hareketler)
                {
                    table.Cell().Element(HucreVeri).Text(h.Tip);
                    table.Cell().Element(HucreVeri).Text(h.Tarih);
                    table.Cell().Element(HucreVeri).Text(h.Modul);
                    table.Cell().Element(HucreVeri).Text(h.BelgeNo);
                    table.Cell().Element(HucreVeri).Text(h.Aciklama);
                    table.Cell().Element(HucreVeri).Text(h.Tedarikci);
                    table.Cell().Element(HucreVeri).Text(h.Saha);
                    table.Cell().Element(HucreVeri).AlignRight().Text(h.TutarMetin);
                }
            });
        });
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
                    row.ConstantItem(logoGenislik).Height(logoYukseklik).AlignLeft().Image(logoYol).FitArea();
                else
                    row.ConstantItem(logoGenislik);

                row.RelativeItem().AlignMiddle().Column(c =>
                {
                    c.Item().AlignCenter().Text(firmaAdi).Bold().FontSize(12);
                    c.Item().AlignCenter().Text(baslik).Bold().FontSize(13).FontColor(Colors.Purple.Medium);
                });

                row.ConstantItem(logoGenislik);
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
        catch { }
    }
}
