using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Controls;

public partial class MalzemeSecimWindow : Window
{
    private IReadOnlyList<string> _tumListe = [];

    public string? SecilenMalzeme { get; private set; }

    public MalzemeSecimWindow(string? baslangicArama = null)
    {
        InitializeComponent();
        _tumListe = MalzemeAdiOneriServisi.TumAlinanMalzemeAdlari();

        if (!string.IsNullOrWhiteSpace(baslangicArama))
            TxtAra.Text = baslangicArama.Trim();

        Filtrele();
        Loaded += (_, _) =>
        {
            TxtAra.Focus();
            TxtAra.SelectAll();
        };
    }

    public static string? Goster(Window? owner, string? baslangicArama = null)
    {
        var pencere = new MalzemeSecimWindow(baslangicArama) { Owner = owner };
        return pencere.ShowDialog() == true ? pencere.SecilenMalzeme : null;
    }

    private void Filtrele()
    {
        var arama = TxtAra.Text?.Trim() ?? "";
        var liste = string.IsNullOrWhiteSpace(arama)
            ? _tumListe
            : MalzemeAdiOneriServisi.AlinanMalzemelerdenAra(arama).ToList();

        Liste.ItemsSource = liste;
        TxtSayac.Text = $"{liste.Count} / {_tumListe.Count} malzeme";

        if (liste.Count > 0)
            Liste.SelectedIndex = 0;
    }

    private void SecVeKapat()
    {
        if (Liste.SelectedItem is not string secim || string.IsNullOrWhiteSpace(secim))
        {
            MessageBox.Show("Lütfen listeden bir malzeme seçin.", Title,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SecilenMalzeme = secim;
        DialogResult = true;
    }

    private void TxtAra_TextChanged(object sender, TextChangedEventArgs e) => Filtrele();

    private void TxtAra_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && Liste.Items.Count > 0)
        {
            Liste.Focus();
            if (Liste.SelectedIndex < 0)
                Liste.SelectedIndex = 0;
            e.Handled = true;
        }
    }

    private void Liste_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SecVeKapat();
            e.Handled = true;
        }
    }

    private void Liste_MouseDoubleClick(object sender, MouseButtonEventArgs e) => SecVeKapat();

    private void Tamam_Click(object sender, RoutedEventArgs e) => SecVeKapat();

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
