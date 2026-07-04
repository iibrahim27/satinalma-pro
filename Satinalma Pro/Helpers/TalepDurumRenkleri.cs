using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class TalepDurumRenkleri
{
    public static (Brush arka, Brush kenar, Brush rozetArka, Brush rozetYazi) Fircalar(string? gorunenDurum)
    {
        var (a, k, ra, ry) = Shared.Helpers.TalepDurumRenkleri.Renkler(gorunenDurum);
        return (Firca(a), Firca(k), Firca(ra), Firca(ry));
    }

    public static void RozetUygula(Border? rozet, TextBlock? metin, string? gorunenDurum)
    {
        if (rozet is null || metin is null)
            return;

        var (_, _, rozetArka, rozetYazi) = Fircalar(gorunenDurum);
        rozet.Background = rozetArka;
        rozet.BorderBrush = rozetYazi;
        metin.Foreground = rozetYazi;
        metin.Text = gorunenDurum ?? "";
    }

    private static SolidColorBrush Firca(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex)!);
}
