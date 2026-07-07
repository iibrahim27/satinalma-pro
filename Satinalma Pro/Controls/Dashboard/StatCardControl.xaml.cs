using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class StatCardControl : UserControl
{
    public StatCardControl() => InitializeComponent();

    public void Bagla(AnaSayfaIstatistik veri)
    {
        TxtBaslik.Text = veri.Baslik;
        TxtDeger.Text = veri.Deger;
        TxtAlt.Text = veri.AltMetin;
        TxtTrend.Text = veri.TrendMetin;

        var renk = AppTheme.Parse(veri.IconRenkHex);
        IconZemin.Background = AppTheme.TintBrush(renk, 36);
        Icon.Kind = veri.Icon;
        Icon.StrokeBrush = AppTheme.Brush(veri.IconRenkHex);

        TxtTrend.Foreground = veri.TrendPozitif
            ? AppTheme.Brush(AppTheme.SuccessHex)
            : AppTheme.Brush(AppTheme.DangerHex);

        Sparkline.Points = veri.Sparkline;
    }
}
