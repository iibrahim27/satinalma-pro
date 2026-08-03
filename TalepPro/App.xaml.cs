using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Views;
using TalepPro.Helpers;

namespace TalepPro;

public partial class App : Application
{
    private string[] _args = [];
    private string[]? _bekleyenDeepLink;
    private bool _arkaPlanModu;

    protected override void OnStartup(StartupEventArgs e)
    {
        _args = e.Args ?? [];
        _arkaPlanModu = TalepProTekOrnek.ArkaPlanBaslatMi(_args);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        if (!TalepProTekOrnek.IlkOrnekMi(_args))
        {
            // İkinci örnek: sessizce öne getir (otomatik başlat / kısayol)
            TalepProTekOrnek.IkinciOrnekSinyaliGonder(_args);
            Shutdown();
            return;
        }

        TalepProOtomatikBaslatma.Etkinlestir();
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            HataGunlugu.Kaydet(args.Exception, "TalepPro.UI");
            MessageBox.Show(
                $"Beklenmeyen bir hata oluştu:\n{args.Exception.Message}",
                "Talep Pro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            OturumYoneticisi.Baslat();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "TalepPro.OturumBaslat");
            if (!_arkaPlanModu)
            {
                MessageBox.Show(
                    $"Oturum servisi başlatılamadı:\n{ex.Message}",
                    "Talep Pro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            Shutdown();
            return;
        }

        _ = BaslatAsync();
    }

    private async Task BaslatAsync()
    {
        try
        {
            var otomatik = await OturumYoneticisi.OtomatikGirisDeneAsync().ConfigureAwait(true);

            await Dispatcher.InvokeAsync(() =>
            {
                if (!otomatik && !OturumYoneticisi.GirisYapildi)
                {
                    var giris = GirisPenceresi.OturumAc(
                        null,
                        "Talep Pro — Giriş",
                        "Talep Pro",
                        "Profesyonel talep ve teklif yönetimi",
                        "Satınalma Pro ile ortak oturum. Talep, teklif ve onay süreçleriniz burada.");
                    if (!giris)
                    {
                        Shutdown();
                        return;
                    }
                }

                if (!OturumYoneticisi.GirisYapildi && OturumYoneticisi.BulutAktif)
                {
                    if (!_arkaPlanModu)
                    {
                        MessageBox.Show(
                            "Giriş tamamlanamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.",
                            "Talep Pro",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }

                    Shutdown();
                    return;
                }

                if (OturumYoneticisi.BulutAktif && !KullaniciYetkileri.ModulGorebilir("Satınalma"))
                {
                    if (!_arkaPlanModu)
                    {
                        MessageBox.Show(
                            "Talep Pro (Satınalma) modülüne erişim yetkiniz yok.",
                            "Talep Pro",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }

                    Shutdown();
                    return;
                }

                try
                {
                    var main = new MainWindow();
                    MainWindow = main;
                    TalepProTepsiYoneticisi.Bagla(main);
                    var deep = _bekleyenDeepLink ?? _args;
                    main.DeepLinkUygula(deep);
                    TalepProTekOrnek.OneGetirDinleyicisiniKur(main, args =>
                    {
                        _bekleyenDeepLink = args;
                        main.DeepLinkUygula(args);
                    });

                    if (_arkaPlanModu)
                        TalepProTepsiYoneticisi.TepsiyeGizle(bildirimGoster: false);
                    else
                    {
                        main.Show();
                        main.Activate();
                    }
                }
                catch (Exception ex)
                {
                    HataGunlugu.Kaydet(ex, "TalepPro.MainWindow");
                    if (!_arkaPlanModu)
                    {
                        MessageBox.Show(
                            $"Talep Pro penceresi açılamadı:\n{ex.Message}\n\nKurulumu yeniden çalıştırmayı deneyin.",
                            "Talep Pro",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }

                    Shutdown();
                }
            });
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "TalepPro.Baslat");
            await Dispatcher.InvokeAsync(() =>
            {
                if (!_arkaPlanModu)
                {
                    MessageBox.Show($"Talep Pro açılamadı:\n{ex.Message}", "Talep Pro",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                Shutdown();
            });
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TalepProTepsiYoneticisi.Temizle();
        TalepProTekOrnek.SerbestBirak();
        base.OnExit(e);
    }
}
