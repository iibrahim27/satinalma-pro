using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class StockAlertPanelControl : UserControl
{
    public event EventHandler? TumunuGorTiklandi;

    public StockAlertPanelControl() => InitializeComponent();

    public void Bagla(IReadOnlyList<AnaSayfaStokUyari> uyarilar)
    {
        Liste.ItemsSource = uyarilar.Select(u => new StokSatir
        {
            Malzeme = u.Malzeme,
            MevcutMetin = u.MevcutMetin,
            Durum = u.Durum,
            DurumArkaplan = AppTheme.TintBrush(AppTheme.Parse(u.DurumRenkHex), 32),
            DurumOnplan = AppTheme.Brush(u.DurumRenkHex)
        }).ToList();
    }

    private void TumunuGor_Click(object sender, RoutedEventArgs e) =>
        TumunuGorTiklandi?.Invoke(this, EventArgs.Empty);

    private sealed class StokSatir
    {
        public required string Malzeme { get; init; }
        public required string MevcutMetin { get; init; }
        public required string Durum { get; init; }
        public required Brush DurumArkaplan { get; init; }
        public required Brush DurumOnplan { get; init; }
    }
}
