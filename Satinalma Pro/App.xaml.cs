using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Views;

namespace SatinalmaPro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (!TekOrnekKorumasi.IlkOrnekMi(e.Args))
        {
            if (!TekOrnekKorumasi.ToastAktivasyonuMu(e.Args))
            {
                TekOrnekKorumasi.MevcutPencereyiOneGetir();
                MessageBox.Show(
                    $"{UygulamaBilgisi.Ad} zaten çalışıyor.",
                    UygulamaBilgisi.Ad,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            Shutdown();
            return;
        }

        base.OnStartup(e);
        MasaustuActionCenterBildirim.Baslat();
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, e) =>
        {
            HataGunlugu.Kaydet(e.Exception, "UI");
            MessageBox.Show(
                $"Beklenmeyen bir hata oluştu:\n{e.Exception.Message}\n\nDetaylar hata günlüğüne kaydedildi.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                HataGunlugu.Kaydet(ex, "AppDomain");
        };

        var splash = new AcilisEkrani();
        splash.Show();

        _ = BaslatAsync(splash);
    }

    private async Task BaslatAsync(AcilisEkrani splash)
    {
        try
        {
            if (await splash.YukleVeBekleAsync())
            {
                Shutdown();
                return;
            }

            if (!GirisPenceresi.OturumAc(splash))
            {
                splash.Close();
                Shutdown();
                return;
            }

            if (OturumYoneticisi.GirisYapildi)
            {
                splash.SenkronBaslat();
                using var zamanAsimi = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var ilerleme = new Progress<(int tamamlanan, int toplam, string adim)>(p =>
                    splash.SenkronIlerle(p.tamamlanan, p.toplam, p.adim));

                try
                {
                    await BulutVeriSenkronu.IkiliSenkronAsync(ilerleme, zamanAsimi.Token);
                    splash.DurumGuncelle("Veriler yüklendi, uygulama açılıyor...");
                }
                catch (OperationCanceledException)
                {
                    splash.DurumGuncelle("Bulut senkronu zaman aşımına uğradı. Yerel verilerle devam ediliyor...");
                }
                catch (Exception)
                {
                    splash.DurumGuncelle("Bulut bağlantısı kurulamadı. Yerel verilerle devam ediliyor...");
                }

                VeriYukleyici.TumunuYukle();
                BulutVeriSenkronu.YoklamayiBaslat();
                BildirimYoneticisi.Baslat();
            }
            else
            {
                VeriYukleyici.TumunuYukle();
            }

            var main = new MainWindow();
            MainWindow = main;
            main.Show();
            splash.Kapat();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Uygulama başlatılamadı:\n{ex.Message}",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            splash.Kapat();
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        OturumYoneticisi.UygulamaKapanirken();
        TekOrnekKorumasi.SerbestBirak();
        base.OnExit(e);
    }
}
