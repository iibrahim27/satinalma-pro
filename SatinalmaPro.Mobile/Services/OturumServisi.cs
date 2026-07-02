using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Shared.Services.Firebase;
using System.Text.Json;

namespace SatinalmaPro.Mobile.Services;

public sealed class OturumServisi
{
    internal const string OturumAnahtari = "mobil_oturum";
    internal const string FcmTokenAnahtari = "mobil_fcm_token";
    private const string ProfilOnbellekAnahtari = "mobil_profil_onbellek";

    public static bool KayitliOturumVar() => UygulamaKurulumServisi.KayitliOturumVar();

    public FirebaseAyarlar Ayarlar { get; private set; }
    public FirebaseAuthServisi Auth { get; }
    public FirestoreVeriServisi Firestore { get; }
    public MobilVeriDeposu Depo { get; }
    public SatinalmaMobilServisi Satinalma { get; }
    public StokMobilServisi Stok { get; }
    public BildirimServisi Bildirimler { get; }
    public BildirimDinleyici Dinleyici { get; }

    public bool GirisYapildi => Auth.OturumAcik && Depo.AktifKullanici?.Aktif == true;

    public event Action? OturumDegisti;
    public event Action? VeriGuncellendi;

    public OturumServisi()
    {
        FirebaseAyarServisi.PaketDosyalariniHazirlaAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        Ayarlar = FirebaseAyarServisi.Yukle();
        Auth = new FirebaseAuthServisi(Ayarlar);
        Firestore = new FirestoreVeriServisi(Ayarlar, Auth);
        Depo = new MobilVeriDeposu(Firestore, Auth);
        var fcm = new FcmPushServisi(Ayarlar, Firestore, FirebaseAyarServisi.FcmServiceAccountYolu);
        Bildirimler = new BildirimServisi(Depo, fcm);
        Stok = new StokMobilServisi(Depo);
        Satinalma = new SatinalmaMobilServisi(Depo, Bildirimler, Stok);
        Dinleyici = new BildirimDinleyici(this);
    }

    public void AyarlariYenile()
    {
        var yeni = FirebaseAyarServisi.Yukle();
        Ayarlar.ApiKey = yeni.ApiKey;
        Ayarlar.ProjectId = yeni.ProjectId;
        Ayarlar.GuncellemeManifestUrl = yeni.GuncellemeManifestUrl;
        Ayarlar.FcmServiceAccountYolu = yeni.FcmServiceAccountYolu;
        Ayarlar.FcmServerKey = yeni.FcmServerKey;
    }

    public async Task<bool> KayitliOturumuDeneAsync(CancellationToken iptal = default)
    {
        if (!Ayarlar.Yapilandirildi)
            return false;

        if (!await Auth.KayitliOturumuDeneAsync(
                () => Preferences.Default.Get(OturumAnahtari, (string?)null), iptal))
            return false;

        return await ProfilYukleAsync(iptal);
    }

    public async Task<bool> OturumuGerekirseYukleAsync(CancellationToken iptal = default)
    {
        if (GirisYapildi)
            return true;

        if (!KayitliOturumVar())
            return false;

        return await KayitliOturumuDeneAsync(iptal);
    }

    public async Task GirisYapAsync(string eposta, string sifre, CancellationToken iptal = default)
    {
        if (!Ayarlar.Yapilandirildi)
            throw new InvalidOperationException("Firebase ayarları yapılandırılmamış.");

        await Auth.GirisYapAsync(eposta, sifre, iptal);
        Auth.OturumuKaydet(m => Preferences.Default.Set(OturumAnahtari, m), true);

        await ProfilYukleAsync(iptal);
    }

