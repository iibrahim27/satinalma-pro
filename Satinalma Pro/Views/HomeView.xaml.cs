using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views;

public partial class HomeView : UserControl
{
    public ObservableCollection<ModulKarti> Modules { get; } = [];

    public event Action<string>? ModuleSelected;

    public HomeView()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        ModulleriYenile();
        AnasayfaLogosunuYenile();
        BildirimYoneticisi.BildirimlerDegisti += ModulleriYenile;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e) =>
        BildirimYoneticisi.BildirimlerDegisti -= ModulleriYenile;

    public void ModulleriYenile()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ModulleriYenile);
            return;
        }

        foreach (var modul in ModuleCatalog.All)
        {
            if (!KullaniciYetkileri.ModulGorebilir(modul.Title))
                continue;

            var mevcut = Modules.FirstOrDefault(m => m.Title == modul.Title);
            var sayi = BildirimYoneticisi.ModulOkunmamisSayisi(modul.Title);
            if (mevcut is null)
                Modules.Add(new ModulKarti(modul, sayi));
            else
                mevcut.BildirimSayisi = sayi;
        }

        for (var i = Modules.Count - 1; i >= 0; i--)
        {
            if (!ModuleCatalog.All.Any(m => m.Title == Modules[i].Title)
                || !KullaniciYetkileri.ModulGorebilir(Modules[i].Title))
                Modules.RemoveAt(i);
        }
    }

    public void AnasayfaLogosunuYenile() =>
        LogoGorselYardimcisi.GorselAyarla(ImgAnasayfaLogo, UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu);

    private void ModuleCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string title })
            ModuleSelected?.Invoke(title);
    }
}

public sealed class ModulKarti : INotifyPropertyChanged
{
    private int _bildirimSayisi;

    public ModulKarti(ModuleInfo bilgi, int bildirimSayisi)
    {
        Bilgi = bilgi;
        _bildirimSayisi = bildirimSayisi;
    }

    public ModuleInfo Bilgi { get; }

    public string Title => Bilgi.Title;
    public string Subtitle => Bilgi.Subtitle;
    public string IconGlyph => Bilgi.IconGlyph;
    public string Number => Bilgi.Number;
    public System.Windows.Media.Color GradientStart => Bilgi.GradientStart;
    public System.Windows.Media.Color GradientEnd => Bilgi.GradientEnd;

    public int BildirimSayisi
    {
        get => _bildirimSayisi;
        set
        {
            if (_bildirimSayisi == value)
                return;

            _bildirimSayisi = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BildirimRozetiGorunur));
            OnPropertyChanged(nameof(BildirimRozetiMetni));
        }
    }

    public bool BildirimRozetiGorunur => BildirimSayisi > 0;

    public string BildirimRozetiMetni => BildirimSayisi > 99 ? "99+" : BildirimSayisi.ToString();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? ad = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ad));
}
