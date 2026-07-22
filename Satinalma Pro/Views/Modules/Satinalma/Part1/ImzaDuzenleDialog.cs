using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public static class ImzaDuzenleDialog
{
    public static ImzaAyari? Goster(Window? owner, string baslik, ImzaAyari? mevcut = null)
    {
        var pencere = new Window
        {
            Title = baslik,
            Width = 440,
            Height = 260,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            ResizeMode = ResizeMode.NoResize
        };

        var panel = new StackPanel { Margin = new Thickness(16) };

        panel.Children.Add(new TextBlock { Text = "Ünvan", Margin = new Thickness(0, 0, 0, 6) });
        var unvan = new TextBox
        {
            Text = mevcut?.Unvan ?? "",
            MinHeight = 28,
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(unvan);

        panel.Children.Add(new TextBlock { Text = "Ad Soyad", Margin = new Thickness(0, 0, 0, 6) });
        var adSoyad = new TextBox
        {
            Text = mevcut?.AdSoyad ?? "",
            MinHeight = 28,
            Margin = new Thickness(0, 0, 0, 12)
        };
        panel.Children.Add(adSoyad);

        var aktif = new CheckBox
        {
            Content = "Aktif",
            IsChecked = mevcut?.Aktif ?? true,
            Margin = new Thickness(0, 0, 0, 14)
        };
        panel.Children.Add(aktif);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        ImzaAyari? sonuc = null;
        var tamam = new Button
        {
            Content = "Kaydet",
            IsDefault = true,
            Padding = new Thickness(14, 6, 14, 6),
            Margin = new Thickness(0, 0, 8, 0)
        };
        tamam.Click += (_, _) =>
        {
            var u = unvan.Text.Trim();
            if (string.IsNullOrWhiteSpace(u))
            {
                MessageBox.Show("Ünvan gerekli.", baslik, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            sonuc = new ImzaAyari
            {
                Unvan = u,
                AdSoyad = adSoyad.Text.Trim(),
                Aktif = aktif.IsChecked == true
            };
            pencere.DialogResult = true;
        };
        var iptal = new Button
        {
            Content = "İptal",
            IsCancel = true,
            Padding = new Thickness(14, 6, 14, 6)
        };
        iptal.Click += (_, _) => pencere.DialogResult = false;

        btnPanel.Children.Add(tamam);
        btnPanel.Children.Add(iptal);
        panel.Children.Add(btnPanel);
        pencere.Content = panel;

        return pencere.ShowDialog() == true ? sonuc : null;
    }
}
