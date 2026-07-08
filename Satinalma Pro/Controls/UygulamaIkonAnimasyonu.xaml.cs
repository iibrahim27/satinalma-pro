using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SatinalmaPro.Controls;

public partial class UygulamaIkonAnimasyonu : UserControl
{
    public static readonly DependencyProperty IkonBoyutuProperty =
        DependencyProperty.Register(nameof(IkonBoyutu), typeof(double), typeof(UygulamaIkonAnimasyonu),
            new PropertyMetadata(96.0));

    public static readonly DependencyProperty GirisModuProperty =
        DependencyProperty.Register(nameof(GirisModu), typeof(bool), typeof(UygulamaIkonAnimasyonu),
            new PropertyMetadata(false, (d, _) => ((UygulamaIkonAnimasyonu)d).GirisModuBaslat()));

    public double IkonBoyutu
    {
        get => (double)GetValue(IkonBoyutuProperty);
        set => SetValue(IkonBoyutuProperty, value);
    }

    public bool GirisModu
    {
        get => (bool)GetValue(GirisModuProperty);
        set => SetValue(GirisModuProperty, value);
    }

    public UygulamaIkonAnimasyonu() => InitializeComponent();

    private void GirisModuBaslat()
    {
        if (!IsLoaded || !GirisModu)
            return;

        if (FindResource("IkonGirisAnimasyonu") is Storyboard giris)
            giris.Begin(this, true);

        if (FindResource("IkonNefesAnimasyonu") is Storyboard nefes)
            nefes.Begin(this, true);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += (_, _) => GirisModuBaslat();
    }

    private void Root_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var anim = new DoubleAnimation(3, TimeSpan.FromMilliseconds(120))
        {
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        };
        IkonEgim.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, anim);
    }

    private void Root_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        IkonEgim.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty,
            new DoubleAnimation(0, TimeSpan.FromMilliseconds(150)));
    }
}

public sealed class LoginBoyutCarpanConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double boyut)
            return 0d;
        var carpan = double.TryParse(parameter?.ToString(), NumberStyles.Any, culture, out var c) ? c : 1.0;
        return boyut * carpan;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
