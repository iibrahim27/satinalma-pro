using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UygulamaKurulumServisi.BaslatmaKontrolu();
        _ = FirebaseAyarServisi.PaketDosyalariniHazirlaAsync();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            System.Diagnostics.Debug.WriteLine($"Unhandled: {e.ExceptionObject}");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"Task hata: {e.Exception}");
            e.SetObserved();
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            var services = IPlatformApplication.Current?.Services
                ?? MauiProgram.Services
                ?? throw new InvalidOperationException("Servisler yüklenemedi.");
            return new Window(new NavigationPage(new Views.AcilisEkraniPage()));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return new Window(new ContentPage
            {
                Content = new Label
                {
                    Text = "Başlatma hatası: " + ex.Message,
                    Margin = 20,
                    VerticalOptions = LayoutOptions.Center
                }
            });
        }
    }
}
