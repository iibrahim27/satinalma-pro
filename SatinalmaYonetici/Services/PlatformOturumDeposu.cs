using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SatinalmaYonetici.Services;

/// <summary>Windows DPAPI ile platform oturum (refresh token) saklama.</summary>
public static class PlatformOturumDeposu
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static string DosyaYolu =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SatinalmaYonetici",
            "oturum.dat");

    public sealed class OturumPaketi
    {
        public string RefreshToken { get; set; } = "";
        public string Eposta { get; set; } = "";
        public bool BeniHatirla { get; set; }
    }

    public static void Kaydet(string refreshToken, string eposta, bool beniHatirla)
    {
        if (!beniHatirla || string.IsNullOrWhiteSpace(refreshToken))
        {
            Sil();
            return;
        }

        var paket = new OturumPaketi
        {
            RefreshToken = refreshToken,
            Eposta = eposta.Trim(),
            BeniHatirla = true
        };
        var ham = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(paket, Json));
        var korunan = ProtectedData.Protect(ham, null, DataProtectionScope.CurrentUser);
        var klasor = Path.GetDirectoryName(DosyaYolu)!;
        Directory.CreateDirectory(klasor);
        File.WriteAllBytes(DosyaYolu, korunan);
    }

    public static OturumPaketi? Oku()
    {
        if (!File.Exists(DosyaYolu))
            return null;

        try
        {
            var korunan = File.ReadAllBytes(DosyaYolu);
            var ham = ProtectedData.Unprotect(korunan, null, DataProtectionScope.CurrentUser);
            var paket = JsonSerializer.Deserialize<OturumPaketi>(Encoding.UTF8.GetString(ham), Json);
            if (paket is null || !paket.BeniHatirla || string.IsNullOrWhiteSpace(paket.RefreshToken))
            {
                Sil();
                return null;
            }

            return paket;
        }
        catch
        {
            Sil();
            return null;
        }
    }

    public static void Sil()
    {
        try
        {
            if (File.Exists(DosyaYolu))
                File.Delete(DosyaYolu);
        }
        catch
        {
            // yoksay
        }
    }
}
