using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ErpDonutChartPanel : UserControl
{
    public ErpDonutChartPanel() => InitializeComponent();

    public void Bagla(IReadOnlyList<AnaSayfaDagilim> dilimler)
    {
        DonutCanvas.Children.Clear();
        LegendPanel.Children.Clear();
        if (dilimler.Count == 0) return;

        const double cx = 80, cy = 80, r = 60, inner = 38;
        var start = -90.0;

        foreach (var d in dilimler)
        {
            var sweep = d.Yuzde / 100.0 * 360.0;
            DonutCanvas.Children.Add(Dilim(cx, cy, r, inner, start, sweep, AppTheme.Brush(d.RenkHex)));
            start += sweep;

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            row.Children.Add(new Border
            {
                Width = 10, Height = 10, CornerRadius = new CornerRadius(2),
                Background = AppTheme.Brush(d.RenkHex), Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(new TextBlock
            {
                Text = $"{d.Etiket}  {d.Yuzde:0.#}%",
                FontSize = 12,
                Foreground = AppTheme.TextBrush,
                VerticalAlignment = VerticalAlignment.Center
            });
            LegendPanel.Children.Add(row);
        }
    }

    private static Path Dilim(double cx, double cy, double r, double inner, double startDeg, double sweepDeg, Brush fill)
    {
        var start = Rad(startDeg);
        var end = Rad(startDeg + sweepDeg);
        var large = sweepDeg > 180 ? 1 : 0;

        var x1 = cx + r * Math.Cos(start);
        var y1 = cy + r * Math.Sin(start);
        var x2 = cx + r * Math.Cos(end);
        var y2 = cy + r * Math.Sin(end);
        var ix1 = cx + inner * Math.Cos(end);
        var iy1 = cy + inner * Math.Sin(end);
        var ix2 = cx + inner * Math.Cos(start);
        var iy2 = cy + inner * Math.Sin(start);

        var fig = new PathFigure { StartPoint = new Point(x1, y1), IsClosed = true };
        fig.Segments.Add(new ArcSegment(new Point(x2, y2), new Size(r, r), 0, large == 1, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(new Point(ix1, iy1), true));
        fig.Segments.Add(new ArcSegment(new Point(ix2, iy2), new Size(inner, inner), 0, large == 1, SweepDirection.Counterclockwise, true));

        return new Path { Fill = fill, Data = new PathGeometry(new[] { fig }) };
    }

    private static double Rad(double deg) => deg * Math.PI / 180.0;
}
