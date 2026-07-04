using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public sealed class DashboardKart
{
    public required string Baslik { get; init; }
    public required string Deger { get; init; }
    public string AltMetin { get; init; } = "";
    public string Renk { get; init; } = "#1B3A5C";
    public string? Route { get; init; }
}

public sealed class DashboardAktivite
{
    public required string Baslik { get; init; }
    public required string Alt { get; init; }
    public string Durum { get; init; } = "";
    public string? Route { get; init; }
    public Guid? TalepId { get; init; }
}

public sealed class DashboardOzet
{
    public required string PanelBasligi { get; init; }
    public required string AltBaslik { get; init; }
    public List<DashboardKart> Kartlar { get; init; } = [];
    public List<DashboardAktivite> SonAktivite { get; init; } = [];
}

public sealed class DashboardVeriKaynagi
{
    public required IReadOnlyList<SatinalmaTalep> Talepler { get; init; }
    public required IReadOnlyList<StokKaydi> Stok { get; init; }
    public required IReadOnlyList<StokHareketKaydi> StokHareketleri { get; init; }
    public required string Uid { get; init; }
    public required string Ad { get; init; }
}

/// <summary>
/// Panel özet kartları — sayılar liste ekranlarıyla aynı sorguları kullanır.
/// </summary>
public static class DashboardServisi
{
    public static DashboardOzet Olustur(
        MobilVeriDeposu depo,
        SatinalmaMobilServisi satinalma,
        string? rol,
        int okunmamisBildirim) =>
        Olustur(
            new DashboardVeriKaynagi
            {
                Talepler = depo.Talepler,
                Stok = depo.Stok,
                StokHareketleri = depo.StokHareketleri,
                Uid = depo.AktifKullanici?.Uid ?? "",
                Ad = depo.AktifKullanici?.AdSoyad ?? ""
            },
            satinalma,
            rol,
            okunmamisBildirim);

    public static DashboardOzet Olustur(
        DashboardVeriKaynagi kaynak,
        ISatinalmaDashboardSorgu satinalma,
        string? rol,
        int okunmamisBildirim)
    {
        rol = KullaniciRolleri.Normalize(rol);
        var uid = kaynak.Uid;
        var ad = kaynak.Ad;

        if (KullaniciRolleri.AdminMi(rol))
            return AdminOzet(kaynak, satinalma, ad, uid, okunmamisBildirim);

        return rol switch
        {
            KullaniciRolleri.Yonetim => YonetimOzet(kaynak, satinalma, ad, okunmamisBildirim),
            KullaniciRolleri.Satinalma => SatinalmaOzet(kaynak, satinalma, ad, okunmamisBildirim),
            KullaniciRolleri.Depo => DepoOzet(kaynak, ad, okunmamisBildirim),
            KullaniciRolleri.Sef => SefOzet(kaynak, satinalma, ad, uid, okunmamisBildirim),
            KullaniciRolleri.Atolye => AtolyeOzet(kaynak, ad, okunmamisBildirim),
            _ => SahaOzet(kaynak, satinalma, ad, uid, okunmamisBildirim)
        };
    }

    private static DashboardOzet AdminOzet(
        DashboardVeriKaynagi kaynak, ISatinalmaDashboardSorgu satinalma, string ad, string uid, int bildirim) =>
        new()
        {
            PanelBasligi = "Admin Paneli",
            AltBaslik = $"Hoş geldiniz, {ad}",
            Kartlar =
            [
                Kart("Yönetim Onayı", satinalma.YonetimTalepleri().Count().ToString(), "Onay bekleyen talep", "#E67E22", "gelen-talepler"),
                Kart("Teklif Onayı", satinalma.YonetimTeklifOnayiBekleyenleri().Count().ToString(), "Karşılaştırma aşaması", "#8E44AD", "teklif-onay"),
                Kart("Teklif Girişi", satinalma.TeklifGirisiBekleyenleri().Count().ToString(), "Teklif bekleyen", "#2980B9", "teklif-gir"),
                Kart("Teklifsiz F/F", satinalma.TeklifsizFirmaFiyatBekleyenleri().Count().ToString(), "Firma/fiyat bekleyen", "#E67E22", "teklifsiz-firma-fiyat"),
                Kart("Mal Kabul", satinalma.MalKabulBekleyenSayisi().ToString(), "Bekleyen malzeme", "#16A085", "onaylanan-malzemeler"),
                Kart("Stok Kritik", StokKritik(kaynak).ToString(), "Kritik / tükenen", "#C0392B", "stok-durum"),
                Kart("Bildirimler", bildirim.ToString(), "Okunmamış", "#1B3A5C", "bildirimler"),
                Kart("Toplam Stok", kaynak.Stok.Count.ToString(), "Malzeme kalemi", "#27AE60", "stok-durum")
            ],
            SonAktivite = SonTalepler(kaynak, 6, satinalma.YonetimTalepleri().Concat(satinalma.YonetimTeklifOnayiBekleyenleri()))
        };

