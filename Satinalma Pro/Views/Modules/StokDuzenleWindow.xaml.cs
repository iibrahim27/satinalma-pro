using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class StokDuzenleWindow : Window
{
    private readonly StokKaydi _kayit;

    public StokDuzenleWindow(StokKaydi kayit)
    {
        InitializeComponent();
        _kayit = kayit;
        MalzemeKategoriDeposu.ComboDoldur(CmbKategori, kayit.Kategori);
        FormuDoldur();
    }

    private void FormuDoldur()
    {
        TxtMalzemeAdi.Text = _kayit.MalzemeAdi;
        if (!string.IsNullOrWhiteSpace(_kayit.Kategori))
            CmbKategori.Text = _kayit.Kategori;
        TxtMevcutMiktar.Text = _kayit.MevcutMiktar.ToString(CultureInfo.CurrentCulture);
        TxtMinimumStok.Text = _kayit.MinimumStok.ToString(CultureInfo.CurrentCulture);
        TxtBirim.Text = _kayit.Birim;
        TxtDepoSaha.Text = _kayit.DepoSaha;
        TxtBirimMaliyet.Text = _kayit.BirimMaliyet.ToString(CultureInfo.CurrentCulture);
        TxtSonGuncelleme.Text = _kayit.SonGuncelleme;
        TxtAciklama.Text = _kayit.Aciklama;
        DegerHesapla(this, new RoutedEventArgs());
    }

    private void DegerHesapla(object sender, RoutedEventArgs e)
    {
        var miktarOk = double.TryParse(TxtMevcutMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar);
        var maliyetOk = decimal.TryParse(TxtBirimMaliyet.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var maliyet);

        if (miktarOk && maliyetOk)
            TxtToplamDeger.Text = ((decimal)miktar * maliyet).ToString("N2", CultureInfo.CurrentCulture);
        else
            TxtToplamDeger.Text = "";
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtMalzemeAdi.Text))
        {
            MessageBox.Show("Malzeme adı zorunludur.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(TxtMevcutMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var mevcut))
        {
            MessageBox.Show("Mevcut miktar geçerli bir sayı olmalıdır.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(TxtMinimumStok.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var minimum))
            minimum = 0;

        if (!decimal.TryParse(TxtBirimMaliyet.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var maliyet))
            maliyet = 0;

        _kayit.MalzemeAdi = TxtMalzemeAdi.Text.Trim();
        _kayit.Kategori = CmbKategori.Text.Trim();
        _kayit.MevcutMiktar = mevcut;
        _kayit.MinimumStok = minimum;
        _kayit.Birim = TxtBirim.Text.Trim();
        _kayit.DepoSaha = TxtDepoSaha.Text.Trim();
        _kayit.BirimMaliyet = maliyet;
        _kayit.SonGuncelleme = string.IsNullOrWhiteSpace(TxtSonGuncelleme.Text)
            ? DateTime.Now.ToString("dd.MM.yyyy")
            : TxtSonGuncelleme.Text.Trim();
        _kayit.Aciklama = TxtAciklama.Text.Trim();
        _kayit.ToplamDegerHesapla();

        ModulVeriDeposu.KaydetStok();
        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
