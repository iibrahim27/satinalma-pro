using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SharedSurum = SatinalmaPro.Shared.Helpers.SurumYardimcisi;

namespace SatinalmaPro.Services;

public static class GuncellemeServisi
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    static GuncellemeServisi()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd($"SatinalmaPro/{UygulamaBilgisi.Versiyon}");
    }

    public static async Task<bool> KontrolEtVeUygulaAsync(
        Action<string, double>? ilerle = null,
        CancellationToken iptal = default) =>
        await KontrolEtVeUygulaAsync(sessiz: ilerle is null, ilerle, iptal);

    public static async Task<bool> KontrolEtVeUygulaAsync(
        bool sessiz,
        Action<string, double>? ilerle = null,
        CancellationToken iptal = default)
    {
        var ilerleme = sessiz ? null : ilerle;
        var manifestUrl = FirebaseAyarDeposu.Ayarlar.GuncellemeManifestUrl;
        if (string.IsNullOrWhiteSpace(manifestUrl))
            return false;

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
            ilerleme?.Invoke("Güncelleme sunucusuna ulaşılamadı, devam ediliyor...", 100);
            return false;
        }

        if (string.IsNullOrWhiteSpace(manifest.Version) || string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            return false;

        var mevcut = UygulamaBilgisi.Versiyon;
        if (!SharedSurum.GuncellemeGerekli(manifest.Version, manifest.Build, mevcut, 0))
            return false;

        ilerleme?.Invoke($"Yeni sürüm bulundu: v{manifest.Version}", 12);

        var adresler = IndirmeAdresleri(manifest).ToList();
        for (var i = 0; i < adresler.Count; i++)
        {
            var (url, tur) = adresler[i];
            var sonDeneme = i == adresler.Count - 1;

            try
            {
                if (tur == IndirmeTuru.KurulumExe)
                {
                    ilerleme?.Invoke("Kurulum dosyası indiriliyor...", 20);
                    var kurulumYol = Path.Combine(Path.GetTempPath(), $"SatinalmaPro_Kurulum_{manifest.Version}.exe");
                    await DosyaIndirAsync(url, kurulumYol, ilerleme, iptal);
                    ilerleme?.Invoke("Güncelleniyor, lütfen bekleyin...", 92);
                    KurulumCalistirVeKapat(kurulumYol);
                    return true;
                }

                var zipYol = Path.Combine(Path.GetTempPath(), $"SatinalmaPro_{manifest.Version}.zip");
                var acYol = Path.Combine(Path.GetTempPath(), $"SatinalmaPro_{manifest.Version}");

                ilerleme?.Invoke("Güncelleme indiriliyor...", 18);
                await DosyaIndirAsync(url, zipYol, ilerleme, iptal);

                ilerleme?.Invoke("Güncelleme hazırlanıyor...", 82);
                if (Directory.Exists(acYol))
                    Directory.Delete(acYol, true);
                ZipFile.ExtractToDirectory(zipYol, acYol);

                ilerleme?.Invoke("Güncelleniyor, lütfen bekleyin...", 92);
                ZipIleGuncelleVeYenidenBaslat(acYol);
                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                if (sonDeneme)
                {
                    ilerleme?.Invoke(GuncellemeHataMetni(ex), 100);
                    await Task.Delay(800, iptal);
                    return false;
                }

                ilerleme?.Invoke("Alternatif indirme deneniyor...", 15);
            }
        }

        return false;
    }

    private enum IndirmeTuru { KurulumExe, Zip }

    private static IEnumerable<(string url, IndirmeTuru tur)> IndirmeAdresleri(GuncellemeManifesti manifest)
    {
        var gorulen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Kaydet(string? url, IndirmeTuru tur, List<(string, IndirmeTuru)> liste)
        {
            if (string.IsNullOrWhiteSpace(url) || !gorulen.Add(url))
                return;
            liste.Add((url, tur));
        }

        var sonuc = new List<(string, IndirmeTuru)>();
        var anaTur = manifest.DownloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? IndirmeTuru.KurulumExe
            : IndirmeTuru.Zip;

        Kaydet(manifest.DownloadUrl, anaTur, sonuc);
        Kaydet(manifest.DownloadUrlZip, IndirmeTuru.Zip, sonuc);

        if (manifest.DownloadUrl.Contains("SatinalmaPro_Kurulum.exe", StringComparison.OrdinalIgnoreCase))
        {
            var zipUrl = manifest.DownloadUrl.Replace(
                "SatinalmaPro_Kurulum.exe", "SatinalmaPro.zip", StringComparison.OrdinalIgnoreCase);
            Kaydet(zipUrl, IndirmeTuru.Zip, sonuc);
        }

        return sonuc;
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

    private static string UygulamaExeYolu()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) &&
            File.Exists(Environment.ProcessPath))
            return Environment.ProcessPath;

        return Path.Combine(
            AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            "SatinalmaPro.exe");
    }

    private static void ArkaPlanBetigiBaslat(string batIcerik, string batDosyaAdi)
    {
        var bat = Path.Combine(Path.GetTempPath(), batDosyaAdi);
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
        tasklist /FI "IMAGENAME eq SatinalmaPro.exe" 2>nul | find /I "SatinalmaPro.exe" >nul
        if not errorlevel 1 (
          set /a SAYAC+=1
          if !SAYAC! LSS 45 (
            timeout /t 1 /nobreak >nul
            goto BEKLE
          )
          taskkill /IM SatinalmaPro.exe /F >nul 2>&1
          timeout /t 2 /nobreak >nul
        )
        if exist "{exeYol}" start "" "{exeYol}" {TekOrnekKorumasi.GuncellemeSonrasiArg}
        """;

    private static void KurulumCalistirVeKapat(string kurulumYol)
    {
        var exeYol = UygulamaExeYolu();
        var batIcerik = $"""
            @echo off
            setlocal EnableDelayedExpansion
            start /wait "" "{kurulumYol}" /VERYSILENT /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS /NORESTART
            {SurecKapanisBetigi(exeYol)}
            del "%~f0"
            """;

        ArkaPlanBetigiBaslat(batIcerik, "satinalma_kurulum_sonrasi.bat");
        Application.Current.Shutdown();
    }

    private static void ZipIleGuncelleVeYenidenBaslat(string acikKlasor)
    {
        var hedef = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var exeYol = UygulamaExeYolu();
        var batIcerik = $"""
            @echo off
            setlocal EnableDelayedExpansion
            timeout /t 2 /nobreak >nul
            xcopy /E /Y /I /Q "{acikKlasor}\*" "{hedef}\"
            {SurecKapanisBetigi(exeYol)}
            del "%~f0"
            """;

        ArkaPlanBetigiBaslat(batIcerik, "satinalma_guncelle.bat");
        Application.Current.Shutdown();
    }
}
