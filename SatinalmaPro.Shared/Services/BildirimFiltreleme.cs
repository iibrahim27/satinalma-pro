using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public static class BildirimFiltreleme
{
    public static bool KullaniciyaMi(BildirimKaydi bildirim, KullaniciProfili? kullanici)
    {
        if (kullanici is null)
            return false;

        if (!string.IsNullOrWhiteSpace(bildirim.HedefUid))
            return bildirim.HedefUid == kullanici.Uid;

        if (!string.IsNullOrWhiteSpace(bildirim.HedefRol))
        {
            var rol = KullaniciRolleri.Normalize(kullanici.Rol);
            return KullaniciRolleri.Normalize(bildirim.HedefRol) == rol;
        }

        return KullaniciRolleri.AdminMi(kullanici.Rol);
    }

    public static IEnumerable<BildirimKaydi> KullaniciBildirimleri(
        IEnumerable<BildirimKaydi> kaynak,
        KullaniciProfili? kullanici) =>
        kaynak.Where(b => KullaniciyaMi(b, kullanici));

    /// <summary>
    /// İşlem tamamlandıysa veya talep artık bu bildirimi gerektirmiyorsa geçersiz sayılır.
    /// </summary>
    public static bool GecerliMi(BildirimKaydi bildirim, IEnumerable<SatinalmaTalep> talepler)
    {
        if (bildirim.TalepId is not { } tid)
            return true;

        var talep = talepler.FirstOrDefault(t => t.Id == tid);
        if (talep is null)
            return false;

        return bildirim.Tip switch
        {
            BildirimTipleri.YonetimeGonderildi =>
                (talep.Teklifler?.Count ?? 0) == 0
                && talep.Durum is SatinalmaTalepDurumlari.ImzaSurecinde
                    or SatinalmaTalepDurumlari.YonetimOnayinda,
            BildirimTipleri.TeklifIstendi =>
                talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi
                && (talep.Teklifler?.Count ?? 0) == 0,
            BildirimTipleri.TeklifDuzeltmeIstendi =>
                SatinalmaTalepYardimcisi.TeklifDuzenlemeDevamEdiyor(talep),
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
        if (bildirim.Tip != BildirimTipleri.TeklifOnayda)
            return false;

        if (bildirim.TalepId is not { } tid)
            return true;

        var talep = talepler.FirstOrDefault(t => t.Id == tid);
        return talep is not null && SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep);
    }
}
