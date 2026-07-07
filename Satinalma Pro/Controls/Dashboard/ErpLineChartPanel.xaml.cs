using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ErpLineChartPanel : UserControl
{
    private IReadOnlyList<AnaSayfaAylikNokta>? _noktalar;
    private double _sonGenislik;
    private double _sonYukseklik;
    private bool _ciziliyor;

    public ErpLineChartPanel()
    {
        InitializeComponent();
        Loaded += (_, _) => Ciz();
        SizeChanged += (_, _) => Ciz();
    }

    public void Bagla(IReadOnlyList<AnaSayfaAylikNokta> noktalar)
    {
        _noktalar = noktalar;
        _sonGenislik = 0;
        _sonYukseklik = 0;
        Ciz();
    }

    private void Ciz()
    {
        if (_ciziliyor)
            return;

        var pts = _noktalar;
        if (pts is null || pts.Count < 2)
            return;

        var w = ChartCanvas.ActualWidth;
        var h = ChartCanvas.ActualHeight;
        if (w <= 0 || h <= 0)
            return;

        if (ChartCanvas.Children.Count > 0
            && Math.Abs(w - _sonGenislik) < 1
            && Math.Abs(h - _sonYukseklik) < 1)
            return;

        _ciziliyor = true;
        try
        {
            _sonGenislik = w;
            _sonYukseklik = h;
            ChartCanvas.Children.Clear();

        var max = pts.Max(p => p.Deger);
        if (max <= 0) max = 1;

        var poly = new Polyline
        {
            Stroke = AppTheme.PrimaryBrush,
            StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round
        };

        for (var i = 0; i < pts.Count; i++)
        {
            var x = 20 + i / (pts.Count - 1.0) * (w - 40);
            var y = h - 30 - (pts[i].Deger / max) * (h - 50);
            poly.Points.Add(new Point(x, y));

            var lbl = new TextBlock
            {
                Text = pts[i].Etiket,
                FontSize = 10,
                Foreground = AppTheme.SecondaryTextBrush,
                Width = 40,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(lbl, x - 20);
            Canvas.SetTop(lbl, h - 22);
            ChartCanvas.Children.Add(lbl);

            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = AppTheme.PrimaryBrush,
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(dot, x - 4);
            Canvas.SetTop(dot, y - 4);
            ChartCanvas.Children.Add(dot);
        }

        ChartCanvas.Children.Insert(0, poly);
        }
        finally
        {
            _ciziliyor = false;
        }
    }
}
