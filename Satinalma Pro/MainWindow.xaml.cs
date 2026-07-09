using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Controls.Dashboard;
using SatinalmaPro.Helpers;
using SatinalmaPro.Theme;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Views;
using SatinalmaPro.Views.Modules;

namespace SatinalmaPro;

public partial class MainWindow : Window
{
    private HomeView? _homeView;
    private readonly Dictionary<string, UserControl> _modulOnbellegi = new(StringComparer.Ordinal);
    private bool _kapanisaIzinVer;
    private bool _kapanisDevamEdiyor;

    public MainWindow()
    {
        InitializeComponent();
        KisayollariBagla();
        Sidebar.NavigasyonSecildi += Sidebar_NavigasyonSecildi;
        Sidebar.CikisTiklandi += (_, _) => Cikis_Click(this, new RoutedEventArgs());
        Header.BildirimTiklandi += (_, _) => Bildirimler_Click(this, new RoutedEventArgs());
        Header.AyarlarTiklandi += (_, _) => OnModuleSelected("Ayarlar");
        AltBilgiyiGuncelle(modulde: false);
        Loaded += OnLoaded;
        Activated += OnActivated;
        Closing += OnClosing;
        if (OturumYoneticisi.GirisYapildi)
        {
            BildirimYoneticisi.Baslat();
            BildirimYoneticisi.BildirimlerDegisti += BildirimRozetiniGuncelle;
            BildirimYoneticisi.BildirimlerDegisti += AnasayfaRozetiniGuncelle;
            BildirimRozetiniGuncelle();
        }
    }

    private void Sidebar_NavigasyonSecildi(string hedef)
    {
        if (hedef == "Ana Sayfa")
            ShowHome();
        else
            OnModuleSelected(hedef);
    }

