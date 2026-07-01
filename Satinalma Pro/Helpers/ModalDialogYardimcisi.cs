using System.Windows;
using System.Windows.Controls;

namespace SatinalmaPro.Helpers;

public static class ModalDialogYardimcisi
{
    public static void Kayit(Window pencere)
    {
        pencere.Closed += (_, _) =>
        {
            if (pencere.Owner is MainWindow mw)
                mw.ModalKapandiEscapeYoksay();
        };
    }

    public static bool EvetHayir(Window? owner, string mesaj, string? baslik = null)
    {
        var pencere = new Window
        {
            Title = baslik ?? UygulamaBilgisi.Ad,
            Owner = owner,
            Width = 440,
            Height = 170,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = owner is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = mesaj,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var evet = new Button
        {
            Content = "Evet",
            IsDefault = true,
            MinWidth = 80,
            Margin = new Thickness(0, 0, 8, 0)
        };
        evet.Click += (_, _) =>
        {
            pencere.DialogResult = true;
            pencere.Close();
        };

        var hayir = new Button
        {
            Content = "Hayır",
            IsCancel = true,
            MinWidth = 80
        };

        var butonlar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        butonlar.Children.Add(evet);
        butonlar.Children.Add(hayir);
        panel.Children.Add(butonlar);
        pencere.Content = panel;

        Kayit(pencere);
        return pencere.ShowDialog() == true;
    }
}
