using System.IO;
using System.Windows;
using System.Windows.Threading;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class BildirimYoneticisi
{
    private const string GosterilenAnahtar = "masaustu_bildirim_gosterilen";

    private static readonly HashSet<Guid> ToastGosterilen = [];
    private static DispatcherTimer? _zamanlayici;
    private static bool _kontrolEdiliyor;

    public static event Action? BildirimlerDegisti;

    public static int OkunmamisSayisi =>
        OturumYoneticisi.AktifKullanici is { } k
            ? MasaustuBildirimFiltreleme.OkunmamisSayisi(
                BildirimDeposu.Bildirimler, k, SatinalmaDepo.Talepler)
            : 0;

    public static IEnumerable<BildirimKaydi> KullaniciBildirimleri()
    {
        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null)
            return [];

        return BildirimDeposu.Bildirimler.Where(b => KullaniciyaMi(b, kullanici));
    }

    public static void Baslat()
    {
        if (!OturumYoneticisi.GirisYapildi)
            return;

        Durdur();
        ToastGosterilen.Clear();
        GosterilenleriYukle();

        _zamanlayici = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _zamanlayici.Tick += async (_, _) => await KontrolEtAsync();
        _zamanlayici.Start();

        _ = IlkYukleAsync();
    }

    public static async Task BildirimleriKontrolEtAsync() => await KontrolEtAsync();

    public static void Durdur()
    {
        _zamanlayici?.Stop();
        _zamanlayici = null;
    }

    public static async Task EkleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        await BildirimDeposu.EkleAsync(bildirim, iptal);
        BildirimlerDegisti?.Invoke();
        ToastGoster(bildirim);
    }

    public static async Task OkunduIsaretleAsync(BildirimKaydi bildirim, CancellationToken iptal = default)
    {
        bildirim.Okundu = true;
        bildirim.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await BildirimDeposu.KaydetAsync(iptal);
        BildirimlerDegisti?.Invoke();
    }

    public static async Task TumunuOkunduIsaretleAsync(CancellationToken iptal = default)
    {
        foreach (var b in KullaniciBildirimleri())
        {
            b.Okundu = true;
            b.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        await BildirimDeposu.KaydetAsync(iptal);
        BildirimlerDegisti?.Invoke();
    }

    public static async Task GecersizleriOkunduYapAsync(CancellationToken iptal = default)
    {
        SatinalmaDepo.Yukle();
        await BildirimDeposu.YukleAsync(zorla: true, iptal: iptal);

        var degisti = false;
        foreach (var b in BildirimDeposu.Bildirimler)
        {
            if (b.Okundu || MasaustuBildirimFiltreleme.GecerliMi(b, SatinalmaDepo.Talepler))
                continue;

            b.Okundu = true;
            b.GuncellemeUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

        BildirimDeposu.Bildirimler.RemoveAll(b => KullaniciyaMi(b, kullanici) && !Korunmali(b));
        await BildirimDeposu.KaydetAsync(iptal);
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
        await KontrolEtAsync(bildirimGoster: true);
        BildirimlerDegisti?.Invoke();
    }

    private static void ToastGoster(BildirimKaydi bildirim)
    {
        var kullanici = OturumYoneticisi.AktifKullanici;
        if (kullanici is null || !ToastGosterilmeli(bildirim, kullanici))
            return;

        if (ToastGosterilen.Contains(bildirim.Id))
            return;

        ToastGosterilen.Add(bildirim.Id);
        GosterilenleriKaydet();

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

    private static async Task KontrolEtAsync(bool bildirimGoster = true)
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

            await BildirimDeposu.YukleAsync(zorla: bildirimGoster);
            await GecersizleriOkunduYapAsync();

            var kullanici = OturumYoneticisi.AktifKullanici;
            if (kullanici is null)
                return;

            if (bildirimGoster)
            {
                foreach (var bildirim in KullaniciBildirimleri()
                             .Where(b => ToastGosterilmeli(b, kullanici)))
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

    private static string GosterilenDosyaYolu =>
        SatinalmaProKlasor.DosyaYolu("bildirim_gosterilen.txt");

    private static void GosterilenleriYukle()
    {
        ToastGosterilen.Clear();
        var yol = GosterilenDosyaYolu;
        if (!File.Exists(yol))
            return;

        foreach (var parca in File.ReadAllText(yol).Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(parca, out var id))
                ToastGosterilen.Add(id);
        }
    }

    private static void GosterilenleriKaydet()
    {
        SatinalmaProKlasor.Olustur();
        var yol = GosterilenDosyaYolu;
        if (ToastGosterilen.Count == 0)
        {
            if (File.Exists(yol))
                File.Delete(yol);
            return;
        }

        File.WriteAllText(yol, string.Join(',', ToastGosterilen));
    }
}
