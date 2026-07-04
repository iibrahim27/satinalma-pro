using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class AppSidebarView : UserControl
{
    private readonly Dictionary<string, Button> _navButtons = new(StringComparer.Ordinal);
    private string _aktif = "Ana Sayfa";

    public event Action<string>? NavigasyonSecildi;
    public event EventHandler? CikisTiklandi;

    public AppSidebarView()
    {
        InitializeComponent();
        Loaded += (_, _) => Yenile();
    }

    public void AktifOgeyiAyarla(string baslik)
    {
        _aktif = baslik;
        foreach (var (anahtar, btn) in _navButtons)
            btn.Tag = anahtar == baslik ? "Active" : null;
    }

    public void Yenile()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();

        EkleNav("Ana Sayfa", DashboardIconKind.Home, null);

        foreach (var modul in ModuleCatalog.All)
        {
            if (!KullaniciYetkileri.ModulGorebilir(modul.Title))
                continue;

            EkleNav(modul.Title, IconProvider.ModulIkonu(modul.Title), modul.Title);
        }

        KullaniciyiGuncelle();
        LogoGuncelle();
        AktifOgeyiAyarla(_aktif);
    }

    private void EkleNav(string etiket, DashboardIconKind ikon, string? modulBaslik)
    {
        var gorunen = modulBaslik is null ? etiket : IconProvider.ModulKisaAd(modulBaslik);
        var btn = new Button { Style = (Style)FindResource("DashNavButtonStyle") };
        btn.Click += (_, _) =>
        {
            var hedef = modulBaslik ?? "Ana Sayfa";
            AktifOgeyiAyarla(hedef);
            NavigasyonSecildi?.Invoke(hedef);
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var ikonRenk = modulBaslik is null || _aktif == (modulBaslik ?? "Ana Sayfa")
            ? AppTheme.PrimaryBrush
            : AppTheme.SecondaryTextBrush;

        var icon = new IconControl
        {
            Kind = ikon,
            IconSize = 18,
            StrokeBrush = ikonRenk,
            Margin = new Thickness(8, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(icon, 0);
        grid.Children.Add(icon);

        var metin = new TextBlock
        {
            Text = gorunen,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = AppTheme.TextBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(metin, 1);
        grid.Children.Add(metin);

        btn.Content = grid;
        var anahtar = modulBaslik ?? "Ana Sayfa";
        _navButtons[anahtar] = btn;
        NavPanel.Children.Add(btn);
    }

    private void KullaniciyiGuncelle()
    {
        var k = OturumYoneticisi.AktifKullanici;
        if (k is null)
        {
            TxtKullaniciAd.Text = "Misafir";
            TxtKullaniciRol.Text = "Oturum yok";
            TxtAvatar.Text = "?";
            return;
        }

        TxtKullaniciAd.Text = k.AdSoyad;
        TxtKullaniciRol.Text = k.Rol;
        TxtAvatar.Text = BasHarfler(k.AdSoyad);
    }

    private void LogoGuncelle()
    {
        var yol = UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu;
        var bitmap = LogoGorselYardimcisi.Yukle(yol) ?? LogoGorselYardimcisi.VarsayilanLogo();
        ImgLogo.Source = bitmap;
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

        if (OturumYoneticisi.BulutAktif && OturumYoneticisi.GirisYapildi)
        {
            var cikis = new MenuItem { Header = "Çıkış Yap" };
            cikis.Click += (_, _) => CikisTiklandi?.Invoke(this, EventArgs.Empty);
            menu.Items.Add(cikis);
        }

        if (menu.Items.Count == 0)
            return;

        menu.PlacementTarget = BtnProfil;
        menu.IsOpen = true;
    }

    private static string BasHarfler(string ad)
    {
        if (string.IsNullOrWhiteSpace(ad))
            return "?";

        var parcalar = ad.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parcalar.Length >= 2)
            return $"{char.ToUpper(parcalar[0][0])}{char.ToUpper(parcalar[^1][0])}";

        return char.ToUpper(ad[0]).ToString();
    }
}
