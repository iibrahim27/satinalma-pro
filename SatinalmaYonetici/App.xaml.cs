using System.Windows;
using SatinalmaYonetici.Services;

namespace SatinalmaYonetici;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Açılışta sessiz sürüm kontrolü — yeni sürüm varsa indirip yeniden başlatır
            if (await GuncellemeServisi.KontrolEtVeUygulaAsync(sessiz: true))
                return; // Shutdown zaten çağrıldı
        }
        catch
        {
            // Ağ yoksa devam et
        }

        var ana = new MainWindow();
        MainWindow = ana;
        ana.Show();
    }
}
