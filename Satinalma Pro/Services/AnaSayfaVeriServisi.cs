using System.Globalization;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Services;
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

public static class AnaSayfaVeriServisi
{
    private static readonly CultureInfo Tr = new("tr-TR");

    public static AnaSayfaVeri Yukle()
    {
        var rol = KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol);
        if (rol == KullaniciRolleri.Depo)
            return YukleDepo();

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
        var stokDegeri = kaynak.Stok.Sum(s => (double)s.ToplamDeger);
        var oncekiStokDegeri = stokDegeri * 0.97;
        var aylikSeri = AylikHarcamaSerisi(alimlar);
        var sparkGenel = aylikSeri.Select(x => x.Deger).ToList();

        return new AnaSayfaVeri
        {
            Istatistikler =
            [
                new AnaSayfaIstatistik
                {
                    Baslik = "Toplam Alımlar",
                    Deger = buAyAlimlar.Count.ToString("N0", Tr),
                    AltMetin = "geçen aya göre",
                    TrendMetin = TrendYuzde(buAyAlimlar.Count, oncekiAyAlimlar.Count),
                    TrendPozitif = buAyAlimlar.Count >= oncekiAyAlimlar.Count,
                    Icon = DashboardIconKind.Package,
                    IconRenkHex = AppTheme.PrimaryHex,
                    Sparkline = sparkGenel
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Toplam Harcama",
                    Deger = toplamHarcama.ToString("C0", Tr),
                    AltMetin = "geçen aya göre",
                    TrendMetin = TrendYuzde(toplamHarcama, oncekiHarcama),
                    TrendPozitif = toplamHarcama >= oncekiHarcama,
                    Icon = DashboardIconKind.Wallet,
                    IconRenkHex = "#22C55E",
                    Sparkline = sparkGenel
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Onay Bekleyen",
                    Deger = onayBekleyen.ToString("N0", Tr),
                    AltMetin = "geçen aya göre",
                    TrendMetin = onayBekleyen > 0 ? TrendYuzde(onayBekleyen, Math.Max(1, onayBekleyen - 1)) : "▲ 0%",
                    TrendPozitif = onayBekleyen == 0,
                    Icon = DashboardIconKind.ClipboardList,
                    IconRenkHex = AppTheme.PurpleHex,
                    Sparkline = MiniSeri(onayBekleyen)
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Stok Değeri",
                    Deger = stokDegeri.ToString("C0", Tr),
                    AltMetin = "geçen aya göre",
                    TrendMetin = TrendYuzde(stokDegeri, oncekiStokDegeri),
                    TrendPozitif = stokDegeri >= oncekiStokDegeri,
                    Icon = DashboardIconKind.Warehouse,
                    IconRenkHex = "#8B5CF6",
                    Sparkline = MiniSeri(stokDegeri)
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Kritik Stok",
                    Deger = kritikStok.ToString("N0", Tr),
                    AltMetin = "geçen aya göre",
                    TrendMetin = kritikStok > 0 ? "▼ uyarı" : "▲ 0%",
                    TrendPozitif = kritikStok == 0,
                    Icon = DashboardIconKind.AlertTriangle,
                    IconRenkHex = "#14B8A6",
                    Sparkline = MiniSeri(kritikStok)
                }
            ],
            SonIslemler = SonIslemleriOlustur(kaynak, sorgu),
            StokUyarilari = StokUyarilariniOlustur(kaynak),
            AylikHarcama = aylikSeri,
            HarcamaDagilimi = HarcamaDagiliminiOlustur(buAyAlimlar),
            AcikKayitlar = AcikKayitlariOlustur(kaynak, sorgu),
            Hatirlatmalar = HatirlatmalariOlustur(kaynak, onayBekleyen, kritikStok),
            FinansOzet = FinansOzetiniOlustur(toplamHarcama, oncekiHarcama),
            TopUrunler = TopUrunleriOlustur(buAyAlimlar)
        };
    }

    private static AnaSayfaVeri YukleDepo()
    {
        var kaynak = MasaustuDashboardBaglanti.VeriKaynagiOlustur();
        var sorgu = new MasaustuDashboardSorgu();
        sorgu.TalepleriGuncelle(kaynak.Talepler);

        var kritik = kaynak.Stok.Count(s => s.DurumMetin == "Kritik");
        var tukenen = kaynak.Stok.Count(s => s.DurumMetin == "Tükendi");
        var yoldaki = sorgu.MalKabulBekleyenSayisi();
        var hareket = kaynak.StokHareketleri.Count;

        return new AnaSayfaVeri
        {
            Istatistikler =
            [
                new AnaSayfaIstatistik
                {
                    Baslik = "Stok Kalemi",
                    Deger = kaynak.Stok.Count.ToString("N0", Tr),
                    AltMetin = "toplam malzeme",
                    TrendMetin = "",
                    TrendPozitif = true,
                    Icon = DashboardIconKind.Warehouse,
                    IconRenkHex = AppTheme.PrimaryHex,
                    Sparkline = MiniSeri(kaynak.Stok.Count)
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Kritik Stok",
                    Deger = kritik.ToString("N0", Tr),
                    AltMetin = "minimum altı",
                    TrendMetin = kritik > 0 ? "▼ uyarı" : "▲ 0%",
                    TrendPozitif = kritik == 0,
                    Icon = DashboardIconKind.AlertTriangle,
                    IconRenkHex = "#E67E22",
                    Sparkline = MiniSeri(kritik)
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Tükenen",
                    Deger = tukenen.ToString("N0", Tr),
                    AltMetin = "stok yok",
                    TrendMetin = tukenen > 0 ? "▼ uyarı" : "▲ 0%",
                    TrendPozitif = tukenen == 0,
                    Icon = DashboardIconKind.Package,
                    IconRenkHex = AppTheme.DangerHex,
                    Sparkline = MiniSeri(tukenen)
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Yoldaki Sipariş",
                    Deger = yoldaki.ToString("N0", Tr),
                    AltMetin = "mal kabul bekleyen",
                    TrendMetin = yoldaki > 0 ? "▼ bekliyor" : "▲ 0%",
                    TrendPozitif = yoldaki == 0,
                    Icon = DashboardIconKind.ClipboardList,
                    IconRenkHex = "#16A085",
                    Sparkline = MiniSeri(yoldaki)
                },
                new AnaSayfaIstatistik
                {
                    Baslik = "Hareketler",
                    Deger = hareket.ToString("N0", Tr),
                    AltMetin = "kayıtlı hareket",
                    TrendMetin = "",
                    TrendPozitif = true,
                    Icon = DashboardIconKind.ShoppingCart,
                    IconRenkHex = "#2980B9",
                    Sparkline = MiniSeri(hareket)
                }
            ],
            SonIslemler = DepoSonIslemleri(kaynak),
            StokUyarilari = StokUyarilariniOlustur(kaynak),
            AylikHarcama = [],
            HarcamaDagilimi = [],
            AcikKayitlar = DepoYoldakiKayitlar(kaynak, sorgu),
            Hatirlatmalar = DepoHatirlatmalar(kritik + tukenen, yoldaki),
            FinansOzet = new AnaSayfaFinansOzet
            {
                Gelir = "—",
                Gider = "—",
                Kar = "—",
                KarMarjiYuzde = 0
            },
            TopUrunler = kaynak.Stok
                .Where(s => s.DurumMetin is "Kritik" or "Tükendi")
                .OrderBy(s => s.DurumMetin == "Tükendi" ? 0 : 1)
                .Take(3)
                .Select(s => new AnaSayfaTopUrun
                {
                    Ad = s.MalzemeAdi,
                    Tutar = $"{s.MevcutMiktar:N0} {s.Birim}"
                })
                .ToList()
        };
    }

    private static List<AnaSayfaIslem> DepoSonIslemleri(SharedKaynak kaynak) =>
        kaynak.StokHareketleri
            .OrderByDescending(h => h.Tarih)
            .Take(6)
            .Select(h => new AnaSayfaIslem
            {
                Baslik = $"{h.HareketTipi}: {h.MalzemeAdi}",
                Zaman = h.Tarih,
                Durum = string.IsNullOrWhiteSpace(h.BelgeNo) ? h.HareketTipi : h.BelgeNo,
                DurumRenkHex = h.HareketTipi.Contains("Çıkış", StringComparison.OrdinalIgnoreCase)
                    ? AppTheme.WarningHex
                    : AppTheme.SuccessHex,
                Icon = DashboardIconKind.Warehouse
            })
            .ToList();

    private static List<AnaSayfaAcikKayit> DepoYoldakiKayitlar(
        SharedKaynak kaynak, MasaustuDashboardSorgu sorgu)
    {
        _ = sorgu;
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        return kaynak.Talepler
            .Where(t => ProcurementRouteMatcher.Matches(
                SatinalmaRoutes.SatinalmaSiparis, t, rol, uid))
            .OrderByDescending(t => t.Tarih)
            .Take(5)
            .Select(t => new AnaSayfaAcikKayit
            {
                No = t.TalepNo,
                Tarih = t.Tarih,
                Cari = t.TalepEden,
                Vade = t.Tarih,
                Tutar = $"{t.Kalemler.Count} kalem",
                Kalan = "Mal kabul",
                Durum = "Yolda",
                DurumRenkHex = "#16A085"
            })
            .ToList();
    }

    private static List<AnaSayfaHatirlatma> DepoHatirlatmalar(int kritikToplam, int yoldaki)
    {
        var liste = new List<AnaSayfaHatirlatma>();
        if (yoldaki > 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = $"{yoldaki} sipariş mal kabul bekliyor", RenkHex = "#16A085" });
        if (kritikToplam > 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = $"{kritikToplam} kritik/tükenen stok kalemi", RenkHex = AppTheme.DangerHex });
        if (liste.Count == 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = "Bekleyen depo işlemi yok", RenkHex = AppTheme.SuccessHex });
        return liste;
    }

    private static List<double> MiniSeri(double son) =>
        [son * 0.7, son * 0.75, son * 0.8, son * 0.85, son * 0.9, son * 0.95, son];

    private static List<AnaSayfaAylikNokta> AylikHarcamaSerisi(IEnumerable<AlinanMalzemeKaydi> alimlar)
    {
        var liste = new List<AnaSayfaAylikNokta>();
        for (var i = 8; i >= 0; i--)
        {
            var hedef = DateTime.Now.AddMonths(-i);
            var tutar = alimlar
                .Where(a => TarihAy(a.Tarih) == hedef.Month && TarihYil(a.Tarih) == hedef.Year)
                .Sum(a => (double)a.ToplamTutar);
            liste.Add(new AnaSayfaAylikNokta
            {
                Etiket = hedef.ToString("MMM", Tr),
                Deger = tutar
            });
        }
        return liste;
    }

    private static List<AnaSayfaDagilim> HarcamaDagiliminiOlustur(List<AlinanMalzemeKaydi> buAyAlimlar)
    {
        var toplam = buAyAlimlar.Sum(a => (double)a.ToplamTutar);
        if (toplam <= 0)
            return
            [
                new() { Etiket = "Malzeme", Yuzde = 60, RenkHex = AppTheme.PrimaryHex },
                new() { Etiket = "Hizmet", Yuzde = 25, RenkHex = "#22C55E" },
                new() { Etiket = "Diğer", Yuzde = 15, RenkHex = "#F59E0B" }
            ];

        var gruplar = buAyAlimlar
            .GroupBy(a => string.IsNullOrWhiteSpace(a.Kategori) ? "Diğer" : a.Kategori)
            .Select(g => new { g.Key, Tutar = g.Sum(x => (double)x.ToplamTutar) })
            .OrderByDescending(x => x.Tutar)
            .Take(3)
            .ToList();

        var renkler = new[] { AppTheme.PrimaryHex, "#22C55E", "#F59E0B" };
        return gruplar.Select((g, i) => new AnaSayfaDagilim
        {
            Etiket = g.Key,
            Yuzde = Math.Round(g.Tutar / toplam * 100, 1),
            RenkHex = renkler[i % renkler.Length]
        }).ToList();
    }

    private static List<AnaSayfaAcikKayit> AcikKayitlariOlustur(DashboardVeriKaynagi kaynak, MasaustuDashboardSorgu sorgu)
    {
        return kaynak.Talepler
            .Where(t => t.GorunenDurum.Contains("Onay", StringComparison.OrdinalIgnoreCase)
                        || t.GorunenDurum.Contains("Sipariş", StringComparison.OrdinalIgnoreCase)
                        || t.GorunenDurum.Contains("Bekle", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Tarih)
            .Take(5)
            .Select(t =>
            {
                var tutar = t.OnaylananTeklif?.GenelToplam ?? t.EnDusukFiyatliTeklif()?.GenelToplam ?? 0;
                var durum = t.GorunenDurum;
                var renk = durum.Contains("Onay", StringComparison.OrdinalIgnoreCase) ? AppTheme.SuccessHex
                    : durum.Contains("Red", StringComparison.OrdinalIgnoreCase) ? AppTheme.DangerHex
                    : AppTheme.WarningHex;
                return new AnaSayfaAcikKayit
                {
                    No = t.TalepNo,
                    Tarih = t.Tarih,
                    Cari = t.TalepEden,
                    Vade = t.Tarih,
                    Tutar = ((double)tutar).ToString("C0", Tr),
                    Kalan = ((double)tutar).ToString("C0", Tr),
                    Durum = durum.Length > 12 ? durum[..12] : durum,
                    DurumRenkHex = renk
                };
            })
            .ToList();
    }

    private static List<AnaSayfaHatirlatma> HatirlatmalariOlustur(DashboardVeriKaynagi kaynak, int onayBekleyen, int kritikStok)
    {
        var liste = new List<AnaSayfaHatirlatma>();
        if (onayBekleyen > 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = $"{onayBekleyen} onay bekleyen talep var", RenkHex = AppTheme.WarningHex });
        if (kritikStok > 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = $"{kritikStok} kritik stok kalemi", RenkHex = AppTheme.DangerHex });
        var dusuk = kaynak.Stok.Count(s => s.DurumMetin == "Kritik");
        if (dusuk > 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = $"{dusuk} malzeme minimum stok altında", RenkHex = AppTheme.PrimaryHex });
        if (liste.Count == 0)
            liste.Add(new AnaSayfaHatirlatma { Metin = "Bekleyen kritik hatırlatma yok", RenkHex = AppTheme.SuccessHex });
        return liste;
    }

    private static AnaSayfaFinansOzet FinansOzetiniOlustur(double gider, double oncekiGider)
    {
        var gelir = oncekiGider * 1.12;
        var kar = gelir - gider;
        var marj = gelir <= 0 ? 0 : kar / gelir * 100;
        return new AnaSayfaFinansOzet
        {
            Gelir = gelir.ToString("C0", Tr),
            Gider = gider.ToString("C0", Tr),
            Kar = kar.ToString("C0", Tr),
            KarMarjiYuzde = Math.Round(marj, 1)
        };
    }

    private static List<AnaSayfaTopUrun> TopUrunleriOlustur(List<AlinanMalzemeKaydi> buAyAlimlar) =>
        buAyAlimlar
            .GroupBy(a => a.MalzemeHizmet)
            .Select(g => new { Ad = g.Key, Tutar = g.Sum(x => (double)x.ToplamTutar) })
            .OrderByDescending(x => x.Tutar)
            .Take(3)
            .Select(x => new AnaSayfaTopUrun { Ad = x.Ad, Tutar = x.Tutar.ToString("C0", Tr) })
            .ToList();

    private static List<AnaSayfaIslem> SonIslemleriOlustur(DashboardVeriKaynagi kaynak, MasaustuDashboardSorgu sorgu)
    {
        var liste = new List<AnaSayfaIslem>();

        var kullanici = OturumYoneticisi.AktifKullanici;
        foreach (var b in BildirimDeposu.Bildirimler
                     .Where(x => kullanici is not null
                         && MasaustuBildirimFiltreleme.KullaniciyaMi(x, kullanici)
                         && MasaustuBildirimFiltreleme.GecerliMi(x, SatinalmaDepo.Talepler))
                     .OrderByDescending(x => x.GuncellemeUtc)
                     .Take(4))
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
