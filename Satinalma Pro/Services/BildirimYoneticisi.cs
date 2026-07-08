using System.Windows;
using System.Windows.Threading;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class BildirimYoneticisi
{
    private static DispatcherTimer? _zamanlayici;
    private static bool _kontrolEdiliyor;

    public static event Action? BildirimlerDegisti;

    public static int OkunmamisSayisi =>
        OturumYoneticisi.AktifKullanici is { } k
            ? MasaustuBildirimFiltreleme.OkunmamisSayisi(
                BildirimDeposu.AnlikListe(), k, SatinalmaDepo.Talepler)
            : 0;

    public static IEnumerable<BildirimKaydi> KullaniciBildirimleri()
    {
        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null)
            return [];

        return BildirimDeposu.AnlikListe()
            .Where(b => KullaniciyaMi(b, kullanici) && MasaustuBildirimFiltreleme.GecerliMi(b, SatinalmaDepo.Talepler))
            .ToList();
    }

    public static void Baslat()
    {
        if (!OturumYoneticisi.GirisYapildi)
            return;

        Durdur();

        _zamanlayici = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _zamanlayici.Tick += async (_, _) => await KontrolEtAsync(hatirlatmaGoster: true);
        _zamanlayici.Start();

        _ = IlkYukleAsync();
    }

    public static async Task BildirimleriKontrolEtAsync() => await KontrolEtAsync(hatirlatmaGoster: true);

    public static void Durdur()
    {
        _zamanlayici?.Stop();
        _zamanlayici = null;
    }

    public static async Task EkleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        var yeni = await BildirimDeposu.EkleAsync(bildirim, iptal);
        BildirimlerDegisti?.Invoke();
        if (yeni)
            ToastGoster(bildirim, ilkGosterim: true);
    }

    public static async Task CokluEkleAsync(IReadOnlyList<BildirimKaydi> bildirimler, CancellationToken iptal = default)
    {
        if (bildirimler.Count == 0)
            return;

        await BildirimDeposu.CokluEkleAsync(bildirimler, iptal);
        BildirimlerDegisti?.Invoke();

        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null)
            return;

        foreach (var bildirim in bildirimler.Where(b => ToastGosterilmeli(b, kullanici)))
            ToastGoster(bildirim, ilkGosterim: true);
    }

    public static async Task OkunduIsaretleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        bildirim.Okundu = true;
        bildirim.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        BildirimHatirlatmaDeposu.Temizle(bildirim);
        await BildirimDeposu.KaydetAsync(iptal);
        BildirimlerDegisti?.Invoke();
    }

    public static async Task TumunuOkunduIsaretleAsync(CancellationToken iptal = default)
    {
        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null)
            return;

        foreach (var b in BildirimDeposu.AnlikListe().Where(x => KullaniciyaMi(x, kullanici)))
        {
            b.Okundu = true;
            b.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            BildirimHatirlatmaDeposu.Temizle(b);
        }

        await BildirimDeposu.KaydetYerelAsync(iptal);
        await BildirimDeposu.InboxTumunuOkunduAsync(iptal);
        BildirimlerDegisti?.Invoke();
    }

    public static async Task GecersizleriSilAsync(CancellationToken iptal = default)
    {
        SatinalmaDepo.Yukle();
        await BildirimDeposu.YukleAsync(zorla: true, iptal: iptal);
        await BildirimDeposu.GecersizleriSilAsync(iptal);
        BildirimlerDegisti?.Invoke();
    }

    public static async Task SifirlamaSonrasiTemizleAsync(CancellationToken iptal = default)
    {
        await GecersizleriSilAsync(iptal);
        await TemizleAsync(iptal);
    }

    public static async Task GecersizleriOkunduYapAsync(CancellationToken iptal = default)
    {
        SatinalmaDepo.Yukle();
        await BildirimDeposu.YukleAsync(zorla: true, iptal: iptal);

        var degisti = false;
        foreach (var b in BildirimDeposu.AnlikListe())
        {
            if (b.Okundu || MasaustuBildirimFiltreleme.GecerliMi(b, SatinalmaDepo.Talepler))
                continue;

            b.Okundu = true;
            b.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            BildirimHatirlatmaDeposu.Temizle(b);
            degisti = true;
        }

        if (degisti)
        {
            await BildirimDeposu.KaydetAsync(iptal);
            BildirimlerDegisti?.Invoke();
        }
    }

    public static async Task TemizleAsync(CancellationToken iptal = default)
    {
        SatinalmaDepo.Yukle();
        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null)
            return;

        foreach (var b in BildirimDeposu.AnlikListe().Where(x => KullaniciyaMi(x, kullanici) && !Korunmali(x)))
            BildirimHatirlatmaDeposu.Temizle(b);

        BildirimDeposu.Sil(b => KullaniciyaMi(b, kullanici) && !Korunmali(b));
        await BildirimDeposu.KaydetYerelAsync(iptal);
        await BildirimDeposu.InboxTemizleAsync(iptal);
        BildirimlerDegisti?.Invoke();
    }

    public static int ModulOkunmamisSayisi(string modulAdi) =>
        KullaniciBildirimleri()
            .Count(b => !b.Okundu
                        && MasaustuBildirimFiltreleme.GecerliMi(b, SatinalmaDepo.Talepler)
                        && MasaustuBildirimFiltreleme.ModulAdi(b) == modulAdi);

    private static bool Korunmali(BildirimKaydi b) =>
        MasaustuBildirimFiltreleme.Temizlenmemeli(b, SatinalmaDepo.Talepler);

    private static async Task IlkYukleAsync()
    {
        await KontrolEtAsync(hatirlatmaGoster: true);
        BildirimlerDegisti?.Invoke();
    }

    private static void ToastGoster(BildirimKaydi bildirim, bool ilkGosterim = false)
    {
        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null || !ToastGosterilmeli(bildirim, kullanici))
            return;

        if (!BildirimHatirlatmaDeposu.GosterilebilirMi(bildirim, ilkGosterim))
            return;

        BildirimHatirlatmaDeposu.Gosterildi(bildirim);

        var windowsToast = false;
        try
        {
            windowsToast = MasaustuActionCenterBildirim.Goster(bildirim);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "ActionCenterBildirim");
        }

        if (!windowsToast)
        {
            HataGunlugu.Kaydet(
                new InvalidOperationException(
                    MasaustuActionCenterBildirim.Calisiyor
                        ? "Action Center bildirimi gösterilemedi."
                        : "Action Center başlatılamadı; yerel toast kullanılıyor."),
                "ActionCenterYedek");

            try
            {
                MasaustuToastBildirim.Goster(bildirim.Baslik, bildirim.Mesaj, () =>
                {
                    Application.Current?.Dispatcher.Invoke(() => MasaustuBildirimNavigasyon.BildirimdenGit(bildirim));
                });
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "ToastBildirim");
            }
        }
    }

    private static async Task KontrolEtAsync(bool hatirlatmaGoster = false)
    {
        if (_kontrolEdiliyor || !OturumYoneticisi.GirisYapildi)
            return;

        _kontrolEdiliyor = true;
        try
        {
            if (OturumYoneticisi.BulutAktif && OturumYoneticisi.Firestore is not null)
                await BulutVeriSenkronu.TalepleriBuluttanCekAsync();
            else
                SatinalmaDepo.Yukle();

            await BildirimDeposu.YukleAsync(zorla: hatirlatmaGoster);
            await GecersizleriSilAsync();

            var kullanici = OturumYoneticisi.AktifKullanici;
            if (kullanici is null)
                return;

            if (hatirlatmaGoster)
            {
                foreach (var bildirim in KullaniciBildirimleri()
                             .Where(b => ToastGosterilmeli(b, kullanici))
                             .ToList())
                    ToastGoster(bildirim);
            }

            BildirimlerDegisti?.Invoke();
        }
        catch
        {
            // ağ hatası sessiz
        }
        finally
        {
            _kontrolEdiliyor = false;
        }
    }

    private static bool KullaniciyaMi(BildirimKaydi bildirim, KullaniciProfili kullanici) =>
        MasaustuBildirimFiltreleme.KullaniciyaMi(bildirim, kullanici);

    private static bool ToastGosterilmeli(BildirimKaydi bildirim, KullaniciProfili kullanici) =>
        MasaustuBildirimFiltreleme.ToastGosterilmeli(bildirim, kullanici, SatinalmaDepo.Talepler);
}
