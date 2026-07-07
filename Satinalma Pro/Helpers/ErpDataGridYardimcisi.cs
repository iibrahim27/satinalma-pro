using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SatinalmaPro.Views.Controls;

namespace SatinalmaPro.Helpers;

public static class ErpDataGridYardimcisi
{
    private static readonly ConditionalWeakTable<Grid, GridLength[]> SatirYukseklikleri = new();
    public static void PremiumGridAyarla(DataGrid grid)
    {
        grid.CanUserReorderColumns = true;
        grid.CanUserResizeColumns = true;
        grid.CanUserSortColumns = true;
        grid.RowHeaderWidth = 0;
        grid.FrozenColumnCount = 0;
    }

    public static void KolonSeciminiGoster(DataGrid grid, Window? owner)
    {
        var pencere = new ErpKolonSecimWindow(grid) { Owner = owner ?? Window.GetWindow(grid) };
        pencere.ShowDialog();
    }

    public static void GruplaMenusunuGoster(
        FrameworkElement hedef,
        IReadOnlyList<(string Baslik, string Alan)> secenekler,
        string? aktifAlan,
        Action<string?> secildi)
    {
        var menu = new ContextMenu { PlacementTarget = hedef, Placement = PlacementMode.Bottom };
        EkleGrupla(menu, aktifAlan is null ? "✓ Gruplama yok" : "Gruplamayı kaldır", () => secildi(null));
        menu.Items.Add(new Separator());

        foreach (var (baslik, alan) in secenekler)
        {
            var etiket = aktifAlan == alan ? $"✓ {baslik}" : baslik;
            EkleGrupla(menu, etiket, () => secildi(alan));
        }

        menu.IsOpen = true;
    }

    public static void FiltrePaneliToggle(Border panel, Button toggleBtn, ref bool acik)
    {
        acik = !acik;
        toggleBtn.Content = acik ? "▲ Filtreleri Gizle" : "▼ Filtreleri Göster";

        if (acik)
        {
            panel.Visibility = Visibility.Visible;
            panel.RenderTransform ??= new TranslateTransform(0, -8);
            panel.Opacity = 0;

            panel.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });

            if (panel.RenderTransform is TranslateTransform tt)
            {
                tt.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-8, 0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });
            }
        }
        else
        {
            var kapan = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            kapan.Completed += (_, _) => panel.Visibility = Visibility.Collapsed;
            panel.BeginAnimation(UIElement.OpacityProperty, kapan);
        }
    }

    public static void YogunGorunumToggle(DataGrid grid, ref bool yogun)
    {
        yogun = !yogun;
        grid.RowHeight = yogun ? 36 : 48;
        grid.FontSize = yogun ? 12 : 13;
    }

    public static void TabloTamEkranToggle(
        Grid anaGrid,
        UIElement tabloKart,
        int tabloSatir,
        int[] gizlenecekSatirlar,
        ref bool aktif,
        Button? btn = null)
    {
        aktif = !aktif;

        if (aktif)
        {
            SatirYukseklikleri.Remove(anaGrid);
            SatirYukseklikleri.Add(anaGrid, anaGrid.RowDefinitions.Select(r => r.Height).ToArray());

            foreach (var satir in gizlenecekSatirlar)
            {
                if (satir < 0 || satir >= anaGrid.RowDefinitions.Count)
                    continue;

                anaGrid.RowDefinitions[satir].Height = new GridLength(0);
            }

            if (tabloSatir >= 0 && tabloSatir < anaGrid.RowDefinitions.Count)
                anaGrid.RowDefinitions[tabloSatir].Height = new GridLength(1, GridUnitType.Star);

            Grid.SetRowSpan(tabloKart, Math.Max(1, anaGrid.RowDefinitions.Count - tabloSatir));
        }
        else
        {
            if (SatirYukseklikleri.TryGetValue(anaGrid, out var heights))
            {
                for (var i = 0; i < heights.Length && i < anaGrid.RowDefinitions.Count; i++)
                    anaGrid.RowDefinitions[i].Height = heights[i];

                SatirYukseklikleri.Remove(anaGrid);
            }

            Grid.SetRowSpan(tabloKart, 1);
        }

        if (btn is not null)
            btn.Content = aktif ? "Çıkış" : "Tam Ekran";
    }

    public static void FiltrePanelineOdakla(FrameworkElement filtreKart)
    {
        filtreKart.BringIntoView();
        if (filtreKart is Border border)
        {
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            timer.Tick += (_, _) =>
            {
                border.ClearValue(Border.BorderBrushProperty);
                timer.Stop();
            };
            timer.Start();
        }
    }

    private static void EkleGrupla(ContextMenu menu, string baslik, Action action)
    {
        var item = new MenuItem { Header = baslik };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }
}
