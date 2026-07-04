using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views;

public partial class TeklifsizFirmaFiyatWindow : Window
{
    private readonly SatinalmaTalep _talep;
    private readonly IReadOnlyList<TeklifsizFirmaFiyatSatiri> _satirlar;

    public TeklifsizFirmaFiyatWindow(SatinalmaTalep talep, IReadOnlyList<TeklifsizFirmaFiyatSatiri> satirlar)
    {
        InitializeComponent();
        _talep = talep;
        _satirlar = satirlar;
        TxtBaslik.Text = talep.TalepNo;
        TxtAlt.Text = $"{talep.TalepEden} · Teklifsiz onay sonrası firma/fiyat";
        KalemListesi.ItemsSource = satirlar;
    }

    private void Iptal_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        foreach (var satir in _satirlar)
        {
            if (!satir.GecerliMi(out _))
            {
                MessageBox.Show($"'{satir.Malzeme}' için firma ve geçerli birim fiyat girin.",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        DialogResult = true;
    }
}
