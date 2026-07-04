using System.Windows.Media;

namespace SatinalmaPro.Helpers;

public static class AgregaRenkleri
{
    private static readonly Dictionary<string, Color> Renkler = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Mıcır"] = Color.FromRgb(224, 242, 254),
        ["Kum"] = Color.FromRgb(254, 249, 195),
        ["Çakıl"] = Color.FromRgb(237, 233, 254),
        ["Karışım"] = Color.FromRgb(209, 250, 229),
        ["Dolgu"] = Color.FromRgb(255, 228, 230),
        ["Diğer"] = Color.FromRgb(241, 245, 249)
    };

    private static readonly Color[] YedekRenkler =
    [
        Color.FromRgb(204, 251, 241),
        Color.FromRgb(219, 234, 254),
        Color.FromRgb(254, 226, 226),
        Color.FromRgb(233, 213, 255)
    ];

    public static Brush GetFirca(string agregaTuru) =>
        FircaOnbellegi.Al($"agr:{agregaTuru}", () => GetRenk(agregaTuru));

    public static Color GetRenk(string agregaTuru)
    {
        if (string.IsNullOrWhiteSpace(agregaTuru))
            return Renkler["Diğer"];

        if (Renkler.TryGetValue(agregaTuru.Trim(), out var renk))
            return renk;

        var index = Math.Abs(agregaTuru.GetHashCode()) % YedekRenkler.Length;
        return YedekRenkler[index];
    }
}
