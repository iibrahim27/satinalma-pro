using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

public static class LogoGorselYardimcisi
{
    public static BitmapImage? Yukle(string? kayitliYol)
    {
        var tam = SatinalmaProLogoDeposu.TamYol(kayitliYol);
        if (string.IsNullOrEmpty(tam))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(tam, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static void GorselAyarla(Image image, string? kayitliYol) =>
        image.Source = Yukle(kayitliYol);
}
