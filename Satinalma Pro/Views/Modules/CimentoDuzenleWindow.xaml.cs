using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class CimentoDuzenleWindow : Window
{
    private readonly CimentoKaydi _kayit;

    public CimentoDuzenleWindow(CimentoKaydi kayit)
    {
        InitializeComponent();
        _kayit = kayit;
        FormuDoldur();
    }

    private void FormuDoldur()
    {
        TxtTarih.Text = _kayit.Tarih;
        TxtIrsaliyeNo.Text = _kayit.IrsaliyeNo;
        CmbCimentoSinifi.Text = _kayit.CimentoSinifi;
        CmbCimentoCinsi.Text = _kayit.CimentoCinsi;
        TxtMiktar.Text = _kayit.Miktar.ToString(CultureInfo.CurrentCulture);
        TxtBirim.Text = _kayit.Birim;
        TxtBirimFiyati.Text = _kayit.BirimFiyati.ToString(CultureInfo.CurrentCulture);
        TxtTedarikci.Text = _kayit.Tedarikci;
        TxtIndirildigiSaha.Text = _kayit.IndirildigiSaha;
        TxtTeslimAlan.Text = _kayit.TeslimAlan;
        TxtAciklama.Text = _kayit.Aciklama;
        TutarHesapla(this, new RoutedEventArgs());
    }

    private void TutarHesapla(object sender, RoutedEventArgs e)
    {
        var miktarOk = double.TryParse(TxtMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar);
        var fiyatOk = decimal.TryParse(TxtBirimFiyati.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var birimFiyati);

        if (miktarOk && fiyatOk)
            TxtToplamTutar.Text = ((decimal)miktar * birimFiyati).ToString("N2", CultureInfo.CurrentCulture);
        else
            TxtToplamTutar.Text = "";
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(TxtMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar))
        {
            MessageBox.Show("Miktar geçerli bir sayı olmalıdır.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtBirimFiyati.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var birimFiyati))
        {
            MessageBox.Show("Birim fiyatı geçerli bir sayı olmalıdır.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _kayit.Tarih = TxtTarih.Text.Trim();
        _kayit.IrsaliyeNo = TxtIrsaliyeNo.Text.Trim();
        _kayit.CimentoSinifi = CmbCimentoSinifi.Text.Trim();
        _kayit.CimentoCinsi = CmbCimentoCinsi.Text.Trim();
        _kayit.Miktar = miktar;
        _kayit.Birim = TxtBirim.Text.Trim();
        _kayit.BirimFiyati = birimFiyati;
        _kayit.Tedarikci = TxtTedarikci.Text.Trim();
        _kayit.IndirildigiSaha = TxtIndirildigiSaha.Text.Trim();
        _kayit.TeslimAlan = TxtTeslimAlan.Text.Trim();
        _kayit.Aciklama = TxtAciklama.Text.Trim();
        _kayit.ToplamTutariHesapla();

        ModulVeriDeposu.KaydetCimento();
        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
