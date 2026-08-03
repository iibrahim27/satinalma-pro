using Microsoft.Win32;
using System.IO;

namespace TalepPro.Helpers;

/// <summary>Windows oturum açılışında Talep Pro'yu tepsi modunda başlatır.</summary>
public static class TalepProOtomatikBaslatma
{
    private const string RunKeyYolu = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string DegerAdi = "TalepPro";

    public static void Etkinlestir()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
                return;

            var komut = $"\"{exe}\" {TalepProTekOrnek.ArkaPlanBaslatArg}";
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
