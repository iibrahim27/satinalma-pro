using System.Globalization;
using SatinalmaPro.Models;
using SatinalmaPro.Theme;
using SharedKaynak = SatinalmaPro.Shared.Services.DashboardVeriKaynagi;

namespace SatinalmaPro.Services;

public sealed class AnaSayfaIstatistik
{
    public required string Baslik { get; init; }
    public required string Deger { get; init; }
    public required string AltMetin { get; init; }
    public string TrendMetin { get; init; } = "";
    public bool TrendPozitif { get; init; } = true;
    public required DashboardIconKind Icon { get; init; }
    public required string IconRenkHex { get; init; }
    public IReadOnlyList<double> Sparkline { get; init; } = [];
}

public sealed class AnaSayfaAylikNokta
{
    public required string Etiket { get; init; }
    public required double Deger { get; init; }
}

public sealed class AnaSayfaDagilim
{
    public required string Etiket { get; init; }
    public required double Yuzde { get; init; }
    public required string RenkHex { get; init; }
}

public sealed class AnaSayfaAcikKayit
{
    public required string No { get; init; }
    public required string Tarih { get; init; }
    public required string Cari { get; init; }
    public required string Vade { get; init; }
    public required string Tutar { get; init; }
    public required string Kalan { get; init; }
    public required string Durum { get; init; }
    public required string DurumRenkHex { get; init; }
}

public sealed class AnaSayfaHatirlatma
{
    public required string Metin { get; init; }
    public required string RenkHex { get; init; }
}

public sealed class AnaSayfaFinansOzet
{
    public required string Gelir { get; init; }
    public required string Gider { get; init; }
    public required string Kar { get; init; }
    public required double KarMarjiYuzde { get; init; }
}

public sealed class AnaSayfaTopUrun
{
    public required string Ad { get; init; }
    public required string Tutar { get; init; }
}

public sealed class AnaSayfaIslem
{
    public required string Baslik { get; init; }
    public required string Zaman { get; init; }
    public required string Durum { get; init; }
    public required string DurumRenkHex { get; init; }
    public required DashboardIconKind Icon { get; init; }
}

public sealed class AnaSayfaStokUyari
{
    public required string Malzeme { get; init; }
    public required string MevcutMetin { get; init; }
    public required string Durum { get; init; }
    public required string DurumRenkHex { get; init; }
}

public sealed class AnaSayfaVeri
{
    public required IReadOnlyList<AnaSayfaIstatistik> Istatistikler { get; init; }
    public required IReadOnlyList<AnaSayfaIslem> SonIslemler { get; init; }
    public required IReadOnlyList<AnaSayfaStokUyari> StokUyarilari { get; init; }
    public required IReadOnlyList<AnaSayfaAylikNokta> AylikHarcama { get; init; }
    public required IReadOnlyList<AnaSayfaDagilim> HarcamaDagilimi { get; init; }
    public required IReadOnlyList<AnaSayfaAcikKayit> AcikKayitlar { get; init; }
    public required IReadOnlyList<AnaSayfaHatirlatma> Hatirlatmalar { get; init; }
    public required AnaSayfaFinansOzet FinansOzet { get; init; }
    public required IReadOnlyList<AnaSayfaTopUrun> TopUrunler { get; init; }
}

/// <summary>
/// Anasayfa özeti — yalnızca Alınan Malzemeler, Stok, Agrega, Çimento, Akaryakıt.
/// </summary>
public static class AnaSayfaVeriServisi
{
    private static readonly CultureInfo Tr = new("tr-TR");

    private static readonly string[] DashboardModulleri =
    [
        "Alınan Malzemeler",
        "Stok Yönetimi",
        "Agrega",
        "Çimento",
        "Akaryakıt Takip"
    ];

    public static IReadOnlyList<string> DashboardModulBasliklari => DashboardModulleri;

