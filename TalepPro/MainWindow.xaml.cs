using System.ComponentModel;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Views;
using SatinalmaPro.Views.Modules;
using TalepPro.Helpers;

namespace TalepPro;

public partial class MainWindow : Window
{
    private SatinalmaShellView? _shell;
    private bool _kapanisaIzinVer;
    private bool _oturumKapatiliyor;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _shell = new SatinalmaShellView();
        _shell.StokModuluIstendi += () =>
        {
            UygulamaKoordinasyonu.SatinalmaProModulAc("Stok Yönetimi");
        };
        _shell.OturumKapatIstendi += () => _ = OturumKapatAsync();
        IcerikAlani.Content = _shell;
    }

    private async Task OturumKapatAsync()
    {
        if (_oturumKapatiliyor)
            return;

        _oturumKapatiliyor = true;
        var gizlendi = !IsVisible;
        try
        {
            var sonuc = await OturumKapatmaServisi.KapatVeYenidenGirAsync(
                this,
                new GirisPenceresiMarka(
                    "Talep Pro — Giriş",
                    "Talep Pro",
                    "Profesyonel talep ve teklif yönetimi",
                    "Satınalma Pro ile ortak oturum. Talep, teklif ve onay süreçleriniz burada."))
                .ConfigureAwait(true);

            if (sonuc == OturumKapatmaSonuc.Iptal)
                return;

            if (sonuc == OturumKapatmaSonuc.GirisIptal)
            {
                _kapanisaIzinVer = true;
                Application.Current.Shutdown();
                return;
            }

            if (sonuc != OturumKapatmaSonuc.Basarili)
                return;

            IcerikAlani.Content = null;
            _shell = new SatinalmaShellView();
            _shell.StokModuluIstendi += () =>
                UygulamaKoordinasyonu.SatinalmaProModulAc("Stok Yönetimi");
            _shell.OturumKapatIstendi += () => _ = OturumKapatAsync();
            IcerikAlani.Content = _shell;

            Show();
            Activate();
            gizlendi = false;
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "TalepPro.OturumKapat");
            MessageBox.Show(this, $"Çıkış sırasında hata: {ex.Message}", "Talep Pro",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            if (gizlendi)
            {
                Show();
                Activate();
            }

            _oturumKapatiliyor = false;
        }
    }

    public void DeepLinkUygula(IEnumerable<string>? args)
    {
        var (talepId, sekme) = TalepProArgumanlari.Coz(args);
        if (_shell is null)
        {
            Loaded += (_, _) => DeepLinkUygula(args);
            return;
        }

        if (talepId is not null || !string.IsNullOrWhiteSpace(sekme))
            _shell.BildirimdenAc(talepId, 0, sekme ?? "talepler");
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_kapanisaIzinVer)
            return;

        e.Cancel = true;
        TalepProTepsiYoneticisi.TepsiyeGizle();
    }

    /// <summary>Sistem tepsisi menüsünden tamamen kapatma.</summary>
    public void TamamenKapatIstendi()
    {
        TalepProTepsiYoneticisi.Temizle();
        _kapanisaIzinVer = true;
        Application.Current.Shutdown();
    }
}
