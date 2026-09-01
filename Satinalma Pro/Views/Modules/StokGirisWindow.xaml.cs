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
        DepoComboDoldur();

        MalzemeGiris.OneriKaynaginiAyarla(MalzemeAdiOneriServisi.Ara);
        MalzemeGiris.MetinOnaylandi += (_, metin) => StoktanBilgileriDoldur(metin);
    }

    private void DepoComboDoldur(string? tercih = null)
    {
        var depolar = ModulVeriDeposu.Stok
            .Select(s => s.DepoSaha?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var varsayilan = tercih?.Trim()
            ?? OturumYoneticisi.AktifKullanici?.Saha?.Trim()
            ?? depolar.FirstOrDefault()
            ?? "";

        CmbDepo.Items.Clear();
        foreach (var d in depolar)
            CmbDepo.Items.Add(d);

        if (!string.IsNullOrWhiteSpace(varsayilan))
        {
            if (!depolar.Any(d => d.Equals(varsayilan, StringComparison.OrdinalIgnoreCase)))
                CmbDepo.Items.Insert(0, varsayilan);
            CmbDepo.Text = varsayilan;
            _seciliDepo = varsayilan;
        }
    }

    private string SeciliDepo()
    {
        var depo = (CmbDepo.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(depo) && CmbDepo.SelectedItem is string s)
            depo = s.Trim();
        return depo;
    }

    private string? SeciliKategori => CmbKategori.SelectedItem?.ToString();

    private void KategoriDegisti(object sender, SelectionChangedEventArgs e)
    {
        if (_malzemeIcGuncelleme) return;
    }

    private void StoktanBilgileriDoldur(string malzemeAdi)
    {
        var depo = SeciliDepo();
        var stok = StokIslemServisi.StokBulMalzemeAdi(malzemeAdi, SeciliKategori, depo)
            ?? StokIslemServisi.StokBulMalzemeAdi(malzemeAdi, depo: depo)
            ?? StokIslemServisi.StokBulMalzemeAdi(malzemeAdi, SeciliKategori)
            ?? StokIslemServisi.StokBulMalzemeAdi(malzemeAdi);
        if (stok is null) return;

        _seciliDepo = stok.DepoSaha;
        _seciliMaliyet = stok.BirimMaliyet;
        if (!string.IsNullOrWhiteSpace(stok.DepoSaha) && string.IsNullOrWhiteSpace(SeciliDepo()))
            CmbDepo.Text = stok.DepoSaha;

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

        var stok = StokIslemServisi.StokBulMalzemeAdi(malzeme, kategori, SeciliDepo())
            ?? StokIslemServisi.StokBulMalzemeAdi(malzeme, depo: SeciliDepo())
            ?? StokIslemServisi.StokBulMalzemeAdi(malzeme, kategori)
            ?? StokIslemServisi.StokBulMalzemeAdi(malzeme);

        var depo = SeciliDepo();
        if (string.IsNullOrWhiteSpace(depo))
            depo = stok?.DepoSaha?.Trim() ?? _seciliDepo;
        if (string.IsNullOrWhiteSpace(depo))
        {
            MessageBox.Show("Depo / saha seçin veya yazın. Giriş hangi depoya yapılacak belirsiz olamaz.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        decimal.TryParse(TxtBirimFiyat.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var maliyet);
        if (maliyet <= 0)
            maliyet = stok?.BirimMaliyet ?? _seciliMaliyet;

        satir.Kategori = kategori;
        satir.Malzeme = malzeme;
        satir.Miktar = miktar;
        satir.Birim = birim;
        satir.BirimFiyat = maliyet;
        satir.DepoSaha = depo;
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

    private StokCikisFisVerisi? FisVerisiOlustur(bool kayitIcin)
    {
        var teslimEdilen = TxtTeslimEdilen.Text.Trim();
        if (string.IsNullOrWhiteSpace(teslimEdilen))
        {
            MessageBox.Show("Teslim edilen kişiyi girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var tedarikci = (CmbTedarikci.Text ?? "").Trim();
        if (kayitIcin && string.IsNullOrWhiteSpace(tedarikci))
        {
            MessageBox.Show("Tedarikçi firması girin veya seçin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        var satirlar = _satirlar.ToList();
        if (satirlar.Count == 0)
        {
            if (!SatirFormuDogrula(out var satir))
                return null;
            satirlar.Add(satir);
        }

        if (satirlar.Count == 0)
        {
            MessageBox.Show("En az bir malzeme satırı ekleyin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        return new StokCikisFisVerisi(
            TxtBelge.Text.Trim(),
            TxtTarih.Text.Trim(),
            TxtTeslimEden.Text.Trim(),
            teslimEdilen,
            satirlar.Select(s => new StokCikisFisSatir(
                s.Malzeme,
                s.MiktarGosterim,
                s.Birim,
                s.DepoSaha)).ToList(),
            IndigiSaha: null,
            Tip: StokFisTipi.Giris,
            Tedarikci: string.IsNullOrWhiteSpace(tedarikci) ? null : tedarikci);
    }

    private void FisOnizle_Click(object sender, RoutedEventArgs e)
    {
        var fis = FisVerisiOlustur(kayitIcin: false);
        if (fis is null)
            return;
        StokCikisPdfOlusturucu.OnizleVeYazdir(fis);
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (FisVerisiOlustur(kayitIcin: true) is null)
            return;

        var teslimEdilen = TxtTeslimEdilen.Text.Trim();
        var tedarikci = (CmbTedarikci.Text ?? "").Trim();

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

        if (_satirlar.Any(s => string.IsNullOrWhiteSpace(s.DepoSaha)))
        {
            MessageBox.Show("Tüm satırlarda Depo / Saha dolu olmalı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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
            _ = BulutVeriSenkronu.StokSonrasiHemenGonderAsync();

            var fisVerisi = new StokCikisFisVerisi(
                belgeNo,
                tarih,
                teslimEden,
                teslimEdilen,
                _satirlar.Select(s => new StokCikisFisSatir(
                    s.Malzeme,
                    s.MiktarGosterim,
                    s.Birim,
                    s.DepoSaha)).ToList(),
                IndigiSaha: null,
                Tip: StokFisTipi.Giris,
                Tedarikci: tedarikci);

            DialogResult = true;
            Close();
            StokCikisPdfOlusturucu.OnizleVeYazdir(fisVerisi);
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
