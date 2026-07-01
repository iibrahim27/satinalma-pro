using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Views;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView
{
    private void PanelSekmesiniHazirla()
    {
        SatinalmaDepo.TaleplerGuncellendi += PaneliYenile;
        Unloaded += (_, _) => SatinalmaDepo.TaleplerGuncellendi -= PaneliYenile;
    }

    private void PaneliYenile()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(PaneliYenile);
            return;
        }

        if (_aktifSekme != MasaustuRolHaritasi.Panel)
            return;

        var ozet = MasaustuDashboardBaglanti.PanelOlustur();
        TxtPanelBaslik.Text = ozet.PanelBasligi;
        TxtPanelAltBaslik.Text = ozet.AltBaslik;
        TxtPanelGuncelleme.Text = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";

        DashboardKartlarPanel.Children.Clear();
        foreach (var kart in ozet.Kartlar.Where(k => DashboardKartGosterilebilir(k.Route)))
            DashboardKartlarPanel.Children.Add(DashboardKartOlustur(kart));

        DashboardAktiviteListe.ItemsSource = ozet.SonAktivite;
        TxtSonIslemBos.Visibility = ozet.SonAktivite.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static bool DashboardKartGosterilebilir(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return true;

        if (route.Equals("bildirimler", StringComparison.OrdinalIgnoreCase))
            return true;

        if (MasaustuRolHaritasi.RouteToSatinalmaSekme(route) is { } satinalmaSekme)
            return KullaniciYetkileri.SekmeGorebilir("Satınalma", satinalmaSekme);

        if (MasaustuRolHaritasi.RouteToStokSekme(route) is { } stokSekme)
            return KullaniciYetkileri.SekmeGorebilir("Stok Yönetimi", stokSekme);

        return false;
    }

    private Border DashboardKartOlustur(DashboardKart kart)
    {
        var renk = RenkCevir(kart.Renk);

        var deger = new TextBlock
        {
            Text = kart.Deger,
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(renk),
            Margin = new Thickness(0, 6, 0, 0)
        };

        var baslik = new TextBlock
        {
            Text = kart.Baslik,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("InkBrush")
        };

        var alt = new TextBlock
        {
            Text = kart.AltMetin,
            FontSize = 11,
            Foreground = (Brush)FindResource("InkMutedBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var icerik = new StackPanel();
        icerik.Children.Add(new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(renk),
            Margin = new Thickness(0, 0, 0, 4)
        });
        icerik.Children.Add(deger);
        icerik.Children.Add(baslik);
        icerik.Children.Add(alt);

        var border = new Border
        {
            Width = 200,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(12),
            Background = Brushes.White,
            BorderBrush = (Brush)FindResource("LineBrush"),
            BorderThickness = new Thickness(1),
            Child = icerik,
            Cursor = Cursors.Hand,
            Tag = kart
        };

        border.MouseLeftButtonUp += DashboardKart_Click;
        return border;
    }

    private void DashboardKart_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: DashboardKart kart })
            return;

        DashboardRouteGit(kart.Route);
    }

    private void DashboardAktivite_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Tag: DashboardAktivite aktivite })
            return;

        if (aktivite.TalepId is { } talepId)
        {
            var sekme = MasaustuRolHaritasi.RouteToSatinalmaSekme(aktivite.Route) ?? MasaustuRolHaritasi.Taleplerim;
            SekmeyeGec(sekme);
            if (sekme == MasaustuRolHaritasi.Taleplerim)
                TalepSec(talepId);
            return;
        }

        DashboardRouteGit(aktivite.Route);
    }

    private void DashboardRouteGit(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return;

        if (route.Equals("bildirimler", StringComparison.OrdinalIgnoreCase))
        {
            var pencere = new BildirimlerWindow { Owner = Window.GetWindow(this) };
            pencere.ShowDialog();
            PaneliYenile();
            return;
        }

        if (MasaustuRolHaritasi.RouteToStokSekme(route) is not null)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ModulAc("Stok Yönetimi");
            return;
        }

        if (MasaustuRolHaritasi.RouteToSatinalmaSekme(route) is { } sekme)
            SekmeyeGec(sekme);
    }

    private static Color RenkCevir(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
        catch
        {
            return Color.FromRgb(27, 58, 92);
        }
    }

    private void TalepSec(Guid talepId)
    {
        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talepId);
        if (talep is null)
            return;

        _seciliTalep = talep;
        TalepListesiniYenile();
        TalepFormuGoster(talep);
    }
}
