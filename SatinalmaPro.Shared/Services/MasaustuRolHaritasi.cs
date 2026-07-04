using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

/// <summary>Android RolNavigasyonu rotalarını masaüstü modül sekmelerine çevirir.</summary>
public static class MasaustuRolHaritasi
{
    public const string Panel = "Panel";
    public const string Taleplerim = "Taleplerim";
    public const string OnayBekleyen = "Onay Bekleyen";
    public const string OnaylananTalepler = "Onaylanan Talepler";
    public const string GelenTalepler = "Gelen Talepler";
    public const string TeklifBekleyen = "Teklif Bekleyen";
    public const string TeklifGirisi = "Teklif Girişi";
    public const string Karsilastirma = "Karşılaştırma";
    public const string TeklifsizFirmaFiyat = "Firma/Fiyat Girişi";
    public const string TeklifOnay = "Teklif Onay";
    public const string OnaylananTeklifler = "Onaylanan Teklifler";
    public const string OnayGecmisi = "Onay Geçmişi";
    public const string AlinanMalzemeler = "Alınan Malzemeler";
    public const string GecmisTalepler = "Geçmiş Talepler";
    public const string GecmisTeklifliOnaylar = "Geçmiş Teklifli Onaylar";
    public const string RedTalepler = "Red Talepler";

    private static readonly Dictionary<string, string> SatinalmaRouteToSekme =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["taleplerim"] = Taleplerim,
            ["onay-bekleyen"] = OnayBekleyen,
            ["onaylanan-talepler"] = OnaylananTalepler,
            ["gelen-talepler"] = GelenTalepler,
            ["teklif-bekleyen"] = TeklifBekleyen,
            ["teklif-gir"] = TeklifGirisi,
            ["teklif-giris"] = TeklifGirisi,
            ["teklif-karsilastirma"] = Karsilastirma,
            ["teklifsiz-firma-fiyat"] = TeklifsizFirmaFiyat,
            ["teklif-onay"] = TeklifOnay,
            ["onaylanan-teklifler"] = OnaylananTeklifler,
            ["onaylanan-malzemeler"] = AlinanMalzemeler,
            ["gecmis-talepler"] = GecmisTalepler,
            ["gecmis-teklifli-onaylar"] = GecmisTeklifliOnaylar,
            ["red-talepler"] = RedTalepler,
            ["onay-gecmisi"] = OnayGecmisi
        };

    private static readonly Dictionary<string, string> StokRouteToSekme =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["stok-durum"] = "Stok Durumu",
            ["stok-hareket"] = "Stok Hareketleri",
            ["stok-giris"] = "Stok Girişi",
            ["stok-cikis"] = "Stok Çıkışı",
            ["stok-sayim"] = "Stok Sayım"
        };

    private static readonly Dictionary<string, string> SekmeAliaslari =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Talepler"] = Taleplerim,
            ["Onaylananlar"] = OnaylananTalepler,
            ["Reddedilenler"] = RedTalepler,
            ["Teklif Değerlendirme"] = Karsilastirma,
            ["Onaylanan Teklifler"] = OnaylananTeklifler,
            ["Siparişler"] = AlinanMalzemeler,
            ["Gelen Siparişler"] = AlinanMalzemeler
        };

    public static IReadOnlyList<string> SatinalmaSekmeleri(string? rol) =>
        RolNavigasyonu.Menuler(rol)
            .Select(m => m.Route)
            .Where(SatinalmaRouteToSekme.ContainsKey)
            .Select(r => SatinalmaRouteToSekme[r])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IReadOnlyList<string> StokSekmeleri(string? rol) =>
        RolNavigasyonu.Menuler(rol)
            .Select(m => m.Route)
            .Where(StokRouteToSekme.ContainsKey)
            .Select(r => StokRouteToSekme[r])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string SatinalmaSekmeNormalize(string sekmeAdi)
    {
        var ad = KullaniciRolleri.SatinalmaSekmeNormalize(sekmeAdi);
        return SekmeAliaslari.TryGetValue(ad, out var hedef) ? hedef : ad;
    }

    public static string? RouteToSatinalmaSekme(string? route) =>
        route is not null && SatinalmaRouteToSekme.TryGetValue(route, out var sekme) ? sekme : null;

    /// <summary>Route slug veya görünen sekme adından bildirim navigasyonu için route slug üretir.</summary>
    public static string? SatinalmaRouteSlug(string? sekmeOrRoute)
    {
        if (string.IsNullOrWhiteSpace(sekmeOrRoute))
            return null;

        if (SatinalmaRouteToSekme.ContainsKey(sekmeOrRoute))
            return sekmeOrRoute;

        var normalized = SatinalmaSekmeNormalize(sekmeOrRoute);
        foreach (var (route, ad) in SatinalmaRouteToSekme)
        {
            if (ad.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                return route;
        }

        return sekmeOrRoute;
    }

    public static string? RouteToStokSekme(string? route) =>
        route is not null && StokRouteToSekme.TryGetValue(route, out var sekme) ? sekme : null;

    public static bool SatinalmaSekmesiGorebilir(string? rol, string sekmeAdi)
    {
        if (KullaniciRolleri.AdminMi(rol))
            return true;

        var hedef = SatinalmaSekmeNormalize(sekmeAdi);
        if (hedef.Equals(Panel, StringComparison.OrdinalIgnoreCase))
            return SatinalmaSekmeleri(rol).Count > 0;

        return SatinalmaSekmeleri(rol).Contains(hedef, StringComparer.OrdinalIgnoreCase);
    }

    public static bool StokSekmesiGorebilir(string? rol, string sekmeAdi) =>
        KullaniciRolleri.AdminMi(rol)
        || StokSekmeleri(rol).Contains(sekmeAdi, StringComparer.OrdinalIgnoreCase);

    /// <summary>Masaüstü ana modül listesi — Android menüsü + masaüstüne özel modüller.</summary>
    public static IReadOnlyList<string> MasaustuModulleri(string? rol)
    {
        rol = KullaniciRolleri.Normalize(rol);

        if (KullaniciRolleri.AdminMi(rol))
            return
            [
                "Alınan Malzemeler", "Stok Yönetimi", "Agrega", "Çimento", "Akaryakıt Takip",
                "Araç Filo Takip", "Finansman Raporlama", "Satınalma", "Raporlamalar", "Ayarlar"
            ];

        if (rol == KullaniciRolleri.Satinalma)
            return
            [
                "Alınan Malzemeler", "Stok Yönetimi", "Agrega", "Çimento", "Akaryakıt Takip",
                "Araç Filo Takip", "Finansman Raporlama", "Satınalma", "Raporlamalar"
            ];

        if (rol == KullaniciRolleri.Yonetim)
            return ["Satınalma", "Stok Yönetimi"];

        var moduller = new List<string>();
        if (SatinalmaSekmeleri(rol).Count > 0)
            moduller.Add("Satınalma");
        if (StokSekmeleri(rol).Count > 0)
            moduller.Add("Stok Yönetimi");

        if (RolNavigasyonu.Menuler(rol).Any(m =>
                m.Route.Equals("onaylanan-malzemeler", StringComparison.OrdinalIgnoreCase)))
            moduller.Add("Alınan Malzemeler");

        if (rol == KullaniciRolleri.Sef)
        {
            moduller.Add("Agrega");
            moduller.Add("Çimento");
        }

        return moduller.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
