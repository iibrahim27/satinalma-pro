namespace SatinalmaPro.Shared.Helpers;

/// <summary>
/// Talep Pro (ayrı exe) çalışırken etkinleştirilir. Satınalma Pro tam sürümünü etkilemez.
/// </summary>
public static class TalepProRuntime
{
    public static bool Aktif { get; private set; }

    public static void Etkinlestir() => Aktif = true;
}
