using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public static class ImzaDuzenleDialog
{
    public static ImzaAyari? Goster(Window? owner, string baslik, ImzaAyari? mevcut = null)
    {
        try
        {
            var sahip = owner ?? Application.Current?.MainWindow;
            if (sahip is { IsLoaded: false })
                sahip = null;

            var pencere = new Window
            {
                Title = baslik,
                Width = 460,
                MinHeight = 280,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = sahip is null
                    ? WindowStartupLocation.CenterScreen
                    : WindowStartupLocation.CenterOwner,
                Owner = sahip,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = "Ünvan",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            });
            var unvan = new TextBox
            {
                Text = mevcut?.Unvan ?? "",
                MinHeight = 32,
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 14)
            };
            panel.Children.Add(unvan);

            panel.Children.Add(new TextBlock
            {
                Text = "Ad Soyad",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            });
            var adSoyad = new TextBox
            {
                Text = mevcut?.AdSoyad ?? "",
                MinHeight = 32,
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 14)
            };
            panel.Children.Add(adSoyad);

            var aktif = new CheckBox
            {
                Content = "Aktif (PDF'de göster)",
                IsChecked = mevcut?.Aktif ?? true,
                Margin = new Thickness(0, 0, 0, 18)
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
                MinWidth = 96,
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            tamam.Click += (_, _) =>
            {
                var u = unvan.Text.Trim();
                if (string.IsNullOrWhiteSpace(u))
                {
                    MessageBox.Show(pencere, "Ünvan gerekli.", baslik,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    unvan.Focus();
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
                MinWidth = 96,
                Padding = new Thickness(16, 8, 16, 8),
                Cursor = Cursors.Hand
            };
            iptal.Click += (_, _) => pencere.DialogResult = false;

            btnPanel.Children.Add(tamam);
            btnPanel.Children.Add(iptal);
            panel.Children.Add(btnPanel);
            pencere.Content = panel;

            pencere.Loaded += (_, _) =>
            {
                pencere.Activate();
                pencere.Focus();
                if (string.IsNullOrWhiteSpace(unvan.Text))
                    unvan.Focus();
                else
                    adSoyad.Focus();
            };

            var ok = pencere.ShowDialog() == true;
            return ok ? sonuc : null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"İmza penceresi açılamadı:\n{ex.Message}",
                baslik,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }
    }
}
