using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Shared.Services;

public static class BildirimFiltreleme
{
    public static bool KendiIslemindenMi(BildirimKaydi bildirim, KullaniciProfili? kullanici) =>
        BildirimRolPolitikasi.IslemYapanKendisiMi(bildirim, kullanici);

    public static bool KullaniciyaMi(BildirimKaydi bildirim, KullaniciProfili? kullanici) =>
        BildirimRolPolitikasi.KullaniciyaMi(bildirim, kullanici);

    public static IEnumerable<BildirimKaydi> KullaniciBildirimleri(
        IEnumerable<BildirimKaydi> kaynak,
        KullaniciProfili? kullanici) =>
        kaynak.Where(b => KullaniciyaMi(b, kullanici));

    public static bool TalepBaglantiliMi(string tip) =>
        BildirimRolPolitikasi.NormalizeTip(tip) is BildirimTipleri.YonetimeGonderildi
            or BildirimTipleri.TeklifIstendi
            or BildirimTipleri.TeklifOnayda
            or BildirimTipleri.TeklifDuzeltmeIstendi
            or BildirimTipleri.Onaylandi
            or BildirimTipleri.Reddedildi
            or BildirimTipleri.SiparisOlusturuldu
            or BildirimTipleri.MalKabulEdildi;

    /// <summary>
    /// İşlem tamamlandıysa veya talep artık bu bildirimi gerektirmiyorsa geçersiz sayılır.
    /// </summary>
    public static bool GecerliMi(BildirimKaydi bildirim, IEnumerable<SatinalmaTalep> talepler)
    {
        var tip = BildirimRolPolitikasi.NormalizeTip(bildirim.Tip);
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

        // Mal kabul/stok tamamlanan talepler için eski bildirimler kalıcı olmasın.
        var tamamlandi = ProcurementStatusResolver.Resolve(talep)
            .Equals(ProcurementStatus.Completed, StringComparison.OrdinalIgnoreCase);

        return tip switch
        {
            BildirimTipleri.YonetimeGonderildi =>
                !tamamlandi
                && (SatinalmaTalepKuyrugu.YonetimTalepler(talep)
                    || SatinalmaTalepKuyrugu.OnayBekleyen(talep)
                    || talep.Durum == SatinalmaTalepDurumlari.Hazirlaniyor),
            BildirimTipleri.TeklifIstendi =>
                !tamamlandi
                && !SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep)
                && !talep.YonetimOnayKilitli
                && (SatinalmaTalepKuyrugu.SatinalmaTeklifGirisiAktif(talep)
                    || (talep.Durum == SatinalmaTalepDurumlari.TeklifGirisi
                        && (talep.Teklifler?.Count ?? 0) == 0)),
            BildirimTipleri.TeklifDuzeltmeIstendi =>
                !tamamlandi
                && SatinalmaTalepYardimcisi.TeklifDuzenlemeDevamEdiyor(talep)
                && talep.Durum != SatinalmaTalepDurumlari.YonetimOnayinda,
            BildirimTipleri.TeklifOnayda =>
                !tamamlandi && SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep),
            BildirimTipleri.Reddedildi =>
                talep.Durum == SatinalmaTalepDurumlari.Reddedildi,
            // Sipariş/mal kabul sonrası «onaylandı» bildirimi aksiyon gerektirmez.
            BildirimTipleri.Onaylandi =>
                !tamamlandi && talep.Durum == SatinalmaTalepDurumlari.Onaylandi,
            // Mal kabul tamamlanınca sipariş bildirimi de düşer.
            BildirimTipleri.SiparisOlusturuldu =>
                !tamamlandi && talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu,
            BildirimTipleri.MalKabulEdildi =>
                !tamamlandi && talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu,
            _ => false
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
        GecerliMi(bildirim, talepler);

    /// <summary>
    /// Onay bekleyen teklif bildirimleri temizleme sırasında korunur.
    /// </summary>
    public static bool Temizlenmemeli(BildirimKaydi bildirim, IEnumerable<SatinalmaTalep> talepler)
    {
        if (BildirimRolPolitikasi.NormalizeTip(bildirim.Tip) != BildirimTipleri.TeklifOnayda)
            return false;

        if (bildirim.TalepId is not { } tid)
            return false;

        var talep = talepler.FirstOrDefault(t => t.Id == tid);
        return talep is not null && SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep);
    }
}
