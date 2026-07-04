using System.Globalization;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Theme;

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
}

public static class AnaSayfaVeriServisi
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public static AnaSayfaVeri Yukle()
    {
        var kaynak = MasaustuDashboardBaglanti.VeriKaynagiOlustur();
        var sorgu = new MasaustuDashboardSorgu();
        sorgu.TalepleriGuncelle(kaynak.Talepler);

        var alimlar = ModulVeriDeposu.AlinanMalzemeler;
        var buAy = DateTime.Now.Month;
        var buYil = DateTime.Now.Year;
        var buAyAlimlar = alimlar.Where(a => TarihAy(a.Tarih) == buAy && TarihYil(a.Tarih) == buYil).ToList();
        var oncekiAyAlimlar = alimlar.Where(a =>
        {
            var ay = TarihAy(a.Tarih);
            var yil = TarihYil(a.Tarih);
            var onceki = DateTime.Now.AddMonths(-1);
            return ay == onceki.Month && yil == onceki.Year;
        }).ToList();

        var toplamHarcama = buAyAlimlar.Sum(a => (double)a.ToplamTutar);
        var oncekiHarcama = oncekiAyAlimlar.Sum(a => (double)a.ToplamTutar);
        var onayBekleyen = sorgu.OnayBekleyenTalepler().Count();
        var kritikStok = kaynak.Stok.Count(s => s.DurumMetin is "Kritik" or "Tükendi");

        return new AnaSayfaVeri
        {
            Istatistikler =
            [
                new AnaSayfaIstatistik
                {
                    Baslik = "Toplam Alımlar",
                    Deger = buAyAlimlar.Count.ToString("N0", Tr),
                    AltMetin = "Bu ay",
                    TrendMetin = TrendYuzde(buAyAlimlar.Count, oncekiAyAlimlar.Count),
                    TrendPozitif = buAyAlimlar.Count >= oncekiAyAlimlar.Count,
                    Icon = DashboardIconKind.Package,
                    IconRenkHex = AppTheme.PrimaryHex
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Toplam Harcama",
                    Deger = toplamHarcama.ToString("C0", Tr),
                    AltMetin = "Bu ay",
                    TrendMetin = TrendYuzde(toplamHarcama, oncekiHarcama),
                    TrendPozitif = toplamHarcama >= oncekiHarcama,
                    Icon = DashboardIconKind.Wallet,
                    IconRenkHex = AppTheme.SuccessHex
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Onay Bekleyen",
                    Deger = onayBekleyen.ToString("N0", Tr),
                    AltMetin = "Aktif talep",
                    TrendMetin = onayBekleyen > 0 ? "▲ dikkat" : "▲ 0%",
                    TrendPozitif = onayBekleyen == 0,
                    Icon = DashboardIconKind.ClipboardList,
                    IconRenkHex = AppTheme.PurpleHex
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Kritik Stok",
                    Deger = kritikStok.ToString("N0", Tr),
                    AltMetin = "Kritik / tükenen",
                    TrendMetin = kritikStok > 0 ? "▲ uyarı" : "▲ 0%",
                    TrendPozitif = kritikStok == 0,
                    Icon = DashboardIconKind.AlertTriangle,
                    IconRenkHex = AppTheme.WarningHex
                }
            ],
            SonIslemler = SonIslemleriOlustur(kaynak, sorgu),
            StokUyarilari = StokUyarilariniOlustur(kaynak)
        };
    }

    private static List<AnaSayfaIslem> SonIslemleriOlustur(DashboardVeriKaynagi kaynak, MasaustuDashboardSorgu sorgu)
    {
        var liste = new List<AnaSayfaIslem>();

        foreach (var b in BildirimDeposu.Bildirimler.OrderByDescending(x => x.GuncellemeUtc).Take(4))
        {
            var tarih = BildirimTarihi(b);
            liste.Add(new AnaSayfaIslem
            {
                Baslik = string.IsNullOrWhiteSpace(b.Baslik) ? b.Mesaj : b.Baslik,
                Zaman = ZamanMetni(tarih),
                Durum = b.Okundu ? "Okundu" : "Yeni",
                DurumRenkHex = b.Okundu ? AppTheme.SecondaryTextHex : AppTheme.PrimaryHex,
                Icon = DashboardIconKind.Bell
            });
        }

        foreach (var t in kaynak.Talepler.OrderByDescending(t => t.Tarih).Take(6 - liste.Count))
        {
            var durum = t.GorunenDurum;
            liste.Add(new AnaSayfaIslem
            {
                Baslik = $"{t.TalepNo} nolu satınalma",
                Zaman = t.Tarih,
                Durum = durum,
                DurumRenkHex = DurumRenk(durum),
                Icon = DashboardIconKind.ShoppingCart
            });
        }

        return liste.Take(6).ToList();
    }

    private static List<AnaSayfaStokUyari> StokUyarilariniOlustur(DashboardVeriKaynagi kaynak) =>
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

    private static string DurumRenk(string durum) => durum switch
    {
        var d when d.Contains("Onay", StringComparison.OrdinalIgnoreCase) => AppTheme.SuccessHex,
        var d when d.Contains("Red", StringComparison.OrdinalIgnoreCase) => AppTheme.DangerHex,
        var d when d.Contains("Bekle", StringComparison.OrdinalIgnoreCase) => AppTheme.WarningHex,
        _ => AppTheme.PrimaryHex
    };

    private static string TrendYuzde(double guncel, double onceki)
    {
        if (onceki <= 0)
            return guncel > 0 ? "▲ 100%" : "▲ 0%";

        var fark = (guncel - onceki) / onceki * 100;
        var isaret = fark >= 0 ? "▲" : "▼";
        return $"{isaret} {Math.Abs(fark):0.#}%";
    }

    private static int TarihAy(string tarih)
    {
        if (DateTime.TryParse(tarih, Tr, DateTimeStyles.None, out var dt))
            return dt.Month;
        return 0;
    }

    private static int TarihYil(string tarih)
    {
        if (DateTime.TryParse(tarih, Tr, DateTimeStyles.None, out var dt))
            return dt.Year;
        return 0;
    }

    private static DateTime BildirimTarihi(BildirimKaydi b)
    {
        if (b.GuncellemeUtc > 0)
            return DateTimeOffset.FromUnixTimeMilliseconds(b.GuncellemeUtc).LocalDateTime;

        if (DateTime.TryParse(b.OlusturmaTarihi, Tr, DateTimeStyles.None, out var dt))
            return dt;

        return DateTime.Now;
    }

    private static string ZamanMetni(DateTime tarih)
    {
        var fark = DateTime.Now - tarih;
        if (fark.TotalMinutes < 1) return "Az önce";
        if (fark.TotalMinutes < 60) return $"{(int)fark.TotalMinutes} dakika önce";
        if (fark.TotalHours < 24) return $"{(int)fark.TotalHours} saat önce";
        if (fark.TotalDays < 7) return $"{(int)fark.TotalDays} gün önce";
        return tarih.ToString("dd.MM.yyyy", Tr);
    }
}
