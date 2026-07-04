using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class MasaustuToastBildirim
{
    private static readonly List<Window> AcikToastlar = [];

    public static void Goster(string baslik, string mesaj, Action? tiklandi = null)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || !dispatcher.CheckAccess())
        {
            dispatcher?.BeginInvoke(() => Goster(baslik, mesaj, tiklandi));
            return;
        }

        TemizleKapali();

        var toast = new Window
        {
            Width = 380,
            MinHeight = 96,
            SizeToContent = SizeToContent.Height,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            ShowInTaskbar = false,
            Topmost = true,
            ResizeMode = ResizeMode.NoResize,
            ShowActivated = false
        };

        var zemin = new System.Windows.Controls.Border
        {
            Background = new SolidColorBrush(Color.FromRgb(27, 58, 92)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10, 14, 10),
            Cursor = System.Windows.Input.Cursors.Hand,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 2,
                Opacity = 0.35
            }
        };

        var panel = new System.Windows.Controls.StackPanel();
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = baslik,
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = mesaj,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 220, 240)),
            FontSize = 12,
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 44
        });
        zemin.Child = panel;
        toast.Content = zemin;

        toast.Loaded += (_, _) => Konumlandir(toast);
        zemin.MouseLeftButtonUp += (_, _) =>
        {
            tiklandi?.Invoke();
            toast.Close();
        };

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            toast.Close();
        };
        toast.Closed += (_, _) =>
        {
            timer.Stop();
            AcikToastlar.Remove(toast);
        };

        AcikToastlar.Add(toast);
        toast.Show();
        timer.Start();

        System.Media.SystemSounds.Asterisk.Play();
    }

    private static void Konumlandir(Window toast)
    {
        toast.UpdateLayout();
        var calisma = SystemParameters.WorkArea;
        var yukseklik = double.IsNaN(toast.ActualHeight) || toast.ActualHeight <= 0 ? 96 : toast.ActualHeight;
        var index = Math.Max(0, AcikToastlar.IndexOf(toast));
        toast.Left = calisma.Right - toast.Width - 16;
        toast.Top = calisma.Bottom - yukseklik - 16 - index * (yukseklik + 10);
    }

    private static void TemizleKapali() =>
        AcikToastlar.RemoveAll(w => !w.IsLoaded);
}
