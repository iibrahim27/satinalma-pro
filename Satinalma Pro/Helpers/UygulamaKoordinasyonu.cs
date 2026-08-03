using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SatinalmaPro.Helpers;

/// <summary>Satınalma Pro ↔ Talep Pro süreçler arası yönlendirme (aynı AppData / oturum).</summary>
public static class UygulamaKoordinasyonu
{
    private static string KuyrukDosyasi =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SatinalmaPro",
            "koordinasyon_kuyruk.json");

    public static void SatinalmaProModulAc(string? modulAdi)
    {
        if (!string.IsNullOrWhiteSpace(modulAdi))
            KuyrugaYaz(new KoordinasyonKomutu { Hedef = "SatinalmaPro", Modul = modulAdi });

        var exe = SatinalmaProExeYolu();
        if (exe is null)
        {
            System.Windows.MessageBox.Show(
                "Satınalma Pro bulunamadı. Kurulum klasöründe SatinalmaPro.exe olmalı.",
                "Talep Pro",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(exe)!
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Satınalma Pro açılamadı:\n{ex.Message}",
                "Talep Pro",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public static void TalepProAc(Guid? talepId = null, string? sekme = null)
    {
        var args = new List<string>();
        if (talepId is { } id)
            args.Add($"--talep={id:D}");
        if (!string.IsNullOrWhiteSpace(sekme))
            args.Add($"--sekme={sekme}");

        var exe = TalepProExeYolu();
        if (exe is null)
        {
            System.Windows.MessageBox.Show(
                "Talep Pro kurulu değil.\nSatınalma Pro kurulumunu yeniden çalıştırın; Talep Pro otomatik eklenir.",
                UygulamaBilgisi.Ad,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = string.Join(' ', args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(exe)!
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Talep Pro açılamadı:\n{ex.Message}",
                UygulamaBilgisi.Ad,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public static string? BekleyenProModulunuAl()
    {
        try
        {
            if (!File.Exists(KuyrukDosyasi))
                return null;
            var json = File.ReadAllText(KuyrukDosyasi);
            File.Delete(KuyrukDosyasi);
            var komut = JsonSerializer.Deserialize<KoordinasyonKomutu>(json);
            if (komut is not null &&
                string.Equals(komut.Hedef, "SatinalmaPro", StringComparison.OrdinalIgnoreCase))
                return komut.Modul;
        }
        catch
        {
            /* ignore */
        }
        return null;
    }

    private static void KuyrugaYaz(KoordinasyonKomutu komut)
    {
        try
        {
            var dir = Path.GetDirectoryName(KuyrukDosyasi)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(KuyrukDosyasi, JsonSerializer.Serialize(komut));
        }
        catch
        {
            /* ignore */
        }
    }

    private static string? TalepProExeYolu()
    {
        // Önce geliştirme çıktısı, sonra kurulum (yan yana) klasörü
        var adaylar = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TalepPro", "bin", "Release", "net9.0-windows10.0.17763.0", "TalepPro.exe")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TalepPro", "bin", "Debug", "net9.0-windows10.0.17763.0", "TalepPro.exe")),
            Path.Combine(AppContext.BaseDirectory, "TalepPro.exe"),
        };
        return adaylar.FirstOrDefault(File.Exists);
    }

    private static string? SatinalmaProExeYolu()
    {
        var adaylar = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SatinalmaPro.exe"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Satinalma Pro", "bin", "Release", "net9.0-windows10.0.17763.0", "SatinalmaPro.exe")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Satinalma Pro", "bin", "Debug", "net9.0-windows10.0.17763.0", "SatinalmaPro.exe")),
        };
        return adaylar.FirstOrDefault(File.Exists);
    }

    private sealed class KoordinasyonKomutu
    {
        public string Hedef { get; set; } = "";
        public string? Modul { get; set; }
    }
}
