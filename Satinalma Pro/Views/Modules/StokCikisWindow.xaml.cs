using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Views.Controls;

namespace SatinalmaPro.Views.Modules;

public partial class StokCikisWindow : Window
{
    private readonly ObservableCollection<StokIslemSatirKaydi> _satirlar = [];
    private bool _malzemeIcGuncelleme;
    private StokKaydi? _seciliStok;

    public StokCikisWindow()
    {
        InitializeComponent();
        SatirGrid.ItemsSource = _satirlar;

        TxtTarih.Text = DateTime.Now.ToString("dd.MM.yyyy");
        TxtBelge.Text = StokBelgeNoUretici.SonrakiCikisBelgeNo();
        TxtTeslimEden.Text = KullaniciYetkileri.AktifKullaniciAdi() ?? "";

        MalzemeKategoriDeposu.ComboDoldur(CmbKategori);
        MalzemeKaynaginiGuncelle();
        MalzemeGiris.MetinOnaylandi += (_, metin) => StoktanBilgileriDoldur(metin);
    }

    private string? SeciliKategori => CmbKategori.SelectedItem?.ToString();

    private void MalzemeKaynaginiGuncelle()
    {
        MalzemeGiris.OneriKaynaginiAyarla(arama =>
            StokIslemServisi.MalzemeListesi(SeciliKategori, arama, sadeceMevcutStok: true));
    }

    private void KategoriDegisti(object sender, SelectionChangedEventArgs e)
    {
        if (_malzemeIcGuncelleme) return;

        _seciliStok = null;
        TxtMevcut.Clear();
        CmbBirim.Items.Clear();
        MalzemeKaynaginiGuncelle();
    }

    private void StoktanBilgileriDoldur(string malzemeAdi)
    {
        var stok = StokIslemServisi.StokBulMalzemeAdi(malzemeAdi, SeciliKategori, sadeceMevcutStok: true)
            ?? StokIslemServisi.StokBulMalzemeAdi(malzemeAdi, sadeceMevcutStok: true);

        if (stok is null)
        {
            _seciliStok = null;
            TxtMevcut.Clear();
            CmbBirim.Items.Clear();
            return;
        }

        _seciliStok = stok;
        TxtMevcut.Text = $"{stok.MevcutMiktar:N2} {stok.Birim}";

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

        var kategori = SeciliKategori?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(kategori))
        {
            MessageBox.Show("Kategori seçin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        stok = _seciliStok
            ?? StokIslemServisi.StokBulMalzemeAdi(malzeme, kategori, sadeceMevcutStok: true)
            ?? StokIslemServisi.StokBulMalzemeAdi(malzeme, sadeceMevcutStok: true)!;

        if (stok is null)
        {
            MessageBox.Show("Seçilen malzeme için yeterli stok bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

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
