using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Services;

public static class MobilGuncellemeServisi
{
    private const string SonDenenenSurumKey = "guncelleme_son_denenen_surum";
    private const string SonDenenenBuildKey = "guncelleme_son_denenen_build";
    private const string SonDenemeSayisiKey = "guncelleme_son_deneme_sayisi";
    private const string SonDenemeZamanKey = "guncelleme_son_deneme_zaman";
    private const int MaxOtomatikDeneme = 2;
    private static readonly TimeSpan DenemeBekleme = TimeSpan.FromHours(12);

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    static MobilGuncellemeServisi()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd($"SatinalmaPro-Mobile/{AppInfo.VersionString}+{AppInfo.BuildString}");
    }

    public static async Task<bool> KontrolEtVeUygulaAsync(
        Action<string, double>? ilerle = null,
        CancellationToken iptal = default)
    {
        var manifestUrl = FirebaseAyarServisi.Yukle().GuncellemeManifestUrl;
        if (string.IsNullOrWhiteSpace(manifestUrl))
            return false;

        ilerle?.Invoke("Sürüm kontrol ediliyor...", 5);

        GuncellemeManifesti manifest;
        try
        {
            var url = manifestUrl.Contains('?', StringComparison.Ordinal)
                ? $"{manifestUrl}&_={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
                : $"{manifestUrl}?_={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            manifest = await Http.GetFromJsonAsync<GuncellemeManifesti>(url, iptal)
                       ?? new GuncellemeManifesti();
        }
        catch
        {
            ilerle?.Invoke("Güncelleme sunucusuna ulaşılamadı, devam ediliyor...", 100);
            return false;
        }

        var apkUrl = ApkIndirmeAdresi(manifest);
        if (string.IsNullOrWhiteSpace(manifest.Version) || string.IsNullOrWhiteSpace(apkUrl))
            return false;

        var yerelSurum = AppInfo.VersionString.Trim();
        var yerelBuild = YerelBuildNo();

        if (!SurumYardimcisi.GuncellemeGerekli(manifest.Version, manifest.Build, yerelSurum, yerelBuild))
        {
            GuncellemeDenemeleriniTemizle();
            return false;
        }

        if (OtomatikGuncellemeEngelliMi(manifest))
        {
            ilerle?.Invoke("Güncelleme kurulumu tamamlanmadı. APK'yı GitHub'dan manuel kurun.", 100);
            await Task.Delay(900, iptal);
            return false;
        }

        ilerle?.Invoke($"Yeni sürüm bulundu: v{manifest.Version}", 12);

        var kurulum = MauiProgram.Services?.GetService<IApkKurulumServisi>();
        if (kurulum is null)
            return false;

        if (!kurulum.KurulumIznineHazir())
        {
            ilerle?.Invoke("Kurulum izni gerekli. Ayarlardan izin verip uygulamayı yeniden açın.", 100);
            await Task.Delay(1200, iptal);
            return false;
        }

        var apkYol = Path.Combine(FileSystem.CacheDirectory, $"SatinalmaPro_{manifest.Version}_b{manifest.Build}.apk");

        try
        {
            ilerle?.Invoke("Güncelleme indiriliyor...", 18);
            await DosyaIndirAsync(apkUrl, apkYol, ilerle, iptal);

            GuncellemeDenemesiKaydet(manifest);

            ilerle?.Invoke("Kurulum ekranı açılıyor...", 92);
            kurulum.Kur(apkYol);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            ilerle?.Invoke(GuncellemeHataMetni(ex), 100);
            await Task.Delay(800, iptal);
            return false;
        }
    }

    private static int YerelBuildNo() =>
        int.TryParse(AppInfo.BuildString, out var build) ? build : 0;

    private static bool OtomatikGuncellemeEngelliMi(GuncellemeManifesti manifest)
    {
        var sonSurum = Preferences.Default.Get(SonDenenenSurumKey, "");
        var sonBuild = Preferences.Default.Get(SonDenenenBuildKey, 0);
        var deneme = Preferences.Default.Get(SonDenemeSayisiKey, 0);
        var sonZaman = Preferences.Default.Get(SonDenemeZamanKey, DateTime.MinValue);

        if (!SurumYardimcisi.AyniSurum(sonSurum, manifest.Version) || sonBuild != manifest.Build)
            return false;

        if (deneme < MaxOtomatikDeneme)
            return false;

        return DateTime.UtcNow - sonZaman < DenemeBekleme;
    }

    private static void GuncellemeDenemesiKaydet(GuncellemeManifesti manifest)
    {
        var oncekiSurum = Preferences.Default.Get(SonDenenenSurumKey, "");
        var oncekiBuild = Preferences.Default.Get(SonDenenenBuildKey, 0);
        var deneme = Preferences.Default.Get(SonDenemeSayisiKey, 0);

        if (!SurumYardimcisi.AyniSurum(oncekiSurum, manifest.Version) || oncekiBuild != manifest.Build)
            deneme = 0;

        Preferences.Default.Set(SonDenenenSurumKey, manifest.Version.Trim());
        Preferences.Default.Set(SonDenenenBuildKey, manifest.Build);
        Preferences.Default.Set(SonDenemeSayisiKey, deneme + 1);
        Preferences.Default.Set(SonDenemeZamanKey, DateTime.UtcNow);
    }

    private static void GuncellemeDenemeleriniTemizle()
    {
        Preferences.Default.Remove(SonDenenenSurumKey);
        Preferences.Default.Remove(SonDenenenBuildKey);
        Preferences.Default.Remove(SonDenemeSayisiKey);
        Preferences.Default.Remove(SonDenemeZamanKey);
    }

    private static string? ApkIndirmeAdresi(GuncellemeManifesti manifest)
    {
        if (!string.IsNullOrWhiteSpace(manifest.DownloadUrlApk))
            return manifest.DownloadUrlApk;

        if (string.IsNullOrWhiteSpace(manifest.Version))
            return null;

        return $"https://github.com/iibrahim27/satinalma-pro/releases/download/v{manifest.Version.Trim()}/SatinalmaPro.apk";
    }

    private static string GuncellemeHataMetni(Exception ex)
    {
        if (ex.Message.Contains("404", StringComparison.Ordinal))
            return "Güncelleme dosyası henüz yüklenmemiş. Devam ediliyor...";

        return "Güncelleme indirilemedi, mevcut sürümle devam ediliyor...";
    }

    private static async Task DosyaIndirAsync(
        string url,
        string hedefYol,
        Action<string, double>? ilerle,
        CancellationToken iptal)
    {
        using var yanit = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, iptal);
        if (yanit.StatusCode == HttpStatusCode.NotFound)
            throw new HttpRequestException($"404 — dosya bulunamadı: {url}");

        yanit.EnsureSuccessStatusCode();

        var toplam = yanit.Content.Headers.ContentLength;
        await using var kaynak = await yanit.Content.ReadAsStreamAsync(iptal);
        await using var hedef = File.Create(hedefYol);

        var araTampon = new byte[81920];
        long indirilen = 0;
        int okunan;

        while ((okunan = await kaynak.ReadAsync(araTampon, iptal)) > 0)
        {
            await hedef.WriteAsync(araTampon.AsMemory(0, okunan), iptal);
            indirilen += okunan;

            if (toplam > 0)
            {
                var oran = indirilen / (double)toplam;
                ilerle?.Invoke($"İndiriliyor... %{(int)(oran * 100)}", 18 + oran * 62);
            }
            else
            {
                ilerle?.Invoke("İndiriliyor...", 45);
            }
        }
    }
}
