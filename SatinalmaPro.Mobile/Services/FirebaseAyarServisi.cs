using System.Reflection;
using System.Text.Json;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Services;

public static class FirebaseAyarServisi
{
    private const string DosyaAdi = "firebase_ayarlar.json";
    private const string GoogleServicesDosyaAdi = "google-services.json";
    private const string FcmDosyaAdi = "fcm-service-account.json";

    /// <summary>APK / paket okumasi basarisiz olursa son care (Firebase Web API key istemcide zaten aciktir).</summary>
    private static readonly FirebaseAyarlar GomuluVarsayilan = new()
    {
        ApiKey = "AIzaSyAG9UeQTtFpX00bmk9WwGLLnUL2ijDrSgk",
        ProjectId = "satinalmapro-8e7da",
        GuncellemeManifestUrl = "https://raw.githubusercontent.com/iibrahim27/satinalma-pro/main/version.json"
    };

    public static FirebaseAyarlar Yukle()
    {
        foreach (var kaynak in TumKaynaklariDene())
        {
            if (kaynak.Yapilandirildi)
            {
                AppDataKaydet(kaynak);
                return kaynak;
            }
        }

        return new FirebaseAyarlar();
    }

    public static async Task PaketDosyalariniHazirlaAsync()
    {
        await PaketiKopyalaAsync(DosyaAdi);
        await PaketiKopyalaAsync(GoogleServicesDosyaAdi);
        await PaketiKopyalaAsync(FcmDosyaAdi);
    }

    public static string? FcmServiceAccountYolu()
    {
        var yol = AppDataYolu(FcmDosyaAdi);
        return File.Exists(yol) ? yol : null;
    }

    public static void Kaydet(FirebaseAyarlar ayarlar)
    {
        Preferences.Default.Set("firebase_apiKey", ayarlar.ApiKey);
        Preferences.Default.Set("firebase_projectId", ayarlar.ProjectId);
        AppDataKaydet(ayarlar);
    }

    private static IEnumerable<FirebaseAyarlar> TumKaynaklariDene()
    {
        yield return AppDataOku();
        var gomulu = GomuluKaynakOku();
        if (gomulu is not null)
            yield return gomulu;
        foreach (var paketAdi in new[] { DosyaAdi, $"Raw/{DosyaAdi}" })
        {
            var paket = PakettenOku(paketAdi);
            if (paket is not null)
                yield return paket;
        }
        var google = GoogleServicesOku();
        if (google is not null)
            yield return google;
        if (Preferences.Default.ContainsKey("firebase_apiKey"))
        {
            yield return new FirebaseAyarlar
            {
                ApiKey = Preferences.Default.Get("firebase_apiKey", ""),
                ProjectId = Preferences.Default.Get("firebase_projectId", "")
            };
        }
        yield return GomuluVarsayilan;
    }

    private static FirebaseAyarlar AppDataOku()
    {
        var yol = AppDataYolu(DosyaAdi);
        if (!File.Exists(yol))
            return new FirebaseAyarlar();

        try { return ParseJson(File.ReadAllText(yol)); }
        catch { return new FirebaseAyarlar(); }
    }

    private static void AppDataKaydet(FirebaseAyarlar ayarlar)
    {
        if (!ayarlar.Yapilandirildi)
            return;

        try
        {
            var yol = AppDataYolu(DosyaAdi);
            var json = JsonSerializer.Serialize(new
            {
                apiKey = ayarlar.ApiKey,
                projectId = ayarlar.ProjectId,
                guncellemeManifestUrl = ayarlar.GuncellemeManifestUrl,
                fcmServerKey = ayarlar.FcmServerKey
            });
            File.WriteAllText(yol, json);
        }
        catch
        {
            // yazma basarisiz olsa da bellekteki ayarlar kullanilir
        }
    }

    private static string AppDataYolu(string dosyaAdi) =>
        Path.Combine(FileSystem.AppDataDirectory, dosyaAdi);

