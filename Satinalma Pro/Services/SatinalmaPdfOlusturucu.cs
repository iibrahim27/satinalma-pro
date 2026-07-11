using SatinalmaPro.Helpers;
using SatinalmaPro.Views.Modules;
using System.Globalization;
using System.IO;
using SatinalmaPro.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SatinalmaPro.Services;

public static class SatinalmaPdfOlusturucu
{
    static SatinalmaPdfOlusturucu() =>
        QuestPDF.Settings.License = LicenseType.Community;

  private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private static string TlGosterim(decimal tutar) => $"{tutar.ToString("N2", Tr)} ₺";

    private static SatinalmaAyarlar PdfGirisHazirla(SatinalmaTalep talep, SatinalmaAyarlar ayarlar)
    {
        SatinalmaDepo.TalebiHazirla(talep);
        return SatinalmaDepo.AyarlariHazirla(ayarlar);
    }

    private static void PdfHataKaydet(Exception ex, string kaynak)
    {
        HataGunlugu.Kaydet(ex, kaynak);
        System.Windows.MessageBox.Show(
            $"PDF oluşturulamadı:\n{ex.Message}",
            UygulamaBilgisi.Ad,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
    }

    private static void PdfOnizle(string onerilenDosyaAdi, string baslik, Action<string> uret) =>
        PdfOnizlemeServisi.Goster(uret, onerilenDosyaAdi, baslik);

    public static void TalepFormuYazdir(SatinalmaTalep talep, SatinalmaAyarlar ayarlar)
    {
        try
        {
            ayarlar = PdfGirisHazirla(talep, ayarlar);

        var ad = $"Talep_{talep.TalepNo}.pdf";
        PdfOnizle(ad, "Satın Alma Talep Formu", dosya =>
        {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "SATIN ALMA TALEP FORMU"));

                page.Content().ScaleToFit().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Tarih: {talep.Tarih}");
                        row.RelativeItem().AlignRight().Text($"Talep No: {talep.TalepNo}").SemiBold();
                    });

                    if (!string.IsNullOrWhiteSpace(talep.TalepEden))
                        col.Item().Text($"Talep Eden: {talep.TalepEden}");

                    if (!string.IsNullOrWhiteSpace(talep.TalepAciklamasi))
                    {
                        col.Item().Text("Talep Açıklaması").SemiBold().FontSize(11);
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                            .Text(talep.TalepAciklamasi);
                    }

                    col.Item().PaddingTop(4).Element(c => KalemTablosu(c, talep.Kalemler));

                    col.Item().ShowEntire().PaddingTop(10)
                        .Element(c => ImzaAlanlariOlustur(c, ayarlar));
                });
            });
        }).GeneratePdf(dosya);
        });
        }
        catch (Exception ex)
        {
            PdfHataKaydet(ex, "TalepPdf");
        }
    }

    public static void TedarikciTeklifTalebiYazdir(SatinalmaTalep talep, SatinalmaAyarlar ayarlar)
    {
        try
        {
            ayarlar = PdfGirisHazirla(talep, ayarlar);

        if (talep.Kalemler.Count == 0)
        {
            System.Windows.MessageBox.Show("Teklif talebi için en az bir malzeme kalemi olmalıdır.", UygulamaBilgisi.Ad);
            return;
        }

        var ad = $"Tedarikci_Teklif_Talebi_{talep.TalepNo}.pdf";
        PdfOnizle(ad, "Tedarikçi Teklif Talep Formu", dosya =>
        {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "TEDARİKÇİ TEKLİF TALEP FORMU"));

                page.Content().ScaleToFit().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Aşağıdaki malzemeler için birim fiyat teklifinizi doldurarak iade ediniz.")
                        .FontSize(10).Italic().FontColor(Colors.Grey.Darken1);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Talep No: {talep.TalepNo}").SemiBold();
                        row.RelativeItem().AlignRight().Text($"Tarih: {talep.Tarih}");
                    });

                    col.Item().PaddingTop(4).Element(c => TedarikciTeklifTablosu(c, talep.Kalemler));

                    col.Item().PaddingTop(12).Element(c => TeklifIstemeSartnameleriBolumu(c, ayarlar.TeklifIstemeSartnameleri));
                });
            });
        }).GeneratePdf(dosya);
        });

        }
        catch (Exception ex)
        {
            PdfHataKaydet(ex, "TedarikciTeklifPdf");
        }
    }

    public static void KarsilastirmaYazdir(SatinalmaTalep talep, SatinalmaAyarlar ayarlar,
        SatinalmaTeklif? onerilenTeklif = null, bool yonetimFormu = false)
    {
        try
        {
            ayarlar = PdfGirisHazirla(talep, ayarlar);

            if (talep.Teklifler.Count == 0)
            {
                System.Windows.MessageBox.Show("Karşılaştırma için en az bir teklif girilmelidir.", UygulamaBilgisi.Ad);
                return;
            }

            if (talep.Kalemler.Count == 0)
            {
                System.Windows.MessageBox.Show("PDF için talep kalemi bulunamadı.", UygulamaBilgisi.Ad);
                return;
            }

            foreach (var t in talep.Teklifler)
            {
                t.Fiyatlar ??= [];
                if (t.Fiyatlar.Count == 0)
                    SatinalmaDepo.TeklifFiyatlariniHazirla(talep, t);
                t.FiyatlariHesapla(talep.Kalemler);
            }

            onerilenTeklif ??= talep.OnerilenTeklifFirma() ?? talep.Teklifler.OrderBy(t => t.GenelToplam).FirstOrDefault();

            UygulamaAyarDeposu.Yukle();

            var ad = yonetimFormu
                ? $"Yonetim_Karsilastirma_{talep.TalepNo}.pdf"
                : $"Karsilastirma_{talep.TalepNo}.pdf";
            var baslik = yonetimFormu
                ? "Fiyat Karşılaştırma — Yönetim Onayı"
                : "Fiyat Karşılaştırma Tablosu";

            PdfOnizle(ad, baslik, dosya =>
            {
            var kalemler = talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
            var teklifler = talep.Teklifler.ToList();
            var markaGoster = KarsilastirmaMarkaSutunuGoster(teklifler);
            var onayKaydiVar = yonetimFormu && YonetimOnayKaydiVar(talep);
            var pdfBaslik = yonetimFormu
                ? "FİYAT KARŞILAŞTIRMA — YÖNETİM ONAYI"
                : "FİYAT KARŞILAŞTIRMA TABLOSU";

            Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(yonetimFormu ? 16 : 28);
                page.DefaultTextStyle(x => x.FontSize(yonetimFormu ? 7.5f : 9).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, pdfBaslik, yonetimFormu));

                var icerik = page.Content().ScaleToFit();

                icerik.Column(col =>
                {
                    col.Spacing(yonetimFormu ? 3 : 6);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Talep No: {talep.TalepNo}");
                        row.RelativeItem().AlignRight().Text($"Tarih: {talep.Tarih}");
                    });

                    if (!string.IsNullOrWhiteSpace(talep.TalepEden))
                        col.Item().Text($"Talep Eden: {talep.TalepEden}");

                    if (talep.SatinalmaKalemOnerisiElleSecildi && SatinalmaOneriYardimcisi.HerhangiKalemOnerili(talep))
                    {
                        var (_, _, genel) = SatinalmaOneriYardimcisi.OnerilenToplamlar(talep);
                        col.Item().Border(1).BorderColor(Colors.Blue.Lighten3)
                            .Background(Colors.Blue.Lighten5).Padding(4).Text(text =>
                            {
                                text.Span("Satınalma Önerisi (kalem bazlı): ").SemiBold();
                                text.Span(SatinalmaOneriYardimcisi.OneriMetni(talep).Replace("Satınalma önerisi (kalem bazlı): ", ""))
                                    .FontColor(Colors.Blue.Darken2);
                            });
                    }
                    else if (onerilenTeklif != null)
                    {
                        col.Item().Border(1).BorderColor(Colors.Blue.Lighten3)
                            .Background(Colors.Blue.Lighten5).Padding(4).Text(text =>
                            {
                                text.Span("Satınalma Önerisi: ").SemiBold();
                                text.Span(onerilenTeklif.FirmaAdi).SemiBold().FontColor(Colors.Blue.Darken2);
                                text.Span($" — KDV Hariç: {onerilenTeklif.AraToplam.ToString("N2", Tr)} ₺");
                                text.Span($" | KDV: {onerilenTeklif.KdvTutari.ToString("N2", Tr)} ₺");
                                text.Span($" | KDV Dahil: {onerilenTeklif.GenelToplam.ToString("N2", Tr)} ₺");
                            });
                    }

                    col.Item().PaddingTop(yonetimFormu ? 2 : 8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(yonetimFormu ? 18 : 24);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(yonetimFormu ? 44 : 52);
                            columns.ConstantColumn(yonetimFormu ? 36 : 42);
                            KarsilastirmaTeklifKolonlariTanimla(columns, teklifler, markaGoster);
                        });

                        KarsilastirmaTabloBasliklariEkle(table, teklifler, onerilenTeklif, yonetimFormu, markaGoster);

                        foreach (var kalem in kalemler)
                        {
                            var fiyatlar = teklifler.Select(t =>
                                t.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id)).ToList();
                            var enDusuk = fiyatlar.Where(f => f != null && f.ToplamTutar > 0)
                                .MinBy(f => f!.ToplamTutar)?.ToplamTutar;

                            table.Cell().Element(c => HucreVeri(c, false, yonetimFormu)).Text(kalem.SiraNo.ToString());
                            table.Cell().Element(c => HucreVeri(c, false, yonetimFormu)).Text(kalem.Malzeme);
                            table.Cell().Element(c => HucreVeri(c, false, yonetimFormu)).AlignRight()
                                .Text(kalem.Miktar.ToString("N2", Tr));
                            table.Cell().Element(c => HucreVeri(c, false, yonetimFormu)).Text(kalem.Birim);

                            for (var i = 0; i < teklifler.Count; i++)
                            {
                                var teklif = teklifler[i];
                                var fiyat = fiyatlar[i];
                                var onerilen = onerilenTeklif != null && teklif.Id == onerilenTeklif.Id;
                                var dusuk = fiyat != null && fiyat.ToplamTutar > 0 && fiyat.ToplamTutar == enDusuk;
                                var onayli = kalem.OnaylananTeklifId == teklif.Id;
                                var vurgula = onayKaydiVar
                                    ? onayli
                                    : SatinalmaOneriYardimcisi.HucreOneriVurgula(talep, kalem, teklif, onerilenTeklif, dusuk);
                                table.Cell().Element(c => HucreVeri(c, vurgula, yonetimFormu)).AlignRight()
                                    .Text(fiyat?.BirimFiyatGosterim(teklif.UsdKuru, teklif.EurKuru) ?? "—");
                                if (markaGoster)
                                {
                                    table.Cell().Element(c => HucreVeri(c, vurgula, yonetimFormu))
                                        .Text(string.IsNullOrWhiteSpace(fiyat?.Marka) ? "—" : fiyat!.Marka);
                                }
                                table.Cell().Element(c => HucreVeri(c, vurgula, yonetimFormu)).AlignRight()
                                    .Text(fiyat != null ? TlGosterim(fiyat.ToplamTutar) : "—");
                            }
                        }

                        KarsilastirmaToplamSatirleriEkle(table, teklifler, onerilenTeklif, yonetimFormu, markaGoster);
                    });

                    if (onayKaydiVar)
                    {
                        var onayliTeklifler = teklifler.Where(t => TeklifOnayliMi(talep, t)).ToList();
                        if (onayliTeklifler.Count > 0)
                        {
                            col.Item().Border(1).BorderColor(Colors.Green.Medium).Background(Colors.Green.Lighten5)
                                .Padding(6).Text(text =>
                                {
                                    text.Span("Onaylanan Firma(lar): ").SemiBold();
                                    text.Span(string.Join(" · ", onayliTeklifler.Select(t => t.FirmaAdi))).SemiBold()
                                        .FontColor(Colors.Green.Darken2);
                                });
                        }

                        col.Item().PaddingTop(6).Element(c => YonetimOnayBilgiKutusuOlustur(c, talep, kompakt: true));
                        col.Item().PaddingTop(4).Element(c => YonetimImzaAlaniOlustur(c, ayarlar, kompakt: true, talep));
                    }
                    else
                    {
                        if (yonetimFormu)
                            col.Item().PaddingTop(4).Text("YÖNETİM ONAY BÖLÜMÜ").SemiBold().FontSize(8.5f);
                        col.Item().PaddingTop(yonetimFormu ? 2 : 4)
                            .Text("Onaylanacak firmayı işaretleyiniz:").FontSize(yonetimFormu ? 7.5f : 9);
                        col.Item().Element(c =>
                            KarsilastirmaFirmaOnayTablosuEkle(c, teklifler, onerilenTeklif, yonetimFormu));

                        col.Item().ShowEntire().PaddingTop(yonetimFormu ? 6 : 12).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Onaylanan Firma:").SemiBold().FontSize(yonetimFormu ? 7.5f : 9);
                                c.Item().PaddingTop(yonetimFormu ? 8 : 12).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                            });
                            row.ConstantItem(yonetimFormu ? 12 : 24);
                            row.RelativeItem().AlignCenter().Element(c =>
                                YonetimImzaAlaniOlustur(c, ayarlar, kompakt: yonetimFormu, talep));
                        });
                    }
                });
            });

            // Sayfa 2+: karşılaştırma kalemlerinin son alım geçmişi
            var alimSatirlari = KarsilastirmaAlimGecmisiYardimcisi.MalzemeBazliAlimlariTopla(
                kalemler, ModulVeriDeposu.AlinanMalzemeler);
            KarsilastirmaAlimGecmisiSayfasiEkle(container, talep, ayarlar, alimSatirlari, yonetimFormu);
        }).GeneratePdf(dosya);
            });
        }
        catch (Exception ex)
        {
            PdfHataKaydet(ex, "KarsilastirmaPdf");
        }
    }

    private static void KarsilastirmaAlimGecmisiSayfasiEkle(
        IDocumentContainer container,
        SatinalmaTalep talep,
        SatinalmaAyarlar ayarlar,
        IReadOnlyList<KarsilastirmaAlimGecmisiYardimcisi.AlimSatiri> satirlari,
        bool yonetimFormu)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(yonetimFormu ? 16 : 28);
            page.DefaultTextStyle(x => x.FontSize(yonetimFormu ? 7.5f : 9).FontFamily("Segoe UI"));

            page.Header().Element(c =>
                BaslikOlustur(c, ayarlar, "SON ALIMLAR (KARŞILAŞTIRMA REFERANSI)", yonetimFormu));

            page.Content().Column(col =>
            {
                col.Spacing(yonetimFormu ? 3 : 6);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"Talep No: {talep.TalepNo}");
                    row.RelativeItem().AlignRight().Text($"Tarih: {talep.Tarih}");
                });

                col.Item().Text("Son 2 aydaki alımlar listelenir; kayıt yoksa en son 2 alım gösterilir.")
                    .FontColor(Colors.Grey.Darken1)
                    .FontSize(yonetimFormu ? 7f : 8f);

                col.Item().PaddingTop(yonetimFormu ? 2 : 4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(yonetimFormu ? 22 : 28);
                        columns.RelativeColumn(2.4f);
                        columns.ConstantColumn(yonetimFormu ? 58 : 68);
                        columns.ConstantColumn(yonetimFormu ? 52 : 60);
                        columns.ConstantColumn(yonetimFormu ? 40 : 48);
                        columns.ConstantColumn(yonetimFormu ? 72 : 84);
                        columns.RelativeColumn(1.8f);
                    });

                    static void BaslikHucre(TableDescriptor t, string metin, bool kompakt) =>
                        t.Cell().Element(c => HucreBaslik(c, false, kompakt)).AlignCenter().Text(metin).SemiBold();

                    BaslikHucre(table, "No", yonetimFormu);
                    BaslikHucre(table, "Malzeme", yonetimFormu);
                    BaslikHucre(table, "Tarih", yonetimFormu);
                    BaslikHucre(table, "Miktar", yonetimFormu);
                    BaslikHucre(table, "Birim", yonetimFormu);
                    BaslikHucre(table, "Birim Fiyat", yonetimFormu);
                    BaslikHucre(table, "Tedarikçi", yonetimFormu);

                    if (satirlari.Count == 0)
                    {
                        table.Cell().ColumnSpan(7).Element(c => HucreVeri(c, false, yonetimFormu))
                            .Text("Karşılaştırma kalemleri için alım kaydı bulunamadı.");
                        return;
                    }

                    var oncekiMalzeme = "";
                    foreach (var satir in satirlari)
                    {
                        var malzemeDegisti = !string.Equals(
                            oncekiMalzeme, satir.Malzeme, StringComparison.OrdinalIgnoreCase);
                        oncekiMalzeme = satir.Malzeme;

                        table.Cell().Element(c => HucreVeri(c, false, yonetimFormu))
                            .Text(malzemeDegisti ? satir.KalemSiraNo.ToString() : "");
                        table.Cell().Element(c => HucreVeri(c, false, yonetimFormu))
                            .Text(malzemeDegisti ? satir.Malzeme : "");

                        if (satir.KayitYok)
                        {
                            table.Cell().ColumnSpan(5).Element(c => HucreVeri(c, false, yonetimFormu))
                                .Text("Alım kaydı yok").FontColor(Colors.Grey.Darken1).Italic();
                            continue;
                        }

                        table.Cell().Element(c => HucreVeri(c, false, yonetimFormu)).AlignCenter()
                            .Text(satir.Tarih);
                        table.Cell().Element(c => HucreVeri(c, false, yonetimFormu)).AlignRight()
                            .Text(satir.Miktar.ToString("N2", Tr));
                        table.Cell().Element(c => HucreVeri(c, false, yonetimFormu)).AlignCenter()
                            .Text(satir.Birim);
                        table.Cell().Element(c => HucreVeri(c, false, yonetimFormu)).AlignRight()
                            .Text(TlGosterim(satir.BirimFiyati));
                        table.Cell().Element(c => HucreVeri(c, false, yonetimFormu))
                            .Text(satir.SonIkiAlimYedegi
                                ? $"{satir.Tedarikci} *"
                                : satir.Tedarikci);
                    }
                });

                if (satirlari.Any(s => s.SonIkiAlimYedegi))
                {
                    col.Item().Text("* Son 2 ayda alım yok; en son 2 alım gösterildi.")
                        .FontSize(yonetimFormu ? 6.5f : 7.5f)
                        .FontColor(Colors.Grey.Darken1);
                }
            });
        });
    }

    public static void YonetimOnayBelgesiYazdir(SatinalmaTalep talep, SatinalmaAyarlar ayarlar)
    {
        try
        {
            ayarlar = PdfGirisHazirla(talep, ayarlar);

            if (talep.Teklifler.Count == 0)
            {
                if (talep.TeklifsizYonetimOnayi && talep.YonetimOnayKilitli)
                {
                    TeklifsizYonetimOnayBelgesiYazdir(talep, ayarlar);
                    return;
                }

                System.Windows.MessageBox.Show("Onay belgesi için teklif kaydı bulunamadı.", UygulamaBilgisi.Ad);
                return;
            }

            if (!talep.HerhangiKalemOnayli && talep.OnaylananTeklifId is null && !talep.TeklifsizYonetimOnayi)
            {
                System.Windows.MessageBox.Show("Henüz onaylanmış firma/teklif yok.", UygulamaBilgisi.Ad);
                return;
            }

        foreach (var t in talep.Teklifler)
            t.FiyatlariHesapla(talep.Kalemler);

        var ad = $"Yonetim_Onay_{talep.TalepNo}.pdf";
        PdfOnizle(ad, "Yönetim Onay Belgesi", dosya =>
        {
        var kalemler = talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
        var teklifler = talep.Teklifler.ToList();
        var markaGoster = KarsilastirmaMarkaSutunuGoster(teklifler);
        var onaylayanAd = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd) ? "—" : talep.YonetimOnaylayanAd;
        var onaylayanEposta = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanEposta) ? "—" : talep.YonetimOnaylayanEposta;
        var onayTarihi = string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi)
            ? DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            : talep.YonetimOnayTarihi;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(x => x.FontSize(7.5f).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "YÖNETİM ONAY BELGESİ", kompakt: true));

                page.Content().ScaleToFit().Column(col =>
                {
                    col.Spacing(4);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Talep No: {talep.TalepNo}").SemiBold();
                        row.RelativeItem().AlignCenter().Text($"Sipariş No: {(string.IsNullOrWhiteSpace(talep.SiparisNo) ? "—" : talep.SiparisNo)}");
                        row.RelativeItem().AlignRight().Text($"Talep Tarihi: {talep.Tarih}");
                    });

                    if (!string.IsNullOrWhiteSpace(talep.TalepEden))
                        col.Item().Text($"Talep Eden: {talep.TalepEden}");

                    if (talep.TalepTuru == TalepTurleri.Acil)
                        col.Item().Text("Talep Türü: Acil / teklifsiz alım").FontColor(Colors.Orange.Darken2).SemiBold();

                    var onayliTeklifler = teklifler.Where(t => TeklifOnayliMi(talep, t)).ToList();
                    if (onayliTeklifler.Count > 0)
                    {
                        col.Item().Border(1).BorderColor(Colors.Green.Medium).Background(Colors.Green.Lighten5)
                            .Padding(6).Text(text =>
                            {
                                text.Span("Onaylanan Firma(lar): ").SemiBold();
                                text.Span(string.Join(" · ", onayliTeklifler.Select(t => t.FirmaAdi))).SemiBold()
                                    .FontColor(Colors.Green.Darken2);
                            });
                    }

                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(18);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(44);
                            columns.ConstantColumn(36);
                            KarsilastirmaTeklifKolonlariTanimla(columns, teklifler, markaGoster);
                        });

                        YonetimOnayTabloBasliklariEkle(table, teklifler, talep, markaGoster);

                        foreach (var kalem in kalemler)
                        {
                            var fiyatlar = teklifler.Select(t =>
                                t.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id)).ToList();

                            table.Cell().Element(c => HucreVeri(c, false, true)).Text(kalem.SiraNo.ToString());
                            table.Cell().Element(c => HucreVeri(c, false, true)).Text(kalem.Malzeme);
                            table.Cell().Element(c => HucreVeri(c, false, true)).AlignRight()
                                .Text(kalem.Miktar.ToString("N2", Tr));
                            table.Cell().Element(c => HucreVeri(c, false, true)).Text(kalem.Birim);

                            for (var i = 0; i < teklifler.Count; i++)
                            {
                                var teklif = teklifler[i];
                                var fiyat = fiyatlar[i];
                                var onayli = kalem.OnaylananTeklifId == teklif.Id;
                                table.Cell().Element(c => HucreVeri(c, onayli, true)).AlignRight()
                                    .Text(fiyat?.BirimFiyatGosterim(teklif.UsdKuru, teklif.EurKuru) ?? "—");
                                if (markaGoster)
                                {
                                    table.Cell().Element(c => HucreVeri(c, onayli, true))
                                        .Text(string.IsNullOrWhiteSpace(fiyat?.Marka) ? "—" : fiyat!.Marka);
                                }
                                table.Cell().Element(c => HucreVeri(c, onayli, true)).AlignRight()
                                    .Text(fiyat != null ? TlGosterim(fiyat.ToplamTutar) : "—");
                            }
                        }

                        KarsilastirmaToplamSatirleriEkle(table, teklifler,
                            teklifler.FirstOrDefault(t => t.Id == talep.OnaylananTeklifId), true, markaGoster);
                    });

                    col.Item().PaddingTop(6).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8)
                        .Element(c => YonetimOnayBilgiKutusuOlustur(c, talep, kompakt: true));
                    if (talep.YonetimOnayKilitli)
                        col.Item().PaddingTop(4).Text("Bu onay yönetim tarafından verilmiştir ve geri alınamaz.")
                            .FontSize(7.5f).Italic().FontColor(Colors.Red.Medium);

                    col.Item().PaddingTop(4).Element(c => YonetimImzaAlaniOlustur(c, ayarlar, kompakt: true, talep));
                });
            });
        }).GeneratePdf(dosya);
        });

        }
        catch (Exception ex)
        {
            PdfHataKaydet(ex, "YonetimOnayPdf");
        }
    }

    private static void TeklifsizYonetimOnayBelgesiYazdir(SatinalmaTalep talep, SatinalmaAyarlar ayarlar)
    {
        ayarlar = SatinalmaDepo.AyarlariHazirla(ayarlar);
        var ad = $"Yonetim_Onay_{talep.TalepNo}.pdf";
        PdfOnizle(ad, "Yönetim Onay Belgesi (Teklifsiz)", dosya =>
        {
        var kalemler = talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
        var onaylayanAd = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd) ? "—" : talep.YonetimOnaylayanAd;
        var onaylayanEposta = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanEposta) ? "—" : talep.YonetimOnaylayanEposta;
        var onayTarihi = string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi)
            ? DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            : talep.YonetimOnayTarihi;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));
                page.Header().Element(c => BaslikOlustur(c, ayarlar, "YÖNETİM ONAY BELGESİ"));
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Talep No: {talep.TalepNo}").SemiBold();
                    col.Item().Text($"Talep Tarihi: {talep.Tarih}");
                    if (!string.IsNullOrWhiteSpace(talep.TalepEden))
                        col.Item().Text($"Talep Eden: {talep.TalepEden}");
                    col.Item().Text("Onay Türü: Teklifsiz yönetim onayı").FontColor(Colors.Green.Darken2).SemiBold();
                    col.Item().PaddingTop(6).Text("Talep Kalemleri").SemiBold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(28);
                            c.RelativeColumn(2);
                            c.ConstantColumn(60);
                            c.ConstantColumn(50);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(c => HucreBaslik(c, false, false)).Text("No");
                            h.Cell().Element(c => HucreBaslik(c, false, false)).Text("Malzeme");
                            h.Cell().Element(c => HucreBaslik(c, false, false)).AlignRight().Text("Miktar");
                            h.Cell().Element(c => HucreBaslik(c, false, false)).Text("Birim");
                        });
                        foreach (var kalem in kalemler)
                        {
                            table.Cell().Element(c => HucreVeri(c, false, false)).Text(kalem.SiraNo.ToString());
                            table.Cell().Element(c => HucreVeri(c, false, false)).Text(kalem.Malzeme);
                            table.Cell().Element(c => HucreVeri(c, false, false)).AlignRight()
                                .Text(kalem.Miktar.ToString("N2", Tr));
                            table.Cell().Element(c => HucreVeri(c, false, false)).Text(kalem.Birim);
                        }
                    });
                    col.Item().PaddingTop(8).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(onay =>
                    {
                        onay.Item().Text("YÖNETİM ONAY BİLGİSİ").SemiBold();
                        onay.Item().PaddingTop(4).Text(text =>
                        {
                            text.Span("Onaylayan: ").SemiBold();
                            text.Span(onaylayanAd);
                            text.Span("   E-posta: ").SemiBold();
                            text.Span(onaylayanEposta);
                            text.Span("   Onay Tarihi: ").SemiBold();
                            text.Span(onayTarihi);
                        });
                        onay.Item().PaddingTop(4).Text("Firma ve birim fiyat bilgisi satınalma birimi tarafından sonradan girilecektir.")
                            .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                    });
                    col.Item().Element(c => YonetimOnayBilgiKutusuOlustur(c, talep));
                    col.Item().Element(c => YonetimImzaAlaniOlustur(c, ayarlar, talep: talep));
                });
            });
        }).GeneratePdf(dosya);
        });
    }

    private static bool TeklifOnayliMi(SatinalmaTalep talep, SatinalmaTeklif teklif) =>
        talep.OnaylananTeklifId == teklif.Id ||
        talep.Kalemler.Any(k => k.OnaylananTeklifId == teklif.Id);

    private static void YonetimOnayTabloBasliklariEkle(TableDescriptor table, List<SatinalmaTeklif> teklifler,
        SatinalmaTalep talep, bool markaGoster)
    {
        var grupSutun = TeklifGrupSutunSayisi(markaGoster);
        table.Cell().RowSpan(2).Element(c => HucreBaslik(c, false, true)).AlignCenter().Text("No");
        table.Cell().RowSpan(2).Element(c => HucreBaslik(c, false, true)).Text("Malzeme");
        table.Cell().RowSpan(2).Element(c => HucreBaslik(c, false, true)).AlignCenter().Text("Miktar");
        table.Cell().RowSpan(2).Element(c => HucreBaslik(c, false, true)).AlignCenter().Text("Birim");

        foreach (var teklif in teklifler)
        {
            var onayli = TeklifOnayliMi(talep, teklif);
            var baslik = onayli ? $"{teklif.FirmaAdi}\n✓ ONAYLANDI" : teklif.FirmaAdi;
            table.Cell().ColumnSpan((uint)grupSutun).Element(c => HucreBaslik(c, onayli, true))
                .AlignCenter().Text(baslik);
        }

        foreach (var teklif in teklifler)
        {
            var onayli = TeklifOnayliMi(talep, teklif);
            table.Cell().Element(c => HucreBaslik(c, onayli, true)).AlignCenter().Text("Birim Fiyatı");
            if (markaGoster)
                table.Cell().Element(c => HucreBaslik(c, onayli, true)).AlignCenter().Text("Marka");
            table.Cell().Element(c => HucreBaslik(c, onayli, true)).AlignCenter().Text("Toplam KDV Hariç");
        }
    }

    public static void SiparisFormuYazdir(SatinalmaTalep talep, SatinalmaAyarlar ayarlar) =>
        SiparisFormlariYazdir(talep, ayarlar);

    public static void SiparisFormlariYazdir(SatinalmaTalep talep, SatinalmaAyarlar ayarlar)
    {
        try
        {
            ayarlar = PdfGirisHazirla(talep, ayarlar);

        var gruplar = OnayliFirmaGruplari(talep);

        if (gruplar.Count == 0)
        {
            System.Windows.MessageBox.Show("Önce en az bir malzeme için firma onayı vermeniz gerekir.", UygulamaBilgisi.Ad);
            return;
        }

        var siparisSartname = SiparisSartnameWindow.DuzenleVeOnayla(ayarlar.SartnameMetni, talep.TalepNo);
        if (siparisSartname is null)
            return;

        var olusturulan = 0;
        foreach (var grup in gruplar)
        {
            var teklif = talep.Teklifler.FirstOrDefault(t => t.Id == grup.Key);
            if (teklif == null)
                continue;

            var kalemler = grup.ToList();
            var siparisNo = SatinalmaDepo.SiparisNoAl(talep, teklif.Id);
            var firmaAdi = string.IsNullOrWhiteSpace(teklif.FirmaAdi) ? "Firma" : teklif.FirmaAdi;
            var ad = $"Siparis_{siparisNo}_{firmaAdi}.pdf";

            PdfOnizle(ad, $"Sipariş Formu — {firmaAdi}",
                dosya => SiparisPdfOlustur(talep, teklif, kalemler, siparisNo, ayarlar, dosya, siparisSartname));
            olusturulan++;
        }

        if (olusturulan == 0)
            return;
        }
        catch (Exception ex)
        {
            PdfHataKaydet(ex, "SiparisPdf");
        }
    }

    public static void SiparisOnayFormlariYazdir(SatinalmaTalep talep, SatinalmaAyarlar ayarlar)
    {
        try
        {
            ayarlar = PdfGirisHazirla(talep, ayarlar);

        var gruplar = OnayliFirmaGruplari(talep);
        if (gruplar.Count == 0)
        {
            System.Windows.MessageBox.Show("Önce en az bir malzeme için firma onayı vermeniz gerekir.", UygulamaBilgisi.Ad);
            return;
        }

        var olusturulan = 0;
        foreach (var grup in gruplar)
        {
            var teklif = talep.Teklifler.FirstOrDefault(t => t.Id == grup.Key);
            if (teklif == null)
                continue;

            var kalemler = grup.ToList();
            var siparisNo = SatinalmaDepo.SiparisNoAl(talep, teklif.Id);
            var firmaAdi = string.IsNullOrWhiteSpace(teklif.FirmaAdi) ? "Firma" : teklif.FirmaAdi;
            var ad = $"Siparis_Onay_{siparisNo}_{firmaAdi}.pdf";

            PdfOnizle(ad, $"Sipariş Onay Formu — {firmaAdi}",
                dosya => SiparisOnayPdfOlustur(talep, teklif, kalemler, siparisNo, ayarlar, dosya));
            olusturulan++;
        }

        if (olusturulan == 0)
            return;
        }
        catch (Exception ex)
        {
            PdfHataKaydet(ex, "SiparisOnayPdf");
        }
    }

    private static List<IGrouping<Guid, SatinalmaTalepKalemi>> OnayliFirmaGruplari(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        var gruplar = talep.Kalemler
            .Where(k => k.OnaylananTeklifId != null)
            .GroupBy(k => k.OnaylananTeklifId!.Value)
            .ToList();

        if (gruplar.Count == 0 && talep.OnaylananTeklifId is { } eskiId)
            gruplar = talep.Kalemler.GroupBy(_ => eskiId).ToList();

        return gruplar;
    }

    private static void SiparisOnayPdfOlustur(SatinalmaTalep talep, SatinalmaTeklif teklif,
        List<SatinalmaTalepKalemi> kalemler, string siparisNo, SatinalmaAyarlar ayarlar, string dosya)
    {
        teklif.Fiyatlar ??= [];
        teklif.FiyatlariHesapla(talep.Kalemler);

        var araToplam = kalemler.Sum(k =>
            teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == k.Id)?.ToplamTutar ?? 0);
        var kdvToplam = kalemler.Sum(k =>
            teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == k.Id)?.KdvTutari ?? 0);
        var genelToplam = kalemler.Sum(k =>
            teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == k.Id)?.ToplamKdvDahil ?? 0);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "SİPARİŞ ONAY FORMU"));

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Talep No: {talep.TalepNo}").SemiBold();
                        row.RelativeItem().AlignCenter().Text($"Sipariş No: {siparisNo}").SemiBold();
                        row.RelativeItem().AlignRight().Text($"Tarih: {DateTime.Now:dd.MM.yyyy}");
                    });

                    if (!string.IsNullOrWhiteSpace(talep.TalepEden))
                        col.Item().Text($"Talep Eden: {talep.TalepEden}");

                    col.Item().PaddingTop(6).Border(1.5f).BorderColor(Colors.Green.Medium)
                        .Background(Colors.Green.Lighten5).Padding(12).Column(firma =>
                        {
                            firma.Item().Text("ONAYLANAN FİRMA").FontSize(9).FontColor(Colors.Green.Darken2).SemiBold();
                            firma.Item().PaddingTop(4).Text(teklif.FirmaAdi).FontSize(14).Bold();
                            firma.Item().PaddingTop(6).Text(text =>
                            {
                                text.Span("Vade: ").SemiBold();
                                text.Span($"{teklif.VadeGunu} gün   ");
                                text.Span("Ödeme: ").SemiBold();
                                text.Span($"{(string.IsNullOrWhiteSpace(teklif.OdemeSekli) ? "—" : teklif.OdemeSekli)}   ");
                                text.Span("Teslim: ").SemiBold();
                                text.Span(string.IsNullOrWhiteSpace(teklif.TeslimSuresi) ? "—" : teklif.TeslimSuresi);
                            });
                        });

                    if (teklif.UsdKuru > 0 || teklif.EurKuru > 0)
                    {
                        var kurMetni = "Döviz Kurları: ";
                        if (teklif.UsdKuru > 0)
                            kurMetni += $"USD {teklif.UsdKuru:N4} ₺   ";
                        if (teklif.EurKuru > 0)
                            kurMetni += $"EUR {teklif.EurKuru:N4} ₺";
                        col.Item().Text(kurMetni).FontSize(9).FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().PaddingTop(4).Text("Onaylanan Malzemeler").SemiBold().FontSize(11);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(26);
                            c.RelativeColumn(2.2f);
                            c.ConstantColumn(64);
                            c.ConstantColumn(48);
                            c.ConstantColumn(78);
                            c.ConstantColumn(82);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(HucreBaslik).AlignCenter().Text("No");
                            h.Cell().Element(HucreBaslik).Text("Malzeme");
                            h.Cell().Element(HucreBaslik).AlignCenter().Text("Miktar");
                            h.Cell().Element(HucreBaslik).AlignCenter().Text("Birim");
                            h.Cell().Element(HucreBaslik).AlignCenter().Text("Birim Fiyat");
                            h.Cell().Element(HucreBaslik).AlignCenter().Text("Toplam (KDV Hariç)");
                        });

                        foreach (var kalem in kalemler.OrderBy(k => k.SiraNo))
                        {
                            var fiyat = teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                            table.Cell().Element(HucreVeri).AlignCenter().Text(kalem.SiraNo.ToString());
                            table.Cell().Element(HucreVeri).Text(kalem.Malzeme);
                            table.Cell().Element(HucreVeri).AlignRight().Text(kalem.Miktar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).AlignCenter().Text(kalem.Birim);
                            table.Cell().Element(HucreVeri).AlignRight()
                                .Text(fiyat?.BirimFiyatGosterim(teklif.UsdKuru, teklif.EurKuru) ?? "—");
                            table.Cell().Element(HucreVeri).AlignRight()
                                .Text(fiyat?.ToplamTutar.ToString("N2", Tr) ?? "—");
                        }

                        table.Cell().ColumnSpan(5).Element(HucreBaslik).AlignRight().Text("Ara Toplam (KDV Hariç)");
                        table.Cell().Element(HucreBaslik).AlignRight().Text(araToplam.ToString("N2", Tr));
                        table.Cell().ColumnSpan(5).Element(HucreBaslik).AlignRight().Text("KDV");
                        table.Cell().Element(HucreBaslik).AlignRight().Text(kdvToplam.ToString("N2", Tr));
                        table.Cell().ColumnSpan(5).Element(c => HucreBaslik(c, true)).AlignRight()
                            .Text("Genel Toplam (KDV Dahil)").SemiBold();
                        table.Cell().Element(c => HucreBaslik(c, true)).AlignRight()
                            .Text(genelToplam.ToString("N2", Tr)).SemiBold();
                    });

                    col.Item().PaddingTop(14).Text(text =>
                    {
                        text.Span("Yukarıda listelenen ");
                        text.Span($"{kalemler.Count} kalem").SemiBold();
                        text.Span(" malzemenin ");
                        text.Span(teklif.FirmaAdi).SemiBold().FontColor(Colors.Green.Darken2);
                        text.Span(" firmasından satın alınması uygun görülmüş; siparişin bu firmaya verilmesi onaylanmıştır.");
                    });

                    col.Item().PaddingTop(28).Element(c => SiparisOnayImzaAlaniOlustur(c, ayarlar));
                });
            });
        }).GeneratePdf(dosya);
    }

    private static void SiparisOnayImzaAlaniOlustur(IContainer container, SatinalmaAyarlar ayarlar)
    {
        ayarlar.SefImzalari ??= [];
        ayarlar.YonetimImzalari ??= [];

        var satinalma = ayarlar.SefImzalari
            .FirstOrDefault(i => i is not null && i.Aktif && (i.Unvan ?? "").Contains("Satınalma", StringComparison.OrdinalIgnoreCase))
            ?? ayarlar.SefImzalari.FirstOrDefault(i => i is not null && i.Aktif)
            ?? new ImzaAyari { Unvan = "Satınalma", Aktif = true };

        var yonetim = ayarlar.YonetimImzalari.FirstOrDefault(i => i is not null && i.Aktif)
            ?? new ImzaAyari { Unvan = "Proje Müdürü", Aktif = true };

        container.Row(row =>
        {
            row.RelativeItem().AlignCenter().PaddingHorizontal(8)
                .Element(c => ImzaHucreOlustur(c, satinalma));
            row.ConstantItem(32);
            row.RelativeItem().AlignCenter().PaddingHorizontal(8)
                .Element(c => ImzaHucreOlustur(c, yonetim));
        });
    }

    private static void SiparisPdfOlustur(SatinalmaTalep talep, SatinalmaTeklif teklif,
        List<SatinalmaTalepKalemi> kalemler, string siparisNo, SatinalmaAyarlar ayarlar, string dosya,
        string? siparisSartnameMetni = null)
    {
        teklif.Fiyatlar ??= [];
        teklif.FiyatlariHesapla(talep.Kalemler);

        var sartnameMetni = siparisSartnameMetni ?? ayarlar.SartnameMetni;

        var gecici = Path.Combine(Path.GetTempPath(), $"satinalmapro_siparis_{Guid.NewGuid():N}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "SİPARİŞ FORMU"));

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Sipariş No: {siparisNo}").SemiBold();
                        row.RelativeItem().AlignRight().Text($"Tarih: {DateTime.Now:dd.MM.yyyy}");
                    });
                    col.Item().Text($"Talep No: {talep.TalepNo}");
                    col.Item().PaddingTop(4).Text("Sipariş Verilen Firma").SemiBold();
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(f =>
                    {
                        f.Item().Text($"Firma: {teklif.FirmaAdi}");
                        f.Item().Text($"Vade: {teklif.VadeGunu} gün  |  Ödeme: {teklif.OdemeSekli}  |  Teslim: {teklif.TeslimSuresi}");
                    });

                    if (!string.IsNullOrWhiteSpace(talep.TalepAciklamasi))
                        col.Item().Text($"Açıklama: {talep.TalepAciklamasi}");

                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(24);
                            c.RelativeColumn(2);
                            c.ConstantColumn(72);
                            c.ConstantColumn(52);
                            c.ConstantColumn(42);
                            c.ConstantColumn(70);
                            c.ConstantColumn(80);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(HucreBaslik).Text("No");
                            h.Cell().Element(HucreBaslik).Text("Malzeme");
                            h.Cell().Element(HucreBaslik).Text("Marka");
                            h.Cell().Element(HucreBaslik).Text("Miktar");
                            h.Cell().Element(HucreBaslik).Text("Birim");
                            h.Cell().Element(HucreBaslik).Text("Birim Fiyat");
                            h.Cell().Element(HucreBaslik).Text("Toplam (KDV Hariç)");
                        });

                        foreach (var kalem in kalemler.OrderBy(k => k.SiraNo))
                        {
                            var fiyat = teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                            table.Cell().Element(HucreVeri).Text(kalem.SiraNo.ToString());
                            table.Cell().Element(HucreVeri).Text(kalem.Malzeme);
                            table.Cell().Element(HucreVeri).Text(string.IsNullOrWhiteSpace(fiyat?.Marka) ? "—" : fiyat!.Marka);
                            table.Cell().Element(HucreVeri).AlignRight().Text(kalem.Miktar.ToString("N2", Tr));
                            table.Cell().Element(HucreVeri).Text(kalem.Birim);
                            table.Cell().Element(HucreVeri).AlignRight()
                                .Text(fiyat?.BirimFiyatGosterim(teklif.UsdKuru, teklif.EurKuru) ?? "—");
                            table.Cell().Element(HucreVeri).AlignRight().Text(fiyat?.ToplamTutar.ToString("N2", Tr) ?? "—");
                        }

                        var araToplam = kalemler.Sum(k =>
                            teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == k.Id)?.ToplamTutar ?? 0);
                        var kdvToplam = kalemler.Sum(k =>
                            teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == k.Id)?.KdvTutari ?? 0);
                        var genelToplam = kalemler.Sum(k =>
                            teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == k.Id)?.ToplamKdvDahil ?? 0);

                        table.Cell().ColumnSpan(6).Element(HucreBaslik).AlignRight().Text("Ara Toplam (KDV Hariç)");
                        table.Cell().Element(HucreBaslik).AlignRight().Text(araToplam.ToString("N2", Tr));
                        table.Cell().ColumnSpan(6).Element(HucreBaslik).AlignRight().Text("KDV");
                        table.Cell().Element(HucreBaslik).AlignRight().Text(kdvToplam.ToString("N2", Tr));
                        table.Cell().ColumnSpan(6).Element(HucreBaslik).AlignRight().Text("Genel Toplam (KDV Dahil)");
                        table.Cell().Element(HucreBaslik).AlignRight().Text(genelToplam.ToString("C2", Tr));
                    });

                    if (!string.IsNullOrWhiteSpace(sartnameMetni))
                    {
                        col.Item().PaddingTop(12).Text("Şartname").SemiBold();
                        col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                            .Text(sartnameMetni);
                    }

                    var dosyaSartnameler = ayarlar.Sartnameler
                        .Where(s => !string.IsNullOrWhiteSpace(s.DosyaYolu) && File.Exists(s.DosyaYolu))
                        .ToList();
                    if (dosyaSartnameler.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Ek Şartname Dosyaları").SemiBold();
                        foreach (var s in dosyaSartnameler)
                            col.Item().Text($"• {s.Ad}");
                    }

                    col.Item().PaddingTop(20).Text(
                        "Bu sipariş formu ile birlikte belirtilen şartnameler siparişin ayrılmaz parçasıdır.");
                });
            });
        }).GeneratePdf(gecici);

        var dosyaSartnameleri = ayarlar.Sartnameler
            .Where(s => !string.IsNullOrWhiteSpace(s.DosyaYolu) && File.Exists(s.DosyaYolu))
            .ToList();

        if (dosyaSartnameleri.Count > 0)
            SartnamelerleBirlestir(gecici, dosya, dosyaSartnameleri);
        else
            File.Copy(gecici, dosya, true);

        try { File.Delete(gecici); } catch { /* ignore */ }

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

    private static void BaslikOlustur(IContainer container, SatinalmaAyarlar ayarlar, string baslik, bool kompakt = false)
    {
        var logoGenislik = kompakt ? 84f : 108f;
        var logoYukseklik = kompakt ? 54f : 68f;
        var firmaBoyut = kompakt ? 11f : 14f;
        var baslikBoyut = kompakt ? 11f : 15f;
        var logoYol = SatinalmaProLogoDeposu.TamYol(UygulamaAyarDeposu.Ayarlar?.LogoDosyaYolu);
        var logoVar = !string.IsNullOrEmpty(logoYol) && File.Exists(logoYol);

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
                    c.Item().AlignCenter().Text(UygulamaAyarDeposu.Ayarlar?.FirmaAdi ?? "Satınalma Pro").Bold().FontSize(firmaBoyut);
                    c.Item().AlignCenter().Text(baslik).Bold().FontSize(baslikBoyut).FontColor(Colors.Red.Medium);
                });

                // Metnin sayfa ortasında kalması için logo genişliğinde denge alanı
                row.ConstantItem(logoGenislik);
            });
            col.Item().PaddingVertical(kompakt ? 3 : 6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
        });
    }

    private static void KalemTablosu(IContainer container, IEnumerable<SatinalmaTalepKalemi> kalemler)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(24);
                c.RelativeColumn(2);
                c.ConstantColumn(60);
                c.ConstantColumn(50);
                c.RelativeColumn();
            });
            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("No");
                h.Cell().Element(HucreBaslik).Text("Malzeme");
                h.Cell().Element(HucreBaslik).Text("Miktar");
                h.Cell().Element(HucreBaslik).Text("Birim");
                h.Cell().Element(HucreBaslik).Text("Açıklama");
            });
            foreach (var k in kalemler.OrderBy(x => x.SiraNo))
            {
                table.Cell().Element(HucreVeri).Text(k.SiraNo.ToString());
                table.Cell().Element(HucreVeri).Text(k.Malzeme);
                table.Cell().Element(HucreVeri).AlignRight().Text(k.Miktar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).Text(k.Birim);
                table.Cell().Element(HucreVeri).Text(k.Aciklama);
            }
        });
    }

    private static void TedarikciTeklifTablosu(IContainer container, IEnumerable<SatinalmaTalepKalemi> kalemler)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(24);
                c.RelativeColumn(2);
                c.ConstantColumn(52);
                c.ConstantColumn(44);
                c.ConstantColumn(72);
                c.ConstantColumn(64);
                c.ConstantColumn(72);
            });
            table.Header(h =>
            {
                h.Cell().Element(HucreBaslik).Text("No");
                h.Cell().Element(HucreBaslik).Text("Malzeme");
                h.Cell().Element(HucreBaslik).AlignCenter().Text("Miktar");
                h.Cell().Element(HucreBaslik).AlignCenter().Text("Birim");
                h.Cell().Element(HucreBaslik).AlignCenter().Text("Birim Fiyatı");
                h.Cell().Element(HucreBaslik).AlignCenter().Text("Marka");
                h.Cell().Element(HucreBaslik).AlignCenter().Text("Toplam");
            });
            foreach (var k in kalemler.OrderBy(x => x.SiraNo))
            {
                table.Cell().Element(HucreVeri).Text(k.SiraNo.ToString());
                table.Cell().Element(HucreVeri).Text(k.Malzeme);
                table.Cell().Element(HucreVeri).AlignRight().Text(k.Miktar.ToString("N2", Tr));
                table.Cell().Element(HucreVeri).AlignCenter().Text(k.Birim);
                table.Cell().Element(HucreVeri).MinHeight(22).Text("");
                table.Cell().Element(HucreVeri).MinHeight(22).Text("");
                table.Cell().Element(HucreVeri).MinHeight(22).Text("");
            }
        });
    }

    private static void TeklifIstemeSartnameleriBolumu(IContainer container, string metin)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text("Teklif İsteme Şartnameleri").SemiBold().FontSize(11);
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8)
                .Text(string.IsNullOrWhiteSpace(metin) ? " " : metin);
        });
    }

    private static bool KarsilastirmaMarkaSutunuGoster(IEnumerable<SatinalmaTeklif> teklifler)
    {
        foreach (var teklif in teklifler)
        {
            if (!string.IsNullOrWhiteSpace(teklif.Marka))
                return true;
            foreach (var f in teklif.Fiyatlar ?? [])
            {
                if (!string.IsNullOrWhiteSpace(f.Marka))
                    return true;
            }
        }
        return false;
    }

    private static int TeklifGrupSutunSayisi(bool markaGoster) => markaGoster ? 3 : 2;

    private static void KarsilastirmaTeklifKolonlariTanimla(TableColumnsDefinitionDescriptor columns,
        List<SatinalmaTeklif> teklifler, bool markaGoster)
    {
        foreach (var _ in teklifler)
        {
            columns.RelativeColumn(markaGoster ? 1.2f : 1.5f);
            if (markaGoster)
                columns.RelativeColumn(0.9f);
            columns.RelativeColumn(markaGoster ? 1.1f : 1.4f);
        }
    }

    private static void KarsilastirmaTabloBasliklariEkle(TableDescriptor table, List<SatinalmaTeklif> teklifler,
        SatinalmaTeklif? onerilenTeklif, bool kompakt, bool markaGoster)
    {
        var grupSutun = TeklifGrupSutunSayisi(markaGoster);
        table.Cell().RowSpan(2).Element(c => HucreBaslik(c, false, kompakt)).AlignCenter().Text("No");
        table.Cell().RowSpan(2).Element(c => HucreBaslik(c, false, kompakt)).Text("Malzeme");
        table.Cell().RowSpan(2).Element(c => HucreBaslik(c, false, kompakt)).AlignCenter().Text("Miktar");
        table.Cell().RowSpan(2).Element(c => HucreBaslik(c, false, kompakt)).AlignCenter().Text("Birim");

        foreach (var teklif in teklifler)
        {
            var oneri = onerilenTeklif != null && teklif.Id == onerilenTeklif.Id;
            table.Cell().ColumnSpan((uint)grupSutun).Element(c => HucreBaslik(c, oneri, kompakt))
                .AlignCenter().Text(teklif.FirmaAdi);
        }

        foreach (var teklif in teklifler)
        {
            var oneri = onerilenTeklif != null && teklif.Id == onerilenTeklif.Id;
            table.Cell().Element(c => HucreBaslik(c, oneri, kompakt)).AlignCenter().Text("Birim Fiyatı");
            if (markaGoster)
                table.Cell().Element(c => HucreBaslik(c, oneri, kompakt)).AlignCenter().Text("Marka");
            table.Cell().Element(c => HucreBaslik(c, oneri, kompakt)).AlignCenter().Text("Toplam KDV Hariç");
        }
    }

    private static void KarsilastirmaToplamSatirleriEkle(TableDescriptor table, List<SatinalmaTeklif> teklifler,
        SatinalmaTeklif? onerilenTeklif, bool kompakt = false, bool markaGoster = true)
    {
        var grupSutun = TeklifGrupSutunSayisi(markaGoster);

        table.Cell().ColumnSpan(4).Element(c => HucreBaslik(c, false, kompakt)).Text("ARA TOPLAM (KDV Hariç)");
        foreach (var teklif in teklifler)
        {
            var onerilen = onerilenTeklif != null && teklif.Id == onerilenTeklif.Id;
            table.Cell().ColumnSpan((uint)grupSutun).Element(c => HucreVeri(c, onerilen, kompakt))
                .AlignCenter().Text(TlGosterim(teklif.AraToplam));
        }
    }

    private static void KarsilastirmaFirmaOnayTablosuEkle(IContainer container, List<SatinalmaTeklif> teklifler,
        SatinalmaTeklif? onerilenTeklif, bool kompakt)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(kompakt ? 22 : 28);
                c.RelativeColumn(1.8f);
                c.RelativeColumn(0.75f);
                c.RelativeColumn(0.85f);
                c.RelativeColumn(0.95f);
                c.RelativeColumn(0.8f);
                c.RelativeColumn(1f);
            });
            table.Header(h =>
            {
                h.Cell().Element(c => HucreBaslik(c, false, kompakt)).AlignCenter().Text("Seç");
                h.Cell().Element(c => HucreBaslik(c, false, kompakt)).Text("Firma");
                h.Cell().Element(c => HucreBaslik(c, false, kompakt)).Text("Vade");
                h.Cell().Element(c => HucreBaslik(c, false, kompakt)).Text("Teslim");
                h.Cell().Element(c => HucreBaslik(c, false, kompakt)).AlignRight().Text("KDV Hariç");
                h.Cell().Element(c => HucreBaslik(c, false, kompakt)).AlignRight().Text("KDV");
                h.Cell().Element(c => HucreBaslik(c, false, kompakt)).AlignRight().Text("KDV Dahil");
            });
            foreach (var teklif in teklifler)
            {
                var onerilen = onerilenTeklif != null && teklif.Id == onerilenTeklif.Id;
                table.Cell().Element(c => HucreVeri(c, onerilen, kompakt)).AlignCenter().Text("☐");
                table.Cell().Element(c => HucreVeri(c, onerilen, kompakt)).Text(teklif.FirmaAdi);
                table.Cell().Element(c => HucreVeri(c, onerilen, kompakt))
                    .Text(teklif.VadeGunu > 0 ? $"{teklif.VadeGunu} gün" : "—");
                table.Cell().Element(c => HucreVeri(c, onerilen, kompakt))
                    .Text(string.IsNullOrWhiteSpace(teklif.TeslimSuresi) ? "—" : teklif.TeslimSuresi);
                table.Cell().Element(c => HucreVeri(c, onerilen, kompakt)).AlignRight()
                    .Text(TlGosterim(teklif.AraToplam));
                table.Cell().Element(c => HucreVeri(c, onerilen, kompakt)).AlignRight()
                    .Text(TlGosterim(teklif.KdvTutari));
                table.Cell().Element(c => HucreVeri(c, onerilen, kompakt)).AlignRight()
                    .Text(TlGosterim(teklif.GenelToplam));
            }
        });
    }

    private static void ImzaAlanlariOlustur(IContainer container, SatinalmaAyarlar ayarlar)
    {
        ayarlar.SefImzalari ??= [];
        ayarlar.YonetimImzalari ??= [];
        var sefler = ayarlar.SefImzalari.Where(i => i is not null && i.Aktif).ToList();
        var yonetim = ayarlar.YonetimImzalari.Where(i => i is not null && i.Aktif).ToList();

        if (sefler.Count == 0 && yonetim.Count == 0)
            return;

        container.Column(col =>
        {
            col.Spacing(10);

            if (sefler.Count > 0)
                col.Item().Element(c => ImzaSatirOlustur(c, sefler));

            if (yonetim.Count > 0)
            {
                col.Item().PaddingTop(36).Row(row =>
                {
                    row.RelativeItem();
                    row.AutoItem().MinWidth(130).MaxWidth(200)
                        .Element(c => ImzaSatirOlustur(c, yonetim));
                    row.RelativeItem();
                });
            }
        });
    }

    private static void ImzaSatirOlustur(IContainer container, List<ImzaAyari> imzalar)
    {
        container.Row(row =>
        {
            foreach (var imza in imzalar)
                row.RelativeItem().PaddingHorizontal(6).Element(c => ImzaHucreOlustur(c, imza));
        });
    }

    private static bool YonetimOnayKaydiVar(SatinalmaTalep talep) =>
        talep.HerhangiKalemOnayli
        || talep.YonetimOnayKilitli
        || talep.OnaylananTeklifId != null
        || !string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd)
        || !string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi);

    private static void YonetimOnayBilgiKutusuOlustur(IContainer container, SatinalmaTalep talep, bool kompakt = false)
    {
        var onaylayanAd = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd) ? "—" : talep.YonetimOnaylayanAd;
        var onaylayanEposta = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanEposta) ? "—" : talep.YonetimOnaylayanEposta;
        var onayTarihi = string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi)
            ? DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            : talep.YonetimOnayTarihi;

        container.Column(onay =>
        {
            onay.Item().Text("YÖNETİM ONAY BİLGİSİ").SemiBold().FontSize(kompakt ? 8.5f : 9);
            onay.Item().PaddingTop(4).Text(text =>
            {
                text.Span("Onaylayan: ").SemiBold();
                text.Span(onaylayanAd);
                text.Span("   E-posta: ").SemiBold();
                text.Span(onaylayanEposta);
                text.Span("   Onay Tarihi: ").SemiBold();
                text.Span(onayTarihi);
            });
        });
    }

    private static void YonetimImzaAlaniOlustur(IContainer container, SatinalmaAyarlar? ayarlar, bool kompakt = false, SatinalmaTalep? talep = null)
    {
        var onayAd = talep?.YonetimOnaylayanAd?.Trim();
        var onayTarih = string.IsNullOrWhiteSpace(talep?.YonetimOnayTarihi)
            ? null
            : talep!.YonetimOnayTarihi.Trim();
        if (ayarlar is not null)
            ayarlar.YonetimImzalari ??= [];
        var yonetim = ayarlar?.YonetimImzalari.Where(i => i is not null && i.Aktif).ToList() ?? [];

        if (yonetim.Count == 0)
        {
            container.Column(col =>
            {
                col.Item().Element(c => ImzaHucreOlustur(
                    c,
                    new ImzaAyari { Aktif = true, Unvan = "Yönetim" },
                    kompakt,
                    onayAd,
                    onayTarih));
            });
            return;
        }

        container.Column(col =>
        {
            foreach (var imza in yonetim)
                col.Item().Element(c => ImzaHucreOlustur(c, imza, kompakt, onayAd, onayTarih));
        });
    }

    private static void ImzaHucreOlustur(
        IContainer container,
        ImzaAyari? imza,
        bool kompakt = false,
        string? onayAd = null,
        string? onayTarih = null)
    {
        if (imza is null || !imza.Aktif)
            return;

        var unvan = string.IsNullOrWhiteSpace(imza.Unvan) ? "Yönetim" : imza.Unvan;
        var gosterilecekAd = !string.IsNullOrWhiteSpace(onayAd) ? onayAd : imza.AdSoyad;
        var gosterilecekTarih = !string.IsNullOrWhiteSpace(onayTarih)
            ? onayTarih
            : "....../....../202.....";

        var unvanBoyut = kompakt ? 7.5f : 9f;
        var adBoyut = kompakt ? 7.5f : 9f;
        var tarihBoyut = kompakt ? 7f : 8f;

        container.Column(col =>
        {
            col.Spacing(kompakt ? 2 : 4);

            col.Item().AlignCenter().Text(unvan).Italic().FontSize(unvanBoyut);

            col.Item().PaddingHorizontal(4).LineHorizontal(0.5f).LineColor(Colors.Black);

            if (!string.IsNullOrWhiteSpace(gosterilecekAd))
            {
                col.Item().AlignCenter().Text(gosterilecekAd).SemiBold().FontSize(adBoyut);
            }
            else
            {
                col.Item().Height(kompakt ? 10 : 14);
            }

            col.Item().AlignCenter().PaddingTop(2)
                .Text(gosterilecekTarih).FontSize(tarihBoyut).FontColor(Colors.Grey.Darken2);
        });
    }

    private static IContainer HucreBaslik(IContainer c) =>
        HucreBaslik(c, false, false);

    private static IContainer HucreVeri(IContainer c) =>
        HucreVeri(c, false, false);

    private static IContainer HucreBaslik(IContainer c, bool vurgula, bool kompakt = false) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(vurgula ? Colors.Blue.Lighten4 : Colors.Grey.Lighten4)
            .Padding(kompakt ? 2 : 4);

    private static IContainer HucreVeri(IContainer c, bool vurgula, bool kompakt = false) =>
        c.Border(1).BorderColor(Colors.Grey.Lighten2)
            .Background(vurgula ? Colors.Green.Lighten4 : Colors.White)
            .Padding(kompakt ? 2 : 4);

    private static void SartnamelerleBirlestir(string kaynakPdf, string hedefPdf, IEnumerable<SartnameDosyasi> sartnameler)
    {
        using var cikti = new PdfDocument();
        using (var siparis = PdfReader.Open(kaynakPdf, PdfDocumentOpenMode.Import))
        {
            for (var i = 0; i < siparis.PageCount; i++)
                cikti.AddPage(siparis.Pages[i]);
        }

        foreach (var sartname in sartnameler)
        {
            if (!File.Exists(sartname.DosyaYolu))
                continue;

            try
            {
                using var ek = PdfReader.Open(sartname.DosyaYolu, PdfDocumentOpenMode.Import);
                for (var i = 0; i < ek.PageCount; i++)
                    cikti.AddPage(ek.Pages[i]);
            }
            catch
            {
                // geçersiz PDF atla
            }
        }

        cikti.Save(hedefPdf);
    }

    public static void OnaylananMalzemelerYazdir(IEnumerable<OnaylananMalzemeSatiri> satirlar, SatinalmaAyarlar ayarlar)
    {
        try
        {
            ayarlar = SatinalmaDepo.AyarlariHazirla(ayarlar);
            var liste = satirlar?.ToList() ?? [];
        if (liste.Count == 0)
        {
            System.Windows.MessageBox.Show("Yazdırılacak onaylı malzeme bulunamadı.", UygulamaBilgisi.Ad);
            return;
        }

        var ad = "OnaylananMalzemeler.pdf";
        PdfOnizle(ad, "Onaylanan Malzemeler", dosya =>
        {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                page.Header().Element(c => BaslikOlustur(c, ayarlar, "ONAYLANAN MALZEMELER"));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(70);
                        c.ConstantColumn(70);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.2f);
                        c.ConstantColumn(55);
                        c.ConstantColumn(45);
                        c.ConstantColumn(65);
                        c.ConstantColumn(70);
                        c.RelativeColumn(1);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(HucreBaslik).Text("Talep No");
                        h.Cell().Element(HucreBaslik).Text("Sipariş");
                        h.Cell().Element(HucreBaslik).Text("Malzeme");
                        h.Cell().Element(HucreBaslik).Text("Firma");
                        h.Cell().Element(HucreBaslik).Text("Miktar");
                        h.Cell().Element(HucreBaslik).Text("Birim");
                        h.Cell().Element(HucreBaslik).Text("Birim Fiyat");
                        h.Cell().Element(HucreBaslik).Text("Toplam");
                        h.Cell().Element(HucreBaslik).Text("Durum");
                    });

                    foreach (var s in liste)
                    {
                        table.Cell().Element(HucreVeri).Text(s.TalepNo);
                        table.Cell().Element(HucreVeri).Text(s.SiparisNo);
                        table.Cell().Element(HucreVeri).Text(s.Malzeme);
                        table.Cell().Element(HucreVeri).Text(s.Firma);
                        table.Cell().Element(HucreVeri).AlignRight().Text(s.Miktar.ToString("N2", Tr));
                        table.Cell().Element(HucreVeri).Text(s.Birim);
                        table.Cell().Element(HucreVeri).AlignRight().Text(s.BirimFiyati.ToString("N2", Tr));
                        table.Cell().Element(HucreVeri).AlignRight().Text(s.ToplamTutar.ToString("N2", Tr));
                        table.Cell().Element(HucreVeri).Text(s.Durum);
                    }
                });
            });
        }).GeneratePdf(dosya);
        });

        }
        catch (Exception ex)
        {
            PdfHataKaydet(ex, "OnaylananMalzemelerPdf");
        }
    }
}
