using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.ViewModels;
using SatinalmaPro.Mobile.Views;

namespace SatinalmaPro.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<OturumServisi>();
        builder.Services.AddSingleton<IBiyometrikKimlikServisi, BiyometrikKimlikServisi>();
#if ANDROID
        builder.Services.AddSingleton<IFcmPlatformServisi, FcmPlatformServisi>();
#else
        builder.Services.AddSingleton<IFcmPlatformServisi, FcmPlatformServisiStub>();
#endif

#if ANDROID
        builder.Services.AddSingleton<IApkKurulumServisi, ApkKurulumServisi>();
#endif

        builder.Services.AddTransient<AcilisEkraniPage>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<KilitAcmaViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<TalepListViewModel>();
        builder.Services.AddTransient<OnayBekleyenTaleplerViewModel>();
        builder.Services.AddTransient<OnaylananTaleplerViewModel>();
        builder.Services.AddTransient<YonetimOnayViewModel>();

        builder.Services.AddTransient<AcilOnayPage>();

        builder.Services.AddTransient<OnaylananMalzemelerPage>();
        builder.Services.AddTransient<StokAktarPage>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<KilitAcmaPage>();
        builder.Services.AddTransient<AnaSayfaPage>();
        builder.Services.AddTransient<TalepListPage>();
        builder.Services.AddTransient<OnayBekleyenTaleplerPage>();
        builder.Services.AddTransient<OnaylananTaleplerPage>();
        builder.Services.AddTransient<TalepDetayPage>();
        builder.Services.AddTransient<YeniTalepPage>();
        builder.Services.AddTransient<TeklifBekleyenPage>();
        builder.Services.AddTransient<YonetimOnaylananTekliflerPage>();
        builder.Services.AddTransient<RedTaleplerPage>();
        builder.Services.AddTransient<GecmisTaleplerPage>();
        builder.Services.AddTransient<GecmisTeklifliOnaylarPage>();
        builder.Services.AddTransient<GelenTaleplerPage>();
        builder.Services.AddTransient<TeklifGirisPage>();
        builder.Services.AddTransient<TeklifKarsilastirmaPage>();
        builder.Services.AddTransient<TeklifOnayPage>();
        builder.Services.AddTransient<TeklifOnayDetayPage>();
        builder.Services.AddTransient<OnayGecmisiPage>();
        builder.Services.AddTransient<OnayGecmisiDetayPage>();
        builder.Services.AddTransient<StokDurumPage>();
        builder.Services.AddTransient<StokHareketPage>();
        builder.Services.AddTransient<StokGirisPage>();
        builder.Services.AddTransient<StokCikisPage>();
        builder.Services.AddTransient<StokSayimPage>();
        builder.Services.AddTransient<TeklifsizFirmaFiyatPage>();
        builder.Services.AddTransient<BildirimPage>();
        builder.Services.AddTransient<ProfilPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        Services = app.Services;

        LocalNotificationCenter.Current.NotificationActionTapped += async (e) =>
        {
            if (!e.IsTapped || string.IsNullOrWhiteSpace(e.Request.ReturningData))
                return;
            await BildirimNavigasyonServisi.RouteGitAsync(e.Request.ReturningData);
        };

        return app;
    }

    public static IServiceProvider? Services { get; private set; }
}
