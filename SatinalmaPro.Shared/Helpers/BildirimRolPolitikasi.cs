using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>
/// Rol × bildirim tipi matrisi — masaüstü, mobil ve Android aynı kuralları kullanır.
/// Talep/teklif onay bildirimleri: Yönetim + Satınalma (aynı karar yetkisi).
/// </summary>
public static class BildirimRolPolitikasi
{
    public static bool IslemYapanKendisiMi(BildirimKaydi bildirim, KullaniciProfili? kullanici)
    {
        if (kullanici is null || string.IsNullOrWhiteSpace(kullanici.Uid))
            return false;
        if (!string.IsNullOrWhiteSpace(bildirim.OlusturanUid)
            && string.Equals(bildirim.OlusturanUid, kullanici.Uid, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>WhatsApp/Instagram kuralı: işlemi yapan kişiye kayıt/push oluşturulmaz.</summary>
    public static bool KayitGonderilmeli(string? hedefRol, string? hedefUid, string? islemYapanUid)
    {
        if (string.IsNullOrWhiteSpace(islemYapanUid))
            return !string.IsNullOrWhiteSpace(hedefRol) || !string.IsNullOrWhiteSpace(hedefUid);

        if (!string.IsNullOrWhiteSpace(hedefUid)
            && string.Equals(hedefUid, islemYapanUid, StringComparison.OrdinalIgnoreCase))
            return false;

        return !string.IsNullOrWhiteSpace(hedefRol) || !string.IsNullOrWhiteSpace(hedefUid);
    }

    public static bool KayitGonderilmeli(BildirimKaydi bildirim) =>
        KayitGonderilmeli(bildirim.HedefRol, bildirim.HedefUid, bildirim.OlusturanUid);

    /// <summary>Admin tüm akış bildirimlerini görür.</summary>
    public static bool RolTipGorebilirMi(string? rol, string tip)
    {
        if (KullaniciRolleri.AdminMi(rol))
            return true;

        var r = KullaniciRolleri.Normalize(rol);
        tip = NormalizeTip(tip);

        return tip switch
        {
            BildirimTipleri.YonetimeGonderildi =>
                r is KullaniciRolleri.Yonetim or KullaniciRolleri.Satinalma,
            BildirimTipleri.TeklifOnayda =>
                r is KullaniciRolleri.Yonetim or KullaniciRolleri.Satinalma,
            BildirimTipleri.TeklifIstendi or BildirimTipleri.TeklifDuzeltmeIstendi =>
                r == KullaniciRolleri.Satinalma,
            BildirimTipleri.Onaylandi =>
                r == KullaniciRolleri.Satinalma,
            BildirimTipleri.Reddedildi =>
                r == KullaniciRolleri.Satinalma,
            BildirimTipleri.SiparisOlusturuldu =>
                r is KullaniciRolleri.Satinalma or KullaniciRolleri.Depo,
            BildirimTipleri.MalKabulEdildi =>
                r == KullaniciRolleri.Satinalma,
            _ => false
        };
    }

    public static bool KullaniciyaMi(BildirimKaydi bildirim, KullaniciProfili? kullanici)
    {
        if (kullanici is null)
            return false;

        // İşlemi yapan kişi kendi aksiyonunun bildirimini asla almaz.
        if (IslemYapanKendisiMi(bildirim, kullanici))
            return false;

        if (!string.IsNullOrWhiteSpace(bildirim.HedefUid))
            return bildirim.HedefUid == kullanici.Uid;

        if (!string.IsNullOrWhiteSpace(bildirim.InboxDocId))
            return RolTipGorebilirMi(kullanici.Rol, bildirim.Tip);

        if (KullaniciRolleri.AdminMi(kullanici.Rol))
            return true;

        if (!string.IsNullOrWhiteSpace(bildirim.HedefRol))
        {
            var rol = KullaniciRolleri.Normalize(kullanici.Rol);
            return KullaniciRolleri.Normalize(bildirim.HedefRol) == rol
                && RolTipGorebilirMi(rol, bildirim.Tip);
        }

        return false;
    }

    public static IEnumerable<(string? HedefRol, string? HedefUid)> YonetimeGonderildiHedefleri() =>
    [
        (KullaniciRolleri.Yonetim, null),
        (KullaniciRolleri.Satinalma, null)
    ];

    public static IEnumerable<(string? HedefRol, string? HedefUid)> TeklifOnaydaHedefleri() =>
    [
        (KullaniciRolleri.Yonetim, null),
        (KullaniciRolleri.Satinalma, null)
    ];

    public static IEnumerable<(string? HedefRol, string? HedefUid)> ReddedildiHedefleri(
        string? talepOlusturanUid,
        string? islemYapanUid)
    {
        yield return (KullaniciRolleri.Satinalma, null);
        if (!string.IsNullOrWhiteSpace(talepOlusturanUid)
            && !string.Equals(talepOlusturanUid, islemYapanUid, StringComparison.OrdinalIgnoreCase))
            yield return (null, talepOlusturanUid);
    }

    public static IEnumerable<(string? HedefRol, string? HedefUid)> SiparisOlusturulduHedefleri(
        string? talepOlusturanUid,
        string? islemYapanUid)
    {
        yield return (KullaniciRolleri.Satinalma, null);
        yield return (KullaniciRolleri.Depo, null);
        if (!string.IsNullOrWhiteSpace(talepOlusturanUid)
            && !string.Equals(talepOlusturanUid, islemYapanUid, StringComparison.OrdinalIgnoreCase))
            yield return (null, talepOlusturanUid);
    }

    public static IEnumerable<(string? HedefRol, string? HedefUid)> MalKabulEdildiHedefleri(
        string? talepOlusturanUid,
        string? islemYapanUid)
    {
        yield return (KullaniciRolleri.Satinalma, null);
        if (!string.IsNullOrWhiteSpace(talepOlusturanUid)
            && !string.Equals(talepOlusturanUid, islemYapanUid, StringComparison.OrdinalIgnoreCase))
            yield return (null, talepOlusturanUid);
    }

    public static string NormalizeTip(string tip)
    {
        var t = tip.Trim().ToLowerInvariant();
        // Event kodları (talep.olusturuldu …)
        t = t switch
        {
            "talep.yonetime_gonderildi" or "talep.olusturuldu"
                or "talep.sla_yaklasiyor" or "talep.sla_asildi" => BildirimTipleri.YonetimeGonderildi,
            "teklif.istendi" => BildirimTipleri.TeklifIstendi,
            "teklif.yonetime_gonderildi" => BildirimTipleri.TeklifOnayda,
            "teklif.duzeltme_istendi" => BildirimTipleri.TeklifDuzeltmeIstendi,
            "talep.onaylandi" => BildirimTipleri.Onaylandi,
            "talep.reddedildi" => BildirimTipleri.Reddedildi,
            "siparis.olusturuldu" => BildirimTipleri.SiparisOlusturuldu,
            "depo.mal_kabul_yapildi" => BildirimTipleri.MalKabulEdildi,
            _ => t
        };

        return t switch
        {
            "yonetimegonderildi" or "yonetime_gonderildi" => BildirimTipleri.YonetimeGonderildi,
            "teklifistendi" or "teklif_istendi" => BildirimTipleri.TeklifIstendi,
            "teklifonayda" or "teklif_onayda" => BildirimTipleri.TeklifOnayda,
            "teklifduzeltmeistendi" or "teklif_duzeltme_istendi" => BildirimTipleri.TeklifDuzeltmeIstendi,
            "onaylandi" => BildirimTipleri.Onaylandi,
            "reddedildi" => BildirimTipleri.Reddedildi,
            "siparisolusturuldu" or "siparis_olusturuldu" => BildirimTipleri.SiparisOlusturuldu,
            "malkabuledildi" or "mal_kabul_edildi" => BildirimTipleri.MalKabulEdildi,
            _ => t
        };
    }
}
