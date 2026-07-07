using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class MasaustuBildirimFiltreleme
{
    public static bool KullaniciyaMi(BildirimKaydi bildirim, KullaniciProfili? kullanici)
    {
        if (kullanici is null)
            return false;

        if (!string.IsNullOrWhiteSpace(bildirim.HedefUid))
            return bildirim.HedefUid == kullanici.Uid;

        if (!string.IsNullOrWhiteSpace(bildirim.OlusturanUid)
            && bildirim.OlusturanUid == kullanici.Uid
            && string.IsNullOrWhiteSpace(bildirim.HedefUid))
            return false;

        if (!string.IsNullOrWhiteSpace(bildirim.InboxDocId))
            return true;

        if (KullaniciRolleri.AdminMi(kullanici.Rol))
        {
            return !string.IsNullOrWhiteSpace(bildirim.HedefRol);
        }

        if (!string.IsNullOrWhiteSpace(bildirim.HedefRol))
        {
            var rol = KullaniciRolleri.Normalize(kullanici.Rol);
            return KullaniciRolleri.Normalize(bildirim.HedefRol) == rol;
        }

        return false;
    }

    public static bool GecerliMi(BildirimKaydi bildirim, IEnumerable<SatinalmaTalep> talepler)
    {
        var tip = NormalizeTip(bildirim.Tip);
        if (!TalepBaglantiliMi(tip))
        {
            if (string.IsNullOrWhiteSpace(tip) && bildirim.TalepId is null)
                return false;
            return bildirim.TalepId is null;
        }

        if (bildirim.TalepId is not { } tid)
            return false;

        var talep = talepler.FirstOrDefault(t => t.Id == tid);
        if (talep is null)
            return false;

        return tip switch
        {
            BildirimTipleri.YonetimeGonderildi =>
                SatinalmaTalepKuyrugu.YonetimTalepler(talep)
                || talep.Durum == SatinalmaTalepDurumlari.Hazirlaniyor,
            BildirimTipleri.TeklifIstendi =>
                !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep)
                && !talep.YonetimOnayKilitli
                && (SatinalmaTalepKuyrugu.SatinalmaTeklifGirisiAktif(talep)
                    || (talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi
                        && (talep.Teklifler?.Count ?? 0) == 0)),
            BildirimTipleri.TeklifDuzeltmeIstendi =>
                SatinalmaTalepYardimcisi.TeklifDuzenlemeDevamEdiyor(talep)
                && talep.Durum != SatinalmaTalepDurumlari.YonetimOnayinda,
            BildirimTipleri.TeklifOnayda =>
                SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep),
            BildirimTipleri.Reddedildi =>
                talep.Durum == SatinalmaTalepDurumlari.Reddedildi,
            BildirimTipleri.Onaylandi =>
                talep.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu,
            BildirimTipleri.SiparisOlusturuldu =>
                talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu,
            BildirimTipleri.MalKabulEdildi => true,
            _ => false
        };
    }

    private static bool TalepBaglantiliMi(string tip) =>
        tip is BildirimTipleri.YonetimeGonderildi
            or BildirimTipleri.TeklifIstendi
            or BildirimTipleri.TeklifOnayda
            or BildirimTipleri.TeklifDuzeltmeIstendi
            or BildirimTipleri.Onaylandi
            or BildirimTipleri.Reddedildi
            or BildirimTipleri.SiparisOlusturuldu
            or BildirimTipleri.MalKabulEdildi;

    private static string NormalizeTip(string tip) => tip.Trim().ToLowerInvariant() switch
    {
        "yonetimegonderildi" => BildirimTipleri.YonetimeGonderildi,
        "teklifistendi" => BildirimTipleri.TeklifIstendi,
        "teklifonayda" => BildirimTipleri.TeklifOnayda,
        "teklifduzeltmeistendi" => BildirimTipleri.TeklifDuzeltmeIstendi,
        "onaylandi" => BildirimTipleri.Onaylandi,
        "reddedildi" => BildirimTipleri.Reddedildi,
        "siparisolusturuldu" => BildirimTipleri.SiparisOlusturuldu,
        "malkabuledildi" => BildirimTipleri.MalKabulEdildi,
        _ => tip.Trim().ToLowerInvariant()
    };
    public static int OkunmamisSayisi(
        IEnumerable<BildirimKaydi> kaynak,
        KullaniciProfili? kullanici,
        IEnumerable<SatinalmaTalep> talepler) =>
        kaynak.Count(b => KullaniciyaMi(b, kullanici) && !b.Okundu && GecerliMi(b, talepler));

    public static bool ToastGosterilmeli(
        BildirimKaydi bildirim,
        KullaniciProfili? kullanici,
        IEnumerable<SatinalmaTalep> talepler) =>
        KullaniciyaMi(bildirim, kullanici) &&
        !bildirim.Okundu &&
        GecerliMi(bildirim, talepler) &&
        bildirim.OlusturanUid != kullanici?.Uid;

    public static bool Temizlenmemeli(BildirimKaydi bildirim, IEnumerable<SatinalmaTalep> talepler)
    {
        if (NormalizeTip(bildirim.Tip) != BildirimTipleri.TeklifOnayda)
            return false;

        if (bildirim.TalepId is not { } tid)
            return false;

        var talep = talepler.FirstOrDefault(t => t.Id == tid);
        return talep is not null && SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep);
    }
    public static string ModulAdi(BildirimKaydi bildirim) =>
        bildirim.Tip switch
        {
            BildirimTipleri.YonetimeGonderildi or BildirimTipleri.TeklifIstendi
                or BildirimTipleri.TeklifOnayda or BildirimTipleri.Onaylandi
                or BildirimTipleri.Reddedildi or BildirimTipleri.SiparisOlusturuldu
                or BildirimTipleri.MalKabulEdildi => "Satınalma",
            _ => "Satınalma"
        };
}
