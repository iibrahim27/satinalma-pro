using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public sealed class MenuOgesi
{
    public required string Baslik { get; init; }
    public required string Route { get; init; }
    public string? Ikon { get; init; }
    public string? Grup { get; init; }
}

public static class RolNavigasyonu
{
    public static IReadOnlyList<MenuOgesi> Menuler(string? rol)
    {
        rol = KullaniciRolleri.Normalize(rol);

        var liste = KullaniciRolleri.AdminMi(rol)
            ? AdminMenuleriTam
            : rol switch
            {
                KullaniciRolleri.Yonetim => YonetimMenuleri,
                KullaniciRolleri.Satinalma => SatinalmaMenuleri,
                KullaniciRolleri.Sef => SefMenuleri,
                KullaniciRolleri.Atolye => AtolyeMenuleri,
                KullaniciRolleri.Saha => SahaMenuleri,
                KullaniciRolleri.Depo => DepoMenuleri,
                _ => SahaMenuleri.Concat(ModulKayitMenuleri).ToList()
            };

        return liste.Concat([Profil]).ToList();
    }

    private static readonly MenuOgesi Profil = new() { Baslik = "Profil / Ayarlar", Route = "profil", Ikon = "👤" };
    private static readonly MenuOgesi Bildirimler = new() { Baslik = "Bildirimler", Route = "bildirimler", Ikon = "🔔", Grup = "Genel" };
    private static readonly MenuOgesi StokDurum = new() { Baslik = "Stok Durumu", Route = "stok-durum", Ikon = "📦", Grup = "Stok" };
    private static readonly MenuOgesi StokHareket = new() { Baslik = "Stok Hareketleri", Route = "stok-hareket", Ikon = "📋", Grup = "Stok" };
    private static readonly MenuOgesi StokGiris = new() { Baslik = "Stok Girişi", Route = "stok-giris", Ikon = "⬇️", Grup = "Stok" };
    private static readonly MenuOgesi StokSayim = new() { Baslik = "Stok Sayım", Route = "stok-sayim", Ikon = "📊", Grup = "Stok" };
    private static readonly MenuOgesi StokCikis = new() { Baslik = "Stok Çıkışı", Route = "stok-cikis", Ikon = "⬆️", Grup = "Stok" };
    private static readonly MenuOgesi Taleplerim = new() { Baslik = "Taleplerim", Route = "taleplerim", Ikon = "📝", Grup = "Talep" };
    private static readonly MenuOgesi YeniTalep = new() { Baslik = "Yeni Talep", Route = "yeni-talep", Ikon = "➕", Grup = "Talep" };
    private static readonly MenuOgesi OnayBekleyenTalepler = new() { Baslik = "Onay Bekleyen", Route = "onay-bekleyen", Ikon = "⏳", Grup = "Talep" };
    private static readonly MenuOgesi OnaylananTalepler = new() { Baslik = "Onaylanan Talepler", Route = "onaylanan-talepler", Ikon = "✅", Grup = "Talep" };
    private static readonly MenuOgesi GelenTalepler = new() { Baslik = "Gelen Talepler", Route = "gelen-talepler", Ikon = "📥", Grup = "Talep" };
    private static readonly MenuOgesi TeklifBekleyen = new() { Baslik = "Teklif Bekleyen", Route = "teklif-bekleyen", Ikon = "⏳", Grup = "Talep" };
    private static readonly MenuOgesi TeklifGir = new() { Baslik = "Teklif Girişi", Route = "teklif-gir", Ikon = "💰", Grup = "Teklif" };
    private static readonly MenuOgesi TeklifKarsilastirma = new() { Baslik = "Karşılaştırma", Route = "teklif-karsilastirma", Ikon = "📊", Grup = "Teklif" };
    private static readonly MenuOgesi TeklifOnay = new() { Baslik = "Teklif Onay", Route = "teklif-onay", Ikon = "✅", Grup = "Teklif" };
    private static readonly MenuOgesi OnaylananMalzemeler = new() { Baslik = "Sipariş & Mal Kabul", Route = "onaylanan-malzemeler", Ikon = "📦", Grup = "Malzeme" };
    private static readonly MenuOgesi SatinalmaSiparis = new() { Baslik = "Yoldaki Malzemeler", Route = "satinalma-siparis", Ikon = "🚚", Grup = "Malzeme" };
    private static readonly MenuOgesi AlinanMalzemeModul = new() { Baslik = "Alınan Malzemeler", Route = "alinan-malzemeler", Ikon = "📋", Grup = "Malzeme" };
    private static readonly MenuOgesi AgregaModul = new() { Baslik = "Agrega", Route = "agrega", Ikon = "🏗", Grup = "Malzeme" };
    private static readonly MenuOgesi CimentoModul = new() { Baslik = "Çimento", Route = "cimento", Ikon = "🧱", Grup = "Malzeme" };
    private static readonly MenuOgesi OnaylananTeklifler = new() { Baslik = "Onaylanan Teklifler", Route = "onaylanan-teklifler", Ikon = "📋", Grup = "Teklif" };
    private static readonly MenuOgesi OnayGecmisi = new() { Baslik = "Onay Geçmişi", Route = "onay-gecmisi", Ikon = "📜", Grup = "Teklif" };
    private static readonly MenuOgesi RedTalepler = new() { Baslik = "Red Talepler", Route = "red-talepler", Ikon = "🚫", Grup = "Talep" };

