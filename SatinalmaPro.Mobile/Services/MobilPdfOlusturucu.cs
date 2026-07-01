using System.Globalization;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Services;

/// <summary>
/// Android uyumlu PDF — PdfSharpCore + ozel FontResolver.
/// </summary>
public static class MobilPdfOlusturucu
{
    private const string FontAilesi = "OpenSans";
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static XFont? _fontNormal;
    private static XFont? _fontBold;
    private static XFont? _fontSmall;
    private static readonly object _fontKilidi = new();

    public static byte[] TalepPdf(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        return PdfOlustur("TALEP FORMU", talep.TalepNo, (gfx, y, w) =>
        {
            y = Metin(gfx, $"Durum: {talep.GorunenDurum}  |  Talep eden: {talep.TalepEden}", 40, y, FontNormal());
            y = Metin(gfx, $"Tarih: {talep.Tarih}  |  Tur: {TalepTurleri.GorunenAd(talep.TalepTuru)}", 40, y, FontNormal());
            if (!string.IsNullOrWhiteSpace(talep.TalepAciklamasi))
                y = Metin(gfx, talep.TalepAciklamasi, 40, y + 4, FontNormal());
            if (!string.IsNullOrWhiteSpace(talep.RedGerekcesi))
                y = Metin(gfx, $"Red gerekcesi: {talep.RedGerekcesi}", 40, y + 4, FontNormal());

            y += 12;
            var kolonlar = new[] { ("#", 28.0), ("Malzeme", w - 220), ("Miktar", 70.0), ("Birim", 60.0) };
            y = TabloBaslik(gfx, kolonlar, 40, y);
            foreach (var k in talep.Kalemler.OrderBy(x => x.SiraNo))
            {
                y = TabloSatir(gfx, kolonlar, 40, y,
                [
                    k.SiraNo.ToString(),
                    k.Malzeme,
                    k.Miktar.ToString("N2", Tr),
                    k.Birim
                ]);
            }

            return y;
        });
    }

    public static byte[] TeklifKarsilastirmaPdf(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        if (talep.Teklifler.Count == 0)
            throw new InvalidOperationException("Karsilastirilacak teklif bulunamadi.");

        var teklifler = talep.Teklifler.OrderBy(t => t.FirmaAdi).ToList();
        TeklifVerisiniHazirla(talep, teklifler);
        var onerilen = talep.OnerilenTeklif();

        return PdfOlustur(
            "TEKLIF KARSILASTIRMASI",
            talep.TalepNo,
            (gfx, y, w) =>
            {
                y = Metin(gfx, $"{talep.TalepEden}  |  {talep.Tarih}  |  {talep.GorunenDurum}", 40, y, FontNormal());
                if (onerilen is not null)
                    y = Metin(gfx, talep.SatinalmaOnerisiElleSecildi
                        ? $"Satinalma onerisi (elle): {onerilen.FirmaAdi} — {onerilen.GenelToplam:N2} TL (KDV dahil)"
                        : $"Satinalma onerisi (en uygun fiyat): {onerilen.FirmaAdi} — {onerilen.GenelToplam:N2} TL (KDV dahil)",
                        40, y + 4, FontBold());

                y += 12;
                var firmaGen = Math.Max(80, (w - 180) / teklifler.Count);
                var kolonlar = new List<(string, double)> { ("Malzeme", 120), ("Miktar", 60) };
                kolonlar.AddRange(teklifler.Select(t => (KisaMetin(t.FirmaAdi, 18), firmaGen)));

                y = TabloBaslik(gfx, kolonlar, 40, y);
                foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
                {
                    var hucreler = new List<string>
                    {
                        kalem.Malzeme,
                        $"{kalem.Miktar:N2} {kalem.Birim}"
                    };
                    hucreler.AddRange(teklifler.Select(teklif =>
                    {
                        var f = teklif.Fiyatlar.FirstOrDefault(x => x.KalemId == kalem.Id);
                        return f is null
                            ? "-"
                            : $"{f.BirimFiyat:N2} {f.ParaBirimi} / {f.TlBirimFiyat(teklif.UsdKuru, teklif.EurKuru):N2} TL";
                    }));
                    y = TabloSatir(gfx, kolonlar, 40, y, hucreler);
                }

                var toplamlar = new List<string> { "TOPLAM", "" };
                toplamlar.AddRange(teklifler.Select(t => $"{t.GenelToplam:N2} TL"));
                y = TabloSatir(gfx, kolonlar, 40, y, toplamlar, kalin: true);
                return y;
            },
            yatay: teklifler.Count > 2);
    }

