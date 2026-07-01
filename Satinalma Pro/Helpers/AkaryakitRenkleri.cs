using System.Windows.Media;

namespace SatinalmaPro.Helpers;

public static class AkaryakitRenkleri
{
    private static readonly Dictionary<string, Color> Renkler = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Araç"] = Color.FromRgb(219, 234, 254),
        ["İş Makinası"] = Color.FromRgb(254, 243, 199),
        ["Motorin"] = Color.FromRgb(254, 249, 195),
        ["Benzin"] = Color.FromRgb(255, 228, 230),
        ["AdBlue"] = Color.FromRgb(224, 242, 254),
        ["Diğer"] = Color.FromRgb(241, 245, 249)
    };

    public static Brush GetFirca(string aracTipi) =>
        FircaOnbellegi.Al($"yak:{aracTipi}", () =>
            Renkler.TryGetValue(aracTipi.Trim(), out var renk) ? renk : Renkler["Diğer"]);
}
