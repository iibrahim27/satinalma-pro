using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class StokYonetimiView : UserControl, IModulKlavyeKisayollari
{
    private readonly ICollectionView _stokGorunum;
    private readonly ICollectionView _hareketGorunum;
    private readonly ICollectionView _sayimGorunum;
    private readonly ModulSayfalamaYoneticisi<StokKaydi> _stokSayfalama = new();
    private readonly ModulSayfalamaYoneticisi<StokHareketKaydi> _hareketSayfalama = new();
    private readonly ModulSayfalamaYoneticisi<StokHareketKaydi> _sayimSayfalama = new();
    private readonly FiltreZamanlayici _filtreZamanlayici;
    private readonly FiltreZamanlayici _hareketFiltreZamanlayici;

    public ObservableCollection<StokKaydi> Kayitlar => UygulamaVeriDeposu.Stok;
    public ObservableCollection<StokHareketKaydi> Hareketler => UygulamaVeriDeposu.StokHareketleri;

    public StokYonetimiView()
    {
        InitializeComponent();
        DataContext = this;

        _filtreZamanlayici = new FiltreZamanlayici(StokFiltreYenile);
        _hareketFiltreZamanlayici = new FiltreZamanlayici(HareketFiltreYenile);

        UygulamaVeriDeposu.OrnekVeriyiYukle();

        _stokGorunum = CollectionViewSource.GetDefaultView(Kayitlar);
        _stokGorunum.Filter = StokFiltresi;
        StokGrid.ItemsSource = _stokSayfalama.SayfaKayitlari;
        ModulSayfalamaYardimcisi.CubukBagla(_stokSayfalama, StokSayfalamaBar);

        _hareketGorunum = CollectionViewSource.GetDefaultView(Hareketler);
        _hareketGorunum.Filter = HareketFiltresi;
        HareketGrid.ItemsSource = _hareketSayfalama.SayfaKayitlari;
        ModulSayfalamaYardimcisi.CubukBagla(_hareketSayfalama, HareketSayfalamaBar);

        _sayimGorunum = new ListCollectionView(Hareketler);
        _sayimGorunum.Filter = o => o is StokHareketKaydi h && h.HareketTipi == StokHareketTipleri.Sayim;
        SayimGrid.ItemsSource = _sayimSayfalama.SayfaKayitlari;
        ModulSayfalamaYardimcisi.CubukBagla(_sayimSayfalama, SayimSayfalamaBar);

        CmbDurum.Items.Add(new ComboBoxItem { Content = "Tümü" });
        CmbDurum.Items.Add(new ComboBoxItem { Content = "Normal" });
        CmbDurum.Items.Add(new ComboBoxItem { Content = "Kritik" });
        CmbDurum.Items.Add(new ComboBoxItem { Content = "Tükendi" });
        CmbDurum.SelectedIndex = 0;

        CmbHareketTip.Items.Add(new ComboBoxItem { Content = "Tümü" });
        foreach (var tip in StokHareketTipleri.Tum)
            CmbHareketTip.Items.Add(new ComboBoxItem { Content = tip });
        CmbHareketTip.SelectedIndex = 0;

        TxtSayimTarih.Text = DateTime.Now.ToString("dd.MM.yyyy");

        Loaded += (_, _) =>
        {
            SekmeYetkileriniUygula();
            StokAksiyonlariniAyarla();
            KullaniciYetkileri.ModulErisiminiUygula(this, "Stok Yönetimi");
            IlkGorunurSekmeyeGec();
            TumunuYenile();
        };
    }

    #region Navigasyon

    private void SekmeYetkileriniUygula()
    {
        BtnNavDurum.Visibility = SekmeGorunur("Stok Durumu");
        BtnNavHareket.Visibility = SekmeGorunur("Stok Hareketleri");
        BtnNavSayim.Visibility = SekmeGorunur("Stok Sayım");
    }

    private void StokAksiyonlariniAyarla()
    {
        BtnStokGiris.Visibility = SekmeGorunur("Stok Girişi");
        BtnStokCikis.Visibility = SekmeGorunur("Stok Çıkışı");
    }

    private static Visibility SekmeGorunur(string sekme) =>
        KullaniciYetkileri.SekmeGorebilir("Stok Yönetimi", sekme) ? Visibility.Visible : Visibility.Collapsed;

    private void NavAktif(Button aktif)
    {
        BtnNavDurum.Style = (Style)FindResource(aktif == BtnNavDurum ? "StokNavActiveStyle" : "SatinalmaNavPillStyle");
        BtnNavHareket.Style = (Style)FindResource(aktif == BtnNavHareket ? "StokNavActiveStyle" : "SatinalmaNavPillStyle");
        BtnNavSayim.Style = (Style)FindResource(aktif == BtnNavSayim ? "StokNavActiveStyle" : "SatinalmaNavPillStyle");
    }

    private void NavDurum_Click(object sender, RoutedEventArgs e)
    {
        NavAktif(BtnNavDurum);
        PanelDurum.Visibility = Visibility.Visible;
        PanelHareket.Visibility = Visibility.Collapsed;
        PanelSayim.Visibility = Visibility.Collapsed;
    }

    private void NavHareket_Click(object sender, RoutedEventArgs e)
    {
        NavAktif(BtnNavHareket);
        PanelDurum.Visibility = Visibility.Collapsed;
        PanelHareket.Visibility = Visibility.Visible;
        PanelSayim.Visibility = Visibility.Collapsed;
    }

    private void NavSayim_Click(object sender, RoutedEventArgs e)
    {
        NavAktif(BtnNavSayim);
        PanelDurum.Visibility = Visibility.Collapsed;
        PanelHareket.Visibility = Visibility.Collapsed;
        PanelSayim.Visibility = Visibility.Visible;
        StokSecimCombolariniGuncelle();
    }

    private void IlkGorunurSekmeyeGec()
    {
        if (BtnNavDurum.Visibility == Visibility.Visible)
            NavDurum_Click(BtnNavDurum, new RoutedEventArgs());
        else if (BtnNavHareket.Visibility == Visibility.Visible)
            NavHareket_Click(BtnNavHareket, new RoutedEventArgs());
        else if (BtnNavSayim.Visibility == Visibility.Visible)
            NavSayim_Click(BtnNavSayim, new RoutedEventArgs());
    }

    private void StokGiris_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.SekmeGorebilir("Stok Yönetimi", "Stok Girişi"))
        {
            MessageBox.Show("Stok girişi yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pencere = new StokGirisWindow { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() == true)
            TumunuYenile();
    }

    private void StokCikis_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.SekmeGorebilir("Stok Yönetimi", "Stok Çıkışı"))
        {
            MessageBox.Show("Stok çıkışı yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pencere = new StokCikisWindow { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() == true)
            TumunuYenile();
    }

    #endregion

    #region Stok Durumu

    private void YeniDurumKayit_Click(object sender, RoutedEventArgs e)
    {
        var yeni = new StokKaydi
        {
            Birim = "Adet",
            SonGuncelleme = DateTime.Now.ToString("dd.MM.yyyy")
        };

        var pencere = new StokDuzenleWindow(yeni) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        Kayitlar.Add(yeni);
        TumunuYenile();
    }

    private void PdfIndir_Click(object sender, RoutedEventArgs e) =>
        StokPdfOlusturucu.Indir(GorunenStoklar(), FiltreOzetiMetni());

    private void DisaAktar_Click(object sender, RoutedEventArgs e) =>
        StokExcelService.ListeyiKaydet(GorunenStoklar(), "StokDurumu.xlsx");

    private void StokFiltreYenile()
    {
        ModulSayfalamaYardimcisi.FiltreSonrasi(
            _stokSayfalama, _stokGorunum, k => ModulSayfalamaYardimcisi.TarihSira(k.SonGuncelleme), StokSayfalamaBar);
        OzetGuncelle();
    }

    private void FiltreDegisti(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox)
            _filtreZamanlayici.Tetikle();
        else
            StokFiltreYenile();
    }

    private bool StokFiltresi(object item)
    {
        if (item is not StokKaydi kayit)
            return false;

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
        {
            var metin = string.Join(" ", kayit.MalzemeAdi, kayit.Kategori, kayit.DepoSaha, kayit.Aciklama, kayit.DurumMetin);
            if (!metin.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var kategori = ComboDegeri(CmbKategori);
        if (!string.IsNullOrEmpty(kategori) && kategori != "Tümü" &&
            !kayit.Kategori.Equals(kategori, StringComparison.OrdinalIgnoreCase))
            return false;

        var depo = ComboDegeri(CmbDepoSaha);
        if (!string.IsNullOrEmpty(depo) && depo != "Tümü" &&
            !kayit.DepoSaha.Equals(depo, StringComparison.OrdinalIgnoreCase))
            return false;

        var durum = ComboDegeri(CmbDurum);
        if (!string.IsNullOrEmpty(durum) && durum != "Tümü" &&
            !kayit.DurumMetin.Equals(durum, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private void StokGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is not StokKaydi kayit)
            return;

        e.Row.Background = kayit.DurumMetin switch
        {
            "Tükendi" => new SolidColorBrush(Color.FromRgb(254, 226, 226)),
            "Kritik" => new SolidColorBrush(Color.FromRgb(254, 243, 199)),
            _ => KategoriRenkleri.GetFirca(kayit.Kategori)
        };
    }

    private void StokGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var secili = StokGrid.SelectedItem is not null;
        BtnKayitDuzenle.IsEnabled = secili;
        BtnKayitSil.IsEnabled = secili;
    }

    private void StokGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (StokGrid.SelectedItem is StokKaydi kayit)
            KaydiDuzenle(kayit);
    }

    private void KayitDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (StokGrid.SelectedItem is StokKaydi kayit)
            KaydiDuzenle(kayit);
    }

    private void KayitSil_Click(object sender, RoutedEventArgs e)
    {
        if (StokGrid.SelectedItem is not StokKaydi kayit)
            return;

        if (MessageBox.Show($"{kayit.MalzemeAdi} stok kaydını silmek istiyor musunuz?", "Kayıt Sil",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Kayitlar.Remove(kayit);
        TumunuYenile();
    }

    private void KaydiDuzenle(StokKaydi kayit)
    {
        var pencere = new StokDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        TumunuYenile();
    }

    #endregion

    #region Hareketler

    private void HareketFiltreDegisti(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox)
            _hareketFiltreZamanlayici.Tetikle();
        else
            HareketFiltreYenile();
    }

    private void HareketFiltreTemizle_Click(object sender, RoutedEventArgs e)
    {
        TxtHareketAra.Text = "";
        CmbHareketTip.SelectedIndex = 0;
        CmbHareketDepo.SelectedIndex = 0;
        HareketFiltreYenile();
    }

    private void HareketFiltreYenile()
    {
        ModulSayfalamaYardimcisi.FiltreSonrasi(
            _hareketSayfalama, _hareketGorunum, h => ModulSayfalamaYardimcisi.TarihSira(h.Tarih), HareketSayfalamaBar);
        ModulSayfalamaYardimcisi.FiltreSonrasi(
            _sayimSayfalama, _sayimGorunum, h => ModulSayfalamaYardimcisi.TarihSira(h.Tarih), SayimSayfalamaBar);
    }

    private bool HareketFiltresi(object item)
    {
        if (item is not StokHareketKaydi h)
            return false;

        var arama = TxtHareketAra.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
        {
            var metin = string.Join(" ", h.MalzemeAdi, h.BelgeNo, h.IslemYapan, h.Aciklama, h.HareketTipi, h.DepoSaha);
            if (!metin.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var tip = ComboDegeri(CmbHareketTip);
        if (!string.IsNullOrEmpty(tip) && tip != "Tümü" &&
            !h.HareketTipi.Equals(tip, StringComparison.OrdinalIgnoreCase))
            return false;

        var depo = ComboDegeri(CmbHareketDepo);
        if (!string.IsNullOrEmpty(depo) && depo != "Tümü" &&
            !h.DepoSaha.Equals(depo, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private void HareketGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        HareketDuzenle_Click(sender, e);

    private void HareketGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject kaynak)
            return;

        var satir = ItemsControl.ContainerFromElement(HareketGrid, kaynak) as DataGridRow;
        if (satir is null)
            return;

        satir.IsSelected = true;
        HareketGrid.SelectedItem = satir.Item;
    }

    private void HareketDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (HareketGrid.SelectedItem is not StokHareketKaydi hareket)
            return;

        var pencere = new StokHareketDuzenleWindow(hareket) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        TumunuYenile();
    }

    private void HareketSil_Click(object sender, RoutedEventArgs e)
    {
        if (HareketGrid.SelectedItem is not StokHareketKaydi hareket)
            return;

        if (MessageBox.Show("Seçili hareket silinecek ve stok miktarı geri alınacak. Devam edilsin mi?",
                "Hareket Sil", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        StokIslemServisi.HareketSil(hareket);
        TumunuYenile();
    }

    #endregion

    #region Sayım

    private void SayimStokSecildi(object sender, SelectionChangedEventArgs e)
    {
        if (CmbSayimStok.SelectedItem is StokSecimOgesi oge)
            TxtSayimMevcut.Text = $"{oge.Kayit.MevcutMiktar:N2} {oge.Kayit.Birim}";
        else
            TxtSayimMevcut.Clear();
        SayimFarkHesapla(sender, e);
    }

    private void SayimFarkHesapla(object sender, RoutedEventArgs e)
    {
        if (CmbSayimStok.SelectedItem is not StokSecimOgesi oge ||
            !double.TryParse(TxtSayimMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var sayim))
        {
            TxtSayimFark.Clear();
            return;
        }

        var fark = sayim - oge.Kayit.MevcutMiktar;
        TxtSayimFark.Text = $"{fark:+#.##;-#.##;0} {oge.Kayit.Birim}";
    }

    private void SayimKaydet_Click(object sender, RoutedEventArgs e)
    {
        if (CmbSayimStok.SelectedItem is not StokSecimOgesi oge)
        {
            MessageBox.Show("Sayım yapılacak stok kalemini seçin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(TxtSayimMiktar.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var sayim) || sayim < 0)
        {
            MessageBox.Show("Geçerli bir sayım miktarı girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            StokIslemServisi.SayimYap(
                TxtSayimTarih.Text.Trim(),
                oge.Kayit,
                sayim,
                TxtSayimIslemYapan.Text.Trim(),
                TxtSayimAciklama.Text.Trim());

            TxtSayimMiktar.Clear();
            TxtSayimFark.Clear();
            TxtSayimAciklama.Clear();
            TumunuYenile();
            MessageBox.Show("Stok sayımı kaydedildi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    public void KisayolYenile() => TumunuYenile();

    private void Yenile_Click(object sender, RoutedEventArgs e) => TumunuYenile();

    private void TumunuYenile()
    {
        foreach (var kayit in Kayitlar)
            kayit.ToplamDegerHesapla();

        FiltreCombolariniGuncelle();
        HareketFiltreCombolariniGuncelle();
        StokSecimCombolariniGuncelle();

        _stokGorunum.Refresh();
        _hareketGorunum.Refresh();
        _sayimGorunum.Refresh();
        OzetGuncelle();
    }

    private void StokSecimCombolariniGuncelle()
    {
        var secili = CmbSayimStok.SelectedItem as StokSecimOgesi;
        var ogeler = Kayitlar.Select(s => new StokSecimOgesi(s)).ToList();
        CmbSayimStok.ItemsSource = ogeler;
        if (secili is not null)
            CmbSayimStok.SelectedItem = ogeler.FirstOrDefault(o => o.Kayit == secili.Kayit);
    }

    private void FiltreCombolariniGuncelle()
    {
        var seciliKategori = ComboDegeri(CmbKategori);
        var seciliDepo = ComboDegeri(CmbDepoSaha);

        CmbKategori.Items.Clear();
        CmbDepoSaha.Items.Clear();
        CmbKategori.Items.Add(new ComboBoxItem { Content = "Tümü" });
        CmbDepoSaha.Items.Add(new ComboBoxItem { Content = "Tümü" });

        foreach (var k in MalzemeKategoriDeposu.TumListe())
            CmbKategori.Items.Add(new ComboBoxItem { Content = k });

        foreach (var d in Kayitlar.Select(k => k.DepoSaha).Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(s => s))
            CmbDepoSaha.Items.Add(new ComboBoxItem { Content = d });

        CmbKategori.SelectedIndex = IndexBul(CmbKategori, seciliKategori);
        CmbDepoSaha.SelectedIndex = IndexBul(CmbDepoSaha, seciliDepo);
    }

    private void HareketFiltreCombolariniGuncelle()
    {
        var seciliDepo = ComboDegeri(CmbHareketDepo);
        CmbHareketDepo.Items.Clear();
        CmbHareketDepo.Items.Add(new ComboBoxItem { Content = "Tümü" });
        foreach (var d in Hareketler.Select(h => h.DepoSaha).Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(s => s))
            CmbHareketDepo.Items.Add(new ComboBoxItem { Content = d });
        CmbHareketDepo.SelectedIndex = IndexBul(CmbHareketDepo, seciliDepo);
    }

    private static string? ComboDegeri(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private static int IndexBul(ComboBox combo, string? deger)
    {
        if (string.IsNullOrEmpty(deger))
            return 0;
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if ((combo.Items[i] as ComboBoxItem)?.Content?.ToString() == deger)
                return i;
        }
        return 0;
    }

    private List<StokKaydi> GorunenStoklar() => _stokGorunum.Cast<StokKaydi>().ToList();

    private string FiltreOzetiMetni()
    {
        var parcalar = new List<string>();
        var kategori = ComboDegeri(CmbKategori);
        if (!string.IsNullOrEmpty(kategori) && kategori != "Tümü") parcalar.Add($"Kategori: {kategori}");
        var depo = ComboDegeri(CmbDepoSaha);
        if (!string.IsNullOrEmpty(depo) && depo != "Tümü") parcalar.Add($"Depo: {depo}");
        var durum = ComboDegeri(CmbDurum);
        if (!string.IsNullOrEmpty(durum) && durum != "Tümü") parcalar.Add($"Durum: {durum}");
        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama)) parcalar.Add($"Arama: {arama}");
        return parcalar.Count == 0 ? "Tüm kayıtlar" : string.Join(" · ", parcalar);
    }

    private void OzetGuncelle()
    {
        var gorunen = GorunenStoklar();
        TxtToplamKalem.Text = Kayitlar.Count.ToString();
        TxtToplamDeger.Text = $"₺{gorunen.Sum(k => k.ToplamDeger):N0}";
        TxtKritikStok.Text = Kayitlar.Count(k => k.DurumMetin == "Kritik").ToString();
        TxtTukenen.Text = Kayitlar.Count(k => k.DurumMetin == "Tükendi").ToString();
    }
}
