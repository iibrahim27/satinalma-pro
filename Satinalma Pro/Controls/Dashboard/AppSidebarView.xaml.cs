using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class AppSidebarView : UserControl
{
    private sealed class NavOge
    {
        public required Button Button { get; init; }
        public required IconControl Icon { get; init; }
        public required TextBlock Metin { get; init; }
        public required Border Shell { get; init; }
        public required Border Serit { get; init; }
    }

    private static readonly SolidColorBrush PasifIkon = AppTheme.Brush("#627D98");
    private static readonly SolidColorBrush PasifMetin = AppTheme.Brush("#102A43");
    private static readonly SolidColorBrush AktifIkon = AppTheme.Brush("#0D7377");
    private static readonly SolidColorBrush AktifMetin = AppTheme.Brush("#0D7377");
    private static readonly SolidColorBrush AktifBg = AppTheme.Brush("#E0FCFF");
    private static readonly SolidColorBrush HoverBg = AppTheme.Brush("#F0F4F8");

    private readonly Dictionary<string, NavOge> _navOgeleri = new(StringComparer.Ordinal);
    private string _aktif = "Ana Sayfa";

    public event Action<string>? NavigasyonSecildi;
    public event EventHandler? CikisTiklandi;

    public AppSidebarView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Yenile();
            MedyaBulutSenkronu.MedyaGuncellendi -= OnMedyaGuncellendi;
            MedyaBulutSenkronu.MedyaGuncellendi += OnMedyaGuncellendi;
        };
        Unloaded += (_, _) => MedyaBulutSenkronu.MedyaGuncellendi -= OnMedyaGuncellendi;
    }

    private void OnMedyaGuncellendi()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(LogoGuncelle);
            return;
        }
        LogoGuncelle();
    }

    public void AktifOgeyiAyarla(string baslik)
    {
        _aktif = baslik;
        foreach (var (anahtar, oge) in _navOgeleri)
        {
            var aktif = anahtar == baslik;
            oge.Button.Tag = aktif ? "Active" : null;
            Stil(oge, aktif);
        }
    }

    private static void Stil(NavOge oge, bool aktif)
    {
        oge.Shell.Background = aktif ? AktifBg : Brushes.Transparent;
        oge.Serit.Visibility = aktif ? Visibility.Visible : Visibility.Collapsed;
        oge.Icon.StrokeBrush = aktif ? AktifIkon : PasifIkon;
        oge.Metin.Foreground = aktif ? AktifMetin : PasifMetin;
        oge.Metin.FontWeight = aktif ? FontWeights.SemiBold : FontWeights.Normal;
    }

    public void Yenile()
    {
        NavPanel.Children.Clear();
        _navOgeleri.Clear();

        EkleNav("Ana Sayfa", DashboardIconKind.Home, null);

        foreach (var modul in ModuleCatalog.All)
        {
            // Satınalma / Talep Pro ayrı uygulamada — masaüstü menüsünde yok
            if (modul.Title.Equals("Satınalma", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!KullaniciYetkileri.ModulGorebilir(modul.Title))
                continue;
            EkleNav(IconProvider.ModulKisaAd(modul.Title), IconProvider.ModulIkonu(modul.Title), modul.Title);
        }

        KullaniciyiGuncelle();
        LogoGuncelle();
        SirketBilgisiniGuncelle();
        CikisButonunuGuncelle();
        AktifOgeyiAyarla(_aktif);
    }

    private void SirketBilgisiniGuncelle()
    {
        var firma = UygulamaAyarDeposu.Ayarlar.FirmaAdi;
        TxtSirketAdi.Text = string.IsNullOrWhiteSpace(firma) ? "Demo Yazılım A.Ş." : firma;
        TxtMaliYil.Text = $"{DateTime.Now.Year} Mali Yılı";
    }

    private void EkleNav(string etiket, DashboardIconKind ikon, string? modulBaslik)
    {
        var serit = new Border
        {
            Width = 3,
            Background = AppTheme.Brush("#0D7377"),
            CornerRadius = new CornerRadius(2),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 6, 0, 6),
            Visibility = Visibility.Collapsed
        };

        var shell = new Border
        {
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var icon = new IconControl
        {
            Kind = ikon,
            IconSize = 18,
            StrokeBrush = PasifIkon,
            Margin = new Thickness(4, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);

        var metin = new TextBlock
        {
            Text = etiket,
            FontSize = 13,
            Foreground = PasifMetin,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(metin, 1);

        grid.Children.Add(icon);
        grid.Children.Add(metin);

        var icerik = new Grid();
        icerik.Children.Add(serit);
        icerik.Children.Add(grid);
        shell.Child = icerik;

        var btn = new Button
        {
            Content = shell,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            ToolTip = etiket,
            Focusable = false
        };
        btn.Template = CreateFlatTemplate();
        btn.Click += (_, _) =>
        {
            var hedef = modulBaslik ?? "Ana Sayfa";
            AktifOgeyiAyarla(hedef);
            NavigasyonSecildi?.Invoke(hedef);
        };
        btn.MouseEnter += (_, _) =>
        {
            if (btn.Tag as string != "Active")
                shell.Background = HoverBg;
        };
        btn.MouseLeave += (_, _) =>
        {
            if (btn.Tag as string != "Active")
                shell.Background = Brushes.Transparent;
        };

        var anahtar = modulBaslik ?? "Ana Sayfa";
        _navOgeleri[anahtar] = new NavOge
        {
            Button = btn,
            Icon = icon,
            Metin = metin,
            Shell = shell,
            Serit = serit
        };
        NavPanel.Children.Add(btn);
    }

    private static ControlTemplate CreateFlatTemplate()
    {
        var t = new ControlTemplate(typeof(Button));
        var f = new FrameworkElementFactory(typeof(ContentPresenter));
        f.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        t.VisualTree = f;
        return t;
    }

    private void KullaniciyiGuncelle()
    {
        var k = OturumYoneticisi.AktifKullanici;
        if (k is null)
        {
            TxtKullaniciAd.Text = "Misafir";
            TxtKullaniciRol.Text = "Oturum yok";
            TxtAvatar.Text = "?";
            BtnProfil.ToolTip = "Misafir";
            CikisButonunuGuncelle();
            return;
        }
        TxtKullaniciAd.Text = k.AdSoyad;
        TxtKullaniciRol.Text = k.Rol;
        TxtAvatar.Text = BasHarfler(k.AdSoyad);
        BtnProfil.ToolTip = $"{k.AdSoyad} · {k.Rol}";
        CikisButonunuGuncelle();
    }

    private void CikisButonunuGuncelle()
    {
        BtnCikis.Visibility = OturumKapatmaServisi.CikisButonuGorunur
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void BtnCikis_Click(object sender, RoutedEventArgs e) =>
        CikisTiklandi?.Invoke(this, EventArgs.Empty);

    private void LogoGuncelle()
    {
        var ayar = UygulamaAyarDeposu.Ayarlar;
        var yol = ayar.AnasayfaLogoDosyaYolu;
        if (string.IsNullOrWhiteSpace(SatinalmaProLogoDeposu.TamYol(yol)))
            yol = ayar.LogoDosyaYolu;
        var bitmap = LogoGorselYardimcisi.Yukle(yol) ?? LogoGorselYardimcisi.VarsayilanLogo();
        ImgLogo.Source = bitmap;
        var varMi = bitmap is not null;
        ImgLogo.Visibility = varMi ? Visibility.Visible : Visibility.Collapsed;
        LogoYedek.Visibility = varMi ? Visibility.Collapsed : Visibility.Visible;
    }

    private void BtnProfil_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        if (KullaniciYetkileri.ModulGorebilir("Ayarlar"))
        {
            var ayarlar = new MenuItem { Header = "Ayarlar" };
            ayarlar.Click += (_, _) => NavigasyonSecildi?.Invoke("Ayarlar");
            menu.Items.Add(ayarlar);
        }
        if (OturumYoneticisi.GirisYapildi)
        {
            var cikis = new MenuItem { Header = "Çıkış Yap" };
            cikis.Click += (_, _) => CikisTiklandi?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(cikis);
        }
        if (menu.Items.Count == 0) return;
        menu.PlacementTarget = BtnProfil;
        menu.IsOpen = true;
    }

    private static string BasHarfler(string ad)
    {
        if (string.IsNullOrWhiteSpace(ad)) return "?";
        var p = ad.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (p.Length >= 2) return $"{char.ToUpper(p[0][0])}{char.ToUpper(p[^1][0])}";
        return char.ToUpper(ad[0]).ToString();
    }
}
