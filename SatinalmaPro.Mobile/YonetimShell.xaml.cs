using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.Views.Yonetim;

namespace SatinalmaPro.Mobile;

public partial class YonetimShell : Shell
{
    public YonetimShell(IServiceProvider services)
    {
        InitializeComponent();

        var tabBar = (TabBar)Items[0];
        tabBar.Items.Add(Sekme("Ana Sayfa", "yonetim-anasayfa", () => services.GetRequiredService<YonetimAnaSayfaPage>()));
        tabBar.Items.Add(Sekme("Talepler", "yonetim-talepler", () => services.GetRequiredService<YonetimTaleplerPage>()));
        tabBar.Items.Add(Sekme("Teklifler", "yonetim-teklifler", () => services.GetRequiredService<YonetimTekliflerPage>()));
        tabBar.Items.Add(Sekme("Bildirimler", "yonetim-bildirimler", () => services.GetRequiredService<YonetimBildirimlerPage>()));
        tabBar.Items.Add(Sekme("Profil", "yonetim-profil", () => services.GetRequiredService<YonetimProfilPage>()));

        Routing.RegisterRoute("yonetim-talep-detay", typeof(YonetimTalepDetayPage));
        Routing.RegisterRoute("yonetim-teklif-detay", typeof(YonetimTeklifDetayPage));
    }

    private static ShellContent Sekme(string baslik, string route, Func<ContentPage> sayfaFabrikasi) =>
        new()
        {
            Title = baslik,
            Route = route,
            ContentTemplate = new DataTemplate(sayfaFabrikasi)
        };

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        if (CurrentPage is ContentPage sayfa)
            SayfaYardimcisi.SurumAltBilgiEkle(sayfa);
    }

    protected override bool OnBackButtonPressed()
    {
        if (GeriNavigasyonServisi.GeriDene())
            return true;

        return base.OnBackButtonPressed();
    }
}
