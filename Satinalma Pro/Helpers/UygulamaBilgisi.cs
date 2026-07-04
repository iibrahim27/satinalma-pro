using System.Reflection;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Helpers;

public static class UygulamaBilgisi
{
    public const string Ad = Shared.Helpers.UygulamaBilgisi.Ad;
    public const string VeriKlasoruAdi = "SatinalmaPro";
    public const string Gelistirici = Shared.Helpers.UygulamaBilgisi.Gelistirici;
    public const string Yazar = Shared.Helpers.UygulamaBilgisi.Yazar;

    public static string Versiyon
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            var bilgi = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(bilgi))
            {
                var arti = bilgi.IndexOf('+', StringComparison.Ordinal);
                return arti > 0 ? bilgi[..arti] : bilgi;
            }

            return asm.GetName().Version?.ToString(3) ?? "1.0.0";
        }
    }

    public static string AltBilgiMetni =>
        Shared.Helpers.UygulamaBilgisi.AltBilgiMetni(Versiyon);
}
