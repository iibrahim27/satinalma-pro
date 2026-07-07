using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SatinalmaPro.Services;

/// <summary>Windows DPAPI ile yerel şifre hatırlama — yalnızca bu kullanıcı/cihaz.</summary>
public static class GirisSifreDeposu
{
    private static readonly string DosyaYolu = SatinalmaProKlasor.DosyaYolu("giris_sifre.dat");

    public static void Kaydet(string sifre)
    {
        if (string.IsNullOrEmpty(sifre))
        {
            Sil();
            return;
        }

        SatinalmaProKlasor.Olustur();
        var ham = Encoding.UTF8.GetBytes(sifre);
        var korunan = ProtectedData.Protect(ham, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(DosyaYolu, korunan);
    }

    public static string? Oku()
    {
        if (!File.Exists(DosyaYolu))
            return null;

        try
        {
            var korunan = File.ReadAllBytes(DosyaYolu);
            var ham = ProtectedData.Unprotect(korunan, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(ham);
        }
        catch
        {
            Sil();
            return null;
        }
    }

    public static void Sil()
    {
        if (!File.Exists(DosyaYolu))
            return;

        try { File.Delete(DosyaYolu); } catch { /* yoksay */ }
    }
}
