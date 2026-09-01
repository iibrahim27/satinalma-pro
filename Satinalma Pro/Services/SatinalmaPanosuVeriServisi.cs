using System.Globalization;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SatinalmaPro.Views.Modules.Satinalma.Part1;
using TalepProRuntime = SatinalmaPro.Shared.Helpers.TalepProRuntime;
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

public sealed class PanosuAylikHarcama
{
    public required string Etiket { get; init; }
    public decimal Harcama { get; init; }
    public int TalepSayisi { get; init; }
}

public sealed class PanosuDurumDilimi
{
    public required string Etiket { get; init; }
    public int Adet { get; init; }
    public double Yuzde { get; init; }
    public required string RenkHex { get; init; }
}

public sealed class PanosuKategoriHarcama
{
    public required string Etiket { get; init; }
    public decimal Tutar { get; init; }
    public double Yuzde { get; init; }
}

public sealed class PanosuKritikSatir
{
    public Guid Id { get; init; }
    public string TalepNo { get; init; } = "";
    public string Malzeme { get; init; } = "";
    public string Santiye { get; init; } = "";
    public string Oncelik { get; init; } = "";
    public string Bekleme { get; init; } = "";
    public Brush OncelikArkaPlan { get; init; } = Brushes.Gainsboro;
    public Brush OncelikYazi { get; init; } = Brushes.Black;
    public Brush BeklemeYazi { get; init; } = Brushes.Black;
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

