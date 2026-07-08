using Microsoft.Win32;
using System.IO;

namespace SatinalmaPro.Helpers;

/// <summary>Windows oturum açılışında uygulamayı tepsi modunda başlatır.</summary>
public static class WindowsOtomatikBaslatma
{
    private const string RunKeyYolu = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string DegerAdi = "SatinalmaPro";

    public static void Etkinlestir()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
                return;

            var komut = $"\"{exe}\" {TekOrnekKorumasi.ArkaPlanBaslatArg}";
            using var anahtar = Registry.CurrentUser.OpenSubKey(RunKeyYolu, writable: true);
            anahtar?.SetValue(DegerAdi, komut);
        }
        catch
        {
            // isteğe bağlı
        }
    }

    public static bool EtkinMi()
    {
        try
        {
            using var anahtar = Registry.CurrentUser.OpenSubKey(RunKeyYolu, false);
            return anahtar?.GetValue(DegerAdi) is string;
        }
        catch
        {
            return false;
        }
    }
}
