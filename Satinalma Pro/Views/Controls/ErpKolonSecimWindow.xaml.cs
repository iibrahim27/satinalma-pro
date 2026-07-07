using System.Windows;
using System.Windows.Controls;

namespace SatinalmaPro.Views.Controls;

public partial class ErpKolonSecimWindow : Window
{
    private readonly DataGrid _grid;
    private readonly List<(DataGridColumn Kolon, CheckBox Kutu)> _ogeler = [];

    public ErpKolonSecimWindow(DataGrid grid)
    {
        InitializeComponent();
        _grid = grid;
        KolonlariOlustur();
    }

    private void KolonlariOlustur()
    {
        KolonListesi.Children.Clear();
        _ogeler.Clear();

        foreach (var kolon in _grid.Columns)
        {
            if (string.IsNullOrWhiteSpace(kolon.Header?.ToString()))
                continue;

            var kutu = new CheckBox
            {
                Content = kolon.Header.ToString(),
                IsChecked = kolon.Visibility == Visibility.Visible,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 13
            };
            kutu.Checked += (_, _) => kolon.Visibility = Visibility.Visible;
            kutu.Unchecked += (_, _) =>
            {
                if (_grid.Columns.Count(c => c.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(c.Header?.ToString())) <= 1)
                {
                    kutu.IsChecked = true;
                    MessageBox.Show("En az bir kolon görünür olmalıdır.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                kolon.Visibility = Visibility.Collapsed;
            };

            _ogeler.Add((kolon, kutu));
            KolonListesi.Children.Add(kutu);
        }
    }

    private void TumunuGoster_Click(object sender, RoutedEventArgs e)
    {
        foreach (var (kolon, kutu) in _ogeler)
        {
            kolon.Visibility = Visibility.Visible;
            kutu.IsChecked = true;
        }
    }

    private void Sifirla_Click(object sender, RoutedEventArgs e)
    {
        foreach (var kolon in _grid.Columns)
            kolon.Visibility = Visibility.Visible;

        KolonlariOlustur();
    }

    private void Tamam_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
