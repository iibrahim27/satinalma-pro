using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SatinalmaPro.Controls.Dashboard;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Views;

public partial class HomeView : UserControl
{
    private readonly DispatcherTimer _saatTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private IReadOnlyList<AnaSayfaAcikKayit> _tumKayitlar = [];
    private string _filtre = "Hepsi";

    public ObservableCollection<ModulKarti> Modules { get; } = [];

    public event Action<string>? ModuleSelected;

    public HomeView()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        QuickActions.ModulSecildi += modul => ModuleSelected?.Invoke(modul);
        StokUyariPanel.TumunuGorTiklandi += (_, _) => ModuleSelected?.Invoke("Stok Yönetimi");
        AktivitePanel.TumunuGorTiklandi += (_, _) => ModuleSelected?.Invoke("Alınan Malzemeler");
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
        TxtKarsilama.Text = $"Hoş geldiniz, {hitap}";
        var rol = KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol);
        TxtAltBaslik.Text = rol switch
        {
            KullaniciRolleri.Depo or KullaniciRolleri.Atolye =>
                "Stok, alınan malzeme ve akaryakıt hareketlerini buradan izleyin.",
            _ => "Alınan malzemeler, stok, agrega, çimento ve akaryakıt özeti"
        };
    }

    private void TarihSaatiGuncelle()
    {
        var simdi = DateTime.Now;
        TxtTarihSaat.Text = $"{simdi:dd MMMM yyyy dddd} | {simdi:HH:mm:ss}";
    }

    private void BtnRaporOlustur_Click(object sender, RoutedEventArgs e) =>
        ModuleSelected?.Invoke("Alınan Malzemeler");

    private void BtnStokGit_Click(object sender, RoutedEventArgs e) =>
        ModuleSelected?.Invoke("Stok Yönetimi");

    private void BtnYeniTalep_Click(object sender, RoutedEventArgs e) =>
        ModuleSelected?.Invoke("Satınalma");

    private void BtnDetaySatinalma_Click(object sender, RoutedEventArgs e) =>
        ModuleSelected?.Invoke("Satınalma");

    private void BtnDetayAc_Click(object sender, RoutedEventArgs e) =>
        ModuleSelected?.Invoke("Satınalma");

    public void ModulleriYenile()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(ModulleriYenile);
            return;
        }

        var dashboardModulleri = AnaSayfaVeriServisi.DashboardModulBasliklari
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var modul in ModuleCatalog.All)
        {
            if (!dashboardModulleri.Contains(modul.Title))
                continue;
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
            if (!dashboardModulleri.Contains(Modules[i].Title)
                || !ModuleCatalog.All.Any(m => m.Title == Modules[i].Title)
                || !KullaniciYetkileri.ModulGorebilir(Modules[i].Title))
                Modules.RemoveAt(i);
        }

        LaunchpadKarolariniOlustur();
        VeriyiYenile();
    }

    private static readonly string[] LaunchpadRenkleri =
    [
        "#0D7377", "#102A43", "#2F9E44", "#F08C00", "#14919B",
        "#243B53", "#E03131", "#0F828F", "#334E68", "#486581"
    ];

    private void LaunchpadKarolariniOlustur()
    {
        ModulPanel.Children.Clear();
        var i = 0;
        foreach (var kart in Modules)
        {
            var renk = LaunchpadRenkleri[i % LaunchpadRenkleri.Length];
            i++;

            var tile = new Border
            {
                Width = 220,
                Height = 128,
                Margin = new Thickness(0, 0, 14, 14),
                Background = AppTheme.Brush("#FFFFFF"),
                BorderBrush = AppTheme.Brush("#D9E2EC"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Cursor = Cursors.Hand,
                Tag = kart.Title,
                Padding = new Thickness(0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 18,
                    ShadowDepth = 0,
                    Opacity = 0.08,
                    Color = Colors.Black
                }
            };

            var root = new Grid();
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var serit = new Border
            {
                Background = AppTheme.Brush(renk),
                CornerRadius = new CornerRadius(16, 0, 0, 16)
            };
            Grid.SetColumn(serit, 0);

            var icerik = new Grid { Margin = new Thickness(16, 14, 14, 14) };
            Grid.SetColumn(icerik, 1);

            var ikonKutu = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(12),
                Background = AppTheme.Brush("#E0FCFF"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = kart.IconGlyph,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 18,
                    Foreground = AppTheme.Brush("#0D7377"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var baslik = new TextBlock
            {
                Text = IconProvider.ModulKisaAd(kart.Title),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = AppTheme.Brush("#102A43"),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 18)
            };

            var alt = new TextBlock
            {
                Text = kart.Subtitle,
                FontSize = 11,
                Foreground = AppTheme.Brush("#627D98"),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxHeight = 32,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            icerik.Children.Add(ikonKutu);
            icerik.Children.Add(baslik);
            icerik.Children.Add(alt);

            if (kart.BildirimRozetiGorunur)
            {
                var badge = new Border
                {
                    Background = AppTheme.Brush("#E03131"),
                    CornerRadius = new CornerRadius(10),
                    MinWidth = 22,
                    Height = 22,
                    Padding = new Thickness(6, 0, 6, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new TextBlock
                    {
                        Text = kart.BildirimRozetiMetni,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                icerik.Children.Add(badge);
            }

            root.Children.Add(serit);
            root.Children.Add(icerik);
            tile.Child = root;
            tile.MouseLeftButtonUp += (_, _) => ModuleSelected?.Invoke(kart.Title);
            ModulPanel.Children.Add(tile);
        }
    }

    private void VeriyiYenile()
    {
        var veri = AnaSayfaVeriServisi.Yukle();
        KarsilamayiGuncelle();
        QuickActions.RolIcinAyarla(OturumYoneticisi.AktifKullanici?.Rol);
        BtnYeniTalep.Visibility = Visibility.Collapsed;

        StatGrid.Children.Clear();
        StatGrid.Columns = Math.Max(1, Math.Min(5, veri.Istatistikler.Count));
        foreach (var stat in veri.Istatistikler.Take(5))
        {
            var kart = new StatCardControl { Margin = new Thickness(0, 0, 10, 0) };
            kart.Bagla(stat);
            StatGrid.Children.Add(kart);
        }

        HarcamaGrafik.Bagla(veri.AylikHarcama);
        DagilimGrafik.Bagla(veri.HarcamaDagilimi);
        StokUyariPanel.Bagla(veri.StokUyarilari.Take(6).ToList());
        AktivitePanel.Bagla(veri.SonIslemler.Take(8).ToList());
        SagOzet.Bagla(veri.Hatirlatmalar.Take(5).ToList(), veri.FinansOzet, veri.TopUrunler.Take(5).ToList());

        _tumKayitlar = veri.AcikKayitlar;
        FiltreSayilariniGuncelle();
        ListeyiUygula();

        var uyariMetinleri = veri.Hatirlatmalar
            .Where(h => !h.Metin.Contains("talep", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .Select(h => h.Metin)
            .ToList();
        if (uyariMetinleri.Count == 0)
            uyariMetinleri = veri.Hatirlatmalar.Take(2).Select(h => h.Metin).ToList();

        if (uyariMetinleri.Count > 0)
        {
            UyariSeridi.Visibility = Visibility.Visible;
            TxtUyari.Text = string.Join("  ·  ", uyariMetinleri);
        }
        else
        {
            UyariSeridi.Visibility = Visibility.Collapsed;
            TxtUyari.Text = "";
        }

        ListeSonIslem.ItemsSource = veri.SonIslemler.Take(6).Select(i => new
        {
            i.Baslik,
            i.Zaman,
            Renk = AppTheme.Brush(i.DurumRenkHex)
        }).ToList();
    }

    private void FiltreSayilariniGuncelle()
    {
        var hepsi = _tumKayitlar.Count;
        var onay = _tumKayitlar.Count(k => DurumEslesir(k.Durum, "Onay"));
        var teklif = _tumKayitlar.Count(k => DurumEslesir(k.Durum, "Teklif"));
        var siparis = _tumKayitlar.Count(k => DurumEslesir(k.Durum, "Siparis"));
        TxtFiltreHepsi.Text = $"Tümü ({hepsi})";
        TxtFiltreOnay.Text = $"Onay ({onay})";
        TxtFiltreTeklif.Text = $"Teklif ({teklif})";
        TxtFiltreSiparis.Text = $"Sipariş ({siparis})";
        TxtListeOzet.Text = $"{hepsi} kayıt";
    }

    private static bool DurumEslesir(string durum, string filtre)
    {
        var d = durum.ToLowerInvariant();
        return filtre switch
        {
            "Onay" => d.Contains("onay"),
            "Teklif" => d.Contains("teklif"),
            "Siparis" => d.Contains("sipariş") || d.Contains("siparis"),
            _ => true
        };
    }

    private void Filtre_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string tag)
            return;
        _filtre = tag;
        FiltreGorunumunuGuncelle();
        ListeyiUygula();
    }

    private void FiltreGorunumunuGuncelle()
    {
        void Stil(Border b, TextBlock t, bool aktif)
        {
            b.Background = aktif ? AppTheme.Brush("#E8F3FF") : AppTheme.Brush("#FFFFFF");
            b.BorderBrush = aktif ? AppTheme.Brush("#E8F3FF") : AppTheme.Brush("#D9D9D9");
            b.BorderThickness = aktif ? new Thickness(0) : new Thickness(1);
            t.Foreground = aktif ? AppTheme.PrimaryBrush : AppTheme.SecondaryTextBrush;
            t.FontWeight = aktif ? FontWeights.SemiBold : FontWeights.Normal;
        }

        Stil(FiltreHepsi, TxtFiltreHepsi, _filtre == "Hepsi");
        Stil(FiltreOnay, TxtFiltreOnay, _filtre == "Onay");
        Stil(FiltreTeklif, TxtFiltreTeklif, _filtre == "Teklif");
        Stil(FiltreSiparis, TxtFiltreSiparis, _filtre == "Siparis");
    }

    private void ListeyiUygula()
    {
        var filtreli = _filtre == "Hepsi"
            ? _tumKayitlar
            : _tumKayitlar.Where(k => DurumEslesir(k.Durum, _filtre)).ToList();

        var satirlar = filtreli.Select(k => new KayitSatirVm(k)).ToList();
        KayitGrid.ItemsSource = satirlar;
        TxtAltKayit.Text = $"Toplam {satirlar.Count} kayıt";

        if (satirlar.Count > 0)
        {
            KayitGrid.SelectedIndex = 0;
            DetayiGoster(satirlar[0]);
        }
        else
            DetayiTemizle();
    }

    private void KayitGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KayitGrid.SelectedItem is KayitSatirVm satir)
            DetayiGoster(satir);
    }

    private void KayitGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        ModuleSelected?.Invoke("Satınalma");

    private void DetayiGoster(KayitSatirVm satir)
    {
        TxtDetayNo.Text = satir.No;
        TxtDetayCari.Text = satir.Cari;
        TxtDetayTarih.Text = satir.Tarih;
        TxtDetayVade.Text = satir.Vade;
        TxtDetayTutar.Text = satir.Tutar;
        TxtDetayKalan.Text = string.IsNullOrWhiteSpace(satir.Kalan) ? "" : $"Kalan: {satir.Kalan}";
        TxtDetayDurum.Text = satir.Durum;
        TxtDetayDurum.Foreground = satir.DurumOnplan;
        DetayDurumKutu.Background = satir.DurumArkaplan;
    }

    private void DetayiTemizle()
    {
        TxtDetayNo.Text = "—";
        TxtDetayCari.Text = "Listeden bir kayıt seçin";
        TxtDetayTarih.Text = "—";
        TxtDetayVade.Text = "—";
        TxtDetayTutar.Text = "—";
        TxtDetayKalan.Text = "";
        TxtDetayDurum.Text = "—";
    }

    private sealed class KayitSatirVm
    {
        public KayitSatirVm(AnaSayfaAcikKayit k)
        {
            No = k.No;
            Tarih = k.Tarih;
            Cari = k.Cari;
            Vade = k.Vade;
            Tutar = k.Tutar;
            Kalan = k.Kalan;
            Durum = k.Durum;
            DurumArkaplan = AppTheme.TintBrush(AppTheme.Parse(k.DurumRenkHex), 55);
            DurumOnplan = AppTheme.Brush(k.DurumRenkHex);
        }

        public string No { get; }
        public string Tarih { get; }
        public string Cari { get; }
        public string Vade { get; }
        public string Tutar { get; }
        public string Kalan { get; }
        public string Durum { get; }
        public Brush DurumArkaplan { get; }
        public Brush DurumOnplan { get; }
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
    public Color GradientStart => Bilgi.GradientStart;
    public Color GradientEnd => Bilgi.GradientEnd;

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
