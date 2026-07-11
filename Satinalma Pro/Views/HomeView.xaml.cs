using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SatinalmaPro.Controls.Dashboard;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views;

public partial class HomeView : UserControl
{
    private readonly DispatcherTimer _saatTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public ObservableCollection<ModulKarti> Modules { get; } = [];

    public event Action<string>? ModuleSelected;

    public HomeView()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        ActivityPanel.TumunuGorTiklandi += (_, _) =>
        {
            var rol = KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol);
            ModuleSelected?.Invoke(rol == KullaniciRolleri.Depo ? "Stok Yönetimi" : "Satınalma");
        };
        QuickActions.ModulSecildi += modul => ModuleSelected?.Invoke(modul);

        _saatTimer.Tick += (_, _) => TarihSaatiGuncelle();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        KarsilamayiGuncelle();
        TarihSaatiGuncelle();
        _saatTimer.Start();
        ModulleriYenile();
        BildirimYoneticisi.BildirimlerDegisti += ModulleriYenile;
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        _saatTimer.Stop();
        BildirimYoneticisi.BildirimlerDegisti -= ModulleriYenile;
    }

    public void KarsilamayiGuncelle()
    {
        var ad = OturumYoneticisi.AktifKullanici?.AdSoyad ?? "Kullanıcı";
        var hitap = ad.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ad;
        TxtKarsilama.Text = $"Hoş geldiniz, {hitap} 👋";
        var depo = KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol) == KullaniciRolleri.Depo;
        TxtAltBaslik.Text = depo
            ? "Stok ve mal kabul işlemlerinizi buradan yönetin."
            : "Satınalma süreçlerinizi kolayca yönetin.";
    }

    private void TarihSaatiGuncelle()
    {
        var simdi = DateTime.Now;
        TxtTarihSaat.Text = $"{simdi:dd MMMM yyyy dddd} | {simdi:HH:mm:ss}";
    }

    private void BtnRaporOlustur_Click(object sender, RoutedEventArgs e) =>
        ModuleSelected?.Invoke("Raporlamalar");

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

        VeriyiYenile();
    }

    private void VeriyiYenile()
    {
        var veri = AnaSayfaVeriServisi.Yukle();
        var depo = KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol) == KullaniciRolleri.Depo;

        KarsilamayiGuncelle();
        QuickActions.RolIcinAyarla(OturumYoneticisi.AktifKullanici?.Rol);
        BtnRaporOlustur.Visibility = depo ? Visibility.Collapsed : Visibility.Visible;
        LineChart.Visibility = depo ? Visibility.Collapsed : Visibility.Visible;
        DonutChart.Visibility = depo ? Visibility.Collapsed : Visibility.Visible;

        StatGrid.Children.Clear();
        StatGrid.Columns = Math.Max(1, veri.Istatistikler.Count);
        foreach (var stat in veri.Istatistikler)
        {
            var kart = new StatCardControl { Margin = new Thickness(0, 0, 12, 0) };
            kart.Bagla(stat);
            StatGrid.Children.Add(kart);
        }

        if (!depo)
        {
            LineChart.Bagla(veri.AylikHarcama);
            DonutChart.Bagla(veri.HarcamaDagilimi);
        }

        OpenRecords.Bagla(veri.AcikKayitlar);
        RightWidgets.Bagla(veri.Hatirlatmalar, veri.FinansOzet, veri.TopUrunler);
        ActivityPanel.Bagla(veri.SonIslemler);
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
