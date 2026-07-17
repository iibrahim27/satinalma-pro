namespace SatinalmaPro.Mobile.Services;

using SatinalmaPro.Mobile;

public static class OturumYonlendirmeServisi
{
    public static async Task SplashSonrasiYonlendirAsync(
        IServiceProvider services,
        Action<double, string, string>? ilerle = null,
        Func<OturumServisi, Task>? karsilamaGoster = null)
    {
        var oturum = services.GetRequiredService<OturumServisi>();
        oturum.AyarlariYenile();

        if (!OturumServisi.KayitliOturumVar())
        {
            ilerle?.Invoke(100, "Hazır", "Giriş ekranı açılıyor...");
            LoginSayfasinaGit(services);
            return;
        }

        if (await GuvenliGirisDeposu.GuvenliGirisAktifMiAsync())
        {
            ilerle?.Invoke(100, "Hazır", "Güvenlik doğrulaması...");
            KilitSayfasinaGit(services);
            return;
        }

        ilerle?.Invoke(100, "Hazır", "Oturum açılıyor...");
        if (await oturum.KayitliOturumuDeneAsync())
        {
            if (karsilamaGoster is not null)
                await karsilamaGoster(oturum);

            ShellAc(services);
            return;
        }

        ilerle?.Invoke(100, "Hazır", "Giriş ekranı açılıyor...");
        LoginSayfasinaGit(services);
    }

    public static void ShellAc(IServiceProvider services)
    {
        var oturum = services.GetRequiredService<OturumServisi>();
        KokSayfaServisi.Ayarla(new AppShell(oturum, services));
    }

    public static void LoginSayfasinaGit(IServiceProvider services)
    {
        var login = services.GetRequiredService<Views.LoginPage>();
        KokSayfaServisi.Ayarla(new NavigationPage(login));
    }

    public static void KilitSayfasinaGit(IServiceProvider services)
    {
        var kilit = services.GetRequiredService<Views.KilitAcmaPage>();
        KokSayfaServisi.Ayarla(new NavigationPage(kilit));
    }
}
