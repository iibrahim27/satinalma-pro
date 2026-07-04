using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class FiloZimmetIptalWindow : Window
{
    private readonly FiloAracKaydi _arac;

    public FiloZimmetIptalWindow(FiloAracKaydi arac, IEnumerable<FiloZimmetKaydi> aktifZimmetler)
    {
        InitializeComponent();
        _arac = arac;
        TxtBaslik.Text = $"{arac.Plaka} — Zimmet İptal";
        ZimmetGrid.ItemsSource = aktifZimmetler.ToList();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        if (ZimmetGrid.SelectedItem is not FiloZimmetKaydi zimmet)
        {
            MessageBox.Show("İptal edilecek zimmeti seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"{zimmet.SoforAdi} için zimmet iptal edilsin mi?", "Zimmet İptal",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        zimmet.Aktif = false;
        zimmet.IptalTarihi = DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        ModulVeriDeposu.KaydetFilo();
        DialogResult = true;
        Close();
    }

    private void Kapat_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
