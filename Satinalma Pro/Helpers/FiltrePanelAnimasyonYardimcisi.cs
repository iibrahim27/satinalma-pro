using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SatinalmaPro.Helpers;

/// <summary>Filtre paneli MaxHeight animasyonu — Infinity kaynaklı DoubleAnimation hatasını önler.</summary>
public static class FiltrePanelAnimasyonYardimcisi
{
    private const double EkBosluk = 4;

    public static void YukseklikOlcul(Border kap, double varsayilanGenislik, out double yukseklik)
    {
        kap.BeginAnimation(FrameworkElement.MaxHeightProperty, null);

        var genislik = kap.ActualWidth > 0 ? kap.ActualWidth : varsayilanGenislik;
        if (genislik <= 0 || !double.IsFinite(genislik))
            genislik = 1200;

        var olculen = kap.Child as FrameworkElement;
        if (olculen is null)
        {
            yukseklik = 220;
            return;
        }

        olculen.Measure(new Size(genislik, double.PositiveInfinity));
        yukseklik = Math.Max(olculen.DesiredSize.Height + EkBosluk, 120);
    }

    public static void Toggle(
        Border kap,
        double varsayilanGenislik,
        ref bool acik,
        ref double kayitliYukseklik)
    {
        acik = !acik;

        if (acik)
            YukseklikOlcul(kap, varsayilanGenislik, out kayitliYukseklik);

        kap.BeginAnimation(FrameworkElement.MaxHeightProperty, null);

        var hedef = kayitliYukseklik;
        if (!double.IsFinite(hedef) || hedef <= 0)
            hedef = 220;

        if (acik)
        {
            kap.MaxHeight = 0;
            var anim = new DoubleAnimation(0, hedef, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.Stop
            };
            anim.Completed += (_, _) => kap.MaxHeight = hedef;
            kap.BeginAnimation(FrameworkElement.MaxHeightProperty, anim);
            return;
        }

        var mevcut = kap.MaxHeight;
        if (!double.IsFinite(mevcut) || mevcut <= 0)
            mevcut = hedef;

        var kapan = new DoubleAnimation(mevcut, 0, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.Stop
        };
        kapan.Completed += (_, _) => kap.MaxHeight = 0;
        kap.BeginAnimation(FrameworkElement.MaxHeightProperty, kapan);
    }
}
