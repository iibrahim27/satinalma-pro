using System.Windows.Media;

namespace SatinalmaPro.Helpers;

public static class FiloRenkleri
{
    private static readonly Dictionary<string, Color> Renkler = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Varlık"] = Color.FromRgb(219, 234, 254),
        ["Tamir"] = Color.FromRgb(255, 228, 230),
        ["Bakım"] = Color.FromRgb(254, 243, 199),
        ["Muayene"] = Color.FromRgb(209, 250, 229),
        ["Sigorta"] = Color.FromRgb(237, 233, 254),
        ["Kasko"] = Color.FromRgb(224, 242, 254),
        ["Zimmet"] = Color.FromRgb(204, 251, 241),
        ["Zimmet İade"] = Color.FromRgb(241, 245, 249)
    };

    public static Brush GetFirca(string kayitTipi) =>
        FircaOnbellegi.Al($"filo:{kayitTipi}", () =>
            Renkler.TryGetValue(kayitTipi.Trim(), out var renk)
                ? renk
                : Color.FromRgb(241, 245, 249));
}
