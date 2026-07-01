using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using Microsoft.Win32;

namespace SatinalmaPro.Views.Modules;

public partial class AracFiloView : UserControl, IModulKlavyeKisayollari
{
    private readonly ICollectionView _gorunum;
    private readonly FiltreZamanlayici _filtreZamanlayici;
    private readonly ModulSayfalamaYoneticisi<FiloAracKaydi> _sayfalama = new();
    private bool _uyariGosterildi;

    public ObservableCollection<FiloAracKaydi> Araclar => ModulVeriDeposu.FiloAraclari;

    public AracFiloView()
    {
        InitializeComponent();
        DataContext = this;
        _filtreZamanlayici = new FiltreZamanlayici(FiltreYenile);

        _gorunum = CollectionViewSource.GetDefaultView(Araclar);
        _gorunum.Filter = KayitFiltresi;
        FiloGrid.ItemsSource = _sayfalama.SayfaKayitlari;
        ModulSayfalamaYardimcisi.CubukBagla(_sayfalama, SayfalamaBar);

        CmbAracTipi.SelectedIndex = 0;
        CmbSahiplik.SelectedIndex = 0;
        KisayolYenile();
    }

    private void AracFiloView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_uyariGosterildi)
            return;

        _uyariGosterildi = true;
        var yaklasan = FiloHesaplayici.MuayenesiYaklasanAraclar(Araclar);
        if (yaklasan.Count == 0)
            return;

        var pencere = new FiloMuayeneUyariWindow(yaklasan) { Owner = Window.GetWindow(this) };
        pencere.ShowDialog();
    }

    private void YeniArac_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new AracFiloDetayWindow { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        VeriGuncellendi();
    }

    private void ExcelYukle_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Excel Dosyası Seç",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var yeniAraclar = FiloExcelService.DosyadanOku(dialog.FileName);
            if (yeniAraclar.Count == 0)
            {
                MessageBox.Show("Dosyada aktarılacak araç bulunamadı.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ModulVeriDeposu.BeginBatch();
            try
            {
                foreach (var arac in yeniAraclar)
                {
                    if (Araclar.Any(a => a.Plaka.Equals(arac.Plaka, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    Araclar.Add(arac);
                }
            }
            finally
            {
                ModulVeriDeposu.EndBatch();
            }

            VeriGuncellendi();
            MessageBox.Show($"{yeniAraclar.Count} araç içe aktarıldı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Excel okunamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SablonIndir_Click(object sender, RoutedEventArgs e) =>
        FiloExcelService.SablonKaydet();

    private void PdfYazdir_Click(object sender, RoutedEventArgs e) =>
        FiloPdfService.Yazdir(GorunenAraclar(), "Araç Filo Park Raporu");

    private void DisaAktar_Click(object sender, RoutedEventArgs e) =>
        FiloExcelService.ListeyiKaydet(GorunenAraclar(), "AracFilo.xlsx");

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();

    private void FiltreDegisti(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox)
            _filtreZamanlayici.Tetikle();
        else
            FiltreYenile();
    }

    private void FiltreleriTemizle_Click(object sender, RoutedEventArgs e)
    {
        TxtArama.Text = "";
        CmbAracTipi.SelectedIndex = 0;
        CmbSahiplik.SelectedIndex = 0;
        FiltreYenile();
    }

    private void FiltreYenile()
    {
        SayfalamayiYenile(ilkSayfayaDon: true);
        OzetGuncelle();
        AracButonlariniGuncelle();
    }

    private void SayfalamayiYenile(bool ilkSayfayaDon = false) =>
        ModulSayfalamaYardimcisi.FiltreSonrasi(
            _sayfalama, _gorunum, k => ModulSayfalamaYardimcisi.TarihSira(k.KayitTarihi), SayfalamaBar, ilkSayfayaDon);

    public void KisayolYenile()
    {
        FiloHesaplayici.Hesapla(Araclar, ModulVeriDeposu.FiloGiderleri, ModulVeriDeposu.FiloZimmetleri);
        FiltreYenile();
    }

    private void VeriGuncellendi()
    {
        FiloHesaplayici.Hesapla(Araclar, ModulVeriDeposu.FiloGiderleri, ModulVeriDeposu.FiloZimmetleri);
        FiltreYenile();
    }

    private bool KayitFiltresi(object item)
    {
        if (item is not FiloAracKaydi arac)
            return false;

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
        {
            var metin = string.Join(" ",
                arac.Plaka, arac.SasiNo, arac.AracTipi, arac.MarkaModel, arac.SahiplikTipi, arac.Sirket,
                arac.Saha, arac.Durum, arac.Aciklama, arac.ZimmetMetin, arac.MuayeneUyariMetin, arac.SigortaUyariMetin);

            if (!metin.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var tip = ComboDegeri(CmbAracTipi);
        if (!string.IsNullOrEmpty(tip) && tip != "Tümü" &&
            !arac.AracTipi.Equals(tip, StringComparison.OrdinalIgnoreCase))
            return false;

        var sahiplik = ComboDegeri(CmbSahiplik);
        if (!string.IsNullOrEmpty(sahiplik) && sahiplik != "Tümü" &&
            !arac.SahiplikTipi.Equals(sahiplik, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string? ComboDegeri(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private void FiloGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is not FiloAracKaydi arac)
            return;

        var bitis = FiloHesaplayici.TarihCoz(arac.MuayeneBitisTarihi);
        if (bitis == DateTime.MinValue)
            return;

        var kalan = (bitis - DateTime.Today).Days;
        if (kalan <= FiloHesaplayici.MuayeneUyariGun)
            e.Row.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199));
    }

    private void FiloGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        AracButonlariniGuncelle();

    private void FiloGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        AracDetay_Click(sender, e);

    private void AracDetay_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliArac() is not { } arac)
            return;

        var pencere = new AracFiloDetayWindow(arac, sadeceGoruntule: true) { Owner = Window.GetWindow(this) };
        pencere.ShowDialog();
    }

    private void AracGider_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliArac() is not { } arac)
            return;

        var pencere = new AracFiloGiderWindow(arac) { Owner = Window.GetWindow(this) };
        pencere.ShowDialog();
        VeriGuncellendi();
    }

    private void AracDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliArac() is not { } arac)
            return;

        var pencere = new AracFiloDetayWindow(arac) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        VeriGuncellendi();
    }

    private void AracSil_Click(object sender, RoutedEventArgs e) => MenuSil_Click(sender, e);

    private void AracSevk_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliArac() is not { } arac)
            return;

        if (!arac.Durum.Equals("Aktif", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Yalnızca aktif araçlar sevk edilebilir.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var pencere = new FiloSevkWindow(arac) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        VeriGuncellendi();
    }

    private void AracZimmet_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliArac() is not { } arac)
            return;

        var pencere = new FiloZimmetWindow(arac) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        FiltreYenile();
    }

    private void ZimmetIptal_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliArac() is not { } arac)
            return;

        var aktif = ModulVeriDeposu.FiloZimmetleri
            .Where(z => z.Aktif && z.Plaka.Equals(arac.Plaka, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (aktif.Count == 0)
        {
            MessageBox.Show("Bu araç için aktif zimmet kaydı bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var pencere = new FiloZimmetIptalWindow(arac, aktif) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        VeriGuncellendi();
    }

    private void ZimmetPdf_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliArac() is not { } arac)
            return;

        var kayitlar = ModulVeriDeposu.FiloZimmetleri
            .Where(z => z.Plaka.Equals(arac.Plaka, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (kayitlar.Count == 0)
        {
            MessageBox.Show("Bu araç için zimmet kaydı bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var pencere = new FiloZimmetGecmisiWindow(arac) { Owner = Window.GetWindow(this) };
        pencere.ShowDialog();
    }

    private void MenuDetay_Click(object sender, RoutedEventArgs e) => AracDetay_Click(sender, e);

    private void MenuSil_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliArac() is not { } arac)
            return;

        if (MessageBox.Show($"{arac.Plaka} plakalı aracı silmek istiyor musunuz?\nAraça ait gider ve zimmet kayıtları da silinir.",
                "Araç Sil", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        foreach (var gider in ModulVeriDeposu.FiloGiderleri
                     .Where(g => g.Plaka.Equals(arac.Plaka, StringComparison.OrdinalIgnoreCase)).ToList())
            ModulVeriDeposu.FiloGiderleri.Remove(gider);

        foreach (var zimmet in ModulVeriDeposu.FiloZimmetleri
                     .Where(z => z.Plaka.Equals(arac.Plaka, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            FiloZimmetPdfDeposu.Sil(zimmet.PdfDosyaYolu);
            ModulVeriDeposu.FiloZimmetleri.Remove(zimmet);
        }

        Araclar.Remove(arac);
        VeriGuncellendi();
    }

    private FiloAracKaydi? SeciliArac() => FiloGrid.SelectedItem as FiloAracKaydi;

    private void AracButonlariniGuncelle()
    {
        var secili = SeciliArac() is not null;
        BtnAracDetay.IsEnabled = secili;
        BtnAracGider.IsEnabled = secili;
        BtnAracDuzenle.IsEnabled = secili;
        BtnAracSil.IsEnabled = secili;
        BtnAracSevk.IsEnabled = secili;
        BtnAracZimmet.IsEnabled = secili;
        BtnZimmetIptal.IsEnabled = secili;
        BtnZimmetPdf.IsEnabled = secili;
    }

    private List<FiloAracKaydi> GorunenAraclar() =>
        _gorunum.Cast<FiloAracKaydi>().ToList();

    private void OzetGuncelle()
    {
        var gorunen = GorunenAraclar();
        TxtToplamArac.Text = Araclar.Count.ToString();
        TxtToplamGider.Text = $"₺{FiloHesaplayici.ToplamGider(ModulVeriDeposu.FiloGiderleri):N0}";
        TxtYaklasanMuayene.Text = FiloHesaplayici.MuayenesiYaklasanSayisi(Araclar).ToString();
        TxtFiltrelenen.Text = gorunen.Count == Araclar.Count
            ? "Tümü"
            : $"{gorunen.Count} / {Araclar.Count} araç";
    }
}
