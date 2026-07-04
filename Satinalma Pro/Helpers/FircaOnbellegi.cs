using System.Windows.Media;

namespace SatinalmaPro.Helpers;

public static class FircaOnbellegi
{
    private static readonly Dictionary<string, Brush> Onbellek = new(StringComparer.OrdinalIgnoreCase);

    public static Brush Al(string anahtar, Color renk)
    {
        if (!Onbellek.TryGetValue(anahtar, out var firca))
        {
            firca = new SolidColorBrush(renk);
            if (firca.CanFreeze)
                firca.Freeze();
            Onbellek[anahtar] = firca;
        }

        return firca;
    }

    public static Brush Al(string anahtar, Func<Color> renkUret)
    {
        if (!Onbellek.TryGetValue(anahtar, out var firca))
        {
            firca = new SolidColorBrush(renkUret());
            if (firca.CanFreeze)
                firca.Freeze();
            Onbellek[anahtar] = firca;
        }

        return firca;
    }
}
