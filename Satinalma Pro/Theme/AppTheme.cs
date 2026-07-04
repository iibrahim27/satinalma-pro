using System.Windows.Media;

namespace SatinalmaPro.Theme;

/// <summary>Ana sayfa ve shell renk sabitleri — tek kaynak.</summary>
public static class AppTheme
{
    public const string PrimaryHex = "#2563EB";
    public const string BackgroundHex = "#F7F9FC";
    public const string CardHex = "#FFFFFF";
    public const string BorderHex = "#E8EDF5";
    public const string TextHex = "#111827";
    public const string SecondaryTextHex = "#64748B";
    public const string SuccessHex = "#10B981";
    public const string WarningHex = "#F59E0B";
    public const string DangerHex = "#EF4444";
    public const string PurpleHex = "#8B5CF6";
    public const string NavActiveBgHex = "#EAF2FF";
    public const string NavHoverBgHex = "#F1F5F9";

    public static Color Primary => Parse(PrimaryHex);
    public static Color Background => Parse(BackgroundHex);
    public static Color Card => Parse(CardHex);
    public static Color Border => Parse(BorderHex);
    public static Color Text => Parse(TextHex);
    public static Color SecondaryText => Parse(SecondaryTextHex);
    public static Color Success => Parse(SuccessHex);
    public static Color Warning => Parse(WarningHex);
    public static Color Danger => Parse(DangerHex);
    public static Color Purple => Parse(PurpleHex);
    public static Color NavActiveBg => Parse(NavActiveBgHex);
    public static Color NavHoverBg => Parse(NavHoverBgHex);

    public static SolidColorBrush PrimaryBrush => Freeze(new SolidColorBrush(Primary));
    public static SolidColorBrush BackgroundBrush => Freeze(new SolidColorBrush(Background));
    public static SolidColorBrush CardBrush => Freeze(new SolidColorBrush(Card));
    public static SolidColorBrush BorderBrush => Freeze(new SolidColorBrush(Border));
    public static SolidColorBrush TextBrush => Freeze(new SolidColorBrush(Text));
    public static SolidColorBrush SecondaryTextBrush => Freeze(new SolidColorBrush(SecondaryText));

    public static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    public static SolidColorBrush Brush(string hex) => Freeze(new SolidColorBrush(Parse(hex)));

    public static SolidColorBrush TintBrush(Color baseColor, byte alpha = 28) =>
        Freeze(new SolidColorBrush(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B)));

    private static SolidColorBrush Freeze(SolidColorBrush brush)
    {
        if (brush.CanFreeze)
            brush.Freeze();
        return brush;
    }
}
