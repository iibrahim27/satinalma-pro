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

        if (!string.IsNullOrWhiteSpace(bildirim.HedefRol))
        {
            var rol = KullaniciRolleri.Normalize(kullanici.Rol);
            return KullaniciRolleri.Normalize(bildirim.HedefRol) == rol;
        }

        return KullaniciRolleri.AdminMi(kullanici.Rol);
    }

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
            BildirimTipleri.TeklifOnayda =>
                SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep),
            BildirimTipleri.Reddedildi =>
                talep.Durum == SatinalmaTalepDurumlari.Reddedildi,
            _ => true
        };
    }

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

    public static bool Temizlenmemeli(BildirimKaydi bildirim, IEnumerable<SatinalmaTalep> talepler) =>
        bildirim.Tip == BildirimTipleri.TeklifOnayda
        && bildirim.TalepId is { } tid
        && talepler.FirstOrDefault(t => t.Id == tid) is { } talep
        && SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep);

    public static string ModulAdi(BildirimKaydi bildirim) =>
        bildirim.Tip switch
        {
            BildirimTipleri.YonetimeGonderildi or BildirimTipleri.TeklifIstendi
                or BildirimTipleri.TeklifOnayda or BildirimTipleri.Onaylandi
                or BildirimTipleri.Reddedildi => "Satınalma",
            _ => "Satınalma"
        };
}
