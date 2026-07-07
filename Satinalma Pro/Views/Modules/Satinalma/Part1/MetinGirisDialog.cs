using System.Windows;
using System.Windows.Controls;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public static class MetinGirisDialog
{
    public static string? Goster(Window? owner, string baslik, string etiket, string varsayilan = "")
    {
        var pencere = new Window
        {
            Title = baslik,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            ResizeMode = ResizeMode.NoResize
        };

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = etiket, Margin = new Thickness(0, 0, 0, 8) });

        var kutu = new TextBox { Text = varsayilan, MinHeight = 28 };
        panel.Children.Add(kutu);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };

        string? sonuc = null;
        var tamam = new Button { Content = "Tamam", IsDefault = true, Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 8, 0) };
        tamam.Click += (_, _) => { sonuc = kutu.Text; pencere.DialogResult = true; };
        var iptal = new Button { Content = "İptal", IsCancel = true, Padding = new Thickness(14, 6, 14, 6) };
        iptal.Click += (_, _) => { pencere.DialogResult = false; };

        btnPanel.Children.Add(tamam);
        btnPanel.Children.Add(iptal);
        panel.Children.Add(btnPanel);
        pencere.Content = panel;

        return pencere.ShowDialog() == true ? sonuc : null;
    }
}
