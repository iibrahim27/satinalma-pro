using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
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

    public MainWindow()
    {
        InitializeComponent();
        KisayollariBagla();
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

    private void Bildirimler_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new BildirimlerWindow { Owner = this };
        pencere.ShowDialog();
        BildirimRozetiniGuncelle();
    }

    private void YonetimTeklifOnay_Click(object sender, RoutedEventArgs e)
    {
        var rol = Models.KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol);
        if (rol is Models.KullaniciRolleri.Satinalma or Models.KullaniciRolleri.Yonetim)
        {
            OnModuleSelected("Satınalma");
            if (MainRegion.Content is SatinalmaView satinalma)
                satinalma.BildirimdenAc(null, 0, "teklif-onay");
            return;
        }

        var pencere = new YonetimTeklifOnayWindow { Owner = this };
        pencere.ShowDialog();
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

        if (!AnasayfadaMi())
            ShowHome();
    }

    private void AnaSayfayaDon()
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
        BildirimYoneticisi.Durdur();
        BulutVeriSenkronu.YoklamayiDurdur();
        ErtelenmisKayit.HemenCalistir();
        try
        {
            BulutVeriSenkronu.BulutaGonderAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "MainWindow.OnClosing");
        }
        ModulVeriDeposu.KaydetTumu();
        FinansmanVeriDeposu.Kaydet();
        UygulamaAyarDeposu.Kaydet();
        SatinalmaDepo.Kaydet();
    }

    private async void Cikis_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BulutVeriSenkronu.YoklamayiDurdur();
            await BulutVeriSenkronu.BulutaGonderAsync();
            OturumYoneticisi.CikisYap();

            Hide();

            if (!GirisPenceresi.OturumAc(null))
            {
                Application.Current.Shutdown();
                return;
            }

            await BulutVeriSenkronu.BuluttanYukleAsync();
            BulutVeriSenkronu.YoklamayiBaslat();
            BildirimYoneticisi.Baslat();
            _modulOnbellegi.Clear();
            ShowHome();
            AltBilgiyiGuncelle(modulde: false);
            Show();
            Activate();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "MainWindow.Cikis");
            MessageBox.Show($"Çıkış sırasında hata: {ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (MainRegion.Content is HomeView home)
        {
            _homeView = home;
            home.ModuleSelected += OnModuleSelected;
        }

        Focus();
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (OturumYoneticisi.GirisYapildi)
            _ = BulutVeriSenkronu.SimdiYenileAsync();
    }

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

    private void Home_Click(object sender, RoutedEventArgs e) => ShowHome();

    private void ShowHome()
    {
        ModulBaslikPanel.Visibility = Visibility.Collapsed;
        Title = UygulamaBilgisi.Ad;
        AltBilgiyiGuncelle(modulde: false);

        _homeView ??= new HomeView();
        _homeView.ModuleSelected -= OnModuleSelected;
        _homeView.ModuleSelected += OnModuleSelected;

        MainRegion.Content = _homeView;
        _homeView.ModulleriYenile();
        _homeView.AnasayfaLogosunuYenile();
        BildirimRozetiniGuncelle();
        Focus();
    }

    private void AltBilgiyiGuncelle(bool modulde)
    {
        var temel = UygulamaBilgisi.AltBilgiMetni;
        if (OturumYoneticisi.GirisYapildi && OturumYoneticisi.AktifKullanici is { } k)
        {
            var ad = string.IsNullOrWhiteSpace(k.AdSoyad) ? k.Eposta : k.AdSoyad;
            temel += $"  ·  {ad} ({k.Rol})";
        }

        TxtAltBilgi.Text = modulde
            ? $"{temel}  ·  Esc / Ctrl+H: Ana Sayfa  ·  F5: Yenile"
            : temel;

        BtnCikis.Visibility = OturumYoneticisi.BulutAktif && OturumYoneticisi.GirisYapildi
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
        BtnBildirimler.Visibility = OturumYoneticisi.GirisYapildi ? Visibility.Visible : Visibility.Collapsed;
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        BtnYonetimTeklifOnay.Visibility = OturumYoneticisi.GirisYapildi && OturumYoneticisi.BulutAktif &&
            (Models.KullaniciRolleri.AdminMi(rol)
             || Models.KullaniciRolleri.Normalize(rol) is Models.KullaniciRolleri.Yonetim or Models.KullaniciRolleri.Satinalma)
            ? Visibility.Visible
            : Visibility.Collapsed;
        BadgeBildirim.Visibility = sayi > 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtBadgeSayi.Text = sayi > 99 ? "99+" : sayi.ToString();
    }

    private void ModulBasliginiGoster(string modulAdi)
    {
        var bilgi = ModuleCatalog.Bul(modulAdi);

        TxtModulBaslik.Text = modulAdi;
        TxtModulAltBaslik.Text = bilgi?.Subtitle ?? "Modül ekranı";
        TxtModulIkon.Text = bilgi?.IconGlyph ?? "\uE8A7";

        var baslangic = bilgi?.GradientStart ?? Color.FromRgb(100, 116, 139);
        var bitis = bilgi?.GradientEnd ?? Color.FromRgb(148, 163, 184);

        ModulSolSerit.BorderBrush = new SolidColorBrush(baslangic);
        ModulIkonZemin.Background = new LinearGradientBrush(baslangic, bitis, new Point(0, 0), new Point(1, 1));

        ModulBaslikPanel.Visibility = Visibility.Visible;
        Title = $"{UygulamaBilgisi.Ad} — {modulAdi}";
        AltBilgiyiGuncelle(modulde: true);
    }

    private void OnModuleSelected(string moduleTitle)
    {
        if (!KullaniciYetkileri.ModulGorebilir(moduleTitle))
        {
            MessageBox.Show("Bu modüle erişim yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ModulBasliginiGoster(moduleTitle);

        if (moduleTitle == "Satınalma")
            _modulOnbellegi.Remove("Satınalma");

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
                "Satınalma" => new SatinalmaView(),
                "Raporlamalar" => new RaporlamalarView(),
                "Finansman Raporlama" => new FinansmanRaporlamaView(),
                "Ayarlar" => new AyarlarView(),
                _ => new ModulePlaceholderView(moduleTitle)
            };
            _modulOnbellegi[moduleTitle] = view;
        }

        MainRegion.Content = view;
        KullaniciYetkileri.ModulErisiminiUygula(view, moduleTitle);
        if (view is IModulKlavyeKisayollari modul)
            modul.KisayolYenile();
        Focus();
    }

    public void BildirimdenModuleGit(MasaustuBildirimHedef hedef)
    {
        if (!KullaniciYetkileri.ModulGorebilir(hedef.Modul))
            return;

        OnModuleSelected(hedef.Modul);

        if (MainRegion.Content is SatinalmaView satinalma)
            satinalma.BildirimdenAc(hedef.TalepId, hedef.Adim, hedef.Sekme);
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


