using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SatinalmaPro.Helpers;

/// <summary>Run gibi Visual olmayan öğelerde VisualTreeHelper.GetParent patlamasın diye.</summary>
public static class VisualTreeYardimcisi
{
    public static DependencyObject? GetParent(DependencyObject current)
    {
        while (true)
        {
            if (current is Visual or Visual3D)
                return VisualTreeHelper.GetParent(current);

            if (current is TextElement { Parent: DependencyObject metinUst })
            {
                current = metinUst;
                continue;
            }

            var mantiksal = LogicalTreeHelper.GetParent(current);
            if (mantiksal is null)
                return null;

            current = mantiksal;
        }
    }

    public static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T match)
                return match;

            source = GetParent(source);
        }

        return null;
    }
}
