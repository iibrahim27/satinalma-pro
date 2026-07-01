using SatinalmaPro.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class AracFiloDetayWindow : Window
{
    private readonly FiloAracKaydi _arac;
    private readonly bool _yeniKayit;
    private readonly bool _sadeceGoruntule;
    private string? _ruhsatYolu;
    private readonly List<string> _gorselYollari = [];

    private const string GorselFiltre = "Görsel Dosyaları|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.gif;*.pdf|Tüm Dosyalar|*.*";

    public AracFiloDetayWindow(FiloAracKaydi? arac = null, bool sadeceGoruntule = false)
    {
        InitializeComponent();
        _sadeceGoruntule = sadeceGoruntule;
        _yeniKayit = arac is null;
        _arac = arac ?? new FiloAracKaydi
        {
            KayitTarihi = DateTime.Now.ToString("dd.MM.yyyy"),
            Durum = "Aktif",
            AracTipi = "Binek",
            SahiplikTipi = "Bizim"
        };

        if (!_yeniKayit)
        {
            _ruhsatYolu = _arac.RuhsatDosyaYolu;
            _gorselYollari.AddRange(_arac.GorselDosyaYollari);
        }

        FormuDoldur();
        GorunumModunuAyarla();
    }

    private void FormuDoldur()
    {
        TxtBaslik.Text = _yeniKayit ? "Yeni Araç" : _sadeceGoruntule
            ? $"{_arac.Plaka} — Araç Detayları"
            : $"{_arac.Plaka} — Araç Düzenle";
        TxtPlaka.Text = _arac.Plaka;
        TxtSasiNo.Text = _arac.SasiNo;
        CmbAracTipi.Text = string.IsNullOrWhiteSpace(_arac.AracTipi) ? "Binek" : _arac.AracTipi;
        TxtMarkaModel.Text = _arac.MarkaModel;
        TxtModelYili.Text = _arac.ModelYili;
        CmbSahiplik.Text = string.IsNullOrWhiteSpace(_arac.SahiplikTipi) ? "Bizim" : _arac.SahiplikTipi;
        CmbDurum.Text = string.IsNullOrWhiteSpace(_arac.Durum) ? "Aktif" : _arac.Durum;
        TxtSirket.Text = _arac.Sirket;
        TxtSaha.Text = _arac.Saha;
        TxtMuayeneBitis.Text = _arac.MuayeneBitisTarihi;
        TxtSigortaBitis.Text = _arac.SigortaBitisTarihi;
        TxtAciklama.Text = _arac.Aciklama;
        RuhsatOnizlemeGuncelle();
        GorselListesiniGuncelle();
    }

    private void GorunumModunuAyarla()
    {
        if (!_sadeceGoruntule)
            return;

        Title = "Araç Detayları";
        BtnKaydet.Visibility = Visibility.Collapsed;
        BtnIptal.Visibility = Visibility.Collapsed;
        BtnKapat.Visibility = Visibility.Visible;

        foreach (var tb in new[] { TxtPlaka, TxtSasiNo, TxtMarkaModel, TxtModelYili, TxtSirket, TxtSaha,
                     TxtMuayeneBitis, TxtSigortaBitis, TxtAciklama })
        {
            tb.IsReadOnly = true;
            tb.Background = System.Windows.Media.Brushes.WhiteSmoke;
        }

        CmbAracTipi.IsEnabled = false;
        CmbSahiplik.IsEnabled = false;
        CmbDurum.IsEnabled = false;
    }

    private void RuhsatOnizlemeGuncelle()
    {
        TxtRuhsatAd.Text = string.IsNullOrWhiteSpace(_ruhsatYolu)
            ? "Ruhsat yüklenmedi"
            : FiloDosyaDeposu.GorunenAd(_ruhsatYolu);
        FiloGorselYardimcisi.GorselAyarla(ImgRuhsat, _ruhsatYolu);
    }

    private void GorselListesiniGuncelle()
    {
        GorselListesi.ItemsSource = null;
        GorselListesi.ItemsSource = _gorselYollari.Select(FiloDosyaDeposu.GorunenAd).ToList();
    }

    private void RuhsatYukle_Click(object sender, RoutedEventArgs e)
    {
        if (_sadeceGoruntule) return;
        var dialog = new OpenFileDialog { Title = "Ruhsat Seç", Filter = GorselFiltre };
        if (dialog.ShowDialog() != true) return;
        _ruhsatYolu = FiloDosyaDeposu.Kaydet(dialog.FileName, "ruhsat");
        RuhsatOnizlemeGuncelle();
    }

    private void RuhsatKaldir_Click(object sender, RoutedEventArgs e)
    {
        if (_sadeceGoruntule) return;
        _ruhsatYolu = null;
        RuhsatOnizlemeGuncelle();
    }

    private void GorselEkle_Click(object sender, RoutedEventArgs e)
    {
        if (_sadeceGoruntule) return;
        var dialog = new OpenFileDialog { Title = "Görsel Seç", Filter = GorselFiltre, Multiselect = true };
        if (dialog.ShowDialog() != true) return;
        foreach (var dosya in dialog.FileNames)
            _gorselYollari.Add(FiloDosyaDeposu.Kaydet(dosya, "gorsel"));
        GorselListesiniGuncelle();
    }

    private void GorselKaldir_Click(object sender, RoutedEventArgs e)
    {
        if (_sadeceGoruntule || GorselListesi.SelectedIndex < 0) return;
        _gorselYollari.RemoveAt(GorselListesi.SelectedIndex);
        GorselListesiniGuncelle();
    }

    private void GorselListesi_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        var plaka = TxtPlaka.Text.Trim();
        if (string.IsNullOrWhiteSpace(plaka))
        {
            MessageBox.Show("Plaka veya makine kodu zorunludur.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_yeniKayit && ModulVeriDeposu.FiloAraclari.Any(a =>
                a.Plaka.Equals(plaka, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Bu plaka zaten kayıtlı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(TxtMuayeneBitis.Text) && !TarihGecerliMi(TxtMuayeneBitis.Text.Trim()))
        {
            MessageBox.Show("Muayene bitiş tarihi gg.aa.yyyy formatında olmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(TxtSigortaBitis.Text) && !TarihGecerliMi(TxtSigortaBitis.Text.Trim()))
        {
            MessageBox.Show("Sigorta bitiş tarihi gg.aa.yyyy formatında olmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var eskiPlaka = _arac.Plaka;
        _arac.Plaka = plaka;
        _arac.SasiNo = TxtSasiNo.Text.Trim();
        _arac.AracTipi = CmbAracTipi.Text.Trim();
        _arac.MarkaModel = TxtMarkaModel.Text.Trim();
        _arac.ModelYili = TxtModelYili.Text.Trim();
        _arac.SahiplikTipi = CmbSahiplik.Text.Trim();
        _arac.Durum = CmbDurum.Text.Trim();
        _arac.Sirket = TxtSirket.Text.Trim();
        _arac.Saha = TxtSaha.Text.Trim();
        _arac.MuayeneBitisTarihi = TxtMuayeneBitis.Text.Trim();
        _arac.SigortaBitisTarihi = TxtSigortaBitis.Text.Trim();
        _arac.Aciklama = TxtAciklama.Text.Trim();
        _arac.RuhsatDosyaYolu = _ruhsatYolu ?? "";
        _arac.GorselDosyaYollari = [.._gorselYollari];

        if (_yeniKayit)
            ModulVeriDeposu.FiloAraclari.Add(_arac);

        if (!eskiPlaka.Equals(plaka, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var gider in ModulVeriDeposu.FiloGiderleri.Where(g =>
                         g.Plaka.Equals(eskiPlaka, StringComparison.OrdinalIgnoreCase)))
                gider.Plaka = plaka;
            foreach (var zimmet in ModulVeriDeposu.FiloZimmetleri.Where(z =>
                         z.Plaka.Equals(eskiPlaka, StringComparison.OrdinalIgnoreCase)))
                zimmet.Plaka = plaka;
        }

        ModulVeriDeposu.KaydetFilo();
        DialogResult = true;
        Close();
    }

    private static bool TarihGecerliMi(string tarih) =>
        DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private void Iptal_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
