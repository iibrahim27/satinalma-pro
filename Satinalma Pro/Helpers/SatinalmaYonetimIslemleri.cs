using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Helpers;

public static class SatinalmaYonetimIslemleri
{
    public static async Task OnaylaAsync(SatinalmaTalep talep, bool teklifIste)
    {
        if (!KullaniciYetkileri.YonetimKararVerebilir())
            throw new InvalidOperationException("Talep onay yetkiniz yok.");

        if (talep.TalepTuru == Models.TalepTurleri.Acil)
        {
            await AcilOnaylaAsync(talep);
            return;
        }

        if (!teklifIste)
        {
            talep.Durum = SatinalmaTalepDurumlari.Onaylandi;
            talep.Status = ProcurementStatus.Approved;
            talep.Priority = ProcurementTalepAdapter.EffectivePriority(talep);
            talep.TeklifsizYonetimOnayi = true;
            YonetimOnayiKaydet(talep);
            await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

            await BildirimGonderAsync(() => SatinalmaBildirimleri.OnaylandiBildirimleriGonderAsync(talep));
            await BildirimYoneticisi.GecersizleriOkunduYapAsync();
            return;
        }

        talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
        talep.Status = ProcurementStatus.QuoteRequested;
        talep.Priority = ProcurementTalepAdapter.EffectivePriority(talep);
        talep.YonetimOnayKilitli = false;
        talep.TeklifsizYonetimOnayi = false;
        SatinalmaTalepYardimcisi.Dokun(talep);
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);
        await BildirimGonderAsync(() => SatinalmaBildirimleri.TeklifIstendiAsync(talep));
        if (!string.IsNullOrWhiteSpace(talep.OlusturanUid))
            await BildirimGonderAsync(() => SatinalmaBildirimleri.TeklifIstendiOlusturucuyaAsync(talep));
        await BildirimYoneticisi.GecersizleriOkunduYapAsync();
    }

    public static async Task ReddetAsync(SatinalmaTalep talep, string? gerekce)
    {
        if (!KullaniciYetkileri.YonetimKararVerebilir())
            throw new InvalidOperationException("Red yetkiniz yok.");

        talep.Durum = SatinalmaTalepDurumlari.Reddedildi;
        talep.Status = ProcurementStatus.Rejected;
        talep.Priority = ProcurementTalepAdapter.EffectivePriority(talep);
        talep.RedGerekcesi = string.IsNullOrWhiteSpace(gerekce) ? "" : gerekce.Trim();
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

        await BildirimYoneticisi.GecersizleriOkunduYapAsync();
        await BildirimGonderAsync(() => SatinalmaBildirimleri.ReddedildiAsync(talep, gerekce ?? ""));
    }

    private static async Task AcilOnaylaAsync(SatinalmaTalep talep)
    {
        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;
        talep.Status = ProcurementStatus.Approved;
        talep.Priority = ProcurementTalepAdapter.EffectivePriority(talep);
        talep.TeklifsizYonetimOnayi = true;
        YonetimOnayiKaydet(talep);
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

        await BildirimGonderAsync(() => SatinalmaBildirimleri.OnaylandiBildirimleriGonderAsync(talep));
        await BildirimYoneticisi.GecersizleriOkunduYapAsync();
    }

    private static async Task BildirimGonderAsync(Func<Task> gonder)
    {
        try
        {
            await gonder();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "SatinalmaYonetimIslemleri.Bildirim");
        }
    }

    public static async Task TeklifGeriGonderAsync(SatinalmaTalep talep, string? gerekce)
    {
        if (!KullaniciYetkileri.YonetimKararVerebilir())
            throw new InvalidOperationException("Geri gönderme yetkiniz yok.");

        if (!SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep))
            throw new InvalidOperationException("Bu talep için geri gönderilecek teklif onayı bulunamadı.");

        talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
        talep.Status = ProcurementStatus.Comparison;
        talep.Priority = ProcurementTalepAdapter.EffectivePriority(talep);
        talep.TeklifDuzeltmeNotu = string.IsNullOrWhiteSpace(gerekce) ? "" : gerekce.Trim();
        talep.OnaylananTeklifId = null;
        talep.Kalemler ??= [];
        foreach (var kalem in talep.Kalemler)
            kalem.OnaylananTeklifId = null;

        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);
        await BildirimGonderAsync(() => SatinalmaBildirimleri.TeklifDuzeltmeyeGonderildiAsync(talep, talep.TeklifDuzeltmeNotu));
        await BildirimYoneticisi.GecersizleriOkunduYapAsync();
    }

    public static async Task TeklifReddetAsync(SatinalmaTalep talep, string? gerekce) =>
        await ReddetAsync(talep, gerekce);

    private static void YonetimOnayiKaydet(SatinalmaTalep talep)
    {
        var k = OturumYoneticisi.AktifKullanici;
        talep.YonetimOnaylayanUid = k?.Uid ?? "";
        talep.YonetimOnaylayanAd = k?.AdSoyad ?? "";
        talep.YonetimOnaylayanEposta = k?.Eposta ?? "";
        talep.YonetimOnayTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        talep.YonetimOnayKilitli = true;
        SatinalmaTalepYardimcisi.Dokun(talep);
    }
}