        foreach (var talep in GorunenTalepler())
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
            Adim("Onaylandı", "\uE73E", onaylandi, SatinalmaPart1Menusu.OnaySonrasiRoute(null), "#16A34A"),
            .. TalepProRuntime.Aktif
                ? Array.Empty<SatinalmaWorkflowAdim>()
                :
                [
                    Adim("Sipariş Verildi", "\uE7BF", siparisVerildi, SatinalmaPart1Menusu.SatinalmaSiparis, "#2563EB"),
                    Adim("Mal Kabul", "\uE8D1", malKabul, SatinalmaPart1Menusu.SatinalmaSiparis, "#0D9488"),
                    Adim("Tamamlandı", "\uE930", tamamlandi, SatinalmaPart1Menusu.SatinalmaMalKabul, "#64748B")
                ]
        ];
    }

    public static IReadOnlyList<SatinalmaPanosuOzetKpi> OzetKpi()
    {
        var t = GorunenTalepler();
        var siparis = t.Count(x => x.Durum == SharedTalepDurumlari.SiparisOlusturuldu);
        var teklifSurecinde = t.Count(x => x.Durum is SharedTalepDurumlari.TeklifGirisi
            or SharedTalepDurumlari.Karsilastirma
            or SharedTalepDurumlari.YonetimOnayinda);
        var malKabul = t.Count(MalKabulAsamasi);
        var bekleyenOnay = t.Count(x =>
            x.Durum is SharedTalepDurumlari.YonetimOnayinda
                or SharedTalepDurumlari.ImzaSurecinde
                or SharedTalepDurumlari.Hazirlaniyor);

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

    /// <summary>Referans panosu — 5 üst KPI (gerçek veri).</summary>
    public static (string OnayBekleyen, string TeklifSurecinde, string SipariseDonusen, string BuAyHarcama, string Geciken)
        DashboardUstKpi()
    {
        var t = GorunenTalepler();
        var onay = t.Count(x =>
            x.Durum is SharedTalepDurumlari.YonetimOnayinda
                or SharedTalepDurumlari.ImzaSurecinde
                or SharedTalepDurumlari.Hazirlaniyor);
        var teklif = t.Count(x =>
            x.Durum is SharedTalepDurumlari.TeklifGirisi
                or SharedTalepDurumlari.Karsilastirma);
        var siparis = t.Count(x =>
            x.Durum == SharedTalepDurumlari.SiparisOlusturuldu
            || SatinalmaPart1Filtreleri.SatinalmaMalKabulEdilmis(x));
        var ay = DateTime.Today;
        var harcama = t.Where(x => AyEslesir(x.Tarih, ay) || AyEslesirUtc(x.GuncellemeUtc, ay))
            .Where(x => x.Durum is SharedTalepDurumlari.Onaylandi
                    or SharedTalepDurumlari.SiparisOlusturuldu
                || SatinalmaPart1Filtreleri.SatinalmaMalKabulEdilmis(x))
            .Sum(TalepTutari);
        // Sonlanma tarihi yok: 3+ gündür bekleyen acil veya onay/teklif aşamasındaki kayıtlar
        var geciken = t.Count(GeciktiMi);

        return (
            onay.ToString("N0", Tr),
            teklif.ToString("N0", Tr),
            siparis.ToString("N0", Tr),
            harcama <= 0 ? "₺0" : harcama.ToString("C0", Tr),
            geciken.ToString("N0", Tr));
    }

    public static IReadOnlyList<PanosuAylikHarcama> AylikHarcamaSerisi()
    {
        var liste = new List<PanosuAylikHarcama>();
        var simdi = DateTime.Now;
        for (var i = 5; i >= 0; i--)
        {
            var ay = simdi.AddMonths(-i);
            var ayTalepler = GorunenTalepler()
                .Where(t => AyEslesir(t.Tarih, ay) || AyEslesirUtc(t.GuncellemeUtc, ay))
                .ToList();
            liste.Add(new PanosuAylikHarcama
            {
                Etiket = ay.ToString("MMM", Tr),
                TalepSayisi = ayTalepler.Count,
                Harcama = ayTalepler.Sum(TalepTutari)
            });
        }

        return liste;
    }

    public static IReadOnlyList<PanosuDurumDilimi> TalepDurumDagilimi()
    {
        var t = GorunenTalepler();
        var gruplar = new (string Etiket, string Renk, Func<SatinalmaTalep, bool> Filtre)[]
        {
            ("Bekliyor", "#F59E0B", BekleyenTalep),
            ("Teklif", "#7C3AED", x => x.Durum is SharedTalepDurumlari.TeklifGirisi or SharedTalepDurumlari.Karsilastirma),
            ("Onaylandı", "#16A34A", x => x.Durum == SharedTalepDurumlari.Onaylandi),
            ("Sipariş", "#2563EB", x => x.Durum == SharedTalepDurumlari.SiparisOlusturuldu || Tamamlandi(x))
        };

        var sayilar = gruplar.Select(g => (g.Etiket, g.Renk, Adet: t.Count(g.Filtre))).ToList();
        var toplam = sayilar.Sum(x => x.Adet);
        if (toplam <= 0) toplam = 1;

        return sayilar.Select(x => new PanosuDurumDilimi
        {
            Etiket = x.Etiket,
            Adet = x.Adet,
            Yuzde = Math.Round(x.Adet * 100d / toplam, 0),
            RenkHex = x.Renk
        }).ToList();
    }

    public static IReadOnlyList<PanosuKritikSatir> KritikBekleyenTalepler(int adet = 5)
    {
        return GorunenTalepler()
            .Where(t =>
                BekleyenTalep(t)
                || t.Durum is SharedTalepDurumlari.TeklifGirisi or SharedTalepDurumlari.Karsilastirma
                || string.Equals(t.TalepTuru, TalepTurleri.Acil, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => string.Equals(t.TalepTuru, TalepTurleri.Acil, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(BeklemeGun)
            .Take(adet)
            .Select(t =>
            {
                var gun = BeklemeGun(t);
                var (onBg, onFg) = OncelikBadge(t.TalepTuru);
                var beklemeFg = gun >= 4
                    ? RenkFircasi("#DC2626")
                    : gun >= 2 ? RenkFircasi("#EA580C") : RenkFircasi("#64748B");
                var ilk = t.Kalemler?.FirstOrDefault();
                return new PanosuKritikSatir
                {
                    Id = t.Id,
                    TalepNo = t.TalepNo,
                    Malzeme = ilk?.Malzeme ?? "—",
                    Santiye = SantiyeMetni(t),
                    Oncelik = TalepTurleri.GorunenAd(t.TalepTuru),
                    Bekleme = $"{gun} gün",
                    OncelikArkaPlan = onBg,
                    OncelikYazi = onFg,
                    BeklemeYazi = beklemeFg
                };
            })
            .ToList();
    }

    public static IReadOnlyList<PanosuKategoriHarcama> KategoriHarcamaDagilimi(int adet = 4)
    {
        var kalemler = GorunenTalepler()
            .SelectMany(t =>
            {
                var tutar = TalepTutari(t);
                var kals = (t.Kalemler ?? []).Where(k => !string.IsNullOrWhiteSpace(k.Malzeme)).ToList();
                if (kals.Count == 0)
                    return new[] { (Kat: "Genel", Tutar: tutar) };
                var pay = tutar / kals.Count;
                return kals.Select(k => (Kat: KategoriBul(k.Malzeme), Tutar: pay));
            })
            .GroupBy(x => x.Kat, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Etiket = g.Key, Tutar = g.Sum(x => x.Tutar) })
            .OrderByDescending(x => x.Tutar)
            .Take(adet)
            .ToList();

        var toplam = kalemler.Sum(x => x.Tutar);
        if (toplam <= 0) toplam = 1;

        return kalemler.Select(x => new PanosuKategoriHarcama
        {
            Etiket = x.Etiket,
            Tutar = x.Tutar,
            Yuzde = Math.Round((double)(x.Tutar / toplam) * 100d, 1)
        }).ToList();
    }

    public static IReadOnlyList<SatinalmaPanosuTalepSatir> SonTalepler(int adet = 12) =>
        GorunenTalepler()
            .OrderByDescending(x => TarihYardimcisi.SiralamaDegeri(x.Tarih))
            .Take(adet)
            .Select(SatirOlustur)
            .ToList();

    public static IReadOnlyList<AnaSayfaAylikNokta> AylikSatinalma() =>
        AylikHarcamaSerisi()
            .Select(x => new AnaSayfaAylikNokta { Etiket = x.Etiket, Deger = x.TalepSayisi })
            .ToList();

    public static IReadOnlyList<AnaSayfaDagilim> KategoriDagilimi() =>
        KategoriHarcamaDagilimi(5)
            .Select(x => new AnaSayfaDagilim
            {
                Etiket = x.Etiket,
                Yuzde = x.Yuzde,
                RenkHex = "#07858E"
            })
            .ToList();

    private static IReadOnlyList<SatinalmaTalep> GorunenTalepler()
    {
        // Panodaki görünürlük tüm taleplerdir; düzenleme/silme sahiplik denetimiyle sınırlıdır.
        return SatinalmaDepo.Talepler.ToList();
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
        if (!DateTime.TryParseExact(tarih, "dd.MM.yyyy", Tr, DateTimeStyles.None, out var dt)
            && !DateTime.TryParse(tarih, Tr, DateTimeStyles.None, out dt))
            return false;
        return dt.Year == ay.Year && dt.Month == ay.Month;
    }

    private static bool AyEslesirUtc(long utcMs, DateTime ay)
    {
        if (utcMs <= 0) return false;
        var dt = DateTimeOffset.FromUnixTimeMilliseconds(utcMs).LocalDateTime;
        return dt.Year == ay.Year && dt.Month == ay.Month;
    }

    private static decimal TalepTutari(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        foreach (var teklif in talep.Teklifler)
            teklif.FiyatlariHesapla(talep.Kalemler);
        var oneri = talep.OnerilenTeklif() ?? talep.EnDusukFiyatliTeklif();
        return oneri?.GenelToplam ?? 0m;
    }

    private static int BeklemeGun(SatinalmaTalep t)
    {
        DateTime baslangic;
        if (t.GuncellemeUtc > 0)
            baslangic = DateTimeOffset.FromUnixTimeMilliseconds(t.GuncellemeUtc).LocalDateTime.Date;
        else if (!DateTime.TryParseExact(t.Tarih, "dd.MM.yyyy", Tr, DateTimeStyles.None, out baslangic)
                 && !DateTime.TryParse(t.Tarih, Tr, DateTimeStyles.None, out baslangic))
            return 0;
        return Math.Max(0, (DateTime.Today - baslangic.Date).Days);
    }

    /// <summary>Sonlanma tarihi alanı yok — 3+ gündür bekleyen onay/teklif veya acil kayıtlar.</summary>
    private static bool GeciktiMi(SatinalmaTalep t)
    {
        if (t.Durum is SharedTalepDurumlari.Onaylandi
            or SharedTalepDurumlari.SiparisOlusturuldu
            or SharedTalepDurumlari.Reddedildi)
            return false;

        var gun = BeklemeGun(t);
        var acil = string.Equals(t.TalepTuru, TalepTurleri.Acil, StringComparison.OrdinalIgnoreCase);
        return gun >= 3 || (acil && gun >= 1);
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
