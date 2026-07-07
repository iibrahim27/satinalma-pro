using System.Windows;

namespace SatinalmaPro.Views.Controls;

public partial class ErpKayitGecmisiWindow : Window
{
    public ErpKayitGecmisiWindow(string baslik, IEnumerable<string> satirlar)
    {
        InitializeComponent();
        TxtBaslik.Text = baslik;
        GecmisListe.ItemsSource = satirlar.ToList();
    }

    private void Kapat_Click(object sender, RoutedEventArgs e) => Close();
}