    private static async Task PaketiKopyalaAsync(string dosyaAdi)
    {
        FirebaseAyarlar? paketIcerik = null;
        foreach (var ad in new[] { dosyaAdi, $"Raw/{dosyaAdi}" })
        {
            paketIcerik = PakettenOku(ad);
            if (paketIcerik?.Yapilandirildi == true || ad == dosyaAdi && PaketVarMi(ad))
                break;
        }

        var hedef = AppDataYolu(dosyaAdi);
        var mevcut = File.Exists(hedef) ? AppDataOku() : new FirebaseAyarlar();
        if (mevcut.Yapilandirildi)
            return;

        if (paketIcerik?.Yapilandirildi == true)
        {
            AppDataKaydet(paketIcerik);
            return;
        }

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(dosyaAdi);
            using var fs = File.Create(hedef);
            await stream.CopyToAsync(fs);
        }
        catch
        {
            foreach (var ad in new[] { $"Raw/{dosyaAdi}" })
            {
                try
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync(ad);
                    using var fs = File.Create(hedef);
                    await stream.CopyToAsync(fs);
                    return;
                }
                catch { /* diger yolu dene */ }
            }
        }
    }

    private static bool PaketVarMi(string dosyaAdi)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(dosyaAdi).ConfigureAwait(false).GetAwaiter().GetResult();
            return stream.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static FirebaseAyarlar? PakettenOku(string dosyaAdi)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(dosyaAdi).ConfigureAwait(false).GetAwaiter().GetResult();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return dosyaAdi.Contains("google-services", StringComparison.OrdinalIgnoreCase)
                ? GoogleServicesParse(json)
                : ParseJson(json);
        }
        catch
        {
            return null;
        }
    }

    private static FirebaseAyarlar? GomuluKaynakOku()
    {
        try
        {
            var asm = typeof(FirebaseAyarServisi).Assembly;
            var ad = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(DosyaAdi, StringComparison.OrdinalIgnoreCase));
            if (ad is null)
                return null;

            using var stream = asm.GetManifestResourceStream(ad);
            if (stream is null)
                return null;

            using var reader = new StreamReader(stream);
            return ParseJson(reader.ReadToEnd());
        }
        catch
        {
            return null;
        }
    }

    private static FirebaseAyarlar? GoogleServicesOku()
    {
        foreach (var ad in new[] { GoogleServicesDosyaAdi, $"Raw/{GoogleServicesDosyaAdi}" })
        {
            var oku = PakettenOku(ad);
            if (oku?.Yapilandirildi == true)
                return oku;
        }

        var yol = AppDataYolu(GoogleServicesDosyaAdi);
        if (!File.Exists(yol))
            return null;

        try { return GoogleServicesParse(File.ReadAllText(yol)); }
        catch { return null; }
    }

    private static FirebaseAyarlar? GoogleServicesParse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("project_info", out var proje))
            return null;

        var projectId = proje.TryGetProperty("project_id", out var p) ? p.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(projectId))
            return null;

        string apiKey = "";
        if (root.TryGetProperty("client", out var clients) && clients.ValueKind == JsonValueKind.Array)
        {
            foreach (var client in clients.EnumerateArray())
            {
                if (client.TryGetProperty("client_info", out var info)
                    && info.TryGetProperty("android_client_info", out var android)
                    && android.TryGetProperty("package_name", out var pkg)
                    && pkg.GetString() is { } paket
                    && paket != "com.metrik.satinalmapro")
                    continue;

                if (client.TryGetProperty("api_key", out var keys) && keys.ValueKind == JsonValueKind.Array)
                {
                    foreach (var key in keys.EnumerateArray())
                    {
                        if (key.TryGetProperty("current_key", out var ck)
                            && !string.IsNullOrWhiteSpace(ck.GetString()))
                        {
                            apiKey = ck.GetString()!;
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(apiKey))
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        return new FirebaseAyarlar
        {
            ApiKey = apiKey,
            ProjectId = projectId,
            GuncellemeManifestUrl = GomuluVarsayilan.GuncellemeManifestUrl
        };
    }

    private static FirebaseAyarlar ParseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new FirebaseAyarlar
        {
            ApiKey = root.TryGetProperty("apiKey", out var k) ? k.GetString() ?? "" : "",
            ProjectId = root.TryGetProperty("projectId", out var p) ? p.GetString() ?? "" : "",
            GuncellemeManifestUrl = root.TryGetProperty("guncellemeManifestUrl", out var g) ? g.GetString() ?? "" : "",
            FcmServiceAccountYolu = root.TryGetProperty("fcmServiceAccountYolu", out var f) ? f.GetString() ?? "" : "",
            FcmServerKey = root.TryGetProperty("fcmServerKey", out var s) ? s.GetString() ?? "" : ""
        };
    }
}