    private static DashboardOzet YonetimOzet(
        DashboardVeriKaynagi kaynak, ISatinalmaDashboardSorgu satinalma, string ad, int bildirim) =>
        new()
        {
            PanelBasligi = "Yönetim Paneli",
            AltBaslik = $"Hoş geldiniz, {ad}",
            Kartlar =
            [
                Kart("Gelen Talepler", satinalma.YonetimTalepleri().Count().ToString(), "Teklifsiz onay", "#E67E22", "gelen-talepler"),
                Kart("Teklif Onay", satinalma.YonetimTeklifOnayiBekleyenleri().Count().ToString(), "Teklif girilmiş", "#8E44AD", "teklif-onay"),
                Kart("Geçmiş Talepler", satinalma.YonetimGecmisTalepleri().Count().ToString(), "Acil / teklifsiz", "#27AE60", "gecmis-talepler"),
                Kart("Teklifli Geçmiş", satinalma.YonetimGecmisTeklifliOnaylari().Count().ToString(), "Onaylanan teklifler", "#1B3A5C", "gecmis-teklifli-onaylar"),
                Kart("Reddedilen", satinalma.YonetimReddedilenleri().Count().ToString(), "Red talepler", "#C0392B", "red-talepler"),
                Kart("Stok", kaynak.Stok.Count.ToString(), "Stok durumu", "#2980B9", "stok-durum"),
                Kart("Bildirimler", bildirim.ToString(), "Okunmamış", "#34495E", "bildirimler")
            ],
            SonAktivite = SonTalepler(kaynak, 6,
                satinalma.YonetimTalepleri().Concat(satinalma.YonetimTeklifOnayiBekleyenleri()))
        };

    private static DashboardOzet SatinalmaOzet(
        DashboardVeriKaynagi kaynak, ISatinalmaDashboardSorgu satinalma, string ad, int bildirim) =>
        new()
        {
            PanelBasligi = "Satınalma Paneli",
            AltBaslik = $"Hoş geldiniz, {ad}",
            Kartlar =
            [
                Kart("Teklif Girişi", satinalma.TeklifGirisiBekleyenleri().Count().ToString(), "Bekleyen talep", "#2980B9", "teklif-gir"),
                Kart("Karşılaştırma", satinalma.KarsilastirmaBekleyenleri().Count().ToString(), "Yönetime gönder", "#8E44AD", "teklif-karsilastirma"),
                Kart("Teklifsiz F/F", satinalma.TeklifsizFirmaFiyatBekleyenleri().Count().ToString(), "Firma/fiyat gir", "#E67E22", "teklifsiz-firma-fiyat"),
                Kart("Onay Bekleyen", satinalma.OnayBekleyenTalepler().Count().ToString(), "İşlemde", "#E67E22", "onay-bekleyen"),
                Kart("Yön. Onaylı", (satinalma.YonetimOnaylananTeklifleri().Count() + satinalma.YonetimOnaylananTalepleri().Count()).ToString(), "Onay belgesi / PDF", "#27AE60", "onaylanan-teklifler"),
                Kart("Mal Kabul", satinalma.MalKabulBekleyenSayisi().ToString(), "Bekleyen malzeme", "#16A085", "onaylanan-malzemeler"),
                Kart("Stok Kritik", StokKritik(kaynak).ToString(), "Depo durumu", "#C0392B", "stok-durum"),
                Kart("Bildirimler", bildirim.ToString(), "Okunmamış", "#1B3A5C", "bildirimler")
            ],
            SonAktivite = SonTalepler(kaynak, 6,
                satinalma.TeklifGirisiBekleyenleri().Concat(satinalma.KarsilastirmaBekleyenleri()))
        };

