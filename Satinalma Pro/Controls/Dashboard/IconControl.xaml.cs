using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Services;

namespace SatinalmaPro.Controls.Dashboard;

public partial class IconControl : UserControl
{
    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(nameof(Kind), typeof(DashboardIconKind), typeof(IconControl),
            new PropertyMetadata(DashboardIconKind.Home, OnKindChanged));

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(IconControl),
            new PropertyMetadata(18.0, OnKindChanged));

    public static readonly DependencyProperty StrokeBrushProperty =
        DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(IconControl),
            new PropertyMetadata(Brushes.Black, OnKindChanged));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(IconControl),
            new PropertyMetadata(1.75, OnKindChanged));

    public DashboardIconKind Kind
    {
        get => (DashboardIconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public Brush StrokeBrush
    {
        get => (Brush)GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public IconControl()
    {
        InitializeComponent();
    }

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconControl control)
            control.Guncelle();
    }

    private void Guncelle()
    {
        IconHost.Children.Clear();
        IconHost.Children.Add(IconProvider.Olustur(Kind, StrokeBrush, IconSize, StrokeThickness));
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => Guncelle();
}
