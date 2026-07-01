namespace SatinalmaPro.Mobile.Services;

/// <summary>
/// Sürüm bilgisini kaydeder. Oturum yalnızca kullanıcı çıkış yaptığında temizlenir.
/// </summary>
public static class UygulamaKurulumServisi
{
    private const string SurumAnahtari = "uyg_kurulum_surum";

    public static void BaslatmaKontrolu()
    {
        var surum = $"{AppInfo.VersionString}|{AppInfo.BuildString}";
        Preferences.Default.Set(SurumAnahtari, surum);
    }

    public static void OturumVerisiniTemizle()
    {
        Preferences.Default.Remove(OturumServisi.OturumAnahtari);
        Preferences.Default.Remove(OturumServisi.FcmTokenAnahtari);
        _ = GuvenliGirisDeposu.GuvenliGirisTemizleAsync();
    }

    public static bool KayitliOturumVar() =>
        !string.IsNullOrWhiteSpace(Preferences.Default.Get(OturumServisi.OturumAnahtari, (string?)null));
}
