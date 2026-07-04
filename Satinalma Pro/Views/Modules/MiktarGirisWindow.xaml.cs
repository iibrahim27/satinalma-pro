using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SatinalmaPro.Views.Modules;

public partial class MiktarGirisWindow : Window
{
    public double GirilenMiktar { get; private set; }

    public MiktarGirisWindow(string baslik, string aciklama, double varsayilan = 0)
    {
        Title = baslik;
        Width = 360;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock
        {
            Text = aciklama,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var txt = new TextBox
        {
            Text = varsayilan > 0 ? varsayilan.ToString("N2", CultureInfo.CurrentCulture) : "",
            Padding = new Thickness(10, 8, 10, 8)
        };
        panel.Children.Add(txt);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        var iptal = new Button { Content = "İptal", Width = 80, Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(8, 6, 8, 6) };
        iptal.Click += (_, _) => { DialogResult = false; Close(); };

        var tamam = new Button { Content = "Tamam", Width = 80, Padding = new Thickness(8, 6, 8, 6), IsDefault = true };
        tamam.Click += (_, _) =>
        {
            if (!double.TryParse(txt.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var miktar) &&
                !double.TryParse(txt.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out miktar))
            {
                MessageBox.Show("Geçerli bir miktar girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (miktar < 0)
            {
                MessageBox.Show("Miktar negatif olamaz.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GirilenMiktar = miktar;
            DialogResult = true;
            Close();
        };

        buttons.Children.Add(iptal);
        buttons.Children.Add(tamam);
        panel.Children.Add(buttons);
        Content = panel;

        Loaded += (_, _) =>
        {
            txt.Focus();
            txt.SelectAll();
        };
    }
}