    public static AnaSayfaVeri Yukle()
    {
        var rol = KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol);
        // Tüm roller aynı malzeme/takip panosunu görür; depo vurgusu stok KPI'larında.
        return YukleMalzemePanosu(depoOdakli: rol is KullaniciRolleri.Depo or KullaniciRolleri.Atolye);
    }

    private static AnaSayfaVeri YukleMalzemePanosu(bool depoOdakli)
    {
        var kaynak = MasaustuDashboardBaglanti.VeriKaynagiOlustur();
        var buAy = DateTime.Now.Month;
        var buYil = DateTime.Now.Year;
        var onceki = DateTime.Now.AddMonths(-1);

        var alimlar = ModulVeriDeposu.AlinanMalzemeler.ToList();
        var agrega = ModulVeriDeposu.Agrega.ToList();
        var cimento = ModulVeriDeposu.Cimento.ToList();
        var akaryakit = ModulVeriDeposu.Akaryakit.ToList();

        var buAyAlim = AyFiltre(alimlar, a => a.Tarih, buAy, buYil);
        var oncekiAlim = AyFiltre(alimlar, a => a.Tarih, onceki.Month, onceki.Year);
        var buAyAgrega = AyFiltre(agrega, a => a.Tarih, buAy, buYil);
        var oncekiAgrega = AyFiltre(agrega, a => a.Tarih, onceki.Month, onceki.Year);
        var buAyCimento = AyFiltre(cimento, a => a.Tarih, buAy, buYil);
        var oncekiCimento = AyFiltre(cimento, a => a.Tarih, onceki.Month, onceki.Year);
        var buAyYakit = AyFiltre(akaryakit, a => a.Tarih, buAy, buYil);
        var oncekiYakit = AyFiltre(akaryakit, a => a.Tarih, onceki.Month, onceki.Year);

        var alimTutar = buAyAlim.Sum(a => (double)a.ToplamTutar);
        var oncekiAlimTutar = oncekiAlim.Sum(a => (double)a.ToplamTutar);
        var agregaTutar = buAyAgrega.Sum(a => (double)a.ToplamTutar);
        var oncekiAgregaTutar = oncekiAgrega.Sum(a => (double)a.ToplamTutar);
        var cimentoTutar = buAyCimento.Sum(a => (double)a.ToplamTutar);
        var oncekiCimentoTutar = oncekiCimento.Sum(a => (double)a.ToplamTutar);

        var yakitAlinanLt = buAyYakit.Where(a => a.AlinanKayit).Sum(a => (double)a.Miktar);
        var yakitDagitilanLt = buAyYakit.Where(a => !a.AlinanKayit).Sum(a => (double)a.Miktar);
        var yakitAlinanTutar = buAyYakit.Where(a => a.AlinanKayit).Sum(a => (double)a.ToplamTutar);
        var oncekiYakitAlinanLt = oncekiYakit.Where(a => a.AlinanKayit).Sum(a => (double)a.Miktar);

        var kritikStok = kaynak.Stok.Count(s => s.DurumMetin is "Kritik" or "Tükendi");
        var stokDegeri = kaynak.Stok.Sum(s => (double)s.ToplamDeger);
        var buAyHareket = kaynak.StokHareketleri.Count(h =>
            TarihAy(h.Tarih) == buAy && TarihYil(h.Tarih) == buYil);

        var faturaBekleyen =
            buAyAlim.Count(a => string.IsNullOrWhiteSpace(a.FaturaNo))
            + buAyAgrega.Count(a => !a.FaturasiKesildi)
            + buAyCimento.Count(a => !a.FaturasiKesildi);

        var modulHarcama = alimTutar + agregaTutar + cimentoTutar + yakitAlinanTutar;
        var aylikSeri = AylikModulHarcamaSerisi(alimlar, agrega, cimento, akaryakit);

        return new AnaSayfaVeri
        {
            Istatistikler = depoOdakli
                ? DepoIstatistikleri(kaynak, kritikStok, stokDegeri, buAyHareket, yakitDagitilanLt, alimTutar, oncekiAlimTutar)
                :
                [
                    new AnaSayfaIstatistik
                    {
                        Baslik = "Alınan Malzeme",
                        Deger = alimTutar.ToString("C0", Tr),
                        AltMetin = $"{buAyAlim.Count:N0} kayıt · bu ay",
                        TrendMetin = TrendYuzde(alimTutar, oncekiAlimTutar),
                        TrendPozitif = alimTutar >= oncekiAlimTutar,
                        Icon = DashboardIconKind.Package,
                        IconRenkHex = AppTheme.PrimaryHex,
                        Sparkline = aylikSeri.Select(x => x.Deger).ToList()
                    },
                    new AnaSayfaIstatistik
                    {
                        Baslik = "Stok Değeri",
                        Deger = stokDegeri.ToString("C0", Tr),
                        AltMetin = kritikStok > 0 ? $"{kritikStok} kritik kalem" : "kritik yok",
                        TrendMetin = kritikStok > 0 ? "▼ uyarı" : "▲ OK",
                        TrendPozitif = kritikStok == 0,
                        Icon = DashboardIconKind.Warehouse,
                        IconRenkHex = "#8B5CF6",
                        Sparkline = MiniSeri(stokDegeri)
                    },
                    new AnaSayfaIstatistik
                    {
                        Baslik = "Agrega",
                        Deger = agregaTutar.ToString("C0", Tr),
                        AltMetin = $"{buAyAgrega.Sum(a => (double)a.Miktar):N0} {BirimOzeti(buAyAgrega.Select(a => a.Birim))} · bu ay",
                        TrendMetin = TrendYuzde(agregaTutar, oncekiAgregaTutar),
                        TrendPozitif = agregaTutar >= oncekiAgregaTutar,
                        Icon = DashboardIconKind.ClipboardList,
                        IconRenkHex = "#2F9E44",
                        Sparkline = MiniSeri(agregaTutar)
                    },
                    new AnaSayfaIstatistik
                    {
                        Baslik = "Çimento",
                        Deger = cimentoTutar.ToString("C0", Tr),
                        AltMetin = $"{buAyCimento.Sum(a => (double)a.Miktar):N0} {BirimOzeti(buAyCimento.Select(a => a.Birim))} · bu ay",
                        TrendMetin = TrendYuzde(cimentoTutar, oncekiCimentoTutar),
                        TrendPozitif = cimentoTutar >= oncekiCimentoTutar,
                        Icon = DashboardIconKind.Package,
                        IconRenkHex = "#64748B",
                        Sparkline = MiniSeri(cimentoTutar)
                    },
                    new AnaSayfaIstatistik
                    {
                        Baslik = "Akaryakıt",
                        Deger = $"{yakitAlinanLt:N0} Lt",
                        AltMetin = $"dağıtılan {yakitDagitilanLt:N0} Lt · bu ay",
                        TrendMetin = TrendYuzde(yakitAlinanLt, oncekiYakitAlinanLt),
                        TrendPozitif = yakitAlinanLt >= oncekiYakitAlinanLt,
                        Icon = DashboardIconKind.Wallet,
                        IconRenkHex = "#F08C00",
                        Sparkline = MiniSeri(yakitAlinanLt)
                    }
                ],
            SonIslemler = ModulSonIslemleri(alimlar, agrega, cimento, akaryakit, kaynak),
            StokUyarilari = StokUyarilariniOlustur(kaynak),
            AylikHarcama = aylikSeri,
            HarcamaDagilimi = ModulDagilimi(alimTutar, agregaTutar, cimentoTutar, yakitAlinanTutar),
            AcikKayitlar = [],
            Hatirlatmalar = HatirlatmalariOlustur(kritikStok, faturaBekleyen, yakitAlinanLt - yakitDagitilanLt, buAyHareket),
            FinansOzet = new AnaSayfaFinansOzet
            {
                Gelir = stokDegeri.ToString("C0", Tr),
                Gider = modulHarcama.ToString("C0", Tr),
                Kar = (stokDegeri - modulHarcama).ToString("C0", Tr),
                KarMarjiYuzde = modulHarcama <= 0 ? 0 : Math.Round((stokDegeri - modulHarcama) / Math.Max(modulHarcama, 1) * 100, 1)
            },
            TopUrunler = TopKalemleriOlustur(buAyAlim, buAyAgrega, buAyCimento)
        };
    }

    private static List<AnaSayfaIstatistik> DepoIstatistikleri(
        SharedKaynak kaynak, int kritikStok, double stokDegeri, int buAyHareket,
        double yakitDagitilanLt, double alimTutar, double oncekiAlimTutar)
    {
        var tukenen = kaynak.Stok.Count(s => s.DurumMetin == "Tükendi");
        return
        [
            new AnaSayfaIstatistik
            {
                Baslik = "Stok Kalemi",
                Deger = kaynak.Stok.Count.ToString("N0", Tr),
                AltMetin = stokDegeri.ToString("C0", Tr),
                Icon = DashboardIconKind.Warehouse,
                IconRenkHex = AppTheme.PrimaryHex,
                Sparkline = MiniSeri(kaynak.Stok.Count)
            },
            new AnaSayfaIstatistik
            {
                Baslik = "Kritik / Tükenen",
                Deger = kritikStok.ToString("N0", Tr),
                AltMetin = tukenen > 0 ? $"{tukenen} tükenen" : "minimum altı",
                TrendMetin = kritikStok > 0 ? "▼ uyarı" : "▲ OK",
                TrendPozitif = kritikStok == 0,
                Icon = DashboardIconKind.AlertTriangle,
                IconRenkHex = "#E67E22",
                Sparkline = MiniSeri(kritikStok)
            },
            new AnaSayfaIstatistik
            {
                Baslik = "Bu Ay Hareket",
                Deger = buAyHareket.ToString("N0", Tr),
                AltMetin = "giriş / çıkış / sayım",
                Icon = DashboardIconKind.ShoppingCart,
                IconRenkHex = "#2980B9",
                Sparkline = MiniSeri(buAyHareket)
            },
            new AnaSayfaIstatistik
            {
                Baslik = "Alınan Malzeme",
                Deger = alimTutar.ToString("C0", Tr),
                AltMetin = "bu ay alım",
                TrendMetin = TrendYuzde(alimTutar, oncekiAlimTutar),
                TrendPozitif = alimTutar >= oncekiAlimTutar,
                Icon = DashboardIconKind.Package,
                IconRenkHex = "#0D7377",
                Sparkline = MiniSeri(alimTutar)
            },
            new AnaSayfaIstatistik
            {
                Baslik = "Yakıt Dağıtım",
                Deger = $"{yakitDagitilanLt:N0} Lt",
                AltMetin = "bu ay dağıtılan",
                Icon = DashboardIconKind.Wallet,
                IconRenkHex = "#F08C00",
                Sparkline = MiniSeri(yakitDagitilanLt)
            }
        ];
    }

    private static List<T> AyFiltre<T>(IEnumerable<T> kaynak, Func<T, string> tarihSec, int ay, int yil) =>
        kaynak.Where(x => TarihAy(tarihSec(x)) == ay && TarihYil(tarihSec(x)) == yil).ToList();

    private static string BirimOzeti(IEnumerable<string> birimler)
    {
        var b = birimler.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        return string.IsNullOrWhiteSpace(b) ? "adet" : b.Trim();
    }

    private static List<AnaSayfaAylikNokta> AylikModulHarcamaSerisi(
        List<AlinanMalzemeKaydi> alimlar,
        List<AgregaKaydi> agrega,
        List<CimentoKaydi> cimento,
        List<AkaryakitKaydi> akaryakit)
    {
        var liste = new List<AnaSayfaAylikNokta>();
        for (var i = 8; i >= 0; i--)
        {
            var hedef = DateTime.Now.AddMonths(-i);
            var tutar =
                alimlar.Where(a => TarihAy(a.Tarih) == hedef.Month && TarihYil(a.Tarih) == hedef.Year)
                    .Sum(a => (double)a.ToplamTutar)
                + agrega.Where(a => TarihAy(a.Tarih) == hedef.Month && TarihYil(a.Tarih) == hedef.Year)
                    .Sum(a => (double)a.ToplamTutar)
                + cimento.Where(a => TarihAy(a.Tarih) == hedef.Month && TarihYil(a.Tarih) == hedef.Year)
                    .Sum(a => (double)a.ToplamTutar)
                + akaryakit.Where(a => a.AlinanKayit && TarihAy(a.Tarih) == hedef.Month && TarihYil(a.Tarih) == hedef.Year)
                    .Sum(a => (double)a.ToplamTutar);
            liste.Add(new AnaSayfaAylikNokta
            {
                Etiket = hedef.ToString("MMM", Tr),
                Deger = tutar
            });
        }
        return liste;
    }

    private static List<AnaSayfaDagilim> ModulDagilimi(
        double alim, double agrega, double cimento, double yakit)
    {
        var kalemler = new (string Etiket, double Tutar, string Renk)[]
        {
            ("Alınan", alim, AppTheme.PrimaryHex),
            ("Agrega", agrega, "#2F9E44"),
            ("Çimento", cimento, "#64748B"),
            ("Akaryakıt", yakit, "#F08C00")
        };

        var toplam = kalemler.Sum(k => k.Tutar);
        if (toplam <= 0)
            return
            [
                new() { Etiket = "Alınan", Yuzde = 40, RenkHex = AppTheme.PrimaryHex },
                new() { Etiket = "Agrega", Yuzde = 25, RenkHex = "#2F9E44" },
                new() { Etiket = "Çimento", Yuzde = 20, RenkHex = "#64748B" },
                new() { Etiket = "Akaryakıt", Yuzde = 15, RenkHex = "#F08C00" }
            ];

        return kalemler
            .Where(k => k.Tutar > 0)
            .Select(k => new AnaSayfaDagilim
            {
                Etiket = k.Etiket,
                Yuzde = Math.Round(k.Tutar / toplam * 100, 1),
                RenkHex = k.Renk
            })
            .ToList();
    }

    private static List<AnaSayfaIslem> ModulSonIslemleri(
        List<AlinanMalzemeKaydi> alimlar,
        List<AgregaKaydi> agrega,
        List<CimentoKaydi> cimento,
        List<AkaryakitKaydi> akaryakit,
        SharedKaynak kaynak)
    {
        var liste = new List<(DateTime Dt, AnaSayfaIslem Islem)>();

        foreach (var a in alimlar)
        {
            if (!TryParseTarih(a.Tarih, out var dt)) continue;
            liste.Add((dt, new AnaSayfaIslem
            {
                Baslik = $"Malzeme: {Kisalt(a.MalzemeHizmet, 36)}",
                Zaman = a.Tarih,
                Durum = a.FaturaDurumuMetin,
                DurumRenkHex = string.IsNullOrWhiteSpace(a.FaturaNo) ? AppTheme.WarningHex : AppTheme.SuccessHex,
                Icon = DashboardIconKind.Package
            }));
        }

        foreach (var a in agrega)
        {
            if (!TryParseTarih(a.Tarih, out var dt)) continue;
            liste.Add((dt, new AnaSayfaIslem
            {
                Baslik = $"Agrega: {Kisalt(BosIse(a.AgregaTuru, a.AgregaCinsi), 36)}",
                Zaman = a.Tarih,
                Durum = a.FaturaDurumuMetin,
                DurumRenkHex = a.FaturasiKesildi ? AppTheme.SuccessHex : AppTheme.WarningHex,
                Icon = DashboardIconKind.ClipboardList
            }));
        }

        foreach (var a in cimento)
        {
            if (!TryParseTarih(a.Tarih, out var dt)) continue;
            liste.Add((dt, new AnaSayfaIslem
            {
                Baslik = $"Çimento: {Kisalt(BosIse(a.CimentoSinifi, a.CimentoCinsi), 36)}",
                Zaman = a.Tarih,
                Durum = a.FaturaDurumuMetin,
                DurumRenkHex = a.FaturasiKesildi ? AppTheme.SuccessHex : AppTheme.WarningHex,
                Icon = DashboardIconKind.Package
            }));
        }

        foreach (var a in akaryakit)
        {
            if (!TryParseTarih(a.Tarih, out var dt)) continue;
            var etiket = a.AlinanKayit
                ? $"Yakıt alım: {a.Miktar:N0} Lt"
                : $"Yakıt dağıtım: {Kisalt(BosIse(a.PlakaVeyaKod, a.AracMakineAdi), 24)}";
            liste.Add((dt, new AnaSayfaIslem
            {
                Baslik = etiket,
                Zaman = a.Tarih,
                Durum = a.AlinanKayit ? "Alınan" : "Dağıtılan",
                DurumRenkHex = a.AlinanKayit ? AppTheme.SuccessHex : AppTheme.WarningHex,
                Icon = DashboardIconKind.Wallet
            }));
        }

        foreach (var h in kaynak.StokHareketleri)
        {
            if (!TryParseTarih(h.Tarih, out var dt)) continue;
            liste.Add((dt, new AnaSayfaIslem
            {
                Baslik = $"{h.HareketTipi}: {Kisalt(h.MalzemeAdi, 28)}",
                Zaman = h.Tarih,
                Durum = string.IsNullOrWhiteSpace(h.BelgeNo) ? h.HareketTipi : h.BelgeNo,
                DurumRenkHex = h.HareketTipi.Contains("Çıkış", StringComparison.OrdinalIgnoreCase)
                    ? AppTheme.WarningHex
                    : AppTheme.SuccessHex,
                Icon = DashboardIconKind.Warehouse
            }));
        }

        return liste
            .OrderByDescending(x => x.Dt)
            .Select(x => x.Islem)
            .Take(8)
            .ToList();
    }

    private static List<AnaSayfaHatirlatma> HatirlatmalariOlustur(
        int kritikStok, int faturaBekleyen, double yakitNetLt, int buAyHareket)
    {
        var liste = new List<AnaSayfaHatirlatma>();
        if (kritikStok > 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = $"{kritikStok} kritik / tükenen stok kalemi", RenkHex = AppTheme.DangerHex });
        if (faturaBekleyen > 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = $"{faturaBekleyen} kayıtta fatura bekleniyor", RenkHex = AppTheme.WarningHex });
        if (yakitNetLt < 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = $"Akaryakıt dağıtımı alımı aştı ({Math.Abs(yakitNetLt):N0} Lt)", RenkHex = "#F08C00" });
        if (buAyHareket == 0 && kritikStok == 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = "Bu ay henüz stok hareketi yok", RenkHex = AppTheme.PrimaryHex });
        if (liste.Count == 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = "Malzeme ve takip modüllerinde kritik uyarı yok", RenkHex = AppTheme.SuccessHex });
        return liste;
    }

    private static List<AnaSayfaTopUrun> TopKalemleriOlustur(
        List<AlinanMalzemeKaydi> alim,
        List<AgregaKaydi> agrega,
        List<CimentoKaydi> cimento)
    {
        var birlesik = alim
            .Select(a => new { Ad = a.MalzemeHizmet, Tutar = (double)a.ToplamTutar })
            .Concat(agrega.Select(a => new { Ad = $"Agrega · {BosIse(a.AgregaTuru, a.AgregaCinsi)}", Tutar = (double)a.ToplamTutar }))
            .Concat(cimento.Select(a => new { Ad = $"Çimento · {BosIse(a.CimentoSinifi, a.CimentoCinsi)}", Tutar = (double)a.ToplamTutar }))
            .Where(x => !string.IsNullOrWhiteSpace(x.Ad) && x.Tutar > 0)
            .GroupBy(x => x.Ad)
            .Select(g => new { Ad = g.Key, Tutar = g.Sum(x => x.Tutar) })
            .OrderByDescending(x => x.Tutar)
            .Take(5)
            .Select(x => new AnaSayfaTopUrun { Ad = Kisalt(x.Ad, 40), Tutar = x.Tutar.ToString("C0", Tr) })
            .ToList();

        return birlesik;
    }

    private static List<AnaSayfaStokUyari> StokUyarilariniOlustur(SharedKaynak kaynak) =>
        kaynak.Stok
            .Where(s => s.DurumMetin != "Normal")
            .OrderBy(s => s.DurumMetin == "Tükendi" ? 0 : 1)
            .ThenBy(s => s.MalzemeAdi)
            .Take(6)
            .Select(s => new AnaSayfaStokUyari
            {
                Malzeme = s.MalzemeAdi,
                MevcutMetin = $"Mevcut: {s.MevcutMiktar:N0} {s.Birim}",
                Durum = s.DurumMetin == "Tükendi" ? "Kritik" : "Düşük",
                DurumRenkHex = s.DurumMetin == "Tükendi" ? AppTheme.DangerHex : AppTheme.WarningHex
            })
            .ToList();

    private static List<double> MiniSeri(double son) =>
        [son * 0.7, son * 0.75, son * 0.8, son * 0.85, son * 0.9, son * 0.95, son];

    private static string TrendYuzde(double guncel, double onceki)
    {
        if (onceki <= 0)
            return guncel > 0 ? "▲ 100%" : "▲ 0%";
        var fark = (guncel - onceki) / onceki * 100;
        var isaret = fark >= 0 ? "▲" : "▼";
        return $"{isaret} {Math.Abs(fark):0.#}%";
    }

    private static int TarihAy(string tarih) =>
        TryParseTarih(tarih, out var dt) ? dt.Month : 0;

    private static int TarihYil(string tarih) =>
        TryParseTarih(tarih, out var dt) ? dt.Year : 0;

    private static bool TryParseTarih(string tarih, out DateTime dt) =>
        DateTime.TryParse(tarih, Tr, DateTimeStyles.None, out dt);

    private static string Kisalt(string? metin, int max)
    {
        var t = (metin ?? "").Trim();
        if (t.Length <= max) return string.IsNullOrEmpty(t) ? "—" : t;
        return t[..(max - 1)] + "…";
    }

    private static string BosIse(string? deger, string? alternatif) =>
        string.IsNullOrWhiteSpace(deger) ? (alternatif ?? "") : deger;
}
