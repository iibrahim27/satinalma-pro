using System.Windows.Media;

namespace SatinalmaPro.Helpers;

public static class CimentoRenkleri
{
    private static readonly Dictionary<string, Color> Renkler = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CEM I"] = Color.FromRgb(254, 243, 199),
        ["CEM II"] = Color.FromRgb(254, 249, 195),
        ["CEM III"] = Color.FromRgb(255, 237, 213),
        ["CEM IV"] = Color.FromRgb(254, 226, 226),
        ["Beyaz Çimento"] = Color.FromRgb(241, 245, 249),
        ["Diğer"] = Color.FromRgb(237, 233, 254)
    };

    private static readonly Color[] YedekRenkler =
    [
        Color.FromRgb(254, 243, 199),
        Color.FromRgb(255, 228, 230),
        Color.FromRgb(219, 234, 254),
        Color.FromRgb(209, 250, 229)
    ];

    public static Brush GetFirca(string cimentoSinifi) =>
        FircaOnbellegi.Al($"cim:{cimentoSinifi}", () => GetRenk(cimentoSinifi));

    public static Color GetRenk(string cimentoSinifi)
    {
        if (string.IsNullOrWhiteSpace(cimentoSinifi))
            return Renkler["Diğer"];

        if (Renkler.TryGetValue(cimentoSinifi.Trim(), out var renk))
            return renk;

        var index = Math.Abs(cimentoSinifi.GetHashCode()) % YedekRenkler.Length;
        return YedekRenkler[index];
    }
}
