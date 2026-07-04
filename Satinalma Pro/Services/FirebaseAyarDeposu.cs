using System.IO;
using System.Text.Json;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Services;

public static class FirebaseAyarDeposu
{
    public const string FcmServiceAccountDosyaAdi = "fcm-service-account.json";

    public static string UygulamaDosyaYolu =>
        Path.Combine(AppContext.BaseDirectory, "firebase_ayarlar.json");

    public static string GoogleServicesCalismaYolu =>
        Path.Combine(AppContext.BaseDirectory, "google-services.json");

    public static string GoogleServicesDosyaYolu =>
        Path.Combine(ProjeKlasoru() ?? AppContext.BaseDirectory, "google-services.json");

    public static string FcmServiceAccountDosyaYolu =>
        Path.Combine(ProjeKlasoru() ?? AppContext.BaseDirectory, FcmServiceAccountDosyaAdi);

    public static string FcmServiceAccountCalismaYolu =>
        Path.Combine(AppContext.BaseDirectory, FcmServiceAccountDosyaAdi);

    private static readonly JsonSerializerOptions JsonSecenekleri = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static FirebaseAyarlar Ayarlar { get; private set; } = new();
    private static bool _yuklendi;

    public static void Yukle()
    {
        if (_yuklendi)
            return;

        _yuklendi = true;
        var yol = CozumleAyarDosyasi();
        if (!File.Exists(yol))
            return;

        try
        {
            Ayarlar = JsonSerializer.Deserialize<FirebaseAyarlar>(File.ReadAllText(yol), JsonSecenekleri)
                      ?? new FirebaseAyarlar();
        }
        catch
        {
            Ayarlar = new FirebaseAyarlar();
        }

        FcmServiceAccountDosyasiniHazirla();
        GoogleServicesDosyasiniHazirla();

        if (FcmServiceAccountMevcut)
            Ayarlar.FcmServiceAccountYolu = FcmServiceAccountCalismaYolu;
    }

    public static void Kaydet()
    {
        if (FcmServiceAccountMevcut)
            Ayarlar.FcmServiceAccountYolu = FcmServiceAccountCalismaYolu;

        var json = JsonSerializer.Serialize(Ayarlar, JsonSecenekleri);
        File.WriteAllText(UygulamaDosyaYolu, json);

        var proje = ProjeKlasoru();
        if (proje is not null)
            File.WriteAllText(Path.Combine(proje, "firebase_ayarlar.json"), json);
    }

    public static void GoogleServicesJsonKaydet(string kaynakDosya)
    {
        var hedef = GoogleServicesDosyaYolu;
        Directory.CreateDirectory(Path.GetDirectoryName(hedef)!);
        File.Copy(kaynakDosya, hedef, overwrite: true);
        File.Copy(kaynakDosya, GoogleServicesCalismaYolu, overwrite: true);

        var mobilHedef = Path.GetFullPath(Path.Combine(
            ProjeKlasoru() ?? AppContext.BaseDirectory,
            "..", "SatinalmaPro.Mobile", "google-services.json"));

        if (Directory.Exists(Path.GetDirectoryName(mobilHedef)))
            File.Copy(kaynakDosya, mobilHedef, overwrite: true);

        var androidAssets = Path.GetFullPath(Path.Combine(
            ProjeKlasoru() ?? AppContext.BaseDirectory,
            "..", "SatinalmaPro.Android", "app", "src", "main", "assets", "google-services.json"));

        if (Directory.Exists(Path.GetDirectoryName(androidAssets)))
            File.Copy(kaynakDosya, androidAssets, overwrite: true);
    }

    public static void FcmServiceAccountKaydet(string kaynakDosya)
    {
        var projeHedef = FcmServiceAccountDosyaYolu;
        Directory.CreateDirectory(Path.GetDirectoryName(projeHedef)!);
        File.Copy(kaynakDosya, projeHedef, overwrite: true);
        File.Copy(kaynakDosya, FcmServiceAccountCalismaYolu, overwrite: true);

        var mobilRaw = Path.GetFullPath(Path.Combine(
            ProjeKlasoru() ?? AppContext.BaseDirectory,
            "..", "SatinalmaPro.Mobile", "Resources", "Raw", FcmServiceAccountDosyaAdi));

        if (Directory.Exists(Path.GetDirectoryName(mobilRaw)))
            File.Copy(kaynakDosya, mobilRaw, overwrite: true);

        var androidAssets = Path.GetFullPath(Path.Combine(
            ProjeKlasoru() ?? AppContext.BaseDirectory,
            "..", "SatinalmaPro.Android", "app", "src", "main", "assets", FcmServiceAccountDosyaAdi));

        if (Directory.Exists(Path.GetDirectoryName(androidAssets)))
            File.Copy(kaynakDosya, androidAssets, overwrite: true);

        Ayarlar.FcmServiceAccountYolu = FcmServiceAccountCalismaYolu;
    }

    public static bool GoogleServicesMevcut =>
        File.Exists(GoogleServicesCalismaYolu) || File.Exists(GoogleServicesDosyaYolu);

    /// <summary>Proje kökündeki google-services.json dosyasını çalışma klasörüne kopyalar.</summary>
    private static void GoogleServicesDosyasiniHazirla()
    {
        try
        {
            if (File.Exists(GoogleServicesCalismaYolu))
                return;

            if (!File.Exists(GoogleServicesDosyaYolu))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(GoogleServicesCalismaYolu)!);
            File.Copy(GoogleServicesDosyaYolu, GoogleServicesCalismaYolu, overwrite: false);
        }
        catch
        {
            // dosya kilitli olabilir
        }
    }

    public static bool FcmServiceAccountMevcut =>
        FcmV1Api.ServiceAccountMevcut(FcmServiceAccountCalismaYolu) ||
        FcmV1Api.ServiceAccountMevcut(FcmServiceAccountDosyaYolu);

    /// <summary>Proje kökündeki Service Account dosyasını çalışma klasörüne kopyalar.</summary>
    private static void FcmServiceAccountDosyasiniHazirla()
    {
        try
        {
            if (File.Exists(FcmServiceAccountCalismaYolu))
                return;

            if (!File.Exists(FcmServiceAccountDosyaYolu))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(FcmServiceAccountCalismaYolu)!);
            File.Copy(FcmServiceAccountDosyaYolu, FcmServiceAccountCalismaYolu, overwrite: false);
        }
        catch
        {
            // dosya kilitli olabilir
        }
    }

    private static string CozumleAyarDosyasi()
    {
        if (File.Exists(UygulamaDosyaYolu))
            return UygulamaDosyaYolu;

        var proje = ProjeKlasoru();
        if (proje is not null)
        {
            var kaynak = Path.Combine(proje, "firebase_ayarlar.json");
            if (File.Exists(kaynak))
                return kaynak;
        }

        return UygulamaDosyaYolu;
    }

    private static string? ProjeKlasoru()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
        {
            if (File.Exists(Path.Combine(dir, "SatinalmaPro.csproj")))
                return dir;
            dir = Path.GetDirectoryName(dir)!;
        }

        return null;
    }
}
