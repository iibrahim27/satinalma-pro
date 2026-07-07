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
                var oneGetirildi = TekOrnekKorumasi.IkinciOrnekSinyaliGonder()
                    || TekOrnekKorumasi.MevcutPencereyiOneGetir();
                if (!oneGetirildi && TekOrnekKorumasi.TakiliSurecVarMi())
                {
                    var cevap = MessageBox.Show(
                        $"{UygulamaBilgisi.Ad} arka planda takılı kalmış görünüyor (pencere yok).\n\n" +
                        "Takılı süreci sonlandırıp yeniden açmak ister misiniz?",
                        UygulamaBilgisi.Ad,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (cevap == MessageBoxResult.Yes
                        && TekOrnekKorumasi.TakiliSureciSonlandir()
                        && TekOrnekKorumasi.IlkOrnekMi(e.Args))
                    {
                        UygulamayiBaslat(e);
                        return;
                    }
                }

                Shutdown();
                return;
            }

            Shutdown();
            return;
        }

        UygulamayiBaslat(e);
    }

    private void UygulamayiBaslat(StartupEventArgs e)
    {
        base.OnStartup(e);
        MasaustuActionCenterBildirim.Baslat();
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        DispatcherUnhandledException += (_, args) =>
        {
            HataGunlugu.Kaydet(args.Exception, "UI");
            MessageBox.Show(
                $"Beklenmeyen bir hata oluştu:\n{args.Exception.Message}\n\nDetaylar hata günlüğüne kaydedildi.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                HataGunlugu.Kaydet(ex, "AppDomain");
        };

        var splash = new AcilisEkrani();
        PencereOneGetirDinleyicisi.Bagla(splash);
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

            if (!await splash.OturumAcAsync())
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
            PencereOneGetirDinleyicisi.Bagla(main);
            main.Show();
            MasaustuTepsiYoneticisi.Bagla(main);
            splash.Kapat();
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
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
        MasaustuTepsiYoneticisi.Temizle();
        OturumYoneticisi.UygulamaKapanirken();
        TekOrnekKorumasi.SerbestBirak();
        base.OnExit(e);
    }
}
