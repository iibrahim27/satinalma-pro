using System.Globalization;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SatinalmaPro.Views.Modules.Satinalma.Part1;
using SharedTalepDurumlari = SatinalmaPro.Shared.Models.SatinalmaTalepDurumlari;

namespace SatinalmaPro.Services;

public sealed class SatinalmaWorkflowAdim
{
    public required string Baslik { get; init; }
    public required string Ikon { get; init; }
    public int Adet { get; init; }
    public string SonHareket { get; init; } = "—";
    public string Route { get; init; } = "";
    public string RenkHex { get; init; } = "#2563EB";
}

public sealed class SatinalmaPanosuTalepSatir
{
    public Guid Id { get; init; }
    public string TalepNo { get; init; } = "";
    public string TalepEden { get; init; } = "";
    public string Santiye { get; init; } = "";
    public string Malzeme { get; init; } = "";
    public string Kategori { get; init; } = "";
    public string Oncelik { get; init; } = "";
    public int TeklifSayisi { get; init; }
    public string Durum { get; init; } = "";
    public string SonIslem { get; init; } = "";
    public Brush DurumArkaPlan { get; init; } = Brushes.Gainsboro;
    public Brush DurumYazi { get; init; } = Brushes.Black;
    public Brush OncelikArkaPlan { get; init; } = Brushes.Gainsboro;
    public Brush OncelikYazi { get; init; } = Brushes.Black;
}

public sealed class SatinalmaPanosuOzetKpi
{
    public required string Baslik { get; init; }
    public required string Deger { get; init; }
    public required string Alt { get; init; }
    public required string RenkHex { get; init; }
    public required string Ikon { get; init; }
}

public static class SatinalmaPanosuVeriServisi
{
    public static IReadOnlyList<SatinalmaWorkflowAdim> WorkflowAdimlari()
    {
        var bekleyen = new List<SatinalmaTalep>();
        var teklifBekleniyor = new List<SatinalmaTalep>();
        var teklifGeldi = new List<SatinalmaTalep>();
        var karsilastiriliyor = new List<SatinalmaTalep>();
        var onaylandi = new List<SatinalmaTalep>();
        var siparisVerildi = new List<SatinalmaTalep>();
        var malKabul = new List<SatinalmaTalep>();
        var tamamlandi = new List<SatinalmaTalep>();

        foreach (var talep in SatinalmaDepo.Talepler)
        {
            if (BekleyenTalep(talep)) bekleyen.Add(talep);
            if (TeklifBekleniyor(talep)) teklifBekleniyor.Add(talep);
            if (TeklifGeldi(talep)) teklifGeldi.Add(talep);
            if (Karsilastiriliyor(talep)) karsilastiriliyor.Add(talep);
            if (Onaylandi(talep)) onaylandi.Add(talep);
            if (SiparisVerildi(talep)) siparisVerildi.Add(talep);
            if (MalKabulAsamasi(talep)) malKabul.Add(talep);
            if (Tamamlandi(talep)) tamamlandi.Add(talep);
        }

        return
        [
            Adim("Bekleyen Talep", "\uE7C3", bekleyen, SatinalmaPart1Menusu.YonetimGelenTalepler, "#2563EB"),
            Adim("Teklif Bekleniyor", "\uE823", teklifBekleniyor, SatinalmaPart1Menusu.SatinalmaTeklifIstenen, "#7C3AED"),
            Adim("Teklif Geldi", "\uE8D1", teklifGeldi, SatinalmaPart1Menusu.SatinalmaTeklifGirilen, "#8B5CF6"),
            Adim("Karşılaştırılıyor", "\uE9D9", karsilastiriliyor, SatinalmaPart1Menusu.SatinalmaKarsilastirma, "#0891B2"),
            Adim("Onaylandı", "\uE73E", onaylandi, SatinalmaPart1Menusu.SatinalmaOnaylanan, "#16A34A"),
            Adim("Sipariş Verildi", "\uE7BF", siparisVerildi, SatinalmaPart1Menusu.SatinalmaSiparis, "#2563EB"),
            Adim("Mal Kabul", "\uE8D1", malKabul, SatinalmaPart1Menusu.SatinalmaSiparis, "#0D9488"),
            Adim("Tamamlandı", "\uE930", tamamlandi, SatinalmaPart1Menusu.SatinalmaMalKabul, "#64748B")
        ];
    }