    private void Bildirimler_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new BildirimlerWindow { Owner = this };
        pencere.ShowDialog();
        BildirimRozetiniGuncelle();
    }

    private void YonetimTeklifOnay_Click(object sender, RoutedEventArgs e)
    {
        OnModuleSelected("Satınalma");
        if (MainRegion.Content is SatinalmaShellView shell)
            shell.BildirimdenAc(null, 0, "yonetim-teklif-girilen");
    }

    private void KisayollariBagla()
    {
        InputBindings.Add(new KeyBinding(new KisayolKomutu(EscapeTusunaBasildi), Key.Escape, ModifierKeys.None));
        InputBindings.Add(new KeyBinding(new KisayolKomutu(AnaSayfayaDon), Key.H, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new KisayolKomutu(YenileKisayolu), Key.F5, ModifierKeys.None));
    }

    private long _escapeYoksayMs;

    internal void ModalKapandiEscapeYoksay(int ms = 400) =>
        _escapeYoksayMs = Environment.TickCount64 + ms;

    private void EscapeTusunaBasildi()
    {
        if (Environment.TickCount64 < _escapeYoksayMs)
            return;

        if (AcikAltPencereyiKapat())
            return;

        if (AktifSatinalmaEscapeIsle())
            return;

        if (!AnasayfadaMi())
            ShowHome();
    }

    private bool AktifSatinalmaEscapeIsle()
    {
        if (MainRegion.Content is SatinalmaShellView shell)
            return shell.EscapeTusunuIsle();

        return false;
    }

    public void AnaSayfayaDon()
    {
        if (!AnasayfadaMi())
            ShowHome();
    }

    private void YenileKisayolu()
    {
        if (MainRegion.Content is IModulKlavyeKisayollari modul)
            modul.KisayolYenile();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_kapanisaIzinVer)
            return;

        e.Cancel = true;
        MasaustuTepsiYoneticisi.TepsiyeGizle();
    }

    /// <summary>Sistem tepsisi menüsünden tamamen kapatma.</summary>
    public void TamamenKapatIstendi()
    {
        if (_kapanisDevamEdiyor)
            return;

        _kapanisDevamEdiyor = true;
        _ = KapanisIsleminiCalistirAsync();
    }

    private async Task KapanisIsleminiCalistirAsync()
    {
        try
        {
            BildirimYoneticisi.Durdur();
            BulutVeriSenkronu.YoklamayiDurdur();
            ErtelenmisKayit.HemenCalistir();

            try
            {
                using var zamanAsimi = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await BulutVeriSenkronu.BulutaGonderAsync(zamanAsimi.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "MainWindow.KapanisBulut");
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ModulVeriDeposu.KaydetTumu();
                FinansmanVeriDeposu.Kaydet();
                UygulamaAyarDeposu.Kaydet();
                SatinalmaDepo.Kaydet();
                MasaustuTepsiYoneticisi.Temizle();
                _kapanisaIzinVer = true;
                System.Windows.Application.Current.Shutdown();
            });
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "MainWindow.KapanisIsleminiCalistirAsync");
            await Dispatcher.InvokeAsync(() =>
            {
                MasaustuTepsiYoneticisi.Temizle();
                _kapanisaIzinVer = true;
                System.Windows.Application.Current.Shutdown();
            });
        }
        finally
        {
            _kapanisDevamEdiyor = false;
        }
    }

    private async void Cikis_Click(object sender, RoutedEventArgs e)
    {
        var gizlendi = false;
        try
        {
            BulutVeriSenkronu.YoklamayiDurdur();
            await BulutVeriSenkronu.BulutaGonderAsync().ConfigureAwait(true);
            OturumYoneticisi.CikisYap();

            Hide();
            gizlendi = true;

            if (!GirisPenceresi.OturumAc(null))
            {
                Application.Current.Shutdown();
                return;
            }

            await BulutVeriSenkronu.BuluttanYukleAsync().ConfigureAwait(true);
            BulutVeriSenkronu.YoklamayiBaslat();
            BildirimYoneticisi.Baslat();
            _modulOnbellegi.Clear();
            ShowHome();
            Sidebar.Yenile();
            AnasayfaKarsilamayiGuncelle();
            AltBilgiyiGuncelle(modulde: false);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "MainWindow.Cikis");
            MessageBox.Show($"Çıkış sırasında hata: {ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            if (gizlendi)
            {
                Show();
                Activate();
                Focus();
            }
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Sidebar.Yenile();
        AnasayfaKarsilamayiGuncelle();

        if (MainRegion.Content is HomeView home)
        {
            _homeView = home;
            home.ModuleSelected += OnModuleSelected;
        }
        else
        {
            ShowHome();
        }

        AltBilgiyiGuncelle(modulde: false);
        Focus();
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (!OturumYoneticisi.GirisYapildi)
            return;

        var simdi = Environment.TickCount64;
        if (simdi - _sonBulutYenilemeMs < 4000)
            return;

        _sonBulutYenilemeMs = simdi;
        _ = BulutVeriSenkronu.SimdiYenileAsync();
    }

    private long _sonBulutYenilemeMs;

    private bool AcikAltPencereyiKapat()
    {
        var acikPencere = OwnedWindows
            .OfType<Window>()
            .Where(w => w.IsVisible)
            .OrderByDescending(w => w.ActualWidth * w.ActualHeight)
            .FirstOrDefault();

        if (acikPencere is null)
            return false;

        acikPencere.Close();
        return true;
    }

    private bool AnasayfadaMi() => MainRegion.Content is HomeView;

    private void Home_Click(object sender, RoutedEventArgs e) => AnaSayfayaDon();

    private void ShowHome()
    {
        ModulBaslikPanel.Visibility = Visibility.Collapsed;
        Header.Visibility = Visibility.Visible;
        Title = UygulamaBilgisi.Ad;
        AltBilgiyiGuncelle(modulde: false);
        Sidebar.AktifOgeyiAyarla("Ana Sayfa");
        Header.BreadcrumbAyarla("Dashboard");

        _homeView ??= new HomeView();
        _homeView.ModuleSelected -= OnModuleSelected;
        _homeView.ModuleSelected += OnModuleSelected;

        MainRegion.Content = _homeView;
        _homeView.ModulleriYenile();
        BildirimRozetiniGuncelle();
        AnasayfaKarsilamayiGuncelle();
        Focus();
    }

    private void AnasayfaKarsilamayiGuncelle() => _homeView?.KarsilamayiGuncelle();

    private void AltBilgiyiGuncelle(bool modulde)
    {
        var temel = UygulamaBilgisi.AltBilgiMetni;
        var firma = SatinalmaPro.Shared.SaaS.KiracıOturumu.TenantAd
            ?? UygulamaAyarDeposu.Ayarlar.FirmaAdi;
        if (!string.IsNullOrWhiteSpace(firma))
            temel += $"  ·  {firma.Trim()}";

        if (OturumYoneticisi.GirisYapildi && OturumYoneticisi.AktifKullanici is { } k)
        {
            var ad = string.IsNullOrWhiteSpace(k.AdSoyad) ? k.Eposta : k.AdSoyad;
            temel += $"  ·  {ad} ({k.Rol})";
        }

        var lisans = SatinalmaPro.Shared.SaaS.KiracıOturumu.Lisans;
        if (lisans is not null)
            temel += $"  ·  {lisans.KisaDurumMetni}";

        TxtAltBilgi.Text = modulde
            ? $"{temel}  ·  Esc / Ctrl+H: Ana Sayfa  ·  F5: Yenile"
            : temel;

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        BtnYonetimTeklifOnay.Visibility = OturumYoneticisi.GirisYapildi && OturumYoneticisi.BulutAktif &&
            (Models.KullaniciRolleri.AdminMi(rol)
             || Models.KullaniciRolleri.Normalize(rol) is Models.KullaniciRolleri.Yonetim or Models.KullaniciRolleri.Satinalma)
            ? Visibility.Visible
            : Visibility.Collapsed;

        BildirimRozetiniGuncelle();
    }

    private void AnasayfaRozetiniGuncelle()
    {
        if (_homeView is null || !AnasayfadaMi())
            return;

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(AnasayfaRozetiniGuncelle);
            return;
        }

        _homeView.ModulleriYenile();
    }

    private void BildirimRozetiniGuncelle()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(BildirimRozetiniGuncelle);
            return;
        }

        var sayi = BildirimYoneticisi.OkunmamisSayisi;
        Header.BildirimRozetiniGuncelle(sayi);
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        BtnYonetimTeklifOnay.Visibility = OturumYoneticisi.GirisYapildi && OturumYoneticisi.BulutAktif &&
            (Models.KullaniciRolleri.AdminMi(rol)
             || Models.KullaniciRolleri.Normalize(rol) is Models.KullaniciRolleri.Yonetim or Models.KullaniciRolleri.Satinalma)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ModulBasliginiGoster(string modulAdi)
    {
        var bilgi = ModuleCatalog.Bul(modulAdi);

        TxtModulBaslik.Text = modulAdi;
        TxtModulAltBaslik.Text = bilgi?.Subtitle ?? "Modül ekranı";
        TxtModulIkon.Text = bilgi?.IconGlyph ?? "\uE8A7";

        var renk = bilgi?.GradientStart ?? Color.FromRgb(37, 99, 235);
        ModulIkonZemin.Background = AppTheme.TintBrush(renk, 40);

        ModulBaslikPanel.Visibility = modulAdi == "Satınalma" ? Visibility.Collapsed : Visibility.Visible;
        Header.Visibility = Visibility.Collapsed;
        Header.BreadcrumbAyarla(modulAdi);
        Sidebar.AktifOgeyiAyarla(modulAdi);
        Title = $"{UygulamaBilgisi.Ad} — {modulAdi}";
        AltBilgiyiGuncelle(modulde: true);
    }

    public void ModulAc(string modulAdi) => OnModuleSelected(modulAdi);

    private void OnModuleSelected(string moduleTitle)
    {
        if (!KullaniciYetkileri.ModulGorebilir(moduleTitle))
        {
            MessageBox.Show("Bu modüle erişim yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ModulBasliginiGoster(moduleTitle);

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            if (!_modulOnbellegi.TryGetValue(moduleTitle, out var view))
            {
                view = moduleTitle switch
                {
                    "Alınan Malzemeler" => new AlinanMalzemelerView(),
                    "Stok Yönetimi" => new StokYonetimiView(),
                    "Agrega" => new AgregaView(),
                    "Çimento" => new CimentoView(),
                    "Akaryakıt Takip" => new AkaryakitView(),
                    "Araç Filo Takip" => new AracFiloView(),
                    "Satınalma" => OlusturSatinalmaShell(),
                    "Raporlamalar" => new RaporlamalarView(),
                    "Finansman Raporlama" => new FinansmanRaporlamaView(),
                    "Ayarlar" => new AyarlarView(),
                    _ => new ModulePlaceholderView(moduleTitle)
                };
                _modulOnbellegi[moduleTitle] = view;
            }

            MainRegion.Content = view;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,
                () => KullaniciYetkileri.ModulErisiminiUygula(view, moduleTitle));
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }

        Focus();
    }

    public void BildirimdenModuleGit(MasaustuBildirimHedef hedef)
    {
        if (!KullaniciYetkileri.ModulGorebilir(hedef.Modul))
            return;

        OnModuleSelected(hedef.Modul);

        if (MainRegion.Content is SatinalmaShellView shell)
            shell.BildirimdenAc(hedef.TalepId, hedef.Adim, hedef.Sekme);
        else if (MainRegion.Content is StokYonetimiView stokView)
            stokView.BildirimdenAc(hedef.Sekme);
    }

    public void SatinalmaOperasyonModunaGec(Guid? talepId = null, string sekme = "talepler")
    {
        if (!KullaniciYetkileri.ModulGorebilir("Satınalma"))
            return;

        OnModuleSelected("Satınalma");

        if (MainRegion.Content is SatinalmaShellView shell)
            shell.BildirimdenAc(talepId, 0, sekme);
    }

    private static SatinalmaShellView OlusturSatinalmaShell()
    {
        var shell = new SatinalmaShellView();
        shell.StokModuluIstendi += () =>
        {
            if (Application.Current.MainWindow is MainWindow ana)
                ana.ModulAc("Stok Yönetimi");
        };
        return shell;
    }
}

file sealed class KisayolKomutu(Action calistir) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => calistir();
}


