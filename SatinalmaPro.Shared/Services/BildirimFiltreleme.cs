using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public static class BildirimFiltreleme
{
    public static bool KendiIslemindenMi(BildirimKaydi bildirim, KullaniciProfili? kullanici) =>
        kullanici is not null
        && !string.IsNullOrWhiteSpace(bildirim.OlusturanUid)
        && bildirim.OlusturanUid == kullanici.Uid
        && (string.IsNullOrWhiteSpace(bildirim.HedefUid) || bildirim.HedefUid != kullanici.Uid);

    public static bool KullaniciyaMi(BildirimKaydi bildirim, KullaniciProfili? kullanici)
    {
        if (kullanici is null)
            return false;

        if (!string.IsNullOrWhiteSpace(bildirim.HedefUid))
            return bildirim.HedefUid == kullanici.Uid;

        if (KendiIslemindenMi(bildirim, kullanici))
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

    public static IEnumerable<BildirimKaydi> KullaniciBildirimleri(
        IEnumerable<BildirimKaydi> kaynak,
        KullaniciProfili? kullanici) =>
        kaynak.Where(b => KullaniciyaMi(b, kullanici));

  public static bool TalepBaglantiliMi(string tip) =>
        NormalizeTip(tip) is BildirimTipleri.YonetimeGonderildi
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

    /// <summary>
    /// İşlem tamamlandıysa veya talep artık bu bildirimi gerektirmiyorsa geçersiz sayılır.
    /// </summary>
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
            _ => true
        };
    }

    public static int OkunmamisSayisi(
        IEnumerable<BildirimKaydi> kaynak,
        KullaniciProfili? kullanici,
        IEnumerable<SatinalmaTalep> talepler) =>
        KullaniciBildirimleri(kaynak, kullanici)
            .Count(b => !b.Okundu && GecerliMi(b, talepler));

    public static bool ToastGosterilmeli(
        BildirimKaydi bildirim,
        KullaniciProfili? kullanici,
        IEnumerable<SatinalmaTalep> talepler) =>
        KullaniciyaMi(bildirim, kullanici) &&
        !bildirim.Okundu &&
        GecerliMi(bildirim, talepler) &&
        bildirim.OlusturanUid != kullanici?.Uid;

    /// <summary>
    /// Onay bekleyen teklif bildirimleri temizleme sırasında korunur.
    /// </summary>
    public static bool Temizlenmemeli(BildirimKaydi bildirim, IEnumerable<SatinalmaTalep> talepler)
    {
        if (NormalizeTip(bildirim.Tip) != BildirimTipleri.TeklifOnayda)
            return false;

        if (bildirim.TalepId is not { } tid)
            return false;

        var talep = talepler.FirstOrDefault(t => t.Id == tid);
        return talep is not null && SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep);
    }
}
