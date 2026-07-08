using System.IO;
using System.Text.Json;
using SatinalmaPro.Models;
using SatinalmaPro.Services.Firebase;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Services;

public static class OturumYoneticisi
{
    private static readonly string OturumDosyasi = SatinalmaProKlasor.DosyaYolu("oturum.json");
    private static readonly string TercihDosyasi = SatinalmaProKlasor.DosyaYolu("giris_tercihleri.json");
    private static readonly string ProfilOnbellekDosyasi = SatinalmaProKlasor.DosyaYolu("profil_onbellek.json");

    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static FirebaseAuthServisi? Auth { get; private set; }
    public static FirestoreVeriServisi? Firestore { get; private set; }
    public static SaaSAuthServisi? SaaSAuth { get; private set; }
    public static KullaniciProfili? AktifKullanici { get; private set; }

    public static bool BulutAktif => FirebaseAyarDeposu.Ayarlar.Yapilandirildi;
    public static bool GirisYapildi => AktifKullanici is { Aktif: true };

    public static event Action? OturumDegisti;

    public static void Baslat()
    {
        FirebaseAyarDeposu.Yukle();
        if (!BulutAktif)
            return;

        Auth = new FirebaseAuthServisi(FirebaseAyarDeposu.Ayarlar);
        Firestore = new FirestoreVeriServisi(FirebaseAyarDeposu.Ayarlar, Auth);
        SaaSAuth = new SaaSAuthServisi(FirebaseAyarDeposu.Ayarlar.ProjectId);
    }

    public static GirisTercihleri TercihleriOku()
    {
        if (!File.Exists(TercihDosyasi))
            return new GirisTercihleri("", false);

        try
        {
            return JsonSerializer.Deserialize<GirisTercihleri>(File.ReadAllText(TercihDosyasi), JsonSecenekleri)
                   ?? new GirisTercihleri("", false);
        }
        catch
        {
            return new GirisTercihleri("", false);
        }
    }

    public static void TercihKutulariniKaydet(string kullaniciAdi, bool beniHatirla, bool sifremiHatirla)
    {
        SatinalmaProKlasor.Olustur();
        var tercih = new GirisTercihleri(
            beniHatirla ? kullaniciAdi.Trim() : "",
            beniHatirla,
            sifremiHatirla);
        File.WriteAllText(TercihDosyasi, JsonSerializer.Serialize(tercih, JsonSecenekleri));

        if (!sifremiHatirla)
            GirisSifreDeposu.Sil();
    }

    public static void TercihleriKaydet(string kullaniciAdi, bool beniHatirla, bool sifremiHatirla, string? sifre)
    {
        SatinalmaProKlasor.Olustur();
        var tercih = new GirisTercihleri(
            beniHatirla ? kullaniciAdi.Trim() : "",
            beniHatirla,
            sifremiHatirla);
        File.WriteAllText(TercihDosyasi, JsonSerializer.Serialize(tercih, JsonSecenekleri));

        if (sifremiHatirla && !string.IsNullOrEmpty(sifre))
            GirisSifreDeposu.Kaydet(sifre);
        else
            GirisSifreDeposu.Sil();
    }

    public static async Task GirisYapAsync(
        string kullaniciAdi,
        string sifre,
        bool beniHatirla,
        bool sifremiHatirla = false,
        CancellationToken iptal = default)
    {
        if (Auth is null || Firestore is null || SaaSAuth is null)
            throw new InvalidOperationException("Firebase yapılandırılmamış.");

        var sonuc = await SaaSAuth.GirisYapAsync(kullaniciAdi, sifre, iptal);
        Auth.OturumuSaaSDenUygula(sonuc);
        KiracıOturumu.Ayarla(sonuc.TenantId, sonuc.TenantAd);

        if (!await ProfiliYukleAsync(iptal))
            throw new InvalidOperationException("Kullanıcı profili bulunamadı. Yöneticinize başvurun.");

        TercihleriKaydet(kullaniciAdi, beniHatirla, sifremiHatirla, sifre);
        OturumDosyasiniGuncelle(beniHatirla, sonuc.TenantId, sonuc.KullaniciAdi ?? kullaniciAdi);
    }

    public static async Task<bool> OtomatikGirisDeneAsync(CancellationToken iptal = default)
    {
        if (Auth is null || Firestore is null || SaaSAuth is null || !BulutAktif)
            return false;

        if (await Auth.KayitliOturumuDeneAsync(OturumDosyasi, iptal))
        {
            var paket = OturumPaketiniOku();
            if (!string.IsNullOrWhiteSpace(paket?.TenantId))
                KiracıOturumu.Ayarla(paket.TenantId, paket.TenantAd);

            if (await ProfiliYukleAsync(iptal))
                return true;
        }

        Auth.OturumuKapat();
        AktifKullanici = null;
        KiracıOturumu.Temizle();

        var tercih = TercihleriOku();
        var kullaniciAdi = tercih.KullaniciAdi;
        if (!tercih.BeniHatirla || string.IsNullOrWhiteSpace(kullaniciAdi) || !tercih.SifremiHatirla)
            return false;

        var sifre = GirisSifreDeposu.Oku();
        if (string.IsNullOrWhiteSpace(sifre))
            return false;

        try
        {
            await GirisYapAsync(kullaniciAdi, sifre, tercih.BeniHatirla, tercih.SifremiHatirla, iptal);
            return GirisYapildi;
        }
        catch
        {
            Auth?.OturumuKapat();
            AktifKullanici = null;
            KiracıOturumu.Temizle();
            return false;
        }
    }

