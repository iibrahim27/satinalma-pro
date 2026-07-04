using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

public static class LogoGorselYardimcisi
{
    private const string VarsayilanLogoUri = "pack://application:,,,/Assets/mv-insaat-logo.png";

    public static BitmapImage? VarsayilanLogo() => UriDenYukle(new Uri(VarsayilanLogoUri));

    public static BitmapImage? Yukle(string? kayitliYol)
    {
        var tam = SatinalmaProLogoDeposu.TamYol(kayitliYol);
        if (string.IsNullOrEmpty(tam))
            return null;

        try
        {
            return UriDenYukle(new Uri(tam, UriKind.Absolute));
        }
        catch
        {
            return null;
        }
    }

    public static void GorselAyarla(Image image, string? kayitliYol) =>
        image.Source = Yukle(kayitliYol) ?? VarsayilanLogo();

    private static BitmapImage? UriDenYukle(Uri uri)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
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
}
