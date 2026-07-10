using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

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
        return DesktopRoleTabManager.GetSatinalmaMenuGroups(rol, uid)
            .Select(g => new MenuGrubu(g.Baslik, g.Ogeler.Select(i => new Oge(i.Baslik, i.Route)).ToList()))
            .ToList();
    }

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
        YonetimTeklifGirilen => ("Teklif İnceleme & Onay", "Teklif girilmiş — onay / red / revize bekleyen talepler"),
        YonetimOnaylananTeklifler => ("Onaylanan Teklifler", "Yönetim tarafından onaylanmış teklifli talepler"),
        YonetimOnayGecmisi => ("Yönetim Onay Geçmişi", "Teklifsiz ve teklifli tüm yönetim onayları — arşiv ve PDF"),
        YonetimDirekOnaylanan => ("Direk Onaylanan Talepler", "Teklif süreci olmadan onaylanan talepler"),
        YonetimRedVerilen => ("Red Verilen Talepler", "Yönetim tarafından reddedilen talepler"),
        YonetimGecmis => ("Talep ve Onaylanan Teklifler Geçmişi", "Tamamlanan talep ve teklif geçmişi"),

        SatinalmaTalep => ("Talep", "Malzeme talebi oluşturun"),
        SatinalmaPanosu => ("Satınalma Panosu", "Satınalma alımları ve sevkiyat kayıtları"),
        SatinalmaTalepler => ("Talepler", "Oluşturduğunuz talepler"),
        SatinalmaTeklifIstenen => ("Teklif İstenen Talepler", "Yönetim teklif istedi — tek teklif ile de yönetime gönderebilirsiniz"),
        SatinalmaTeklifGirilen => ("Teklif Girişi Bekleyenler", "Teklif girişi yapılacak talepler"),
        SatinalmaTeklifDuzeltme => ("Düzeltme Bekleyen Teklifler", "Yönetimden geri gönderilen teklifler — düzeltip yeniden gönderin"),
        SatinalmaKarsilastirma => ("Karşılaştırma", "Teklif karşılaştırma ve seçim"),
        SatinalmaOnaylanan => ("Onaylanan Teklifler ve Talepler", "Onaylanmış talep ve teklifler — sipariş bekleyen"),
        SatinalmaOnayGecmisi => ("Geçmiş Onaylananlar", "Tüm onaylı talep ve teklifler — sipariş/mal kabul sonrası kalıcı arşiv, PDF ve firma teklif geçmişi"),
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
