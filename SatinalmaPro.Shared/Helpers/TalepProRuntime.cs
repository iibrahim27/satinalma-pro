using System.Reflection;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>
/// Talep Pro (ayrı exe) çalışırken etkinleştirilir. Satınalma Pro tam sürümünü etkilemez.
/// </summary>
public static class TalepProRuntime
{
    public static bool Aktif { get; private set; }

    public static void Etkinlestir() => Aktif = true;

    /// <summary>TalepPro.exe ile açıldıysa modu etkinleştir (App başlatma sırasından bağımsız güvence).</summary>
    public static void EtkinlestirGerekirse()
    {
        var ad = Assembly.GetEntryAssembly()?.GetName().Name;
        if (string.Equals(ad, "TalepPro", StringComparison.OrdinalIgnoreCase))
            Etkinlestir();
    }
}
