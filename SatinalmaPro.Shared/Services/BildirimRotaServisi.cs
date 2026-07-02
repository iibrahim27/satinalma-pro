using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public record MasaustuBildirimHedef(string Modul, string Sekme, int Adim, Guid? TalepId);

public static class BildirimRotaServisi
{
    public static string HedefRoute(BildirimKaydi bildirim, string? kullaniciRol = null)
    {
        var rol = KullaniciRolleri.Normalize(kullaniciRol);

        if (bildirim.TalepId is { } id && bildirim.Tip is BildirimTipleri.Reddedildi)
            return rol == KullaniciRolleri.Yonetim ? "red-talepler" : $"talep-detay?id={id}";

        return bildirim.Tip switch
        {
            BildirimTipleri.YonetimeGonderildi => "gelen-talepler",
            BildirimTipleri.TeklifIstendi => rol == KullaniciRolleri.Yonetim
                ? "gelen-talepler"
                : "teklif-gir",
            BildirimTipleri.TeklifDuzeltmeIstendi => "teklif-gir",
            BildirimTipleri.TeklifOnayda when bildirim.TalepId is { } tid => $"teklif-onay-detay?id={tid}",
            BildirimTipleri.TeklifOnayda => "teklif-onay",
            BildirimTipleri.Onaylandi when bildirim.TalepId is { } tid =>
                rol switch
                {
                    KullaniciRolleri.Satinalma => "onaylanan-malzemeler",
                    KullaniciRolleri.Yonetim => "gecmis-talepler",
                    _ => $"talep-detay?id={tid}"
                },
            BildirimTipleri.Onaylandi => rol == KullaniciRolleri.Yonetim
                ? "gecmis-talepler"
                : "bildirimler",
            BildirimTipleri.SiparisOlusturuldu => "onaylanan-malzemeler",
            BildirimTipleri.MalKabulEdildi => rol switch
            {
                KullaniciRolleri.Depo => "stok-durum",
                _ => "onaylanan-malzemeler"
            },
            _ => bildirim.TalepId is { } t ? $"talep-detay?id={t}" : "bildirimler"
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
            BildirimTipleri.TeklifOnayda => new("Satınalma", "teklif-onay", 0, bildirim.TalepId),
            BildirimTipleri.TeklifIstendi => new("Satınalma", "teklif-giris", 0, bildirim.TalepId),
            BildirimTipleri.TeklifDuzeltmeIstendi => new("Satınalma", "teklif-giris", 0, bildirim.TalepId),
            BildirimTipleri.Onaylandi => rol switch
            {
                KullaniciRolleri.Satinalma => new("Satınalma", "alinan-malzemeler", 0, bildirim.TalepId),
                KullaniciRolleri.Yonetim => new("Satınalma", "gecmis-talepler", 0, bildirim.TalepId),
                _ => new("Satınalma", "talepler", 0, bildirim.TalepId)
            },
            BildirimTipleri.YonetimeGonderildi => new("Satınalma", "gelen-talepler", 0, bildirim.TalepId),
            BildirimTipleri.Reddedildi => new("Satınalma", "red-talepler", 0, bildirim.TalepId),
            BildirimTipleri.SiparisOlusturuldu => new("Satınalma", "alinan-malzemeler", 0, bildirim.TalepId),
            BildirimTipleri.MalKabulEdildi => rol == KullaniciRolleri.Depo
                ? new("Stok", "stok", 0, bildirim.TalepId)
                : new("Satınalma", "alinan-malzemeler", 0, bildirim.TalepId),
            _ => new("Satınalma", "talepler", 0, bildirim.TalepId)
        };
    }

    public static Dictionary<string, string> FcmVeri(BildirimKaydi bildirim, string? kullaniciRol = null) =>
        new()
        {
            ["bildirimId"] = bildirim.Id.ToString(),
            ["talepId"] = bildirim.TalepId?.ToString() ?? "",
            ["tip"] = bildirim.Tip,
            ["route"] = HedefRoute(bildirim, kullaniciRol),
            ["title"] = bildirim.Baslik,
            ["body"] = bildirim.Mesaj
        };
}
