using System.ComponentModel;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Views.Modules;
using TalepPro.Helpers;

namespace TalepPro;

public partial class MainWindow : Window
{
    private SatinalmaShellView? _shell;
    private bool _kapanisaIzinVer;

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
        IcerikAlani.Content = _shell;
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
