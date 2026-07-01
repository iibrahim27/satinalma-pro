using System.Globalization;
using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class FinansmanGelirDuzenleWindow : Window
{
    private readonly FinansmanGelirKaydi _kayit;

    public FinansmanGelirDuzenleWindow(FinansmanGelirKaydi kayit)
    {
        InitializeComponent();
        _kayit = kayit;

        foreach (var k in FinansmanGelirKategorileri.Tum)
            CmbKategori.Items.Add(k);

        foreach (var o in new[] { "Havale", "EFT", "Çek", "Nakit", "Kredi Kartı" })
            CmbOdemeSekli.Items.Add(o);

        FormuDoldur();
    }

    private void FormuDoldur()
    {
        TxtTarih.Text = string.IsNullOrWhiteSpace(_kayit.Tarih)
            ? DateTime.Today.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : _kayit.Tarih;
        TxtBelgeNo.Text = _kayit.BelgeNo;
        CmbKategori.Text = string.IsNullOrWhiteSpace(_kayit.Kategori) ? "Hakediş" : _kayit.Kategori;
        TxtAciklama.Text = _kayit.Aciklama;
        TxtKaynak.Text = _kayit.Kaynak;
        TxtSaha.Text = _kayit.Saha;
        TxtTutar.Text = _kayit.Tutar > 0 ? _kayit.Tutar.ToString(CultureInfo.CurrentCulture) : "";
        CmbOdemeSekli.Text = string.IsNullOrWhiteSpace(_kayit.OdemeSekli) ? "Havale" : _kayit.OdemeSekli;
        TxtNotlar.Text = _kayit.Notlar;
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtTutar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var tutar) || tutar <= 0)
        {
            MessageBox.Show("Geçerli bir tutar girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtAciklama.Text) && string.IsNullOrWhiteSpace(TxtKaynak.Text))
        {
            MessageBox.Show("Açıklama veya kaynak bilgisi girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _kayit.Tarih = TxtTarih.Text.Trim();
        _kayit.BelgeNo = TxtBelgeNo.Text.Trim();
        _kayit.Kategori = CmbKategori.Text.Trim();
        _kayit.Aciklama = TxtAciklama.Text.Trim();
        _kayit.Kaynak = TxtKaynak.Text.Trim();
        _kayit.Saha = TxtSaha.Text.Trim();
        _kayit.Tutar = tutar;
        _kayit.OdemeSekli = CmbOdemeSekli.Text.Trim();
        _kayit.Notlar = TxtNotlar.Text.Trim();

        FinansmanVeriDeposu.Kaydet();
        DialogResult = true;
        Close();
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
