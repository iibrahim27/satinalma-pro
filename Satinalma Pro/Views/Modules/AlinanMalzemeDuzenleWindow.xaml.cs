using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class AlinanMalzemeDuzenleWindow : Window
{
    private readonly AlinanMalzemeKaydi _kayit;
    private bool _yukleniyor;

    public AlinanMalzemeDuzenleWindow(AlinanMalzemeKaydi kayit)
    {
        InitializeComponent();
        _kayit = kayit;
        MalzemeKategoriDeposu.ComboDoldur(CmbKategori, kayit.Kategori);
        MalzemeBirimDeposu.ComboDoldur(CmbBirim, kayit.Birim);
        CmbParaBirimi.ItemsSource = ParaBirimleri.Liste;
        FormuDoldur();
    }

    private void FormuDoldur()
    {
        _yukleniyor = true;
        try
        {
            SatinalmaDepo.DovizKurlariniDisktenSenkronizeEt();

            TxtTarih.Text = _kayit.Tarih;
            TxtFaturaNo.Text = _kayit.FaturaNo;
            if (!string.IsNullOrWhiteSpace(_kayit.Kategori))
                CmbKategori.Text = _kayit.Kategori;
            TxtMalzemeHizmet.Text = _kayit.MalzemeHizmet;
            TxtMiktar.Text = _kayit.Miktar.ToString(CultureInfo.CurrentCulture);

            if (!string.IsNullOrWhiteSpace(_kayit.Birim))
                CmbBirim.Text = _kayit.Birim;

            TxtBirimFiyati.Text = _kayit.BirimFiyati.ToString(CultureInfo.CurrentCulture);

            var pb = string.IsNullOrWhiteSpace(_kayit.ParaBirimi)
                ? ParaBirimleri.Try
                : _kayit.ParaBirimi.Trim().ToUpperInvariant();
            CmbParaBirimi.SelectedItem = ParaBirimleri.Liste.Contains(pb) ? pb : ParaBirimleri.Try;

            var usd = _kayit.UsdKuru > 0 ? _kayit.UsdKuru : SatinalmaDepo.Ayarlar.VarsayilanUsdKuru;
            var eur = _kayit.EurKuru > 0 ? _kayit.EurKuru : SatinalmaDepo.Ayarlar.VarsayilanEurKuru;
            TxtUsdKuru.Text = usd > 0 ? usd.ToString(CultureInfo.CurrentCulture) : "";
            TxtEurKuru.Text = eur > 0 ? eur.ToString(CultureInfo.CurrentCulture) : "";

            TxtTedarikci.Text = _kayit.Tedarikci;
            TxtIndirildigiSaha.Text = _kayit.IndirildigiSaha;
            TxtTeslimAlan.Text = _kayit.TeslimAlan;
            TxtAciklama.Text = _kayit.Aciklama;
            DovizPaneliniGuncelle();
        }
        finally
        {
            _yukleniyor = false;
        }

        TutarHesapla(this, new RoutedEventArgs());
    }

    private void ParaBirimi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_yukleniyor)
            return;
        DovizPaneliniGuncelle();
        TutarHesapla(sender, e);
    }

    private void DovizPaneliniGuncelle()
    {
        var pb = SeciliParaBirimi();
        PanelDovizKurlari.Visibility = ParaBirimleri.TryMi(pb)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private string SeciliParaBirimi() =>
        (CmbParaBirimi.SelectedItem as string ?? ParaBirimleri.Try).Trim().ToUpperInvariant();

    private void TutarHesapla(object sender, RoutedEventArgs e)
    {
        if (_yukleniyor)
            return;

        var miktarOk = double.TryParse(TxtMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar);
        var fiyatOk = decimal.TryParse(TxtBirimFiyati.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var birimFiyati);
        if (!miktarOk || !fiyatOk)
        {
            TxtToplamTutar.Text = "";
            return;
        }

        var tlBirim = ParaBirimleri.TlCevir(
            birimFiyati,
            SeciliParaBirimi(),
            KurOku(TxtUsdKuru.Text),
            KurOku(TxtEurKuru.Text));
        TxtToplamTutar.Text = ((decimal)miktar * tlBirim).ToString("N2", CultureInfo.CurrentCulture);
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

        var paraBirimi = SeciliParaBirimi();
        var usd = KurOku(TxtUsdKuru.Text);
        var eur = KurOku(TxtEurKuru.Text);

        if (string.Equals(paraBirimi, ParaBirimleri.Usd, StringComparison.OrdinalIgnoreCase) && usd <= 0)
        {
            MessageBox.Show("USD için döviz kuru girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.Equals(paraBirimi, ParaBirimleri.Eur, StringComparison.OrdinalIgnoreCase) && eur <= 0)
        {
            MessageBox.Show("EUR için döviz kuru girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var birim = (CmbBirim.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(birim))
        {
            MessageBox.Show("Birim seçin veya yazın.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _kayit.Tarih = TxtTarih.Text.Trim();
        _kayit.FaturaNo = TxtFaturaNo.Text.Trim();
        _kayit.Kategori = CmbKategori.Text.Trim();
        _kayit.MalzemeHizmet = TxtMalzemeHizmet.Text.Trim();
        _kayit.Miktar = miktar;
        _kayit.Birim = birim;
        _kayit.BirimFiyati = birimFiyati;
        _kayit.ParaBirimi = paraBirimi;
        _kayit.UsdKuru = usd;
        _kayit.EurKuru = eur;
        _kayit.Tedarikci = TxtTedarikci.Text.Trim();
        _kayit.IndirildigiSaha = TxtIndirildigiSaha.Text.Trim();
        _kayit.TeslimAlan = TxtTeslimAlan.Text.Trim();
        _kayit.Aciklama = TxtAciklama.Text.Trim();
        _kayit.ToplamTutariHesapla();

        if (!string.IsNullOrWhiteSpace(_kayit.Kategori))
            MalzemeKategoriDeposu.Ekle(_kayit.Kategori);
        MalzemeBirimDeposu.Ekle(_kayit.Birim);

        ModulVeriDeposu.KaydetAlinanMalzemeler();
        DialogResult = true;
        Close();
    }

    private static decimal KurOku(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            return 0;
        return decimal.TryParse(metin.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out var v)
            ? v
            : 0;
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
