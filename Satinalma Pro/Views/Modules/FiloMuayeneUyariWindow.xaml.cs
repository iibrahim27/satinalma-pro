using System.Windows;
using SatinalmaPro.Models;

namespace SatinalmaPro.Views.Modules;

public partial class FiloMuayeneUyariWindow : Window
{
    public FiloMuayeneUyariWindow(IEnumerable<FiloAracKaydi> araclar)
    {
        InitializeComponent();
        var liste = araclar.ToList();
        UyariGrid.ItemsSource = liste;
        TxtAciklama.Text = liste.Count == 1
            ? "1 aracın muayenesi 15 gün içinde doluyor."
            : $"{liste.Count} aracın muayenesi 15 gün içinde doluyor.";
    }

    private void Tamam_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
