using System.Reflection;

namespace SatinalmaYonetici.Helpers;

public static class UygulamaBilgisi
{
    public const string Ad = "Satınalma Yönetici";

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

    public static string AltBilgiMetni => $"{Ad} v{Versiyon}";
}
