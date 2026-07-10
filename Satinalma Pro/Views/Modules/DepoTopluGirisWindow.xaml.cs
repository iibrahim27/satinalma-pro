using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using MalzemeAdiOneriYardimcisi = SatinalmaPro.Shared.Helpers.MalzemeAdiOneriYardimcisi;

namespace SatinalmaPro.Views.Modules;

public partial class DepoTopluGirisWindow : Window
{
    private readonly ObservableCollection<StokIslemSatirKaydi> _satirlar = [];
    private List<string> _tumKategoriler = [];
    private bool _kategoriIcGuncelleme;

    public DepoTopluGirisWindow()
    {
        InitializeComponent();
        SatirGrid.ItemsSource = _satirlar;

        TxtTarih.Text = DateTime.Now.ToString("dd.MM.yyyy");
        KategoriListesiniYenile();
        MalzemeBirimDeposu.ComboDoldur(CmbBirim);

        MalzemeGiris.OneriKaynaginiAyarla(MalzemeOner);
        MalzemeGiris.MetinOnaylandi += (_, metin) => MalzemedenBilgiDoldur(metin);
    }

    private IEnumerable<string> MalzemeOner(string? arama)
    {
        ModulVeriDeposu.Yukle();
        var kategori = (CmbKategori.Text ?? "").Trim();
        var kaynak = ModulVeriDeposu.AlinanMalzemeler.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(kategori))
        {
            kaynak = kaynak.Where(k =>
                !string.IsNullOrWhiteSpace(k.Kategori) &&
                k.Kategori.Equals(kategori, StringComparison.OrdinalIgnoreCase));
        }

        var adlar = kaynak
            .Select(k => k.MalzemeHizmet?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!);

        if (!adlar.Any())
            return MalzemeAdiOneriServisi.AlinanMalzemelerdenAra(arama);

        return MalzemeAdiOneriYardimcisi.Filtrele(
            adlar.Distinct(StringComparer.CurrentCultureIgnoreCase), arama);
    }

    private void MalzemedenBilgiDoldur(string malzemeAdi)
    {
        var kayit = ModulVeriDeposu.AlinanMalzemeler.FirstOrDefault(k =>
            k.MalzemeHizmet.Equals(malzemeAdi.Trim(), StringComparison.OrdinalIgnoreCase));
        if (kayit is null)
            return;

        if (!string.IsNullOrWhiteSpace(kayit.Kategori))
        {
            _kategoriIcGuncelleme = true;
            CmbKategori.Text = kayit.Kategori.Trim();
            _kategoriIcGuncelleme = false;
        }

        if (!string.IsNullOrWhiteSpace(kayit.Birim))
            MalzemeBirimDeposu.ComboDoldur(CmbBirim, kayit.Birim);
    }

    private void KategoriListesiniYenile()
    {
        _tumKategoriler = MalzemeKategoriDeposu.TumListe().ToList();
        KategoriListesiniGoster(_tumKategoriler);
    }

    private void KategoriListesiniGoster(IEnumerable<string> liste)
    {
        var secili = CmbKategori.Text;
        CmbKategori.Items.Clear();
        foreach (var kategori in liste)
            CmbKategori.Items.Add(kategori);

        if (!string.IsNullOrWhiteSpace(secili))
            CmbKategori.Text = secili;
    }

    private void CmbKategori_Loaded(object sender, RoutedEventArgs e)
    {
        if (CmbKategori.Template.FindName("PART_EditableTextBox", CmbKategori) is not TextBox kutu)
            return;

        kutu.TextChanged -= KategoriMetniDegisti;
        kutu.TextChanged += KategoriMetniDegisti;
    }

    private void KategoriMetniDegisti(object sender, TextChangedEventArgs e)
    {
        if (_kategoriIcGuncelleme)
            return;

        var arama = ((TextBox)sender).Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(arama))
        {
            KategoriListesiniGoster(_tumKategoriler);
            return;
        }

        var filtre = MalzemeAdiOneriYardimcisi.Filtrele(_tumKategoriler, arama).ToList();
        KategoriListesiniGoster(filtre);
        CmbKategori.IsDropDownOpen = filtre.Count > 0;
    }

    private bool SatirFormuDogrula(out StokIslemSatirKaydi satir)
    {
        satir = new StokIslemSatirKaydi();

        var kategori = (CmbKategori.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(kategori))
        {
            MessageBox.Show("Kategori girin veya seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var malzeme = (MalzemeGiris.Metin ?? "").Trim();
        if (string.IsNullOrWhiteSpace(malzeme))
        {
            MessageBox.Show("Malzeme adı girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!double.TryParse(TxtMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar) || miktar <= 0)
        {
            MessageBox.Show("Geçerli bir miktar girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var birim = (CmbBirim.Text ?? CmbBirim.SelectedItem?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(birim))
        {
            MessageBox.Show("Birim girin veya seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var depo = TxtDepoSaha.Text.Trim();
        if (string.IsNullOrWhiteSpace(depo))
        {
            MessageBox.Show("Depo / saha girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        satir.Kategori = kategori;
        satir.Malzeme = malzeme;
        satir.Miktar = miktar;
        satir.Birim = birim;
        satir.DepoSaha = depo;
        return true;
    }

    private void SatirFormunuTemizle()
    {
        MalzemeGiris.MetniTemizle();
        TxtMiktar.Clear();
        if (CmbBirim.Items.Count > 0)
            CmbBirim.SelectedIndex = 0;
        else
            CmbBirim.Text = "";
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

    private void DepoyuTamamla_Click(object sender, RoutedEventArgs e)
    {
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

        if (_satirlar.Count == 0)
        {
            if (!SatirFormuDogrula(out var satir))
                return;
            _satirlar.Add(satir);
            SatirFormunuTemizle();
        }

        if (_satirlar.Count == 0)
        {
            MessageBox.Show("En az bir satır ekleyin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var depo = TxtDepoSaha.Text.Trim();
        if (string.IsNullOrWhiteSpace(depo))
        {
            MessageBox.Show("Depo / saha girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var onay = MessageBox.Show(
            $"{_satirlar.Count} ürün {depo} deposuna stok olarak eklenecek.\nDevam etmek istiyor musunuz?",
            "Depoyu Tamamla",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        var tarih = TxtTarih.Text.Trim();
        if (string.IsNullOrWhiteSpace(tarih))
            tarih = DateTime.Now.ToString("dd.MM.yyyy");

        var belgeNo = StokBelgeNoUretici.SonrakiGirisBelgeNo();
        var islemYapan = KullaniciYetkileri.AktifKullaniciAdi() ?? "Admin";

        try
        {
            ModulVeriDeposu.BeginBatch();
            foreach (var satir in _satirlar)
            {
                MalzemeKategoriDeposu.Ekle(satir.Kategori);
                MalzemeBirimDeposu.Ekle(satir.Birim);

                StokIslemServisi.GirisYap(
                    tarih,
                    satir.Malzeme,
                    satir.Kategori,
                    satir.Birim,
                    satir.Miktar,
                    depo,
                    0,
                    belgeNo,
                    islemYapan,
                    "Depo stok girişi");
            }

            ModulVeriDeposu.EndBatch();
            MalzemeAdiOneriServisi.OnbellekSifirla();

            MessageBox.Show(
                $"{_satirlar.Count} ürün depo stokuna eklendi.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

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
