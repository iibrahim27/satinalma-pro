using System.Windows;
using System.Windows.Controls;

namespace SatinalmaPro.Controls;

public partial class UygulamaIkonAnimasyonu : UserControl
{
    public static readonly DependencyProperty IkonBoyutuProperty =
        DependencyProperty.Register(nameof(IkonBoyutu), typeof(double), typeof(UygulamaIkonAnimasyonu),
            new PropertyMetadata(96.0));

    public double IkonBoyutu
    {
        get => (double)GetValue(IkonBoyutuProperty);
        set => SetValue(IkonBoyutuProperty, value);
    }

    public UygulamaIkonAnimasyonu() => InitializeComponent();
}