    public static async Task<bool> ProfiliYukleAsync(CancellationToken iptal = default)
    {
        if (Auth?.Uid is null || Firestore is null)
            return false;

        KullaniciProfili? profil;
        try
        {
            profil = await Firestore.KullaniciOkuAsync(Auth.Uid, iptal);
        }
        catch (Exception ex) when (KotaHatasiMi(ex))
        {
            profil = ProfilOnbellegindenOku();
            if (profil is null)
            {
                throw new InvalidOperationException(
                    "Firebase okuma kotası doldu veya bağlantı kurulamadı. Birkaç dakika sonra tekrar deneyin.");
            }
        }

        if (profil is null || !profil.Aktif)
        {
            AktifKullanici = null;
            return false;
        }

        if (!string.IsNullOrWhiteSpace(profil.TenantId))
            KiracıOturumu.Ayarla(profil.TenantId);

        ProfilOnbellegineKaydet(profil);
        AktifKullanici = profil;
        OturumDegisti?.Invoke();
        return true;
    }

    private static bool KotaHatasiMi(Exception ex) =>
        ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase);

    private static void ProfilOnbellegineKaydet(KullaniciProfili profil)
    {
        try
        {
            SatinalmaProKlasor.Olustur();
            File.WriteAllText(ProfilOnbellekDosyasi, JsonSerializer.Serialize(profil, JsonSecenekleri));
        }
        catch
        {
            // isteğe bağlı
        }
    }

    private static KullaniciProfili? ProfilOnbellegindenOku()
    {
        if (!File.Exists(ProfilOnbellekDosyasi))
            return null;

        try
        {
            return JsonSerializer.Deserialize<KullaniciProfili>(
                File.ReadAllText(ProfilOnbellekDosyasi), JsonSecenekleri);
        }
        catch
        {
            return null;
        }
    }

    public static async Task SifreSifirlamaEpostasiGonderAsync(string kullaniciAdi, CancellationToken iptal = default)
    {
        if (SaaSAuth is null)
            throw new InvalidOperationException("Firebase yapılandırılmamış.");

        await SaaSAuth.SifreSifirlaAsync(kullaniciAdi, iptal);
    }

    public static void CikisYap()
    {
        BildirimYoneticisi.Durdur();
        Auth?.OturumuKapat();
        AktifKullanici = null;
        KiracıOturumu.Temizle();
        OturumDosyasiniSil();
        OturumDegisti?.Invoke();
    }

    public static void GirisHatirlatmasiniTemizle()
    {
        OturumDosyasiniSil();
        GirisSifreDeposu.Sil();

        if (!File.Exists(TercihDosyasi))
            return;

        try { File.Delete(TercihDosyasi); } catch { /* yoksay */ }
    }

    public static void UygulamaKapanirken()
    {
        BildirimYoneticisi.Durdur();
        BulutVeriSenkronu.YoklamayiDurdur();

        var tercih = TercihleriOku();
        if (tercih.BeniHatirla && Auth is not null)
        {
            var paket = OturumPaketiniOku();
            Auth.OturumuKaydet(OturumDosyasi, beniHatirla: true, paket?.TenantId, paket?.TenantAd, paket?.KullaniciAdi);
        }
        else
        {
            Auth?.OturumuKapat();
            OturumDosyasiniSil();
        }

        AktifKullanici = null;
    }

    private static void OturumDosyasiniGuncelle(bool beniHatirla, string tenantId, string kullaniciAdi)
    {
        if (beniHatirla)
            Auth?.OturumuKaydet(OturumDosyasi, beniHatirla: true, tenantId, KiracıOturumu.TenantAd, kullaniciAdi);
        else
            OturumDosyasiniSil();
    }

    private static OturumPaketi? OturumPaketiniOku()
    {
        if (!File.Exists(OturumDosyasi))
            return null;

        try
        {
            return JsonSerializer.Deserialize<OturumPaketi>(File.ReadAllText(OturumDosyasi), JsonSecenekleri);
        }
        catch
        {
            return null;
        }
    }

    private static void OturumDosyasiniSil()
    {
        if (!File.Exists(OturumDosyasi))
            return;

        try { File.Delete(OturumDosyasi); } catch { /* yoksay */ }
    }

    private sealed class OturumPaketi
    {
        public string? RefreshToken { get; set; }
        public string? Uid { get; set; }
        public string? Eposta { get; set; }
        public string? TenantId { get; set; }
        public string? TenantAd { get; set; }
        public string? KullaniciAdi { get; set; }
        public bool BeniHatirla { get; set; }
    }
}

public sealed class GirisTercihleri
{
    public string KullaniciAdi { get; set; } = "";

    [Obsolete("KullaniciAdi kullanın.")]
    public string Eposta
    {
        get => KullaniciAdi;
        set => KullaniciAdi = value;
    }

    public bool BeniHatirla { get; set; }
    public bool SifremiHatirla { get; set; }

    public GirisTercihleri() { }

    public GirisTercihleri(string kullaniciAdi, bool beniHatirla, bool sifremiHatirla = false)
    {
        KullaniciAdi = kullaniciAdi;
        BeniHatirla = beniHatirla;
        SifremiHatirla = sifremiHatirla;
    }
}