    private static DashboardOzet SefOzet(
        DashboardVeriKaynagi kaynak, ISatinalmaDashboardSorgu satinalma, string ad, string uid, int bildirim)
    {
        var taleplerim = KayitliTumTalepler(satinalma).Count();

        return new()
        {
            PanelBasligi = "Şef Paneli",
            AltBaslik = $"Hoş geldiniz, {ad}",
            Kartlar =
            [
                Kart("Taleplerim", taleplerim.ToString(), "Toplam talep", "#1B3A5C", "taleplerim"),
                Kart("Onay Bekleyen", satinalma.OnayBekleyenTalepler().Count().ToString(), "İşlemde", "#E67E22", "onay-bekleyen"),
                Kart("Onaylanan", satinalma.OnaylanmisTalepler().Count().ToString(), "Firma seçildi", "#27AE60", "onaylanan-talepler"),
                Kart("Siparişte", KayitliTumTalepler(satinalma).Count(SiparisDurum).ToString(), "Teslim bekliyor", "#2980B9", "onaylanan-talepler"),
                Kart("Reddedilen", KayitliTumTalepler(satinalma).Count(t => t.Durum == SatinalmaTalepDurumlari.Reddedildi).ToString(), "Geri dönen", "#C0392B", "taleplerim"),
                Kart("Alınan Malz.", satinalma.OnaylananMalzemeleriOlustur().Count(s => s.KabulEdilenMiktar > 0.0001).ToString(), "Teslim alınan", "#8E44AD", "onaylanan-malzemeler"),
                Kart("Stok Kritik", StokKritik(kaynak).ToString(), "Depo durumu", "#34495E", "stok-durum"),
                Kart("Bildirimler", bildirim.ToString(), "Okunmamış", "#7F8C8D", "bildirimler")
            ],
            SonAktivite = SonTalepler(kaynak, 5, KayitliTumTalepler(satinalma))
        };
    }

    private static DashboardOzet AtolyeOzet(DashboardVeriKaynagi kaynak, string ad, int bildirim) =>
        new()
        {
            PanelBasligi = "Atölye Paneli",
            AltBaslik = $"Hoş geldiniz, {ad}",
            Kartlar =
            [
                Kart("Stok Kalemi", kaynak.Stok.Count.ToString(), "Depodaki malzeme", "#1B3A5C", "stok-durum"),
                Kart("Kritik Stok", kaynak.Stok.Count(s => s.DurumMetin == "Kritik").ToString(), "Minimum altı", "#E67E22", "stok-durum"),
                Kart("Tükenen", kaynak.Stok.Count(s => s.DurumMetin == "Tükendi").ToString(), "Stok yok", "#C0392B", "stok-durum"),
                Kart("Bildirimler", bildirim.ToString(), "Okunmamış", "#34495E", "bildirimler")
            ],
            SonAktivite = []
        };

    private static DashboardOzet SahaOzet(
        DashboardVeriKaynagi kaynak, ISatinalmaDashboardSorgu satinalma, string ad, string uid, int bildirim)
    {
        var taleplerim = KayitliTumTalepler(satinalma).Count();

        return new()
        {
            PanelBasligi = "Saha Paneli",
            AltBaslik = $"Hoş geldiniz, {ad}",
            Kartlar =
            [
                Kart("Taleplerim", taleplerim.ToString(), "Toplam talep", "#1B3A5C", "taleplerim"),
                Kart("Onay Bekleyen", satinalma.OnayBekleyenTalepler().Count().ToString(), "İşlemde", "#E67E22", "onay-bekleyen"),
                Kart("Onaylanan", satinalma.OnaylanmisTalepler().Count().ToString(), "Firma onaylandı", "#27AE60", "onaylanan-talepler"),
                Kart("Siparişte", KayitliTumTalepler(satinalma).Count(SiparisDurum).ToString(), "Teslim bekliyor", "#2980B9", "onaylanan-talepler"),
                Kart("Reddedilen", KayitliTumTalepler(satinalma).Count(t => t.Durum == SatinalmaTalepDurumlari.Reddedildi).ToString(), "Geri dönen", "#C0392B", "taleplerim"),
                Kart("Stok Kritik", StokKritik(kaynak).ToString(), "Depo durumu", "#8E44AD", "stok-durum"),
                Kart("Bildirimler", bildirim.ToString(), "Okunmamış", "#34495E", "bildirimler")
            ],
            SonAktivite = SonTalepler(kaynak, 5, KayitliTumTalepler(satinalma))
        };
    }

