using System.IO;
using System.Text.Json;
using SatinalmaPro.Models;
using SatinalmaPro.Services.Firebase;
using SatinalmaPro.Shared.Helpers;

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

    public static void TercihKutulariniKaydet(string eposta, bool beniHatirla, bool sifremiHatirla)
    {
        SatinalmaProKlasor.Olustur();
        var tercih = new GirisTercihleri(
            beniHatirla ? eposta.Trim() : "",
            beniHatirla,
            sifremiHatirla);
        File.WriteAllText(TercihDosyasi, JsonSerializer.Serialize(tercih, JsonSecenekleri));

        if (!sifremiHatirla)
            GirisSifreDeposu.Sil();
    }

    public static void TercihleriKaydet(string eposta, bool beniHatirla, bool sifremiHatirla, string? sifre)
    {
        SatinalmaProKlasor.Olustur();
        var tercih = new GirisTercihleri(
            beniHatirla ? eposta.Trim() : "",
            beniHatirla,
            sifremiHatirla);
        File.WriteAllText(TercihDosyasi, JsonSerializer.Serialize(tercih, JsonSecenekleri));

        if (sifremiHatirla && !string.IsNullOrEmpty(sifre))
            GirisSifreDeposu.Kaydet(sifre);
        else
            GirisSifreDeposu.Sil();
    }

    public static async Task GirisYapAsync(
        string eposta,
        string sifre,
        bool beniHatirla,
        bool sifremiHatirla = false,
        CancellationToken iptal = default)
    {
        if (Auth is null || Firestore is null)
            throw new InvalidOperationException("Firebase yapılandırılmamış.");

        await Auth.GirisYapAsync(eposta, sifre, iptal);
        if (!await ProfiliYukleAsync(iptal))
            throw new InvalidOperationException("Kullanıcı profili bulunamadı. Yöneticinize başvurun.");

        TercihleriKaydet(eposta, beniHatirla, sifremiHatirla, sifre);
        OturumDosyasiniGuncelle(beniHatirla);
    }

    /// <summary>Kayıtlı oturum veya hatırlanan e-posta/şifre ile sessiz giriş dener.</summary>
    public static async Task<bool> OtomatikGirisDeneAsync(CancellationToken iptal = default)
    {
        if (Auth is null || Firestore is null || !BulutAktif)
            return false;

        if (await Auth.KayitliOturumuDeneAsync(OturumDosyasi, iptal)
            && await ProfiliYukleAsync(iptal))
            return true;

        Auth.OturumuKapat();
        AktifKullanici = null;

        var tercih = TercihleriOku();
        if (!tercih.BeniHatirla || string.IsNullOrWhiteSpace(tercih.Eposta) || !tercih.SifremiHatirla)
            return false;

        var sifre = GirisSifreDeposu.Oku();
        if (string.IsNullOrWhiteSpace(sifre))
            return false;

        try
        {
            await GirisYapAsync(tercih.Eposta, sifre, tercih.BeniHatirla, tercih.SifremiHatirla, iptal);
            return GirisYapildi;
        }
        catch
        {
            Auth?.OturumuKapat();
            AktifKullanici = null;
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

    public static async Task SifreSifirlamaEpostasiGonderAsync(string eposta, CancellationToken iptal = default)
    {
        if (Auth is null)
            throw new InvalidOperationException("Firebase yapılandırılmamış.");

        await Auth.SifreSifirlamaEpostasiGonderAsync(eposta, iptal);
    }

    public static void CikisYap()
    {
        BildirimYoneticisi.Durdur();
        Auth?.OturumuKapat();
        AktifKullanici = null;
        OturumDosyasiniSil();
        OturumDegisti?.Invoke();
    }

    /// <summary>Kayıtlı e-posta / şifre hatırlatma dosyalarını siler.</summary>
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
            Auth.OturumuKaydet(OturumDosyasi, beniHatirla: true);
        else
        {
            Auth?.OturumuKapat();
            OturumDosyasiniSil();
        }

        AktifKullanici = null;
    }

    private static void OturumDosyasiniGuncelle(bool beniHatirla)
    {
        if (beniHatirla)
            Auth?.OturumuKaydet(OturumDosyasi, beniHatirla: true);
        else
            OturumDosyasiniSil();
    }

    private static void OturumDosyasiniSil()
    {
        if (!File.Exists(OturumDosyasi))
            return;

        try { File.Delete(OturumDosyasi); } catch { /* yoksay */ }
    }
}

public sealed class GirisTercihleri
{
    public string Eposta { get; set; } = "";
    public bool BeniHatirla { get; set; }
    public bool SifremiHatirla { get; set; }

    public GirisTercihleri() { }

    public GirisTercihleri(string eposta, bool beniHatirla, bool sifremiHatirla = false)
    {
        Eposta = eposta;
        BeniHatirla = beniHatirla;
        SifremiHatirla = sifremiHatirla;
    }
}
