using SatinalmaPro.Mobile.Views.Controls;

namespace SatinalmaPro.Mobile.Helpers;

public static class SayfaYardimcisi
{
    private static readonly BindableProperty SurumCubuguEklendiProperty =
        BindableProperty.CreateAttached(
            "SurumCubuguEklendi",
            typeof(bool),
            typeof(SayfaYardimcisi),
            false);

    public static void SurumAltBilgiEkle(ContentPage sayfa)
    {
        if (sayfa.GetValue(SurumCubuguEklendiProperty) is true)
            return;

        var icerik = sayfa.Content;
        if (icerik is null)
            return;

        sayfa.SetValue(SurumCubuguEklendiProperty, true);
        sayfa.Content = null;

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        Grid.SetRow(icerik, 0);
        grid.Add(icerik);

        var cubuk = new SurumAltBilgiBar();
        Grid.SetRow(cubuk, 1);
        grid.Add(cubuk);

        sayfa.Content = grid;
    }
}