    private static DashboardOzet DepoOzet(DashboardVeriKaynagi kaynak, string ad, int bildirim) =>
        new()
        {
            PanelBasligi = "Depo Paneli",
            AltBaslik = $"Hoş geldiniz, {ad}",
            Kartlar =
            [
                Kart("Stok Kalemi", kaynak.Stok.Count.ToString(), "Toplam malzeme", "#1B3A5C", "stok-durum"),
                Kart("Kritik Stok", kaynak.Stok.Count(s => s.DurumMetin == "Kritik").ToString(), "Minimum altı", "#E67E22", "stok-durum"),
                Kart("Tükenen", kaynak.Stok.Count(s => s.DurumMetin == "Tükendi").ToString(), "Stok yok", "#C0392B", "stok-durum"),
                Kart("Hareketler", kaynak.StokHareketleri.Count.ToString(), "Kayıtlı hareket", "#2980B9", "stok-hareket"),
                Kart("Stok Girişi", "→", "Malzeme girişi", "#27AE60", "stok-giris"),
                Kart("Stok Çıkışı", "→", "Malzeme çıkışı", "#8E44AD", "stok-cikis"),
                Kart("Bildirimler", bildirim.ToString(), "Okunmamış", "#34495E", "bildirimler")
            ],
            SonAktivite = SonStokHareketleri(kaynak, 6)
        };

    private static IEnumerable<SatinalmaTalep> KayitliTumTalepler(ISatinalmaDashboardSorgu satinalma) =>
        satinalma.KayitliTalepler();

    private static IEnumerable<SatinalmaTalep> KayitliKullaniciTalepleri(ISatinalmaDashboardSorgu satinalma, string uid) =>
        satinalma.KullaniciTalepleri(uid).Where(SatinalmaTalepKuyrugu.KayitliTalep);

    private static int StokKritik(DashboardVeriKaynagi kaynak) =>
        kaynak.Stok.Count(s => s.DurumMetin is "Kritik" or "Tükendi");

    private static bool SiparisDurum(SatinalmaTalep t) =>
        t.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu;

    private static DashboardKart Kart(string baslik, string deger, string alt, string renk, string? route) =>
        new() { Baslik = baslik, Deger = deger, AltMetin = alt, Renk = renk, Route = route };

    private static List<DashboardAktivite> SonTalepler(
        DashboardVeriKaynagi kaynak, int adet, IEnumerable<SatinalmaTalep>? kaynakListe = null)
    {
        var liste = (kaynakListe ?? kaynak.Talepler.AsEnumerable())
            .OrderByDescending(t => t.Tarih)
            .Take(adet);

        return liste.Select(t => new DashboardAktivite
        {
            Baslik = string.IsNullOrWhiteSpace(t.TalepAciklamasi) ? t.TalepNo : t.TalepAciklamasi,
            Alt = $"{t.TalepNo} · {t.TalepEden}",
            Durum = t.GorunenDurum,
            Route = "taleplerim",
            TalepId = t.Id
        }).ToList();
    }

    private static List<DashboardAktivite> SonStokHareketleri(DashboardVeriKaynagi kaynak, int adet) =>
        kaynak.StokHareketleri
            .OrderByDescending(h => h.Tarih)
            .Take(adet)
            .Select(h => new DashboardAktivite
            {
                Baslik = h.MalzemeAdi,
                Alt = $"{h.HareketTipi} · {h.Miktar} {h.Birim}",
                Durum = h.Tarih,
                Route = "stok-hareket"
            })
            .ToList();
}
