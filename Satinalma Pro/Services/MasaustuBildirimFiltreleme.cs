using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Services;
using DesktopBildirimKaydi = SatinalmaPro.Models.BildirimKaydi;
using DesktopKullaniciProfili = SatinalmaPro.Models.KullaniciProfili;
using DesktopSatinalmaTalep = SatinalmaPro.Models.SatinalmaTalep;
using SharedBildirimKaydi = SatinalmaPro.Shared.Models.BildirimKaydi;

namespace SatinalmaPro.Services;

public static class MasaustuBildirimFiltreleme
{
    public static bool KullaniciyaMi(DesktopBildirimKaydi bildirim, DesktopKullaniciProfili? kullanici) =>
        BildirimFiltreleme.KullaniciyaMi(ToShared(bildirim), ToSharedProfil(kullanici));

    public static bool GecerliMi(DesktopBildirimKaydi bildirim, IEnumerable<DesktopSatinalmaTalep> talepler)
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
            return true;

        return tip switch
        {
            BildirimTipleri.YonetimeGonderildi =>
                SatinalmaTalepKuyrugu.YonetimTalepler(talep)
                || SatinalmaTalepKuyrugu.OnayBekleyen(talep)
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
        BildirimFiltreleme.TalepBaglantiliMi(tip);

    private static string NormalizeTip(string tip) =>
        SatinalmaPro.Shared.Helpers.BildirimRolPolitikasi.NormalizeTip(tip);

    public static int OkunmamisSayisi(
        IEnumerable<DesktopBildirimKaydi> kaynak,
        DesktopKullaniciProfili? kullanici,
        IEnumerable<DesktopSatinalmaTalep> talepler) =>
        kaynak.Count(b => KullaniciyaMi(b, kullanici) && !b.Okundu && GecerliMi(b, talepler));

    public static bool ToastGosterilmeli(
        DesktopBildirimKaydi bildirim,
        DesktopKullaniciProfili? kullanici,
        IEnumerable<DesktopSatinalmaTalep> talepler) =>
        KullaniciyaMi(bildirim, kullanici) &&
        !bildirim.Okundu &&
        GecerliMi(bildirim, talepler);

    public static bool Temizlenmemeli(DesktopBildirimKaydi bildirim, IEnumerable<DesktopSatinalmaTalep> talepler)
    {
        if (NormalizeTip(bildirim.Tip) != BildirimTipleri.TeklifOnayda)
            return false;

        if (bildirim.TalepId is not { } tid)
            return false;

        var talep = talepler.FirstOrDefault(t => t.Id == tid);
        return talep is not null && SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep);
    }

    public static string ModulAdi(DesktopBildirimKaydi bildirim) =>
        bildirim.Tip switch
        {
            BildirimTipleri.YonetimeGonderildi or BildirimTipleri.TeklifIstendi
                or BildirimTipleri.TeklifOnayda or BildirimTipleri.Onaylandi
                or BildirimTipleri.Reddedildi or BildirimTipleri.SiparisOlusturuldu
                or BildirimTipleri.MalKabulEdildi => "Satınalma",
            _ => "Satınalma"
        };

    private static SharedBildirimKaydi ToShared(DesktopBildirimKaydi b) => new()
    {
        Id = b.Id,
        Baslik = b.Baslik,
        Mesaj = b.Mesaj,
        Tip = b.Tip,
        TalepId = b.TalepId,
        HedefRol = b.HedefRol,
        HedefUid = b.HedefUid,
        OlusturanUid = b.OlusturanUid,
        OlusturanAd = b.OlusturanAd,
        OlusturmaTarihi = b.OlusturmaTarihi,
        Okundu = b.Okundu,
        GuncellemeUtc = b.GuncellemeUtc,
        InboxDocId = b.InboxDocId
    };

    private static SatinalmaPro.Shared.Models.KullaniciProfili? ToSharedProfil(DesktopKullaniciProfili? k) =>
        k is null
            ? null
            : new SatinalmaPro.Shared.Models.KullaniciProfili
            {
                Uid = k.Uid,
                AdSoyad = k.AdSoyad,
                Rol = k.Rol
            };
}
