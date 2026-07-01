using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Views.Controls;

namespace SatinalmaPro.Views.Modules;

public partial class StokGirisWindow : Window
{
    private readonly ObservableCollection<StokIslemSatirKaydi> _satirlar = [];
    private bool _malzemeIcGuncelleme;
    private string _seciliDepo = "";
    private decimal _seciliMaliyet;

    public StokGirisWindow()
    {
        InitializeComponent();
        SatirGrid.ItemsSource = _satirlar;

        TxtTarih.Text = DateTime.Now.ToString("dd.MM.yyyy");
        TxtBelge.Text = StokBelgeNoUretici.SonrakiGirisBelgeNo();
        TxtTeslimEden.Text = KullaniciYetkileri.AktifKullaniciAdi() ?? "";

        MalzemeKategoriDeposu.ComboDoldur(CmbKategori);
        MalzemeBirimDeposu.ComboDoldur(CmbBirim);
        TedarikciDeposu.ComboDoldur(CmbTedarikci);

        MalzemeGiris.OneriKaynaginiAyarla(MalzemeAdiOneriServisi.Ara);
        MalzemeGiris.MetinOnaylandi += (_, metin) => StoktanBilgileriDoldur(metin);
    }

    private string? SeciliKategori => CmbKategori.SelectedItem?.ToString();

    private void KategoriDegisti(object sender, SelectionChangedEventArgs e)
    {
        if (_malzemeIcGuncelleme) return;
    }

    private void StoktanBilgileriDoldur(string malzemeAdi)
    {
        var stok = StokIslemServisi.StokBulMalzemeAdi(malzemeAdi, SeciliKategori)
            ?? StokIslemServisi.StokBulMalzemeAdi(malzemeAdi);
        if (stok is null) return;

        _seciliDepo = stok.DepoSaha;
        _seciliMaliyet = stok.BirimMaliyet;

        if (!string.IsNullOrWhiteSpace(stok.Kategori))
        {
            _malzemeIcGuncelleme = true;
            for (var i = 0; i < CmbKategori.Items.Count; i++)
            {
                if (CmbKategori.Items[i]?.ToString()?.Equals(stok.Kategori, StringComparison.OrdinalIgnoreCase) == true)
                {
                    CmbKategori.SelectedIndex = i;
                    break;
                }
            }
            _malzemeIcGuncelleme = false;
        }

        if (!string.IsNullOrWhiteSpace(stok.Birim))
            MalzemeBirimDeposu.ComboDoldur(CmbBirim, stok.Birim);

        if (stok.BirimMaliyet > 0)
            TxtBirimFiyat.Text = stok.BirimMaliyet.ToString(CultureInfo.CurrentCulture);
    }

    private bool SatirFormuDogrula(out StokIslemSatirKaydi satir)
    {
        satir = new StokIslemSatirKaydi();

        if (!double.TryParse(TxtMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar) || miktar <= 0)
        {
            MessageBox.Show("Geçerli bir miktar girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var malzeme = (MalzemeGiris.Metin ?? "").Trim();
        if (string.IsNullOrWhiteSpace(malzeme))
        {
            MessageBox.Show("Malzeme seçin veya yazın.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var kategori = SeciliKategori?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(kategori))
        {
            MessageBox.Show("Kategori seçin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var birim = CmbBirim.SelectedItem?.ToString()?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(birim))
        {
            MessageBox.Show("Birim seçin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var stok = StokIslemServisi.StokBulMalzemeAdi(malzeme, kategori)
            ?? StokIslemServisi.StokBulMalzemeAdi(malzeme);

        decimal.TryParse(TxtBirimFiyat.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var maliyet);
        if (maliyet <= 0)
            maliyet = stok?.BirimMaliyet ?? _seciliMaliyet;

        satir.Kategori = kategori;
        satir.Malzeme = malzeme;
        satir.Miktar = miktar;
        satir.Birim = birim;
        satir.BirimFiyat = maliyet;
        satir.DepoSaha = stok?.DepoSaha ?? _seciliDepo;
        return true;
    }

    private void SatirFormunuTemizle()
    {
        MalzemeGiris.MetniTemizle();
        TxtMiktar.Clear();
        TxtBirimFiyat.Clear();
        _seciliDepo = "";
        _seciliMaliyet = 0;
        if (CmbBirim.Items.Count > 0)
            CmbBirim.SelectedIndex = 0;
    }

    private void SatirEkle_Click(object sender, RoutedEventArgs e)
    {
        if (!SatirFormuDogrula(out var satir))
            return;

        _satirlar.Add(satir);
        SatirFormunuTemizle();
    }

    private void SatirSil_Click(object sender, RoutedEventArgs e)
    {
        if (SatirGrid.SelectedItem is StokIslemSatirKaydi satir)
            _satirlar.Remove(satir);
    }

    private void SatirGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        BtnSatirSil.IsEnabled = SatirGrid.SelectedItem is not null;

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        var teslimEdilen = TxtTeslimEdilen.Text.Trim();
        if (string.IsNullOrWhiteSpace(teslimEdilen))
        {
            MessageBox.Show("Teslim edilen kişiyi girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var tedarikci = (CmbTedarikci.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(tedarikci))
        {
            MessageBox.Show("Tedarikçi firması girin veya seçin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_satirlar.Count == 0)
        {
            if (!SatirFormuDogrula(out var satir))
                return;
            _satirlar.Add(satir);
            SatirFormunuTemizle();
        }

        if (_satirlar.Count == 0)
        {
            MessageBox.Show("En az bir malzeme satırı ekleyin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var tarih = TxtTarih.Text.Trim();
        var belgeNo = TxtBelge.Text.Trim();
        var teslimEden = TxtTeslimEden.Text.Trim();

        try
        {
            ModulVeriDeposu.BeginBatch();
            foreach (var satir in _satirlar)
            {
                StokIslemServisi.GirisYap(
                    tarih,
                    satir.Malzeme,
                    satir.Kategori,
                    satir.Birim,
                    satir.Miktar,
                    satir.DepoSaha,
                    satir.BirimFiyat,
                    belgeNo,
                    teslimEden,
                    teslimEdilen);

                StokIslemServisi.AlinanMalzemeyeKaydet(
                    satir,
                    tarih,
                    belgeNo,
                    tedarikci,
                    teslimEdilen);
            }
            ModulVeriDeposu.EndBatch();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ModulVeriDeposu.EndBatch();
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