    private async Task<bool> ProfilYukleAsync(CancellationToken iptal)
    {
        if (string.IsNullOrEmpty(Auth.Uid))
            return false;

        KullaniciProfili? profil = null;
        try
        {
            profil = await Firestore.KullaniciOkuAsync(Auth.Uid, iptal);
        }
        catch (Exception ex) when (KotaHatasiMi(ex))
        {
            profil = ProfilOnbellegindenOku();
            if (profil is null)
                throw new InvalidOperationException(
                    "Firebase günlük okuma kotası doldu. Birkaç saat sonra tekrar deneyin.");
        }

        if (profil is null)
            throw new InvalidOperationException("Kullanıcı profili bulunamadı. Masaüstünden kullanıcı oluşturulmalıdır.");

        if (!profil.Aktif)
            throw new InvalidOperationException("Hesabınız pasif durumda.");

        ProfilOnbellegineKaydet(profil);
        Depo.AktifKullaniciyiAyarla(profil);
        await Depo.GirisSonrasiSenkronizeEtAsync(iptal);

        Dinleyici.Baslat();
        await Dinleyici.IlkKontrolAsync();
        OturumDegisti?.Invoke();

        _ = BildirimAltyapisiBaslatAsync(iptal);
        return true;
    }

    private static bool KotaHatasiMi(Exception ex) =>
        ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase);

    private static void ProfilOnbellegineKaydet(KullaniciProfili profil) =>
        Preferences.Default.Set(ProfilOnbellekAnahtari, JsonSerializer.Serialize(profil));

    private static KullaniciProfili? ProfilOnbellegindenOku()
    {
        var json = Preferences.Default.Get(ProfilOnbellekAnahtari, (string?)null);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<KullaniciProfili>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task BildirimAltyapisiBaslatAsync(CancellationToken iptal)
    {
        try
        {
            await Task.Delay(600, iptal);

            var fcm = IPlatformApplication.Current?.Services.GetService<IFcmPlatformServisi>();
            if (fcm is not null)
            {
                await fcm.BaslatAsync();
                for (var deneme = 0; deneme < 3; deneme++)
                {
                    var token = await fcm.TokenAlAsync();
                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        FcmTokenAyarla(token);
                        break;
                    }

                    await Task.Delay(1500 * (deneme + 1), iptal);
                }
            }

            await FcmTokenKaydetAsync(iptal);
#if ANDROID
            AndroidBildirimKanali.Olustur();
            BildirimForegroundService.Baslat(global::Android.App.Application.Context);
            await Dinleyici.IlkKontrolAsync();
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bildirim altyapısı: {ex.Message}");
        }
    }

    public async Task FcmTokenKaydetAsync(CancellationToken iptal = default)
    {
        if (string.IsNullOrEmpty(Auth.Uid))
            return;

        var token = Preferences.Default.Get(FcmTokenAnahtari, (string?)null);
        if (string.IsNullOrWhiteSpace(token))
            return;

        try
        {
            await Firestore.KullaniciFcmTokenGuncelleAsync(Auth.Uid, token, iptal);
            if (Depo.AktifKullanici is not null)
                Depo.AktifKullanici.FcmToken = token;
        }
        catch
        {
            // FCM henüz yapılandırılmamış olabilir
        }
    }

    public static void FcmTokenAyarla(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            Preferences.Default.Remove(FcmTokenAnahtari);
        else
            Preferences.Default.Set(FcmTokenAnahtari, token);
    }

    public void VeriGuncellendiBildir() => VeriGuncellendi?.Invoke();

    public async Task VerileriYenileAsync(CancellationToken iptal = default)
    {
        if (!GirisYapildi)
            return;
        await Depo.SenkronizeEtAsync(zorla: true, iptal);
        VeriGuncellendi?.Invoke();
    }

    public void CikisYap()
    {
        Dinleyici.Durdur();
#if ANDROID
        BildirimForegroundService.Durdur(global::Android.App.Application.Context);
#endif
        Auth.OturumuKapat();
        Depo.AktifKullaniciyiAyarla(null);
        UygulamaKurulumServisi.OturumVerisiniTemizle();
        OturumDegisti?.Invoke();
    }

    public string Rol => KullaniciRolleri.Normalize(Depo.AktifKullanici?.Rol);
    public string KullaniciAdi => Depo.AktifKullanici?.AdSoyad ?? "";

}
