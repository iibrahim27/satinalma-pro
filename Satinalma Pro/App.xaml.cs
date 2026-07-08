using System.Windows;

using SatinalmaPro.Helpers;

using SatinalmaPro.Services;

using SatinalmaPro.Views;



namespace SatinalmaPro;



public partial class App : Application

{

    private bool _arkaPlanModu;



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

        _arkaPlanModu = TekOrnekKorumasi.ArkaPlanBaslatMi(e.Args);

        GirisHatirlatmaKorumasi.KurulumVeSurumKontrolu(e.Args);

        WindowsOtomatikBaslatma.Etkinlestir();

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



        if (_arkaPlanModu)

        {

            _ = ArkaPlanBaslatAsync();

            return;

        }



        var splash = new AcilisEkrani();

        PencereOneGetirDinleyicisi.Bagla(splash);

        splash.Show();

        _ = BaslatAsync(splash, tepsideBaslat: false);

    }



    private async Task ArkaPlanBaslatAsync()

    {

        try

        {

            OturumYoneticisi.Baslat();



            if (await GuncellemeServisi.KontrolEtVeUygulaAsync(sessiz: true))

            {

                Shutdown();

                return;

            }



            var girisOk = await OturumYoneticisi.OtomatikGirisDeneAsync();

            if (!girisOk)

            {

                await Dispatcher.InvokeAsync(() =>

                {

                    var splash = new AcilisEkrani();

                    PencereOneGetirDinleyicisi.Bagla(splash);

                    splash.Show();

                    _ = BaslatAsync(splash, tepsideBaslat: true, atlaYuklemeAnimasyonu: true);

                });

                return;

            }



            await OturumSonrasiHazirlikAsync();



            await Dispatcher.InvokeAsync(() =>

            {

                var main = new MainWindow();

                MainWindow = main;

                PencereOneGetirDinleyicisi.Bagla(main);

                MasaustuTepsiYoneticisi.Bagla(main);

                MasaustuTepsiYoneticisi.TepsiyeGizle(bildirimGoster: false);

            });

        }

        catch (Exception ex)

        {

            HataGunlugu.Kaydet(ex, "App.ArkaPlanBaslat");

            Shutdown();

        }

    }



    private async Task BaslatAsync(AcilisEkrani splash, bool tepsideBaslat, bool atlaYuklemeAnimasyonu = false)
    {
        try
        {
            if (!atlaYuklemeAnimasyonu && await splash.YukleVeBekleAsync())
            {
                Shutdown();
                return;
            }

            // Eksik kiracı / bozuk oturumda uygulamayı kapatmak yerine login'e düş.
            for (var deneme = 0; deneme < 2; deneme++)
            {
                if (!await splash.OturumAcAsync())
                {
                    splash.Kapat();
                    Shutdown();
                    return;
                }

                try
                {
                    await OturumSonrasiHazirlikAsync(splash);
                    break;
                }
                catch (Exception hazirlikEx) when (KiraciOturumuHatasiMi(hazirlikEx) && deneme == 0)
                {
                    OturumYoneticisi.OturumuTemizle();
                    MessageBox.Show(
                        "Oturum bilgisi eksik veya süresi dolmuş.\nLütfen tekrar giriş yapın.",
                        UygulamaBilgisi.Ad,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    // Döngü login ekranını yeniden açacak
                }
            }

            if (!OturumYoneticisi.GirisYapildi || !SatinalmaPro.Shared.SaaS.KiracıOturumu.Aktif)
            {
                splash.Kapat();
                Shutdown();
                return;
            }

            var main = new MainWindow();
            MainWindow = main;
            PencereOneGetirDinleyicisi.Bagla(main);
            MasaustuTepsiYoneticisi.Bagla(main);

            if (tepsideBaslat)
                MasaustuTepsiYoneticisi.TepsiyeGizle(bildirimGoster: false);
            else
                main.Show();

            splash.Kapat();
        }
        catch (Exception ex)
        {
            if (KiraciOturumuHatasiMi(ex))
            {
                OturumYoneticisi.OturumuTemizle();
                MessageBox.Show(
                    "Oturum bilgisi eksik veya süresi dolmuş.\nLütfen tekrar giriş yapın.",
                    UygulamaBilgisi.Ad,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                try
                {
                    if (await splash.OturumAcAsync())
                    {
                        await OturumSonrasiHazirlikAsync(splash);
                        var main = new MainWindow();
                        MainWindow = main;
                        PencereOneGetirDinleyicisi.Bagla(main);
                        MasaustuTepsiYoneticisi.Bagla(main);
                        if (tepsideBaslat)
                            MasaustuTepsiYoneticisi.TepsiyeGizle(bildirimGoster: false);
                        else
                            main.Show();
                        splash.Kapat();
                        return;
                    }
                }
                catch
                {
                    // aşağıdaki Shutdown'a düş
                }
            }
            else
            {
                MessageBox.Show(
                    $"Uygulama başlatılamadı:\n{ex.Message}",
                    UygulamaBilgisi.Ad,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            splash.Kapat();
            Shutdown();
        }
    }

    private static bool KiraciOturumuHatasiMi(Exception ex) =>
        ex.Message.Contains("Kiracı oturumu", StringComparison.OrdinalIgnoreCase)
        || ex.Message.Contains("tenant", StringComparison.OrdinalIgnoreCase);



    private static async Task OturumSonrasiHazirlikAsync(AcilisEkrani? splash = null)
    {
        if (OturumYoneticisi.GirisYapildi)
        {
            if (!SatinalmaPro.Shared.SaaS.KiracıOturumu.Aktif)
                throw new InvalidOperationException("Kiracı oturumu bulunamadı. Tekrar giriş yapın.");

            splash?.SenkronBaslat();

            using var zamanAsimi = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var ilerleme = new Progress<(int tamamlanan, int toplam, string adim)>(p =>

                splash?.SenkronIlerle(p.tamamlanan, p.toplam, p.adim));



            try

            {

                await BulutVeriSenkronu.IkiliSenkronAsync(ilerleme, zamanAsimi.Token);

                splash?.DurumGuncelle("Veriler yüklendi, uygulama açılıyor...");

            }

            catch (OperationCanceledException)

            {

                splash?.DurumGuncelle("Bulut senkronu zaman aşımına uğradı. Yerel verilerle devam ediliyor...");

            }

            catch (Exception)

            {

                splash?.DurumGuncelle("Bulut bağlantısı kurulamadı. Yerel verilerle devam ediliyor...");

            }



            VeriYukleyici.TumunuYukle();

            BulutVeriSenkronu.YoklamayiBaslat();

            BildirimYoneticisi.Baslat();

        }

        else

        {

            VeriYukleyici.TumunuYukle();

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