    public static IReadOnlyList<SatinalmaPanosuOzetKpi> OzetKpi()
    {
        var t = SatinalmaDepo.Talepler;
        var siparis = t.Count(x => x.Durum == SharedTalepDurumlari.SiparisOlusturuldu);
        var teklifSurecinde = t.Count(x => x.Durum is SharedTalepDurumlari.TeklifGirisi or SharedTalepDurumlari.Karsilastirma);
        var malKabul = t.Count(MalKabulAsamasi);
        var bekleyenOnay = t.Count(x => x.Durum == SharedTalepDurumlari.YonetimOnayinda);

        return
        [
            new() { Baslik = "Toplam Talep", Deger = t.Count.ToString("N0", Tr), Alt = "Aktif kayıtlar", RenkHex = "#2563EB", Ikon = "\uE8F1" },
            new() { Baslik = "Toplam Sipariş", Deger = siparis.ToString("N0", Tr), Alt = "Sipariş aşamasında", RenkHex = "#0891B2", Ikon = "\uE7BF" },
            new() { Baslik = "Teklif Sürecinde", Deger = teklifSurecinde.ToString("N0", Tr), Alt = "Giriş veya karşılaştırma", RenkHex = "#8B5CF6", Ikon = "\uE8D1" },
            new() { Baslik = "Mal Kabul", Deger = malKabul.ToString("N0", Tr), Alt = "Teslimat sürecinde", RenkHex = "#16A34A", Ikon = "\uE896" },
            new() { Baslik = "Bekleyen Onay", Deger = bekleyenOnay.ToString("N0", Tr), Alt = "Talep / teklif onayı", RenkHex = "#F59E0B", Ikon = "\uE823" },
            new() { Baslik = "Ort. Onay Süresi", Deger = "2,4 gün", Alt = "Son 30 gün", RenkHex = "#64748B", Ikon = "\uE916" }
        ];
    }

    public static IReadOnlyList<SatinalmaPanosuTalepSatir> SonTalepler(int adet = 12) =>
        SatinalmaDepo.Talepler
            .OrderByDescending(x => TarihYardimcisi.SiralamaDegeri(x.Tarih))
            .Take(adet)
            .Select(SatirOlustur)
            .ToList();

    public static IReadOnlyList<AnaSayfaAylikNokta> AylikSatinalma()
    {
        var liste = new List<AnaSayfaAylikNokta>();
        var simdi = DateTime.Now;
        for (var i = 5; i >= 0; i--)
        {
            var ay = simdi.AddMonths(-i);
            var etiket = ay.ToString("MMM", Tr);
            var adet = SatinalmaDepo.Talepler.Count(t => AyEslesir(t.Tarih, ay));
            liste.Add(new AnaSayfaAylikNokta { Etiket = etiket, Deger = adet });
        }

        return liste;
    }

    public static IReadOnlyList<AnaSayfaDagilim> KategoriDagilimi()
    {
        var gruplar = SatinalmaDepo.Talepler
            .SelectMany(t => t.Kalemler.Select(k => KategoriBul(k.Malzeme)))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToList();

        var renkler = new[] { "#2563EB", "#16A34A", "#F59E0B", "#7C3AED", "#0891B2" };
        var toplam = gruplar.Sum(g => g.Count());
        if (toplam <= 0) toplam = 1;

        return gruplar.Select((g, i) => new AnaSayfaDagilim
        {
            Etiket = g.Key,
            Yuzde = Math.Round((double)g.Count() / toplam * 100d, 1),
            RenkHex = renkler[i % renkler.Length]
        }).ToList();
    }

    private static SatinalmaWorkflowAdim Adim(
        string baslik, string ikon, IReadOnlyList<SatinalmaTalep> kaynak,
        string route, string renk)
    {
        var son = kaynak.Count == 0
            ? null
            : kaynak.MaxBy(t => TarihYardimcisi.SiralamaDegeri(t.Tarih));
        return new SatinalmaWorkflowAdim
        {
            Baslik = baslik,
            Ikon = ikon,
            Adet = kaynak.Count,
            SonHareket = son is null ? "—" : $"{son.TalepNo} · {son.Tarih}",
            Route = route,
            RenkHex = renk
        };
    }

    private static SatinalmaPanosuTalepSatir SatirOlustur(SatinalmaTalep t)
    {
        var (durumBg, durumFg) = DurumBadge(t.Durum);
        var (onBg, onFg) = OncelikBadge(t.TalepTuru);
        var ilkKalem = t.Kalemler.FirstOrDefault();

        return new SatinalmaPanosuTalepSatir
        {
            Id = t.Id,
            TalepNo = t.TalepNo,
            TalepEden = t.TalepEden,
            Santiye = SantiyeMetni(t),
            Malzeme = ilkKalem?.Malzeme ?? "—",
            Kategori = KategoriBul(ilkKalem?.Malzeme ?? ""),
            Oncelik = t.TalepTuru,
            TeklifSayisi = t.Teklifler?.Count ?? 0,
            Durum = UiDurum(t.Durum),
            SonIslem = t.Tarih,
            DurumArkaPlan = durumBg,
            DurumYazi = durumFg,
            OncelikArkaPlan = onBg,
            OncelikYazi = onFg
        };
    }

