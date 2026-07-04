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

public static class RaporlamaPdfOlusturucu
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static RaporlamaPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public static void Indir(
        RaporFiltreleri filtre,
        List<RaporModulOzeti> modulOzetleri,
        List<RaporDetaySatiri> detaySatirlari,
        List<RaporGrupOzeti> grupOzetleri,
        string filtreMetni)
    {
        if (!VeriVarMi(filtre.RaporTuru, modulOzetleri, detaySatirlari, grupOzetleri))
            return;

        var dosya = DosyaKaydetDialog($"Rapor_{DateTime.Now:yyyyMMdd}.pdf");
        if (dosya == null) return;

        PdfOlustur(dosya, filtre, filtreMetni, modulOzetleri, detaySatirlari, grupOzetleri);
        DosyaAc(dosya, "Rapor PDF olarak kaydedildi.");
    }

    public static void Yazdir(
        RaporFiltreleri filtre,
        string filtreMetni,
        List<RaporModulOzeti> modulOzetleri,
        List<RaporDetaySatiri> detaySatirlari,
        List<RaporGrupOzeti> grupOzetleri)
    {
        if (!VeriVarMi(filtre.RaporTuru, modulOzetleri, detaySatirlari, grupOzetleri))
            return;

        var temp = Path.Combine(Path.GetTempPath(), $"Rapor_{Guid.NewGuid():N}.pdf");
        PdfOlustur(temp, filtre, filtreMetni, modulOzetleri, detaySatirlari, grupOzetleri);

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
        string raporTuru,
        List<RaporModulOzeti> modulOzetleri,
        List<RaporDetaySatiri> detaySatirlari,
        List<RaporGrupOzeti> grupOzetleri)
    {
        var varMi = raporTuru switch
        {
            RaporTurleri.GenelOzet => modulOzetleri.Any(o => o.KayitSayisi > 0),
            RaporTurleri.TedarikciOzeti or RaporTurleri.SahaOzeti or RaporTurleri.KategoriOzeti =>
                grupOzetleri.Count > 0,
            _ => detaySatirlari.Count > 0
        };

        if (!varMi)
            MessageBox.Show("Seçilen filtrelere uygun rapor verisi bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);

        return varMi;
    }

    private static void PdfOlustur(
        string dosya,
        RaporFiltreleri filtre,
        string filtreMetni,
        List<RaporModulOzeti> modulOzetleri,
        List<RaporDetaySatiri> detaySatirlari,
        List<RaporGrupOzeti> grupOzetleri)
    {
        var ayarlar = UygulamaAyarDeposu.Ayarlar;
        var genelToplam = modulOzetleri.Sum(o => o.ToplamTutar);

        Document.Create(container =>
        {
            if (filtre.DetayliPdfModu)
            {
                DetayliRaporSayfalari(container, ayarlar, filtreMetni, detaySatirlari);
            }
            else
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(22);
                    page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Segoe UI"));

                    page.Header().Element(c => BaslikOlustur(c, ayarlar, "RAPORLAMALAR — " + filtre.RaporTuru.ToUpper(Tr)));

                    page.Content().Column(col =>
                    {
                        UstBilgi(col, filtreMetni, genelToplam);

                        col.Item().PaddingTop(8).Element(c =>
                        {
                            if (filtre.RaporTuru == RaporTurleri.GenelOzet)
                                ModulOzetTablosu(c, modulOzetleri);
                            else if (filtre.RaporTuru is RaporTurleri.TedarikciOzeti or RaporTurleri.SahaOzeti or RaporTurleri.KategoriOzeti)
                                GrupOzetTablosu(c, filtre.RaporTuru, grupOzetleri);
                            else
                                DetayTablosu(c, detaySatirlari);
                        });
                    });

                    page.Footer().Element(SayfaAlti);
                });
            }
        }).GeneratePdf(dosya);
    }

    private static void DetayliRaporSayfalari(
        IDocumentContainer container,
        UygulamaAyarlar ayarlar,
        string filtreMetni,
        List<RaporDetaySatiri> detaySatirlari)
    {
        var analizler = RaporlamaServisi.MalzemeAnalizleri(detaySatirlari);
        var aylikOzetler = RaporlamaServisi.AylikAlimOzetleri(detaySatirlari);
        var toplamTutar = detaySatirlari.Sum(s => s.Tutar);
        var toplamMiktar = detaySatirlari.Sum(s => s.Miktar);

        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Segoe UI"));

            page.Header().Element(c => BaslikOlustur(c, ayarlar, "DETAYLI RAPOR — ALIM ANALİZİ"));

            page.Content().Column(col =>
            {
                UstBilgi(col, filtreMetni, toplamTutar);

                col.Item().PaddingTop(6).Text(
                    $"Kayıt: {detaySatirlari.Count} · Toplam Miktar: {toplamMiktar:N2} · Toplam Tutar: {toplamTutar:C2}")
                    .SemiBold();

                col.Item().PaddingTop(10).Text("ALINAN LİSTESİ").Bold().FontSize(9).FontColor(Colors.Teal.Medium);
                col.Item().PaddingTop(4).Element(c => AlimListesiTablosu(c, detaySatirlari));

                col.Item().PaddingTop(12).Text("AYLIK ALIM ÖZETİ").Bold().FontSize(9).FontColor(Colors.Teal.Medium);
                col.Item().PaddingTop(4).Element(c => AylikAlimOzetTablosu(c, aylikOzetler));

                col.Item().PaddingTop(12).Text("TOPLAM MALİYET TABLOSU").Bold().FontSize(9).FontColor(Colors.Teal.Medium);
                col.Item().PaddingTop(4).Element(c => ToplamMaliyetTablosu(c, analizler));
            });

            page.Footer().Element(SayfaAlti);
        });
    }

    private static void UstBilgi(ColumnDescriptor col, string filtreMetni, decimal genelToplam)
    {
        col.Item().Row(row =>
        {
            row.RelativeItem().Text($"Oluşturma: {DateTime.Now:dd.MM.yyyy HH:mm}");
            row.RelativeItem().AlignRight().Text($"Genel Toplam: {genelToplam:C2}").SemiBold();
        });

        col.Item().PaddingTop(4).Text($"Filtre: {filtreMetni}")
            .FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
    }

    private static void AlimListesiTablosu(IContainer container, List<RaporDetaySatiri> satirlar)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(58);
                c.ConstantColumn(62);
                c.ConstantColumn(58);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(1.1f);
                c.ConstantColumn(42);
                c.ConstantColumn(34);
                c.ConstantColumn(52);
                c.ConstantColumn(52);
                c.ConstantColumn(44);
                c.RelativeColumn(0.8f);
                c.RelativeColumn(0.7f);
            });

            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("Modül");
                h.Cell().Element(HucreBaslik).Text("Tarih");
                h.Cell().Element(HucreBaslik).Text("Belge");
                h.Cell().Element(HucreBaslik).Text("Kategori");
                h.Cell().Element(HucreBaslik).Text("Malzeme / Hizmet");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Miktar");
                h.Cell().Element(HucreBaslik).Text("Birim");
                h.Cell().Element(HucreBaslik).AlignRight().Text("B.Fiyat");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Tutar");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Artış");
                h.Cell().Element(HucreBaslik).Text("Tedarikçi");
                h.Cell().Element(HucreBaslik).Text("Saha");
            });

            foreach (var s in satirlar.OrderBy(x => x.Tarih))
            {
                table.Cell().Element(HucreVeri).Text(s.Modul);
                table.Cell().Element(HucreVeri).Text(s.Tarih);
                table.Cell().Element(HucreVeri).Text(s.BelgeNo);
                table.Cell().Element(HucreVeri).Text(s.Kategori);
                table.Cell().Element(HucreVeri).Text(s.Aciklama);
                table.Cell().Element(HucreVeri).AlignRight().Text(s.Miktar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).Text(s.Birim);
                table.Cell().Element(HucreVeri).AlignRight().Text(s.BirimFiyati > 0 ? s.BirimFiyati.ToString("N2", Tr) : "—");
                table.Cell().Element(HucreVeri).AlignRight().Text(s.Tutar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).AlignRight().Text(s.ArtisYuzdesiMetin);
                table.Cell().Element(HucreVeri).Text(s.Tedarikci);
                table.Cell().Element(HucreVeri).Text(s.Saha);
            }

            var toplamMiktar = satirlar.Sum(s => s.Miktar);
            var toplamTutar = satirlar.Sum(s => s.Tutar);
            table.Cell().ColumnSpan(5).Element(HucreToplam).Text("TOPLAM").SemiBold();
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamMiktar.ToString("N2", Tr)).SemiBold();
            table.Cell().Element(HucreToplam).Text("");
            table.Cell().Element(HucreToplam).Text("");
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamTutar.ToString("N2", Tr)).SemiBold();
            table.Cell().ColumnSpan(3).Element(HucreToplam).Text("");
        });
    }

    private static void ToplamMaliyetTablosu(IContainer container, List<RaporMalzemeAnalizi> analizler)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.5f);
                c.RelativeColumn(0.8f);
                c.ConstantColumn(52);
                c.ConstantColumn(42);
                c.ConstantColumn(62);
                c.ConstantColumn(58);
                c.ConstantColumn(52);
                c.ConstantColumn(52);
                c.ConstantColumn(58);
                c.ConstantColumn(68);
                c.ConstantColumn(48);
            });

            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("Malzeme / Hizmet");
                h.Cell().Element(HucreBaslik).Text("Kategori");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Top. Miktar");
                h.Cell().Element(HucreBaslik).Text("Birim");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Top. Maliyet");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Ort. Fiyat");
                h.Cell().Element(HucreBaslik).AlignRight().Text("İlk Alış");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Son Alış");
                h.Cell().Element(HucreBaslik).AlignRight().Text("K/Z (₺)");
                h.Cell().Element(HucreBaslik).AlignRight().Text("K/Z Toplam (₺)");
                h.Cell().Element(HucreBaslik).AlignRight().Text("K/Z (%)");
            });

            foreach (var a in analizler)
            {
                table.Cell().Element(HucreVeri).Text(a.MalzemeAdi);
                table.Cell().Element(HucreVeri).Text(a.Kategori);
                table.Cell().Element(HucreVeri).AlignRight().Text(a.ToplamMiktar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).Text(a.Birim);
                table.Cell().Element(HucreVeri).AlignRight().Text(a.ToplamTutar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).AlignRight().Text(a.AgirlikliOrtalamaFiyat.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).AlignRight().Text(a.IlkBirimFiyat > 0 ? a.IlkBirimFiyat.ToString("N2", Tr) : "—");
                table.Cell().Element(HucreVeri).AlignRight().Text(a.SonBirimFiyat > 0 ? a.SonBirimFiyat.ToString("N2", Tr) : "—");
                table.Cell().Element(HucreVeri).AlignRight().Text(a.KarZiyanTlMetin);
                table.Cell().Element(HucreVeri).AlignRight().Text(a.KarZiyanToplamTlMetin);
                table.Cell().Element(HucreVeri).AlignRight().Text(a.ToplamArtisMetin);
            }

            var toplamMiktar = analizler.Sum(a => a.ToplamMiktar);
            var toplamMaliyet = analizler.Sum(a => a.ToplamTutar);
            var toplamKarZiyan = analizler.Sum(a => a.KarZiyanToplamTl);
            var ortFiyat = toplamMiktar > 0 ? toplamMaliyet / (decimal)toplamMiktar : 0m;

            table.Cell().ColumnSpan(2).Element(HucreToplam).Text("TOPLAM").SemiBold();
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamMiktar.ToString("N2", Tr)).SemiBold();
            table.Cell().Element(HucreToplam).Text("");
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamMaliyet.ToString("N2", Tr)).SemiBold();
            table.Cell().Element(HucreToplam).AlignRight().Text(ortFiyat.ToString("N2", Tr)).SemiBold();
            table.Cell().ColumnSpan(2).Element(HucreToplam).Text("");
            table.Cell().Element(HucreToplam).Text("");
            table.Cell().Element(HucreToplam).AlignRight().Text(
                toplamKarZiyan == 0 ? "—" : toplamKarZiyan > 0 ? $"+{toplamKarZiyan:N2} ₺" : $"{toplamKarZiyan:N2} ₺").SemiBold();
            table.Cell().Element(HucreToplam).Text("");
        });
    }

    private static void AylikAlimOzetTablosu(IContainer container, List<RaporAylikAlimOzeti> aylikOzetler)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.1f);
                c.ConstantColumn(62);
                c.ConstantColumn(42);
                c.ConstantColumn(68);
                c.ConstantColumn(62);
                c.ConstantColumn(58);
                c.ConstantColumn(52);
            });

            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("Ay");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Top. Miktar");
                h.Cell().Element(HucreBaslik).Text("Birim");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Top. Tutar (₺)");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Ort. B.Fiyat");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Artış (₺)");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Artış (%)");
            });

            foreach (var a in aylikOzetler)
            {
                table.Cell().Element(HucreVeri).Text(a.AyEtiketi);
                table.Cell().Element(HucreVeri).AlignRight().Text(a.ToplamMiktar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).Text(a.Birim);
                table.Cell().Element(HucreVeri).AlignRight().Text(a.ToplamTutar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).AlignRight().Text(a.OrtBirimFiyat > 0 ? a.OrtBirimFiyat.ToString("N2", Tr) : "—");
                table.Cell().Element(HucreVeri).AlignRight().Text(a.ArtisTlMetin);
                table.Cell().Element(HucreVeri).AlignRight().Text(a.ArtisYuzdesiMetin);
            }

            if (aylikOzetler.Count == 0)
            {
                table.Cell().ColumnSpan(7).Element(HucreVeri).Text("Tarihli kayıt bulunamadı.");
                return;
            }

            var toplamMiktar = aylikOzetler.Sum(a => a.ToplamMiktar);
            var toplamTutar = aylikOzetler.Sum(a => a.ToplamTutar);
            var ortGenel = toplamMiktar > 0 ? toplamTutar / (decimal)toplamMiktar : 0m;

            table.Cell().Element(HucreToplam).Text("TOPLAM").SemiBold();
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamMiktar.ToString("N2", Tr)).SemiBold();
            table.Cell().Element(HucreToplam).Text("");
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamTutar.ToString("N2", Tr)).SemiBold();
            table.Cell().Element(HucreToplam).AlignRight().Text(ortGenel > 0 ? ortGenel.ToString("N2", Tr) : "—").SemiBold();
            table.Cell().ColumnSpan(2).Element(HucreToplam).Text("");
        });
    }

    private static void ModulOzetTablosu(IContainer container, List<RaporModulOzeti> ozetler)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.ConstantColumn(70);
                c.ConstantColumn(100);
                c.ConstantColumn(80);
            });

            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("Modül");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Kayıt");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Toplam Tutar");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Pay %");
            });

            var genel = ozetler.Sum(o => o.ToplamTutar);
            foreach (var o in ozetler)
            {
                var pay = genel > 0 ? o.ToplamTutar / genel * 100m : 0;
                table.Cell().Element(HucreVeri).Text(o.ModulAdi);
                table.Cell().Element(HucreVeri).AlignRight().Text(o.KayitSayisi.ToString());
                table.Cell().Element(HucreVeri).AlignRight().Text(o.ToplamTutar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).AlignRight().Text($"{pay:N1}%");
            }

            table.Cell().Element(HucreBaslik).Text("GENEL TOPLAM").SemiBold();
            table.Cell().Element(HucreBaslik).AlignRight().Text(ozetler.Sum(o => o.KayitSayisi).ToString());
            table.Cell().Element(HucreBaslik).AlignRight().Text(genel.ToString("N2", Tr));
            table.Cell().Element(HucreBaslik).AlignRight().Text("100%");
        });
    }

    private static void GrupOzetTablosu(IContainer container, string raporTuru, List<RaporGrupOzeti> gruplar)
    {
        var baslik = raporTuru switch
        {
            RaporTurleri.TedarikciOzeti => "Tedarikçi / Hizmet Veren",
            RaporTurleri.SahaOzeti => "Saha",
            _ => "Kategori"
        };

        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.5f);
                c.ConstantColumn(60);
                c.ConstantColumn(90);
                c.RelativeColumn(2);
            });

            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text(baslik);
                h.Cell().Element(HucreBaslik).AlignRight().Text("Kayıt");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Toplam");
                h.Cell().Element(HucreBaslik).Text("Modül Dağılımı");
            });

            foreach (var g in gruplar)
            {
                table.Cell().Element(HucreVeri).Text(g.GrupAdi);
                table.Cell().Element(HucreVeri).AlignRight().Text(g.KayitSayisi.ToString());
                table.Cell().Element(HucreVeri).AlignRight().Text(g.ToplamTutar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).Text(g.ModulDagilimi);
            }

            var toplamKayit = gruplar.Sum(g => g.KayitSayisi);
            var toplamTutar = gruplar.Sum(g => g.ToplamTutar);
            table.Cell().Element(HucreToplam).Text("TOPLAM").SemiBold();
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamKayit.ToString()).SemiBold();
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamTutar.ToString("N2", Tr)).SemiBold();
            table.Cell().Element(HucreToplam).Text("");
        });
    }

    private static void DetayTablosu(IContainer container, List<RaporDetaySatiri> satirlar)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(68);
                c.RelativeColumn(0.8f);
                c.ConstantColumn(62);
                c.ConstantColumn(68);
                c.RelativeColumn(1.1f);
                c.ConstantColumn(38);
                c.ConstantColumn(48);
                c.ConstantColumn(52);
                c.RelativeColumn(0.8f);
                c.RelativeColumn(0.7f);
                c.ConstantColumn(62);
            });

            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("Modül");
                h.Cell().Element(HucreBaslik).Text("Tarih");
                h.Cell().Element(HucreBaslik).Text("Belge");
                h.Cell().Element(HucreBaslik).Text("Kategori");
                h.Cell().Element(HucreBaslik).Text("Açıklama");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Miktar");
                h.Cell().Element(HucreBaslik).Text("Birim");
                h.Cell().Element(HucreBaslik).AlignRight().Text("B.Fiyat");
                h.Cell().Element(HucreBaslik).Text("Tedarikçi");
                h.Cell().Element(HucreBaslik).Text("Saha");
                h.Cell().Element(HucreBaslik).AlignRight().Text("Tutar");
            });

            foreach (var s in satirlar)
            {
                table.Cell().Element(HucreVeri).Text(s.Modul);
                table.Cell().Element(HucreVeri).Text(s.Tarih);
                table.Cell().Element(HucreVeri).Text(s.BelgeNo);
                table.Cell().Element(HucreVeri).Text(s.Kategori);
                table.Cell().Element(HucreVeri).Text(s.Aciklama);
                table.Cell().Element(HucreVeri).AlignRight().Text(s.Miktar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).Text(s.Birim);
                table.Cell().Element(HucreVeri).AlignRight().Text(s.BirimFiyati > 0 ? s.BirimFiyati.ToString("N2", Tr) : "—");
                table.Cell().Element(HucreVeri).Text(s.Tedarikci);
                table.Cell().Element(HucreVeri).Text(s.Saha);
                table.Cell().Element(HucreVeri).AlignRight().Text(s.Tutar.ToString("N2", Tr));
            }

            var toplamMiktar = satirlar.Sum(s => s.Miktar);
            var toplamTutar = satirlar.Sum(s => s.Tutar);
            table.Cell().ColumnSpan(5).Element(HucreToplam).Text("TOPLAM").SemiBold();
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamMiktar.ToString("N2", Tr)).SemiBold();
            table.Cell().Element(HucreToplam).Text("");
            table.Cell().Element(HucreToplam).Text("");
            table.Cell().ColumnSpan(2).Element(HucreToplam).Text("");
            table.Cell().Element(HucreToplam).AlignRight().Text(toplamTutar.ToString("N2", Tr)).SemiBold();
        });
    }

    private static void BaslikOlustur(IContainer container, UygulamaAyarlar ayarlar, string baslik)
    {
        const float logoGenislik = 96f;
        const float logoYukseklik = 60f;
        var logoYol = SatinalmaProLogoDeposu.TamYol(ayarlar.LogoDosyaYolu);
        var firmaAdi = string.IsNullOrWhiteSpace(ayarlar.FirmaAdi) ? UygulamaBilgisi.Ad : ayarlar.FirmaAdi;

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (!string.IsNullOrEmpty(logoYol))
                    row.ConstantItem(logoGenislik).Height(logoYukseklik).AlignLeft().Image(logoYol).FitArea();
                else
                    row.ConstantItem(logoGenislik);

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

    private static void SayfaAlti(IContainer container) =>
        container.AlignCenter().Text(t =>
        {
            t.Span("Sayfa ");
            t.CurrentPageNumber();
            t.Span(" / ");
            t.TotalPages();
        });

    private static IContainer HucreBaslik(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(3);

    private static IContainer HucreVeri(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(3);

    private static IContainer HucreToplam(IContainer c) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten3).Padding(3);

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