    private static readonly IReadOnlyList<MenuOgesi> ModulKayitMenuleri =
        [AgregaModul, CimentoModul, AlinanMalzemeModul];

    private static readonly IReadOnlyList<MenuOgesi> AdminMenuleri =
    [
        YeniTalep, Taleplerim, OnayBekleyenTalepler, OnaylananTalepler, GelenTalepler, TeklifBekleyen, TeklifGir, TeklifKarsilastirma,
        TeklifOnay, OnaylananTeklifler, OnayGecmisi, RedTalepler,
        OnaylananMalzemeler, StokDurum, StokHareket, StokGiris, StokCikis, StokSayim, Bildirimler
    ];

    private static readonly IReadOnlyList<MenuOgesi> AdminMenuleriTam =
        AdminMenuleri.Concat(ModulKayitMenuleri).ToList();

    private static readonly MenuOgesi GecmisTalepler = new() { Baslik = "Geçmiş Talepler", Route = "gecmis-talepler", Ikon = "📜", Grup = "Talep" };
    private static readonly MenuOgesi GecmisTeklifliOnaylar = new() { Baslik = "Geçmiş Teklifli Onaylar", Route = "gecmis-teklifli-onaylar", Ikon = "📋", Grup = "Talep" };

    private static readonly IReadOnlyList<MenuOgesi> YonetimMenuleri =
    [
        GelenTalepler, TeklifOnay, GecmisTalepler, GecmisTeklifliOnaylar, RedTalepler,
        Bildirimler
    ];

    private static readonly IReadOnlyList<MenuOgesi> SatinalmaMenuleri =
        new List<MenuOgesi>
        {
            GelenTalepler, OnayBekleyenTalepler, OnaylananTalepler, RedTalepler,
            TeklifBekleyen, TeklifGir, TeklifKarsilastirma, TeklifOnay,
            OnaylananTeklifler, OnayGecmisi, OnaylananMalzemeler,
            Bildirimler
        };

    private static readonly IReadOnlyList<MenuOgesi> SahaMenuleri =
        [YeniTalep, Taleplerim, OnayBekleyenTalepler, OnaylananTalepler, OnaylananMalzemeler,
         StokDurum, StokHareket, Bildirimler];

    private static readonly IReadOnlyList<MenuOgesi> AtolyeMenuleri =
        [StokDurum];

    private static readonly IReadOnlyList<MenuOgesi> SefMenuleri =
        SahaMenuleri;

    private static readonly IReadOnlyList<MenuOgesi> DepoMenuleri =
        [StokDurum, StokGiris, StokCikis, StokHareket];
}