    private static bool BekleyenTalep(SatinalmaTalep t) =>
        t.Durum is SharedTalepDurumlari.YonetimOnayinda
            or SharedTalepDurumlari.ImzaSurecinde
            or SharedTalepDurumlari.Hazirlaniyor;

    private static bool TeklifBekleniyor(SatinalmaTalep t) =>
        t.Durum == SharedTalepDurumlari.TeklifGirisi && (t.Teklifler?.Count ?? 0) == 0;

    private static bool TeklifGeldi(SatinalmaTalep t) =>
        t.Durum == SharedTalepDurumlari.TeklifGirisi && (t.Teklifler?.Count ?? 0) > 0;

    private static bool Karsilastiriliyor(SatinalmaTalep t) =>
        t.Durum == SharedTalepDurumlari.Karsilastirma;

    private static bool Onaylandi(SatinalmaTalep t) =>
        t.Durum == SharedTalepDurumlari.Onaylandi;

    private static bool SiparisVerildi(SatinalmaTalep t) =>
        t.Durum == SharedTalepDurumlari.SiparisOlusturuldu
        && !t.Kalemler.Any(k => k.KabulEdilenMiktar > 0.0001);

    private static bool MalKabulAsamasi(SatinalmaTalep t) =>
        t.Durum == SharedTalepDurumlari.SiparisOlusturuldu
        && t.Kalemler.Any(k => k.KabulEdilenMiktar > 0.0001)
        && !SatinalmaPart1Filtreleri.MalKabulTamam(t);

    private static bool Tamamlandi(SatinalmaTalep t) =>
        SatinalmaPart1Filtreleri.SatinalmaMalKabulEdilmis(t);

    private static string SantiyeMetni(SatinalmaTalep t) =>
        !string.IsNullOrWhiteSpace(t.SantiyeAdi) ? t.SantiyeAdi : t.TalepEden;

    private static string KategoriBul(string malzeme)
    {
        if (string.IsNullOrWhiteSpace(malzeme)) return "Genel";
        var m = malzeme.ToLowerInvariant();
        if (m.Contains("çimento") || m.Contains("cimento")) return "Çimento";
        if (m.Contains("demir") || m.Contains("nervür")) return "Demir";
        if (m.Contains("agrega") || m.Contains("kum") || m.Contains("mıcır")) return "Agrega";
        if (m.Contains("boya") || m.Contains("astari")) return "Boya";
        return "Malzeme";
    }

    private static bool AyEslesir(string tarih, DateTime ay)
    {
        if (!DateTime.TryParseExact(tarih, "dd.MM.yyyy", Tr, DateTimeStyles.None, out var dt))
            return false;
        return dt.Year == ay.Year && dt.Month == ay.Month;
    }

    private static string UiDurum(string durum) => durum switch
    {
        SharedTalepDurumlari.YonetimOnayinda => "Bekliyor",
        SharedTalepDurumlari.TeklifGirisi => "Teklif Geldi",
        SharedTalepDurumlari.Karsilastirma => "Karşılaştırılıyor",
        SharedTalepDurumlari.Onaylandi => "Onaylandı",
        SharedTalepDurumlari.SiparisOlusturuldu => "Sipariş",
        SharedTalepDurumlari.Reddedildi => "Reddedildi",
        _ => "Bekliyor"
    };

    private static Brush RenkFircasi(string hex) =>
        FircaOnbellegi.Al(hex, (Color)ColorConverter.ConvertFromString(hex)!);

    private static (Brush bg, Brush fg) DurumBadge(string durum) => durum switch
    {
        SharedTalepDurumlari.Reddedildi => (RenkFircasi("#FEE2E2"), RenkFircasi("#DC2626")),
        SharedTalepDurumlari.Onaylandi => (RenkFircasi("#DCFCE7"), RenkFircasi("#16A34A")),
        SharedTalepDurumlari.SiparisOlusturuldu => (RenkFircasi("#DBEAFE"), RenkFircasi("#2563EB")),
        SharedTalepDurumlari.TeklifGirisi or SharedTalepDurumlari.Karsilastirma => (RenkFircasi("#EDE9FE"), RenkFircasi("#7C3AED")),
        _ => (RenkFircasi("#FEF3C7"), RenkFircasi("#D97706"))
    };

    private static (Brush bg, Brush fg) OncelikBadge(string oncelik)
    {
        if (oncelik.Contains("Acil", StringComparison.OrdinalIgnoreCase))
            return (RenkFircasi("#FEE2E2"), RenkFircasi("#DC2626"));
        return (RenkFircasi("#F1F5F9"), RenkFircasi("#64748B"));
    }

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
}
