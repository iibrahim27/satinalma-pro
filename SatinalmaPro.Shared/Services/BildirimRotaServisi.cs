using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public record MasaustuBildirimHedef(string Modul, string Sekme, int Adim, Guid? TalepId);

public static class BildirimRotaServisi
{
    public static string HedefRoute(BildirimKaydi bildirim, string? kullaniciRol = null)
    {
        var rol = KullaniciRolleri.Normalize(kullaniciRol);
        var tid = bildirim.TalepId;

        if (tid is { } redId && bildirim.Tip is BildirimTipleri.Reddedildi)
            return rol == KullaniciRolleri.Yonetim ? "red-talepler" : $"talep-detay?id={redId}";

        return bildirim.Tip switch
        {
            BildirimTipleri.YonetimeGonderildi => "gelen-talepler",
            BildirimTipleri.TeklifIstendi => rol == KullaniciRolleri.Yonetim
                ? "gelen-talepler"
                : tid is { } teklifTid ? $"teklif-gir?id={teklifTid}" : "satinalma-teklif-istenen",
            BildirimTipleri.TeklifDuzeltmeIstendi => tid is { } duzTid
                ? $"satinalma-teklif-duzeltme?id={duzTid}"
                : "satinalma-teklif-duzeltme",
            BildirimTipleri.TeklifOnayda when tid is { } onayTid => $"teklif-onay-detay?id={onayTid}",
            BildirimTipleri.TeklifOnayda => "yonetim-teklif-girilen",
            BildirimTipleri.Onaylandi when tid is null && rol == KullaniciRolleri.Yonetim => "gecmis-talepler",
            BildirimTipleri.Onaylandi when tid is null && rol == KullaniciRolleri.Satinalma => "satinalma-onaylanan",
            BildirimTipleri.Onaylandi when tid is null => "bildirimler",
            BildirimTipleri.Onaylandi when rol == KullaniciRolleri.Satinalma => $"talep-detay?id={tid}&view=onaylanan",
            BildirimTipleri.Onaylandi when rol == KullaniciRolleri.Yonetim => $"talep-detay?id={tid}",
            BildirimTipleri.Onaylandi when tid is { } onayTid => $"talep-detay?id={onayTid}",
            BildirimTipleri.SiparisOlusturuldu when tid is not null && rol is KullaniciRolleri.Satinalma or KullaniciRolleri.Admin
                => $"talep-detay?id={tid}&view=siparis",
            BildirimTipleri.SiparisOlusturuldu when tid is not null
                => $"talep-detay?id={tid}&view=siparis",
            BildirimTipleri.SiparisOlusturuldu when rol is KullaniciRolleri.Satinalma or KullaniciRolleri.Admin
                => "satinalma-siparis",
            BildirimTipleri.SiparisOlusturuldu => "onaylanan-malzemeler?section=siparis",
            BildirimTipleri.MalKabulEdildi when rol == KullaniciRolleri.Depo => "stok-durum",
            BildirimTipleri.MalKabulEdildi when tid is not null && rol is KullaniciRolleri.Satinalma or KullaniciRolleri.Admin
                => $"talep-detay?id={tid}&view=malkabul",
            BildirimTipleri.MalKabulEdildi when tid is not null
                => $"talep-detay?id={tid}&view=malkabul",
            BildirimTipleri.MalKabulEdildi when rol is KullaniciRolleri.Satinalma or KullaniciRolleri.Admin
                => "satinalma-mal-kabul",
            BildirimTipleri.MalKabulEdildi => "onaylanan-malzemeler?section=malkabul",
            _ => tid is { } t ? $"talep-detay?id={t}" : "bildirimler"
        };
    }

    public static MasaustuBildirimHedef MasaustuHedef(BildirimKaydi bildirim, string? kullaniciRol = null)
    {
        var rol = KullaniciRolleri.Normalize(kullaniciRol);

        if (bildirim.TalepId is { } redId && bildirim.Tip is BildirimTipleri.Reddedildi)
            return rol == KullaniciRolleri.Yonetim
                ? new("Satınalma", "red-talepler", 0, redId)
                : new("Satınalma", "talepler", 0, redId);

        return bildirim.Tip switch
        {
            BildirimTipleri.TeklifOnayda => new("Satınalma", "teklif-onay-pencere", 0, bildirim.TalepId),
            BildirimTipleri.TeklifIstendi => rol switch
            {
                KullaniciRolleri.Yonetim => new("Satınalma", "gelen-talepler", 0, bildirim.TalepId),
                _ => new("Satınalma", "satinalma-teklif-istenen", 0, bildirim.TalepId)
            },
            BildirimTipleri.TeklifDuzeltmeIstendi => new("Satınalma", "satinalma-teklif-duzeltme", 0, bildirim.TalepId),
            BildirimTipleri.Onaylandi => rol switch
            {
                KullaniciRolleri.Satinalma => new("Satınalma", "satinalma-onaylanan", 0, bildirim.TalepId),
                KullaniciRolleri.Yonetim => new("Satınalma", "yonetim-gecmis", 0, bildirim.TalepId),
                _ => new("Satınalma", "talepler", 0, bildirim.TalepId)
            },
            BildirimTipleri.YonetimeGonderildi => new("Satınalma", "gelen-talepler", 0, bildirim.TalepId),
            BildirimTipleri.Reddedildi => new("Satınalma", "red-talepler", 0, bildirim.TalepId),
            BildirimTipleri.SiparisOlusturuldu => new("Satınalma", "satinalma-siparis", 0, bildirim.TalepId),
            BildirimTipleri.MalKabulEdildi => rol == KullaniciRolleri.Depo
                ? new("Stok Yönetimi", "stok-durum", 0, bildirim.TalepId)
                : new("Satınalma", "satinalma-mal-kabul", 0, bildirim.TalepId),
            _ => new("Satınalma", "talepler", 0, bildirim.TalepId)
        };
    }

    public static Dictionary<string, string> FcmVeri(BildirimKaydi bildirim, string? kullaniciRol = null)
    {
        var route = HedefRoute(bildirim, kullaniciRol);
        var masaustu = MasaustuHedef(bildirim, kullaniciRol);
        var deepLink = bildirim.DeepLink;
        if (string.IsNullOrWhiteSpace(deepLink) && bildirim.TalepId is { } tid)
        {
            var ekran = route.Split('?')[0];
            var view = route.Split('&')
                .Select(p => p.Split('='))
                .FirstOrDefault(p => p.Length == 2 && p[0] == "view")?[1];
            deepLink = DeepLinkServisi.Olustur(new DeepLinkParametreleri(
                "satinalma", ekran, "view", "procurement_request", tid.ToString(),
                Tab: view,
                EventCode: bildirim.EventCode));
        }

        return new Dictionary<string, string>
        {
            ["bildirimId"] = bildirim.Id.ToString(),
            ["talepId"] = bildirim.TalepId?.ToString() ?? "",
            ["tip"] = bildirim.Tip,
            ["route"] = route,
            ["title"] = bildirim.Baslik,
            ["body"] = bildirim.Mesaj,
            ["module"] = "satinalma",
            ["screen"] = route.Split('?')[0],
            ["action"] = "view",
            ["deepLink"] = deepLink ?? "",
            ["desktopRoute"] = bildirim.DesktopRoute ?? $"{masaustu.Modul}|{masaustu.Sekme}|{masaustu.TalepId}",
            ["eventCode"] = bildirim.EventCode ?? bildirim.Tip
        };
    }
}
