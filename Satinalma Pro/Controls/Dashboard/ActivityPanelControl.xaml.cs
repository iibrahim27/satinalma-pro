using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ActivityPanelControl : UserControl
{
    public event EventHandler? TumunuGorTiklandi;

    public ActivityPanelControl() => InitializeComponent();

    public void Bagla(IReadOnlyList<AnaSayfaIslem> islemler)
    {
        Liste.ItemsSource = islemler.Select(i => new ActivitySatir
        {
            Baslik = i.Baslik,
            Zaman = i.Zaman,
            Durum = i.Durum,
            Icon = i.Icon,
            DurumArkaplan = AppTheme.TintBrush(AppTheme.Parse(i.DurumRenkHex), 32),
            DurumOnplan = AppTheme.Brush(i.DurumRenkHex)
        }).ToList();
    }

    private void TumunuGor_Click(object sender, RoutedEventArgs e) =>
        TumunuGorTiklandi?.Invoke(this, EventArgs.Empty);

    private sealed class ActivitySatir
    {
        public required string Baslik { get; init; }
        public required string Zaman { get; init; }
        public required string Durum { get; init; }
        public required DashboardIconKind Icon { get; init; }
        public required Brush DurumArkaplan { get; init; }
        public required Brush DurumOnplan { get; init; }
    }
}
