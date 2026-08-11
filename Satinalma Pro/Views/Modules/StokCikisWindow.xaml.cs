using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class StokCikisWindow : Window
{
    private readonly ObservableCollection<StokIslemSatirKaydi> _satirlar = [];
    private StokKaydi? _seciliStok;

    public StokCikisWindow()
    {
        InitializeComponent();
        SatirGrid.ItemsSource = _satirlar;

        TxtTarih.Text = DateTime.Now.ToString("dd.MM.yyyy");
        TxtBelge.Text = StokBelgeNoUretici.SonrakiCikisBelgeNo();
        TxtTeslimEden.Text = StokCikisPdfOlusturucu.TeslimEdenMetni();

        MalzemeGiris.OneriKaynaginiAyarla(arama =>
            StokIslemServisi.MalzemeListesi(kategori: null, arama, sadeceMevcutStok: true));
        MalzemeGiris.MetinOnaylandi += (_, metin) => StoktanBilgileriDoldur(metin);
        MalzemeGiris.MetinYazildi += (_, metin) => StoktanBilgileriDoldur(metin);
    }

    private void StoktanBilgileriDoldur(string malzemeAdi)
    {
        if (string.IsNullOrWhiteSpace(malzemeAdi))
        {
            _seciliStok = null;
            CmbDepo.Items.Clear();
            TxtMevcut.Clear();
            CmbBirim.Items.Clear();
            return;
        }

        var depolar = ModulVeriDeposu.Stok
            .Where(s => s.MalzemeAdi.Equals(malzemeAdi.Trim(), StringComparison.OrdinalIgnoreCase)
                        && s.MevcutMiktar > 0
                        && !string.IsNullOrWhiteSpace(s.DepoSaha))
            .Select(s => s.DepoSaha.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        CmbDepo.SelectionChanged -= DepoDegisti;
        CmbDepo.Items.Clear();
        foreach (var d in depolar)
            CmbDepo.Items.Add(d);

        var tercih = OturumYoneticisi.AktifKullanici?.Saha?.Trim();
        var secili = depolar.FirstOrDefault(d =>
                        !string.IsNullOrWhiteSpace(tercih) &&
                        d.Equals(tercih, StringComparison.OrdinalIgnoreCase))
                    ?? depolar.FirstOrDefault();
        if (secili is not null)
            CmbDepo.SelectedItem = secili;
        CmbDepo.SelectionChanged += DepoDegisti;

        SeciliDepodanStokDoldur(malzemeAdi);
    }

    private void DepoDegisti(object sender, SelectionChangedEventArgs e)
    {
        var malzeme = (MalzemeGiris.Metin ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(malzeme))
            SeciliDepodanStokDoldur(malzeme);
    }

    private void SeciliDepodanStokDoldur(string malzemeAdi)
    {
        var depo = (CmbDepo.SelectedItem as string)?.Trim()
                   ?? CmbDepo.Text?.Trim()
                   ?? "";
        var stok = string.IsNullOrWhiteSpace(depo)
            ? StokIslemServisi.StokBulMalzemeAdi(malzemeAdi, sadeceMevcutStok: true)
            : StokIslemServisi.StokBul(malzemeAdi, depo);

        if (stok is null || stok.MevcutMiktar <= 0)
        {
            _seciliStok = null;
            TxtMevcut.Clear();
            CmbBirim.Items.Clear();
            return;
        }

        if (string.IsNullOrWhiteSpace(stok.Kategori))
            stok.Kategori = StokIslemServisi.KategoriCozumle(stok.MalzemeAdi);

        _seciliStok = stok;
        TxtMevcut.Text = $"{stok.MevcutMiktar:N2} {stok.Birim} ({stok.DepoSaha})";

        if (!string.IsNullOrWhiteSpace(stok.Birim))
            MalzemeBirimDeposu.ComboDoldur(CmbBirim, stok.Birim);
    }

    private bool SatirFormuDogrula(out StokIslemSatirKaydi satir, out StokKaydi? stok)
    {
        satir = new StokIslemSatirKaydi();
        stok = null;

        var malzeme = (MalzemeGiris.Metin ?? "").Trim();
        if (string.IsNullOrWhiteSpace(malzeme))
        {
            MessageBox.Show("Malzeme seçin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var depo = (CmbDepo.SelectedItem as string)?.Trim()
                   ?? CmbDepo.Text?.Trim()
                   ?? "";
        if (string.IsNullOrWhiteSpace(depo))
        {
            MessageBox.Show("Depo / saha seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        stok = _seciliStok is { } s && s.DepoSaha.Equals(depo, StringComparison.OrdinalIgnoreCase)
            ? _seciliStok
            : StokIslemServisi.StokBul(malzeme, depo);

        if (stok is null || stok.MevcutMiktar <= 0)
        {
            MessageBox.Show("Seçilen malzeme ve depo için yeterli stok bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var kategori = StokIslemServisi.KategoriCozumle(stok.MalzemeAdi, stok.Kategori);
        if (string.IsNullOrWhiteSpace(stok.Kategori))
            stok.Kategori = kategori;

        if (!double.TryParse(TxtMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar) || miktar <= 0)
        {
            MessageBox.Show("Geçerli bir miktar girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (miktar > stok.MevcutMiktar)
        {
            MessageBox.Show($"Yetersiz stok. Mevcut: {stok.MevcutMiktar:N2} {stok.Birim}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        satir.Kategori = kategori;
        satir.Malzeme = stok.MalzemeAdi;
        satir.Miktar = miktar;
        satir.Birim = stok.Birim;
        satir.DepoSaha = stok.DepoSaha;
        satir.MevcutStokMetin = $"{stok.MevcutMiktar:N2} {stok.Birim}";
        return true;
    }

    private void SatirFormunuTemizle()
    {
        MalzemeGiris.MetniTemizle();
        TxtMevcut.Clear();
        TxtMiktar.Clear();
        CmbBirim.Items.Clear();
        _seciliStok = null;
    }

    private void SatirEkle_Click(object sender, RoutedEventArgs e)
    {
        if (!SatirFormuDogrula(out var satir, out _))
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

    private bool CikisToplaminiDogrula(IEnumerable<StokIslemSatirKaydi> satirlar)
    {
        foreach (var grup in satirlar.GroupBy(s => (s.Malzeme, s.DepoSaha)))
        {
            var stok = StokIslemServisi.StokBul(grup.Key.Malzeme, grup.Key.DepoSaha);
            if (stok is null)
            {
                MessageBox.Show($"{grup.Key.Malzeme} için stok kaydı bulunamadı.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var toplam = grup.Sum(s => s.Miktar);
            if (toplam > stok.MevcutMiktar)
            {
                MessageBox.Show(
                    $"{grup.Key.Malzeme} için toplam çıkış ({toplam:N2}) mevcut stoktan ({stok.MevcutMiktar:N2} {stok.Birim}) fazla.",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        return true;
    }

    private void FisOnizle_Click(object sender, RoutedEventArgs e)
    {
        var teslimEdilen = TxtTeslimEdilen.Text.Trim();
        if (string.IsNullOrWhiteSpace(teslimEdilen))
        {
            MessageBox.Show("Teslim edilen kişiyi girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var satirlar = _satirlar.ToList();
        if (satirlar.Count == 0)
        {
            if (!SatirFormuDogrula(out var satir, out _))
                return;
            satirlar.Add(satir);
        }

        if (satirlar.Count == 0)
        {
            MessageBox.Show("En az bir malzeme satırı ekleyin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fisVerisi = new StokCikisFisVerisi(
            TxtBelge.Text.Trim(),
            TxtTarih.Text.Trim(),
            TxtTeslimEden.Text.Trim(),
            teslimEdilen,
            satirlar.Select(s => new StokCikisFisSatir(
                s.Malzeme,
                s.MiktarGosterim,
                s.Birim,
                s.DepoSaha)).ToList());

        StokCikisPdfOlusturucu.OnizleVeYazdir(fisVerisi);
    }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        var teslimEdilen = TxtTeslimEdilen.Text.Trim();
        if (string.IsNullOrWhiteSpace(teslimEdilen))
        {
            MessageBox.Show("Teslim edilen kişiyi girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_satirlar.Count == 0)
        {
            if (!SatirFormuDogrula(out var satir, out _))
                return;
            _satirlar.Add(satir);
            SatirFormunuTemizle();
        }

        if (_satirlar.Count == 0)
        {
            MessageBox.Show("En az bir malzeme satırı ekleyin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!CikisToplaminiDogrula(_satirlar))
            return;

        var tarih = TxtTarih.Text.Trim();
        var belgeNo = TxtBelge.Text.Trim();
        var teslimEden = TxtTeslimEden.Text.Trim();

        try
        {
            ModulVeriDeposu.BeginBatch();
            foreach (var satir in _satirlar)
            {
                StokIslemServisi.CikisYap(
                    tarih,
                    satir.Malzeme,
                    satir.DepoSaha,
                    satir.Miktar,
                    belgeNo,
                    teslimEden,
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
                    s.DepoSaha)).ToList());

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
