using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public static class SatinalmaPart1Menusu
{
    public sealed record Oge(string Baslik, string Route);
    public sealed record MenuGrubu(string? Baslik, IReadOnlyList<Oge> Ogeler);
    // Yönetim
    public const string YonetimGelenTalepler = "yonetim-gelen-talepler";
    public const string YonetimTeklifBekleyen = "yonetim-teklif-bekleyen";
    public const string YonetimTeklifGirilen = "yonetim-teklif-girilen";
    public const string YonetimDirekOnaylanan = "yonetim-direk-onaylanan";
    public const string YonetimRedVerilen = "yonetim-red-verilen";
    public const string YonetimOnaylananTeklifler = "yonetim-onaylanan-teklifler";
    public const string YonetimOnayGecmisi = "yonetim-onay-gecmisi";
    public const string YonetimGecmis = "yonetim-gecmis";

    // Satınalma
    public const string SatinalmaPanosu = "satinalma-panosu";
    public const string SatinalmaTalep = "satinalma-talep";
    public const string SatinalmaTalepler = "satinalma-talepler";
    public const string SatinalmaTeklifIstenen = "satinalma-teklif-istenen";
    public const string SatinalmaTeklifGirilen = "satinalma-teklif-girilen";
    public const string SatinalmaTeklifDuzeltme = "satinalma-teklif-duzeltme";
    public const string SatinalmaKarsilastirma = "satinalma-karsilastirma";
    public const string SatinalmaOnaylanan = "satinalma-onaylanan";
    public const string SatinalmaOnayGecmisi = "satinalma-onay-gecmisi";
    public const string SatinalmaSiparis = "satinalma-siparis";
    public const string SatinalmaMalKabul = "satinalma-mal-kabul";
    public const string SatinalmaOnayBekleyen = "satinalma-onay-bekleyen";
    public const string SatinalmaOnaylananTalepler = "satinalma-onaylanan-talepler";
    public const string SatinalmaIade = "satinalma-iade";
    public const string SatinalmaTedarikciler = "satinalma-tedarikciler";

    // Geriye dönük route adları (bildirimler vb.)
    public const string YeniTalep = SatinalmaTalep;
    public const string Taleplerim = SatinalmaTalepler;
    public const string GelenTalepler = YonetimGelenTalepler;
    public const string TeklifBekleyen = YonetimTeklifBekleyen;
    public const string OnaylananTeklifler = SatinalmaOnaylanan;

    public static IReadOnlyList<Oge> Menuler(string? rol)
    {
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        return DesktopRoleTabManager.GetFlatMenu(rol, uid)
            .Select(i => new Oge(i.Baslik, i.Route))
            .ToList();
    }

    public static IReadOnlyList<MenuGrubu> MenuGruplari(string? rol)
    {
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var ham = DesktopRoleTabManager.GetSatinalmaMenuGroups(rol, uid)
            .Select(g => new MenuGrubu(g.Baslik, g.Ogeler.Select(i => new Oge(i.Baslik, i.Route)).ToList()))
            .ToList();

        return MasaustuMenuGruplariniDuzenle(rol, ham);
    }

    /// <summary>Talep Pro — onay sonrası geçmiş sekmesine yönlendir.</summary>
    public static string OnaySonrasiRoute(string? rol)
    {
        if (!TalepProRuntime.Aktif)
            return SatinalmaOnaylanan;

        return KullaniciRolleri.Normalize(rol) is KullaniciRolleri.Admin or KullaniciRolleri.Yonetim
            ? YonetimOnayGecmisi
            : SatinalmaOnayGecmisi;
    }

    /// <summary>Bildirim/deep-link: Talep Pro'da operasyon sekmeleri yerine geçmiş arşiv.</summary>
    public static string BildirimRouteDonustur(string route, string? rol)
    {
        if (!TalepProRuntime.Aktif)
            return route;

        return route switch
        {
            SatinalmaOnaylanan or YonetimOnaylananTeklifler or SatinalmaOnaylananTalepler
                or YonetimDirekOnaylanan or "teklifsiz-firma-fiyat"
                => OnaySonrasiRoute(rol),
            SatinalmaSiparis or SatinalmaMalKabul or SatinalmaIade or SatinalmaTedarikciler
                or YonetimGecmis
                => OnaySonrasiRoute(rol),
            _ => route
        };
    }

    /// <summary>Talep Pro sol menü: GENEL / TALEP SÜRECİ / OPERASYON.</summary>
    private static IReadOnlyList<MenuGrubu> MasaustuMenuGruplariniDuzenle(
        string? rol, IReadOnlyList<MenuGrubu> ham)
    {
        var key = KullaniciRolleri.Normalize(rol);
        if (key is not ("admin" or "satinalma"))
            return ham;

        var flat = ham.SelectMany(g => g.Ogeler).ToList();
        Oge? Bul(string route) => flat.FirstOrDefault(o => o.Route == route);
        Oge? YenidenAdlandir(string route, string baslik)
        {
            var o = Bul(route);
            return o is null ? null : o with { Baslik = baslik };
        }

        var genel = new List<Oge>();
        if (Bul(SatinalmaPanosu) is { } pano) genel.Add(pano);
        if (YenidenAdlandir(SatinalmaTalep, "Yeni Talep") is { } yeni) genel.Add(yeni);

        var surecRouteSirasi = TalepProRuntime.Aktif
            ? new (string Route, string? Baslik)[]
            {
                (SatinalmaTalepler, null),
                (YonetimGelenTalepler, null),
                (SatinalmaTeklifIstenen, "Teklif İstemi Yapılanlar"),
                (SatinalmaTeklifGirilen, null),
                (YonetimTeklifGirilen, null),
                (SatinalmaKarsilastirma, "Fiyat Karşılaştırma"),
                (SatinalmaOnayGecmisi, "Geçmiş Onaylananlar"),
                (YonetimOnayGecmisi, "Geçmiş Onaylananlar"),
                (YonetimRedVerilen, "Reddedilenler")
            }
            : new (string Route, string? Baslik)[]
            {
                (SatinalmaTalepler, null),
                (YonetimGelenTalepler, null),
                (SatinalmaTeklifIstenen, "Teklif İstemi Yapılanlar"),
                (SatinalmaTeklifGirilen, null),
                (YonetimTeklifGirilen, null),
                (SatinalmaKarsilastirma, "Fiyat Karşılaştırma"),
                (SatinalmaOnaylanan, "Onaylananlar"),
                (YonetimOnaylananTeklifler, "Onaylananlar"),
                (SatinalmaOnayGecmisi, "Geçmiş Onaylananlar"),
                (YonetimOnayGecmisi, "Geçmiş Onaylananlar"),
                (YonetimRedVerilen, "Reddedilenler")
            };

        var surec = new List<Oge>();
        foreach (var (route, baslik) in surecRouteSirasi)
        {
            var o = baslik is null ? Bul(route) : YenidenAdlandir(route, baslik);
            if (o is not null && surec.All(x => x.Route != o.Route))
                surec.Add(o);
        }

        var operasyon = new List<Oge>();
        if (!TalepProRuntime.Aktif)
        {
            foreach (var route in new[] { SatinalmaSiparis, SatinalmaMalKabul, SatinalmaIade, SatinalmaTedarikciler })
            {
                if (Bul(route) is { } o)
                    operasyon.Add(o);
            }
        }

        // Admin yönetim arşivleri süreç sonunda kalsın
        foreach (var o in flat)
        {
            if (surec.Any(x => x.Route == o.Route) || genel.Any(x => x.Route == o.Route)
                || operasyon.Any(x => x.Route == o.Route))
                continue;
            if (TalepProRuntime.Aktif)
            {
                if (o.Route is YonetimTeklifBekleyen)
                    surec.Add(o);
                continue;
            }

            if (o.Route is YonetimDirekOnaylanan or YonetimGecmis or YonetimTeklifBekleyen
                or YonetimOnayGecmisi)
                surec.Add(o);
        }

        var sonuc = new List<MenuGrubu>();
        if (genel.Count > 0) sonuc.Add(new MenuGrubu("Genel", genel));
        if (surec.Count > 0) sonuc.Add(new MenuGrubu("Talep Süreci", surec));
        if (operasyon.Count > 0) sonuc.Add(new MenuGrubu("Operasyon", operasyon));
        return sonuc.Count > 0 ? sonuc : ham;
    }

    public static string Breadcrumb(string route) => route switch
    {
        YonetimTeklifGirilen or SatinalmaKarsilastirma or SatinalmaTeklifIstenen
            or SatinalmaTeklifGirilen or YonetimTeklifBekleyen
            => "Satınalma / Teklif Süreci",
        SatinalmaSiparis or SatinalmaMalKabul or SatinalmaIade or SatinalmaTedarikciler
            => "Satınalma / Operasyon",
        SatinalmaPanosu => "Satınalma / Genel",
        SatinalmaTalep or SatinalmaTalepler => "Satınalma / Genel",
        _ => "Satınalma / Talep Süreci"
    };

    public static bool TalepAcabilir(string? rol) =>
        DesktopRoleTabManager.TalepFormuAcabilir(rol);

    public static string IlkRoute(string? rol) =>
        DesktopRoleTabManager.IlkRoute(rol);
    public static bool PanosuRoute(string route) => route == SatinalmaPanosu;

    public static bool StokRoute(string route) => route == "stok-durum";

    public static bool TalepFormuRoute(string route) =>
        route == SatinalmaTalep;

    public static bool ListeRoute(string route) =>
        route is SatinalmaTalepler
            or YonetimGelenTalepler
            or YonetimTeklifBekleyen
            or YonetimTeklifGirilen
            or YonetimDirekOnaylanan
            or YonetimRedVerilen
            or YonetimGecmis
            or SatinalmaTeklifIstenen
            or SatinalmaTeklifGirilen
            or SatinalmaTeklifDuzeltme
            or SatinalmaKarsilastirma
            or SatinalmaOnayBekleyen
            or SatinalmaOnaylananTalepler;

    public static bool OnaylananListeRoute(string route) =>
        route is SatinalmaOnaylanan or YonetimOnaylananTeklifler or YonetimOnayGecmisi or SatinalmaOnayGecmisi;

    public static bool OnayGecmisiRoute(string route) =>
        route is YonetimOnayGecmisi or SatinalmaOnayGecmisi;

    public static bool SiparisListeRoute(string route) =>
        route is SatinalmaSiparis;

    public static bool MalKabulListeRoute(string route) =>
        route is SatinalmaMalKabul;

    public static bool IadeRoute(string route) => route == SatinalmaIade;

    public static bool TedarikciRoute(string route) => route == SatinalmaTedarikciler;

    public static bool TeklifGirisRoute(string route) =>
        route is SatinalmaTeklifIstenen or SatinalmaTeklifGirilen or SatinalmaTeklifDuzeltme or SatinalmaKarsilastirma;

    public static bool YonetimTeklifIncelemeRoute(string route) =>
        route is YonetimTeklifGirilen;

    public static bool YonetimArsivListeRoute(string route) =>
        route is YonetimDirekOnaylanan or YonetimRedVerilen or YonetimGecmis;

    public static YonetimTalepDetayModu YonetimDetayModu(string route) => route switch
    {
        YonetimDirekOnaylanan => YonetimTalepDetayModu.DirekOnaylanan,
        YonetimRedVerilen => YonetimTalepDetayModu.Reddedildi,
        _ => YonetimTalepDetayModu.Gecmis
    };

    public static (string baslik, string aciklama) Baslik(string route) => route switch
    {
        YonetimGelenTalepler => ("Gelen Talepler", "Onaya gönderilen talepler"),
        YonetimTeklifBekleyen => ("Teklif Bekleyen Talepler", "Satınalmadan teklif beklenen talepler"),
        YonetimTeklifGirilen => ("Teklif İnceleme & Onay", "Teklifleri karşılaştırın, değerlendirin ve onaylayın."),
        YonetimOnaylananTeklifler => ("Onaylanan Teklifler", "Yönetim tarafından onaylanmış teklifli talepler"),
        YonetimOnayGecmisi => ("Yönetim Onay Geçmişi", "Teklifsiz ve teklifli tüm yönetim onayları — arşiv ve PDF"),
        YonetimDirekOnaylanan => ("Direk Onaylanan Talepler", "Teklif süreci olmadan onaylanan talepler"),
        YonetimRedVerilen => ("Red Verilen Talepler", "Yönetim tarafından reddedilen talepler"),
        YonetimGecmis => ("Talep ve Onaylanan Teklifler Geçmişi", "Tamamlanan talep ve teklif geçmişi"),

        SatinalmaTalep => ("Talep", "Malzeme talebi oluşturun"),
        SatinalmaPanosu => ("Satınalma Panosu", "Satınalma performansını, bekleyen işleri ve hızlı işlemleri tek ekranda görün."),
        SatinalmaTalepler => ("Talepler", "Oluşturduğunuz talepler"),
        SatinalmaTeklifIstenen => ("Teklif İstenen Talepler", "Yönetim teklif istedi — tek teklif ile de yönetime gönderebilirsiniz"),
        SatinalmaTeklifGirilen => ("Teklif Girişi Bekleyenler", "Teklif girişi yapılacak talepler"),
        SatinalmaTeklifDuzeltme => ("Düzeltme Bekleyen Teklifler", "Yönetimden geri gönderilen teklifler — düzeltip yeniden gönderin"),
        SatinalmaKarsilastirma => ("Karşılaştırma", "Teklif karşılaştırma ve seçim"),
        SatinalmaOnaylanan => ("Onaylanan Teklifler ve Talepler", "Onaylanmış talep ve teklifler — sipariş bekleyen"),
        SatinalmaOnayGecmisi => TalepProRuntime.Aktif
            ? ("Geçmiş Onaylananlar", "Onaylanmış talep ve teklifler — kalıcı arşiv ve PDF")
            : ("Geçmiş Onaylananlar", "Tüm onaylı talep ve teklifler — sipariş/mal kabul sonrası kalıcı arşiv, PDF ve firma teklif geçmişi"),
        SatinalmaSiparis => ("Sipariş Verilen Talep ve Teklifler", "Sipariş oluşturulmuş talepler"),
        SatinalmaMalKabul => ("Mal Kabul Edilmiş Talep ve Teklifler", "Mal kabulü tamamlanan talepler"),
        SatinalmaOnayBekleyen => ("Onay Bekleyen", "Yönetim onayı bekleyen talepleriniz"),
        SatinalmaOnaylananTalepler => ("Onaylanan Talepler", "Onaylanmış, siparişe dönüşmemiş talepleriniz"),
        SatinalmaIade => ("İade", "İade kayıtları ve takibi"),
        SatinalmaTedarikciler => ("Tedarikçiler", "Tedarikçi performans ve değerlendirme"),
        "stok-durum" => ("Güncel Stok Durumu", "Depodaki malzeme miktarları — salt okunur"),

        _ => (route, "")
    };

    private static bool TalepEdenMenusuGoster(string rol) => false;}
