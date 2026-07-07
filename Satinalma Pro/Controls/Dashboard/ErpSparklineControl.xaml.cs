using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ErpSparklineControl : UserControl
{
    public static readonly DependencyProperty PointsProperty =
        DependencyProperty.Register(nameof(Points), typeof(IReadOnlyList<double>), typeof(ErpSparklineControl),
            new PropertyMetadata(null, (d, _) => ((ErpSparklineControl)d).Ciz()));

    public IReadOnlyList<double>? Points
    {
        get => (IReadOnlyList<double>?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    private readonly Canvas _canvas = new() { ClipToBounds = true };

    public ErpSparklineControl()
    {
        Content = _canvas;
        Loaded += (_, _) => Ciz();
        SizeChanged += (_, _) => Ciz();
    }

    public void Ciz()
    {
        _canvas.Children.Clear();
        var pts = Points;
        if (pts is null || pts.Count < 2 || ActualWidth <= 0 || ActualHeight <= 0)
            return;

        var min = pts.Min();
        var max = pts.Max();
        var range = max - min;
        if (range <= 0) range = 1;

        var w = ActualWidth;
        var h = ActualHeight;
        var poly = new Polyline
        {
            Stroke = AppTheme.PrimaryBrush,
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round
        };

        for (var i = 0; i < pts.Count; i++)
        {
            var x = i / (pts.Count - 1.0) * w;
            var y = h - ((pts[i] - min) / range) * (h - 4) - 2;
            poly.Points.Add(new Point(x, y));
        }

        _canvas.Children.Add(poly);
    }
}
