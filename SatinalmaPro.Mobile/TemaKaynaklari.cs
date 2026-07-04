namespace SatinalmaPro.Mobile;

public static class TemaKaynaklari
{
    private static bool Koyu => Application.Current?.RequestedTheme == AppTheme.Dark;

    public static Color BirincilMetin => Al("PrimaryTextLight", "PrimaryTextDark");
    public static Color IkincilMetin => Al("SecondaryTextLight", "SecondaryTextDark");
    public static Color SolukMetin => Al("MutedTextLight", "MutedTextDark");
    public static Color VurguMetin => Al("AccentTextLight", "AccentTextDark");
    public static Color KartArkaPlan => Al("CardBackgroundLight", "CardBackgroundDark");
    public static Color KartCerceve => Al("CardBorderLight", "CardBorderDark");
    public static Color OnayPanelArkaPlan => Al("OnayPanelBgLight", "OnayPanelBgDark");
    public static Color OnayPanelMetin => Al("OnayPanelTextLight", "OnayPanelTextDark");
    public static Color Basari => Al("Success", "Success");
    public static Color Tehlike => Al("Danger", "Danger");
    public static Color Marka => Al("BrandPrimary", "BrandPrimaryDark");

    public static Color OneriCerceve => Al("Success", "Success");

    public static Color OneriArkaPlan =>
        Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#14532D")
            : Color.FromArgb("#F0FDF4");

    public static Color Al(string acikAnahtar, string koyuAnahtar)
    {
        var anahtar = Koyu ? koyuAnahtar : acikAnahtar;
        if (Application.Current?.Resources.TryGetValue(anahtar, out var deger) == true && deger is Color renk)
            return renk;

        return Koyu ? Colors.White : Colors.Black;
    }
}
