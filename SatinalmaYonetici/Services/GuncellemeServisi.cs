using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using YerelBilgi = SatinalmaYonetici.Helpers.UygulamaBilgisi;

namespace SatinalmaYonetici.Services;

/// <summary>Açılışta version-yonetici.json kontrol eder; varsa indirip kendini günceller.</summary>
public static class GuncellemeServisi
{
    private const string VarsayilanManifestUrl =
        "https://raw.githubusercontent.com/iibrahim27/satinalma-pro/main/version-yonetici.json";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    static GuncellemeServisi()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd($"SatinalmaYonetici/{YerelBilgi.Versiyon}");
    }

    public static async Task<bool> KontrolEtVeUygulaAsync(
        bool sessiz = true,
        Action<string, double>? ilerle = null,
        CancellationToken iptal = default)
    {
        var ilerleme = sessiz ? null : ilerle;
        var manifestUrl = ManifestUrlOku() ?? VarsayilanManifestUrl;

        ilerleme?.Invoke("Sürüm kontrol ediliyor...", 5);

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
            ilerleme?.Invoke("Güncelleme sunucusuna ulaşılamadı.", 100);
            return false;
        }

        if (string.IsNullOrWhiteSpace(manifest.Version) || string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            return false;

        if (!SurumYardimcisi.GuncellemeGerekli(manifest.Version, manifest.Build, YerelBilgi.Versiyon, 0))
            return false;

        ilerleme?.Invoke($"Yeni sürüm: v{manifest.Version}", 12);

        var adresler = IndirmeAdresleri(manifest).ToList();
        for (var i = 0; i < adresler.Count; i++)
        {
            var (url, tur) = adresler[i];
            var sonDeneme = i == adresler.Count - 1;
            try
            {
                if (tur == IndirmeTuru.KurulumExe)
                {
                    ilerleme?.Invoke("Kurulum indiriliyor...", 20);
                    var yol = Path.Combine(Path.GetTempPath(), $"SatinalmaYonetici_Kurulum_{manifest.Version}.exe");
                    await DosyaIndirAsync(url, yol, ilerleme, iptal);
                    ilerleme?.Invoke("Güncelleniyor...", 92);
                    KurulumCalistirVeKapat(yol);
                    return true;
                }

                var zipYol = Path.Combine(Path.GetTempPath(), $"SatinalmaYonetici_{manifest.Version}.zip");
                var acYol = Path.Combine(Path.GetTempPath(), $"SatinalmaYonetici_{manifest.Version}");
                ilerleme?.Invoke("Güncelleme indiriliyor...", 18);
                await DosyaIndirAsync(url, zipYol, ilerleme, iptal);
                if (Directory.Exists(acYol))
                    Directory.Delete(acYol, true);
                ZipFile.ExtractToDirectory(zipYol, acYol);
                ilerleme?.Invoke("Güncelleniyor...", 92);
                ZipIleGuncelleVeYenidenBaslat(acYol);
                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                if (sonDeneme)
                {
                    ilerleme?.Invoke("Güncelleme indirilemedi, mevcut sürümle devam...", 100);
                    return false;
                }
            }
        }

        return false;
    }

    private static string? ManifestUrlOku()
    {
        try
        {
            var yol = Path.Combine(AppContext.BaseDirectory, "firebase_ayarlar.json");
            if (!File.Exists(yol)) return null;
            using var belge = JsonDocument.Parse(File.ReadAllText(yol));
            if (belge.RootElement.TryGetProperty("yoneticiGuncellemeManifestUrl", out var u))
            {
                var s = u.GetString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
            if (belge.RootElement.TryGetProperty("guncellemeManifestUrl", out var g))
            {
                // Ana uygulamanın version.json'u değil — yonetici için ayrı URL tercih et
                var s = g.GetString();
                if (!string.IsNullOrWhiteSpace(s) && s.Contains("version-yonetici", StringComparison.OrdinalIgnoreCase))
                    return s;
            }
        }
        catch
        {
            // yoksay
        }

        return null;
    }

    private enum IndirmeTuru { KurulumExe, Zip }

    private static IEnumerable<(string url, IndirmeTuru tur)> IndirmeAdresleri(GuncellemeManifesti manifest)
    {
        var gorulen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sonuc = new List<(string, IndirmeTuru)>();

        void Kaydet(string? url, IndirmeTuru tur)
        {
            if (string.IsNullOrWhiteSpace(url) || !gorulen.Add(url)) return;
            sonuc.Add((url, tur));
        }

        var anaTur = manifest.DownloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? IndirmeTuru.KurulumExe
            : IndirmeTuru.Zip;
        Kaydet(manifest.DownloadUrl, anaTur);
        Kaydet(manifest.DownloadUrlZip, IndirmeTuru.Zip);

        if (manifest.DownloadUrl.Contains("SatinalmaYonetici_Kurulum.exe", StringComparison.OrdinalIgnoreCase))
        {
            Kaydet(
                manifest.DownloadUrl.Replace(
                    "SatinalmaYonetici_Kurulum.exe", "SatinalmaYonetici.zip", StringComparison.OrdinalIgnoreCase),
                IndirmeTuru.Zip);
        }

        return sonuc;
    }

    private static async Task DosyaIndirAsync(
        string url, string hedefYol, Action<string, double>? ilerle, CancellationToken iptal)
    {
        using var yanit = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, iptal);
        if (yanit.StatusCode == HttpStatusCode.NotFound)
            throw new HttpRequestException($"404 — {url}");
        yanit.EnsureSuccessStatusCode();

        var toplam = yanit.Content.Headers.ContentLength;
        await using var kaynak = await yanit.Content.ReadAsStreamAsync(iptal);
        await using var hedef = File.Create(hedefYol);
        var tampon = new byte[81920];
        long indirilen = 0;
        int okunan;
        while ((okunan = await kaynak.ReadAsync(tampon, iptal)) > 0)
        {
            await hedef.WriteAsync(tampon.AsMemory(0, okunan), iptal);
            indirilen += okunan;
            if (toplam is > 0)
                ilerle?.Invoke($"İndiriliyor... %{(int)(indirilen * 100.0 / toplam.Value)}", 18 + indirilen * 62.0 / toplam.Value);
        }
    }

    private static string UygulamaExeYolu()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) && File.Exists(Environment.ProcessPath))
            return Environment.ProcessPath;
        return Path.Combine(AppContext.BaseDirectory.TrimEnd('\\', '/'), "SatinalmaYonetici.exe");
    }

    private static void ArkaPlanBetigiBaslat(string batIcerik, string dosyaAdi)
    {
        var bat = Path.Combine(Path.GetTempPath(), dosyaAdi);
        File.WriteAllText(bat, batIcerik, System.Text.Encoding.Default);
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{bat}\"\"",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetTempPath()
        });
    }

    private static string SurecKapanisBetigi(string exeYol) =>
        $"""
        set /a SAYAC=0
        :BEKLE
        tasklist /FI "IMAGENAME eq SatinalmaYonetici.exe" 2>nul | find /I "SatinalmaYonetici.exe" >nul
        if not errorlevel 1 (
          set /a SAYAC+=1
          if !SAYAC! LSS 45 (
            timeout /t 1 /nobreak >nul
            goto BEKLE
          )
          taskkill /IM SatinalmaYonetici.exe /F >nul 2>&1
          timeout /t 2 /nobreak >nul
        )
        if exist "{exeYol}" start "" "{exeYol}"
        """;

    private static void KurulumCalistirVeKapat(string kurulumYol)
    {
        var exeYol = UygulamaExeYolu();
        var bat = $"""
            @echo off
            setlocal EnableDelayedExpansion
            start /wait "" "{kurulumYol}" /VERYSILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /NORESTART
            {SurecKapanisBetigi(exeYol)}
            del "%~f0"
            """;
        ArkaPlanBetigiBaslat(bat, "yonetici_kurulum_sonrasi.bat");
        Application.Current.Shutdown();
    }

    private static void ZipIleGuncelleVeYenidenBaslat(string acikKlasor)
    {
        var hedef = AppContext.BaseDirectory.TrimEnd('\\', '/');
        var exeYol = UygulamaExeYolu();
        var bat = $"""
            @echo off
            setlocal EnableDelayedExpansion
            timeout /t 2 /nobreak >nul
            xcopy /E /Y /I /Q "{acikKlasor}\*" "{hedef}\"
            {SurecKapanisBetigi(exeYol)}
            del "%~f0"
            """;
        ArkaPlanBetigiBaslat(bat, "yonetici_guncelle.bat");
        Application.Current.Shutdown();
    }
}
