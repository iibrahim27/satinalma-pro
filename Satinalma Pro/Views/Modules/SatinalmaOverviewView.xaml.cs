using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Controls.Dashboard;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaOverviewView : UserControl
{
    public event Action<string>? SekmeIstendi;

    public SatinalmaOverviewView()
    {
        InitializeComponent();
        Loaded += (_, _) => Yenile();
    }

    public void Yenile()
    {
        KpiGrid.Children.Clear();
        HizliErisimPanel.Children.Clear();

        var kpiler =
            new (string baslik, string sekme, string renk, DashboardIconKind icon)[]
            {
                ("Bekleyen Talepler", "Onay Bekleyen", AppTheme.PrimaryHex, DashboardIconKind.ClipboardList),
                ("Teklif Girişi", "Teklif Girişi", AppTheme.PurpleHex, DashboardIconKind.ShoppingCart),
                ("Onay Bekleyen", "Teklif Onay", AppTheme.WarningHex, DashboardIconKind.Wallet),
                ("Mal Kabul", "Alınan Malzemeler", AppTheme.SuccessHex, DashboardIconKind.Truck)
            };

        foreach (var (baslik, sekme, renk, icon) in kpiler)
        {
            if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", sekme))
                continue;

            var kart = new StatCardControl { Margin = new Thickness(0, 0, 16, 0), Cursor = Cursors.Hand };
            kart.MouseLeftButtonUp += (_, _) => SekmeIstendi?.Invoke(sekme);
            kart.Bagla(new AnaSayfaIstatistik
            {
                Baslik = baslik,
                Deger = SatinalmaSekmeSayaclari.Say(sekme).ToString("N0"),
                AltMetin = "Aktif kuyruk",
                TrendMetin = "→ git",
                TrendPozitif = true,
                Icon = icon,
                IconRenkHex = renk
            });
            KpiGrid.Children.Add(kart);
        }

        foreach (var grup in SatinalmaNavYapisi.TumGruplar)
        {
            foreach (var oge in grup.Ogeler.Where(o => o.SekmeAdi is not null))
            {
                if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", oge.SekmeAdi!))
                    continue;

                var sayi = SatinalmaSekmeSayaclari.Say(oge.SekmeAdi!);
                if (sayi <= 0)
                    continue;

                HizliErisimPanel.Children.Add(HizliKart(oge, sayi));
            }
        }

        BekleyenListe.ItemsSource = BekleyenIslemleriOlustur();
    }

    private Border HizliKart(SatinalmaNavOge oge, int sayi)
    {
        var border = new Border
        {
            Width = 220,
            Height = 88,
            Margin = new Thickness(0, 0, 14, 14),
            Background = AppTheme.CardBrush,
            BorderBrush = AppTheme.BorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16, 12, 16, 12),
            Cursor = Cursors.Hand,
            Tag = oge.SekmeAdi
        };
        border.Effect = (System.Windows.Media.Effects.Effect?)TryFindResource("DashCardShadow")
            ?? Application.Current.TryFindResource("DashCardShadow") as System.Windows.Media.Effects.Effect;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var iconHost = new IconControl
        {
            Kind = oge.Icon,
            IconSize = 18,
            StrokeBrush = AppTheme.PrimaryBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(iconHost);

        var stack = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = oge.Baslik,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = AppTheme.TextBrush
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"{sayi} kayıt",
            FontSize = 12,
            Foreground = AppTheme.SecondaryTextBrush,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(stack, 1);
        grid.Children.Add(stack);
        border.Child = grid;

        border.MouseLeftButtonUp += (_, _) =>
        {
            if (border.Tag is string sekme)
                SekmeIstendi?.Invoke(sekme);
        };

        return border;
    }

    private static List<BekleyenSatir> BekleyenIslemleriOlustur()
    {
        var liste = new List<BekleyenSatir>();
        var sekmeler = new[] { "Onay Bekleyen", "Teklif Girişi", "Teklif Onay", "Alınan Malzemeler" };

        foreach (var sekme in sekmeler)
        {
            if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", sekme))
                continue;

            var sayi = SatinalmaSekmeSayaclari.Say(sekme);
            if (sayi <= 0)
                continue;

            liste.Add(new BekleyenSatir
            {
                Baslik = $"{sayi} kayıt bekliyor",
                Alt = SatinalmaView.SekmeBasliklari.TryGetValue(sekme, out var b) ? b : sekme,
                Sekme = sekme,
                SekmeAdi = sekme
            });
        }

        return liste;
    }

    private void HizliAc_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string sekme })
            SekmeIstendi?.Invoke(sekme);
    }

    private sealed class BekleyenSatir
    {
        public required string Baslik { get; init; }
        public required string Alt { get; init; }
        public required string Sekme { get; init; }
        public required string SekmeAdi { get; init; }
    }
}
