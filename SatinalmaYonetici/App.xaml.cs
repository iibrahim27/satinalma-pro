using System.Windows;
using System.Windows.Threading;
using SatinalmaYonetici.Services;

namespace SatinalmaYonetici;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            HataGunlugu.Kaydet(args.Exception, "DispatcherUnhandledException");
            args.Handled = true;
            try
            {
                MessageBox.Show(
                    $"Beklenmeyen hata:\n{args.Exception.Message}\n\nAyrıntı: {HataGunlugu.DosyaYolu}",
                    "Satınalma Yönetici",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // UI yoksa sessiz
            }
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                HataGunlugu.Kaydet(ex, "AppDomain.UnhandledException");
            else
                HataGunlugu.Kaydet(args.ExceptionObject?.ToString() ?? "bilinmeyen", "AppDomain");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            HataGunlugu.Kaydet(args.Exception, "UnobservedTaskException");
            args.SetObserved();
        };

        base.OnStartup(e);

        try
        {
            // Zorunlu değilse sessiz kurulum/kapanış yapma — panel açılışta ölmesin.
            if (await GuncellemeServisi.KontrolEtVeUygulaAsync(sessiz: true))
                return;
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "App.Guncelleme");
        }

        try
        {
            var ana = new MainWindow();
            MainWindow = ana;
            ana.Show();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "App.MainWindow");
            MessageBox.Show(
                $"Uygulama açılamadı:\n{ex.Message}\n\nLog: {HataGunlugu.DosyaYolu}",
                "Satınalma Yönetici",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
