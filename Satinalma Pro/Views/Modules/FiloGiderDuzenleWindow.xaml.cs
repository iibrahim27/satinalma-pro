using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class FiloGiderDuzenleWindow : Window
{
    private readonly FiloGiderKaydi _gider;

    public FiloGiderDuzenleWindow(FiloGiderKaydi gider)
    {
        InitializeComponent();
        _gider = gider;
        TxtTarih.Text = gider.Tarih;
        CmbGiderTipi.Text = string.IsNullOrWhiteSpace(gider.GiderTipi) ? "Bakım" : gider.GiderTipi;
        TxtTutar.Text = gider.Tutar > 0 ? gider.Tutar.ToString(CultureInfo.CurrentCulture) : "";
        TxtBelgeNo.Text = gider.BelgeNo;
        TxtAciklama.Text = gider.Aciklama;
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtTutar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var tutar))
        {
            MessageBox.Show("Tutar geçerli bir sayı olmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _gider.Tarih = TxtTarih.Text.Trim();
        _gider.GiderTipi = CmbGiderTipi.Text.Trim();
        _gider.Tutar = tutar;
        _gider.BelgeNo = TxtBelgeNo.Text.Trim();
        _gider.Aciklama = TxtAciklama.Text.Trim();

        ModulVeriDeposu.KaydetFilo();
        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
