using System.Windows.Media;

namespace SatinalmaPro.Theme;

/// <summary>Workspace 26 — charcoal rail + teal accent (SAP Quartz / Control Center değil).</summary>
public static class AppTheme
{
    public const string PrimaryHex = "#0D7377";
    public const string BackgroundHex = "#EEF2F5";
    public const string CardHex = "#FFFFFF";
    public const string BorderHex = "#D9E2EC";
    public const string TextHex = "#102A43";
    public const string SecondaryTextHex = "#627D98";
    public const string SuccessHex = "#2F9E44";
    public const string WarningHex = "#F08C00";
    public const string DangerHex = "#E03131";
    public const string PurpleHex = "#0D7377";
    public const string NavActiveBgHex = "#E0FCFF";
    public const string NavHoverBgHex = "#F0F4F8";
    public const string ShellHex = "#102A43";

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
    public static Color Shell => Parse(ShellHex);

    public static SolidColorBrush PrimaryBrush => Freeze(new SolidColorBrush(Primary));
    public static SolidColorBrush BackgroundBrush => Freeze(new SolidColorBrush(Background));
    public static SolidColorBrush CardBrush => Freeze(new SolidColorBrush(Card));
    public static SolidColorBrush BorderBrush => Freeze(new SolidColorBrush(Border));
    public static SolidColorBrush TextBrush => Freeze(new SolidColorBrush(Text));
    public static SolidColorBrush SecondaryTextBrush => Freeze(new SolidColorBrush(SecondaryText));
    public static SolidColorBrush ShellBrush => Freeze(new SolidColorBrush(Shell));
    public static SolidColorBrush NavActiveBgBrush => Freeze(new SolidColorBrush(NavActiveBg));

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