    public static byte[] YonetimOnayBelgesiPdf(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        if (!talep.YonetimOnayKilitli && !talep.HerhangiKalemOnayli)
            throw new InvalidOperationException("Henüz yönetim onayı verilmemiş.");

        var onaylayanAd = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd) ? "—" : talep.YonetimOnaylayanAd;
        var onaylayanEposta = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanEposta) ? "—" : talep.YonetimOnaylayanEposta;
        var onayTarihi = string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi)
            ? DateTime.Now.ToString("dd.MM.yyyy HH:mm", Tr)
            : talep.YonetimOnayTarihi;

        var onayliTeklifler = talep.Teklifler
            .Where(t => talep.Kalemler.Any(k => k.OnaylananTeklifId == t.Id) || t.Onaylandi)
            .ToList();

        return PdfOlustur(
            "YONETIM ONAY BELGESI",
            talep.TalepNo,
            (gfx, y, w) =>
            {
                y = Metin(gfx, $"Onaylayan: {onaylayanAd}", 40, y, FontBold());
                y = Metin(gfx, $"E-posta: {onaylayanEposta}", 40, y, FontNormal());
                y = Metin(gfx, $"Onay tarihi: {onayTarihi}", 40, y, FontNormal());
                y = Metin(gfx, $"Talep eden: {talep.TalepEden}  |  Sipariş no: {(string.IsNullOrWhiteSpace(talep.SiparisNo) ? "—" : talep.SiparisNo)}", 40, y + 4, FontNormal());

                if (onayliTeklifler.Count > 0)
                {
                    y = Metin(gfx, "Onaylanan firma(lar): " + string.Join(" · ", onayliTeklifler.Select(t => t.FirmaAdi)), 40, y + 8, FontBold());
                }

                y += 12;
                var kolonlar = new[] { ("Malzeme", w - 220), ("Miktar", 70.0), ("Firma", 90.0), ("Birim Fiyat", 70.0), ("Toplam", 70.0) };
                y = TabloBaslik(gfx, kolonlar, 40, y);

                foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
                {
                    var teklif = kalem.OnaylananTeklifId is { } tid
                        ? talep.Teklifler.FirstOrDefault(t => t.Id == tid)
                        : talep.OnaylananTeklif;
                    teklif?.FiyatlariHesapla(talep.Kalemler);
                    var fiyat = teklif?.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                    var birim = fiyat is null ? "—" : $"{fiyat.TlBirimFiyat(teklif.UsdKuru, teklif.EurKuru):N2} ₺";
                    var toplam = fiyat is null ? "—" : $"{fiyat.ToplamTutar:N2} ₺";
                    y = TabloSatir(gfx, kolonlar, 40, y,
                    [
                        kalem.Malzeme,
                        $"{kalem.Miktar:N2} {kalem.Birim}",
                        teklif?.FirmaAdi ?? "—",
                        birim,
                        toplam
                    ]);
                }

                y += 24;
                y = Metin(gfx, "Yönetim onayı / imza", 40, y, FontBold());
                gfx.DrawLine(XPens.Gray, 40, y + 28, 280, y + 28);
                Metin(gfx, onaylayanAd, 40, y + 32, FontNormal());
                return y;
            },
            yatay: false);
    }

    public static byte[] OnaylananMalzemePdf(OnaylananMalzemeSatiri satir) =>
        PdfOlustur("SIPARIS OZETI", satir.TalepNo, (gfx, y, w) =>
        {
            y = EtiketDeger(gfx, "Malzeme", satir.Malzeme, 40, y);
            y = EtiketDeger(gfx, "Firma", satir.Firma, 40, y);
            y = EtiketDeger(gfx, "Marka", satir.Marka, 40, y);
            y = EtiketDeger(gfx, "Siparis No", satir.SiparisNo, 40, y);
            y = EtiketDeger(gfx, "Vade", $"{satir.VadeGunu} gun", 40, y);
            y = EtiketDeger(gfx, "Miktar", $"{satir.SiparisMiktari:N2} {satir.Birim}", 40, y);
            y = EtiketDeger(gfx, "Kabul", $"{satir.KabulEdilenMiktar:N2} {satir.Birim}", 40, y);
            y = EtiketDeger(gfx, "Birim fiyat", $"{satir.BirimFiyati:N2} TL", 40, y);
            y = EtiketDeger(gfx, "Toplam", $"{satir.ToplamTutar:N2} TL", 40, y);
            return y;
        });

    private static byte[] PdfOlustur(string baslik, string altBaslik, Func<XGraphics, double, double, double> ciz, bool yatay = false)
    {
        try
        {
            MobilPdfFontResolver.Baslat();
            FontlariHazirla();

            var doc = new PdfDocument();
            doc.Info.Title = "Satinalma Pro";
            doc.Info.Creator = "Satinalma Pro Mobile";

            var page = doc.AddPage();
            if (yatay)
            {
                page.Width = XUnit.FromMillimeter(297);
                page.Height = XUnit.FromMillimeter(210);
            }

            var gfx = XGraphics.FromPdfPage(page);
            var w = page.Width.Point;
            var y = 40.0;

            y = Metin(gfx, "Satinalma Pro", 40, y, FontBold(14));
            y = Metin(gfx, baslik, 40, y + 2, FontBold(12));
            y = Metin(gfx, altBaslik, 40, y + 2, FontNormal(10));
            gfx.DrawLine(XPens.LightGray, 40, y + 4, w - 40, y + 4);
            y += 16;

            y = ciz(gfx, y, w - 80);

            var alt = $"Olusturma: {DateTime.Now.ToString("dd.MM.yyyy HH:mm", Tr)}";
            gfx.DrawString(alt, FontSmall(), XBrushes.Gray, new XRect(40, page.Height.Point - 30, w - 80, 20), XStringFormats.TopLeft);

            using var ms = new MemoryStream();
            doc.Save(ms, false);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"PDF olusturulamadi: {ex.Message}", ex);
        }
    }

    private static double EtiketDeger(XGraphics gfx, string etiket, string deger, double x, double y)
    {
        gfx.DrawString(etiket, FontBold(), XBrushes.Black, x, y);
        Metin(gfx, deger, x + 100, y, FontNormal());
        return y + 16;
    }

    private static double TabloBaslik(XGraphics gfx, IReadOnlyList<(string Metin, double Genislik)> kolonlar, double x, double y)
    {
        var cx = x;
        foreach (var (metin, gen) in kolonlar)
        {
            gfx.DrawRectangle(XPens.Gray, XBrushes.LightGray, cx, y, gen, 18);
            gfx.DrawString(metin, FontBold(8), XBrushes.Black, new XRect(cx + 2, y + 2, gen - 4, 14), XStringFormats.TopLeft);
            cx += gen;
        }

        return y + 18;
    }

    private static double TabloSatir(XGraphics gfx, IReadOnlyList<(string Metin, double Genislik)> kolonlar, double x, double y, IReadOnlyList<string> hucreler, bool kalin = false)
    {
        var font = kalin ? FontBold(8) : FontSmall();
        var satirYuk = 16.0;
        var cx = x;
        for (var i = 0; i < kolonlar.Count; i++)
        {
            var gen = kolonlar[i].Genislik;
            var metin = i < hucreler.Count ? hucreler[i] : "";
            gfx.DrawRectangle(XPens.LightGray, cx, y, gen, satirYuk);
            gfx.DrawString(KisaMetin(metin, 40), font, XBrushes.Black, new XRect(cx + 2, y + 2, gen - 4, satirYuk - 2), XStringFormats.TopLeft);
            cx += gen;
        }

        return y + satirYuk;
    }

    private static double Metin(XGraphics gfx, string metin, double x, double y, XFont font)
    {
        gfx.DrawString(metin ?? "", font, XBrushes.Black, new XRect(x, y, 500, 40), XStringFormats.TopLeft);
        return y + font.Height + 2;
    }

    private static string KisaMetin(string? metin, int max) =>
        string.IsNullOrWhiteSpace(metin) ? "-" : metin.Length <= max ? metin : metin[..(max - 1)] + "…";

    private static void TeklifVerisiniHazirla(SatinalmaTalep talep, IReadOnlyList<SatinalmaTeklif> teklifler)
    {
        foreach (var teklif in teklifler)
        {
            teklif.Fiyatlar ??= [];
            if (teklif.Fiyatlar.Count == 0 && talep.Kalemler.Count > 0)
            {
                foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
                {
                    teklif.Fiyatlar.Add(new SatinalmaTeklifFiyati
                    {
                        KalemId = kalem.Id,
                        KdvOrani = teklif.KdvOrani > 0 ? teklif.KdvOrani : 20
                    });
                }
            }

            teklif.FiyatlariHesapla(talep.Kalemler);
        }
    }

    private static void FontlariHazirla()
    {
        lock (_fontKilidi)
        {
            if (_fontNormal is not null)
                return;

            _fontNormal = new XFont(FontAilesi, 9, XFontStyle.Regular);
            _fontBold = new XFont(FontAilesi, 9, XFontStyle.Bold);
            _fontSmall = new XFont(FontAilesi, 8, XFontStyle.Regular);
        }
    }

    private static XFont FontNormal(double size = 9)
    {
        FontlariHazirla();
        return size == 9 ? _fontNormal! : new XFont(FontAilesi, size, XFontStyle.Regular);
    }

    private static XFont FontBold(double size = 9)
    {
        FontlariHazirla();
        return size == 9 ? _fontBold! : new XFont(FontAilesi, size, XFontStyle.Bold);
    }

    private static XFont FontSmall() => _fontSmall ?? FontNormal(8);
}
