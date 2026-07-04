using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class AlinanMalzemeDuzenleWindow : Window
{
    private readonly AlinanMalzemeKaydi _kayit;

    public AlinanMalzemeDuzenleWindow(AlinanMalzemeKaydi kayit)
    {
        InitializeComponent();
        _kayit = kayit;
        MalzemeKategoriDeposu.ComboDoldur(CmbKategori, kayit.Kategori);
        FormuDoldur();
    }

    private void FormuDoldur()
    {
        TxtTarih.Text = _kayit.Tarih;
        TxtFaturaNo.Text = _kayit.FaturaNo;
        if (!string.IsNullOrWhiteSpace(_kayit.Kategori))
            CmbKategori.Text = _kayit.Kategori;
        TxtMalzemeHizmet.Text = _kayit.MalzemeHizmet;
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
        _kayit.FaturaNo = TxtFaturaNo.Text.Trim();
        _kayit.Kategori = CmbKategori.Text.Trim();
        _kayit.MalzemeHizmet = TxtMalzemeHizmet.Text.Trim();
        _kayit.Miktar = miktar;
        _kayit.Birim = TxtBirim.Text.Trim();
        _kayit.BirimFiyati = birimFiyati;
        _kayit.Tedarikci = TxtTedarikci.Text.Trim();
        _kayit.IndirildigiSaha = TxtIndirildigiSaha.Text.Trim();
        _kayit.TeslimAlan = TxtTeslimAlan.Text.Trim();
        _kayit.Aciklama = TxtAciklama.Text.Trim();
        _kayit.ToplamTutariHesapla();

        if (!string.IsNullOrWhiteSpace(_kayit.Kategori))
            MalzemeKategoriDeposu.Ekle(_kayit.Kategori);

        ModulVeriDeposu.KaydetAlinanMalzemeler();
        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
