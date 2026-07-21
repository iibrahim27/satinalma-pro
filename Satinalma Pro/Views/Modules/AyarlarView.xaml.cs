using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using MainWindow = SatinalmaPro.MainWindow;
using SatinalmaPro.Views;
using Microsoft.Win32;

namespace SatinalmaPro.Views.Modules;

public partial class AyarlarView : UserControl
{
    private readonly ObservableCollection<VeriKaydiDurumu> _veriDurumlari = [];
    private readonly ObservableCollection<string> _malzemeKategorileri = [];
    private readonly ObservableCollection<string> _malzemeBirimleri = [];
    private readonly Dictionary<string, Button> _navButtons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UIElement> _paneller = new(StringComparer.Ordinal);

    private bool _sartnameYukleniyor;
    private bool _teklifSartnameYukleniyor;
    private bool _genelYukleniyor;
    private bool _filoZimmetYukleniyor;
    private bool _dovizYukleniyor;
    private string _aktifNav = "genel";

    private sealed record NavOge(string Id, string Baslik, string Ikon, string? IzinSekmesi);

    private static readonly NavOge[] NavOgeleri =
    [
        new("genel", "Genel", "\uE713", "Genel"),
        new("firma", "Firma Bilgileri", "\uE821", "Genel"),
        new("satinalma", "Satınalma", "\uE719", "Satınalma"),
        new("stok", "Stok", "\uE7B8", "Malzeme Kategorileri"),
        new("filo", "Araç Filo", "\uE804", "Araç Filo"),
        new("akaryakit", "Akaryakıt", "\uE909", "Genel"),
        new("raporlar", "Raporlar", "\uE9F9", "Genel"),
        new("bildirimler", "Bildirimler", "\uE7ED", "Genel"),
        new("veritabani", "Veritabanı", "\uE968", "Veri Dosyaları"),
        new("yedekleme", "Yedekleme", "\uE8B7", "Yedekleme"),
        new("loglar", "Loglar", "\uE8A1", "Genel"),
        new("guncelleme", "Güncelleme", "\uE898", "Genel")
    ];

    public AyarlarView()
    {
        InitializeComponent();
        PanelleriKaydet();
        NavigasyonuOlustur();
        VeriDurumGrid.ItemsSource = _veriDurumlari;
        KategoriListesi.ItemsSource = _malzemeKategorileri;
        BirimListesi.ItemsSource = _malzemeBirimleri;
        TxtVeriKlasoru.Text = SatinalmaProKlasor.Yol;
        AyarlariYukle();
        VeriDurumlariniYenile();
        KpiKartlariniGuncelle();
        NavigasyonAc("genel");

        Loaded += (_, _) =>
        {
            MenuleriUygula();
            KullaniciYetkileri.ModulErisiminiUygula(this, "Ayarlar");
        };
    }

    private void PanelleriKaydet()
    {
        _paneller["genel"] = PanelGenel;
        _paneller["firma"] = PanelFirma;
        _paneller["satinalma"] = PanelSatinalma;
        _paneller["stok"] = PanelStok;
        _paneller["filo"] = PanelFilo;
        _paneller["akaryakit"] = PanelAkaryakit;
        _paneller["raporlar"] = PanelRaporlar;
        _paneller["bildirimler"] = PanelBildirimler;
        _paneller["veritabani"] = PanelVeritabani;
        _paneller["yedekleme"] = PanelYedekleme;
        _paneller["loglar"] = PanelLoglar;
        _paneller["guncelleme"] = PanelGuncelleme;
    }

    private void NavigasyonuOlustur()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();

        foreach (var oge in NavOgeleri)
        {
            var btn = new Button
            {
                Style = (Style)FindResource("AyarNavItem"),
                Tag = oge.Id,
                ToolTip = oge.Baslik
            };
            btn.Click += (_, _) => NavigasyonAc(oge.Id);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var accent = new Border { CornerRadius = new CornerRadius(2), Margin = new Thickness(0, 2, 0, 2) };
            Grid.SetColumn(accent, 0);
            var ikonBlok = new TextBlock
            {
                Text = oge.Ikon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 15,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("InkMutedBrush")
            };
            Grid.SetColumn(ikonBlok, 1);
            var metin = new TextBlock
            {
                Text = oge.Baslik,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)FindResource("InkSoftBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(metin, 2);
            grid.Children.Add(accent);
            grid.Children.Add(ikonBlok);
            grid.Children.Add(metin);
            btn.Content = grid;

            _navButtons[oge.Id] = btn;
            NavPanel.Children.Add(btn);
        }
    }

    private void MenuleriUygula()
    {
        foreach (var oge in NavOgeleri)
        {
            if (!_navButtons.TryGetValue(oge.Id, out var btn))
                continue;

            var gorunur = oge.IzinSekmesi is null
                || KullaniciYetkileri.SekmeGorebilir("Ayarlar", oge.IzinSekmesi)
                || (oge.Id == "stok" && KullaniciYetkileri.SekmeGorebilir("Ayarlar", "Birim Terimleri"));

            btn.Visibility = gorunur ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_navButtons.TryGetValue(_aktifNav, out var aktif) && aktif.Visibility != Visibility.Visible)
        {
            var ilk = NavOgeleri.FirstOrDefault(o =>
                _navButtons.TryGetValue(o.Id, out var b) && b.Visibility == Visibility.Visible);
            if (ilk is not null)
                NavigasyonAc(ilk.Id);
        }
    }

    private void NavigasyonAc(string id)
    {
        _aktifNav = id;
        foreach (var (key, panel) in _paneller)
            panel.Visibility = key == id ? Visibility.Visible : Visibility.Collapsed;

        foreach (var (navId, btn) in _navButtons)
        {
            btn.Tag = navId == id ? "Active" : navId;
            if (btn.Content is Grid grid && grid.Children.Count >= 3)
            {
                var aktif = navId == id;
                if (grid.Children[0] is Border accent)
                    accent.Background = aktif
                        ? (Brush)FindResource("AyarPrimaryBrush")
                        : Brushes.Transparent;
                if (grid.Children[1] is TextBlock ikon)
                    ikon.Foreground = aktif
                        ? (Brush)FindResource("AyarPrimaryBrush")
                        : (Brush)FindResource("InkMutedBrush");
                if (grid.Children[2] is TextBlock metin)
                {
                    metin.FontWeight = aktif ? FontWeights.SemiBold : FontWeights.Normal;
                    metin.Foreground = aktif
                        ? (Brush)FindResource("AyarPrimaryBrush")
                        : (Brush)FindResource("InkSoftBrush");
                }
            }
        }

        IcerikScroll.ScrollToVerticalOffset(0);
    }

    private void KpiKartlariniGuncelle()
    {
        TxtKpiSurum.Text = $"v{UygulamaBilgisi.Versiyon}";
        TxtSistemSurum.Text = UygulamaBilgisi.Versiyon;
        TxtKpiVeritabani.Text = "Yerel JSON · Bağlı";
        TxtKpiFirebase.Text = FirebaseAyarDeposu.Ayarlar.Yapilandirildi
            ? "Connected"
            : "Yapılandırılmadı";
        TxtKpiYedek.Text = SonYedekTarihiBul();
    }

    private static string SonYedekTarihiBul()
    {
        try
        {
            var klasor = SatinalmaProKlasor.Yol;
            if (!Directory.Exists(klasor))
                return "—";

            var son = Directory.EnumerateFiles(klasor, "*.zip", SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            return son is null ? "Henüz yok" : son.LastWriteTime.ToString("dd.MM.yyyy HH:mm");
        }
        catch
        {
            return "—";
        }
    }

    private void DegisiklikIsaretle()
    {
        if (_genelYukleniyor || _sartnameYukleniyor || _teklifSartnameYukleniyor
            || _filoZimmetYukleniyor || _dovizYukleniyor)
            return;

        DegisiklikBanner.Visibility = Visibility.Visible;
    }

    private void DegisiklikTemizle() =>
        DegisiklikBanner.Visibility = Visibility.Collapsed;

    private void LogKlasoruAc_Click(object sender, RoutedEventArgs e)
    {
        var klasor = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SatinalmaPro");
        Directory.CreateDirectory(klasor);
        Process.Start(new ProcessStartInfo { FileName = klasor, UseShellExecute = true });
    }

    private void AyarlariYukle()
    {
        _genelYukleniyor = true;
        TxtFirmaAdi.Text = SatinalmaPro.Shared.SaaS.KiracıOturumu.TenantAd
            ?? UygulamaAyarDeposu.Ayarlar.FirmaAdi;
        TxtLogoYolu.Text = SatinalmaProLogoDeposu.GorunenAd(UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu);
        TxtAnasayfaLogoYolu.Text = SatinalmaProLogoDeposu.GorunenAd(UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu);
        _genelYukleniyor = false;
        LogoOnizlemeGuncelle();
        AnasayfaLogoOnizlemeGuncelle();

        var satinalma = SatinalmaDepo.Ayarlar;
        _sartnameYukleniyor = true;
        TxtSartnameMetni.Text = satinalma.SartnameMetni;
        _sartnameYukleniyor = false;
        _teklifSartnameYukleniyor = true;
        TxtTeklifIstemeSartnameleri.Text = satinalma.TeklifIstemeSartnameleri;
        _teklifSartnameYukleniyor = false;
        _dovizYukleniyor = true;
        TxtVarsayilanUsdKuru.Text = satinalma.VarsayilanUsdKuru > 0
            ? satinalma.VarsayilanUsdKuru.ToString(CultureInfo.CurrentCulture)
            : "";
        TxtVarsayilanEurKuru.Text = satinalma.VarsayilanEurKuru > 0
            ? satinalma.VarsayilanEurKuru.ToString(CultureInfo.CurrentCulture)
            : "";
        _dovizYukleniyor = false;
        ImzaGridleriYenile();
        KategoriListesiniYenile();
        BirimListesiniYenile();
        FiloZimmetMaddeleriniYukle();
    }

    private void FiloZimmetMaddeleriniYukle()
    {
        _filoZimmetYukleniyor = true;
        var liste = UygulamaAyarDeposu.Ayarlar.FiloZimmetFormMaddeleri;
        TxtFiloZimmetMaddeleri.Text = liste.Count == 0
            ? ""
            : string.Join(Environment.NewLine, liste);
        _filoZimmetYukleniyor = false;
    }

    private void FiloZimmetMaddeleriDegisti(object sender, TextChangedEventArgs e)
    {
        if (_filoZimmetYukleniyor) return;
        DegisiklikIsaretle();
        FiloZimmetMaddeleriniKaydet(sessiz: true);
    }

    private void FiloZimmetKaydet_Click(object sender, RoutedEventArgs e) =>
        FiloZimmetMaddeleriniKaydet(sessiz: false);

    private void FiloZimmetMaddeleriniKaydet(bool sessiz)
    {
        UygulamaAyarDeposu.Ayarlar.FiloZimmetFormMaddeleri =
            ZimmetMaddeYardimcisi.Ayikla(TxtFiloZimmetMaddeleri.Text);
        UygulamaAyarDeposu.Kaydet();

        if (!sessiz)
        {
            MessageBox.Show("Zimmet formu maddeleri kaydedildi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void AyarlarKaydet_Click(object sender, RoutedEventArgs e)
    {
        // Firma adı Yönetici kaynağıdır — UI'dan yazılmaz.
        UygulamaAyarDeposu.FirmaAdiniOturumdanSenkronizeEt();
        FiloZimmetMaddeleriniKaydet(sessiz: true);
        SatinalmaDepo.Ayarlar.SartnameMetni = TxtSartnameMetni.Text;
        SatinalmaDepo.Ayarlar.TeklifIstemeSartnameleri = TxtTeklifIstemeSartnameleri.Text;
        DovizKurlariniKaydet(sessiz: true);
        UygulamaAyarDeposu.Kaydet();
        SatinalmaDepo.Kaydet();

        DegisiklikTemizle();
        KpiKartlariniGuncelle();
        MessageBox.Show("Tüm ayarlar kaydedildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void KategoriListesiniYenile()
    {
        _malzemeKategorileri.Clear();
        foreach (var kategori in MalzemeKategoriDeposu.Liste)
            _malzemeKategorileri.Add(kategori);
    }

    private void KategoriEkle_Click(object sender, RoutedEventArgs e)
    {
        var ad = TxtYeniKategori.Text.Trim();
        if (string.IsNullOrWhiteSpace(ad))
        {
            MessageBox.Show("Kategori adı girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!MalzemeKategoriDeposu.Ekle(ad))
        {
            MessageBox.Show("Bu kategori zaten listede veya geçersiz.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        TxtYeniKategori.Clear();
        KategoriListesiniYenile();
    }

    private void KategoriSil_Click(object sender, RoutedEventArgs e)
    {
        if (KategoriListesi.SelectedItem is not string secili)
        {
            MessageBox.Show("Silmek için listeden bir kategori seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MalzemeKategoriDeposu.Liste.Count <= 1)
        {
            MessageBox.Show("En az bir kategori bulunmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!MalzemeKategoriDeposu.Sil(secili))
        {
            MessageBox.Show("Kategori silinemedi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        KategoriListesiniYenile();
    }

    private void BirimListesiniYenile()
    {
        _malzemeBirimleri.Clear();
        foreach (var birim in MalzemeBirimDeposu.Liste)
            _malzemeBirimleri.Add(birim);
    }

    private void BirimEkle_Click(object sender, RoutedEventArgs e)
    {
        var ad = TxtYeniBirim.Text.Trim();
        if (string.IsNullOrWhiteSpace(ad))
        {
            MessageBox.Show("Birim terimi girin.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!MalzemeBirimDeposu.Ekle(ad))
        {
            MessageBox.Show("Bu birim zaten listede veya geçersiz.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        TxtYeniBirim.Clear();
        BirimListesiniYenile();
    }

    private void BirimSil_Click(object sender, RoutedEventArgs e)
    {
        if (BirimListesi.SelectedItem is not string secili)
        {
            MessageBox.Show("Silmek için listeden bir birim seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MalzemeBirimDeposu.Liste.Count <= 1)
        {
            MessageBox.Show("En az bir birim terimi bulunmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!MalzemeBirimDeposu.Sil(secili))
        {
            MessageBox.Show("Birim silinemedi.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BirimListesiniYenile();
    }

    private void VeriDurumlariniYenile()
    {
        _veriDurumlari.Clear();
        foreach (var durum in SatinalmaProVeriKatalogu.DurumlariOlustur())
            _veriDurumlari.Add(durum);
    }

    #region Genel

    private void GenelAyarDegisti(object sender, TextChangedEventArgs e)
    {
        if (_genelYukleniyor) return;
        DegisiklikIsaretle();
    }

    private void LogoSec_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Firma Logosu Seç",
            Filter = "Görsel Dosyaları|*.png;*.jpg;*.jpeg;*.bmp;*.webp"
        };

        if (dialog.ShowDialog() != true) return;

        UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu = SatinalmaProLogoDeposu.Kaydet(dialog.FileName, "firma");
        TxtLogoYolu.Text = SatinalmaProLogoDeposu.GorunenAd(UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu);
        LogoOnizlemeGuncelle();
        UygulamaAyarDeposu.Kaydet();
    }

    private void AnasayfaLogoSec_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Anasayfa Logosu Seç",
            Filter = "Görsel Dosyaları|*.png;*.jpg;*.jpeg;*.bmp;*.webp"
        };

        if (dialog.ShowDialog() != true) return;

        UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu = SatinalmaProLogoDeposu.Kaydet(dialog.FileName, "anasayfa");
        TxtAnasayfaLogoYolu.Text = SatinalmaProLogoDeposu.GorunenAd(UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu);
        AnasayfaLogoOnizlemeGuncelle();
        UygulamaAyarDeposu.Kaydet();
    }

    private void LogoOnizlemeGuncelle() =>
        LogoGorselYardimcisi.GorselAyarla(ImgLogoOnizleme, UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu);

    private void AnasayfaLogoOnizlemeGuncelle() =>
        LogoGorselYardimcisi.GorselAyarla(ImgAnasayfaLogoOnizleme, UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu);

    #endregion

    #region Satınalma

    private void ImzaGridleriYenile()
    {
        SatinalmaDepo.ImzalariHazirla(SatinalmaDepo.Ayarlar);
        SefImzaGrid.ItemsSource = null;
        YonetimImzaGrid.ItemsSource = null;
        SefImzaGrid.ItemsSource = SatinalmaDepo.Ayarlar.SefImzalari;
        YonetimImzaGrid.ItemsSource = SatinalmaDepo.Ayarlar.YonetimImzalari;
    }

    private void ImzaDuzenlemesiniBaslat()
    {
        SatinalmaDepo.ImzaAyarlariniOzellestir();
    }

    private void SartnameMetniDegisti(object sender, TextChangedEventArgs e)
    {
        if (_sartnameYukleniyor) return;
        SatinalmaDepo.Ayarlar.SartnameMetni = TxtSartnameMetni.Text;
        SatinalmaDepo.Kaydet();
        DegisiklikIsaretle();
    }

    private void TeklifIstemeSartnameleriDegisti(object sender, TextChangedEventArgs e)
    {
        if (_teklifSartnameYukleniyor) return;
        SatinalmaDepo.Ayarlar.TeklifIstemeSartnameleri = TxtTeklifIstemeSartnameleri.Text;
        SatinalmaDepo.Kaydet();
        DegisiklikIsaretle();
    }

    private void DovizKuruDegisti(object sender, TextChangedEventArgs e)
    {
        if (_dovizYukleniyor) return;
        DovizKurlariniKaydet(sessiz: true);
        DegisiklikIsaretle();
    }

    private void DovizKurlariniKaydet(bool sessiz)
    {
        SatinalmaDepo.Ayarlar.VarsayilanUsdKuru = OndalikOku(TxtVarsayilanUsdKuru.Text);
        SatinalmaDepo.Ayarlar.VarsayilanEurKuru = OndalikOku(TxtVarsayilanEurKuru.Text);
        SatinalmaDepo.Kaydet();
    }

    private static decimal OndalikOku(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            return 0;

        var temiz = metin.Trim();
        if (decimal.TryParse(temiz, NumberStyles.Any, CultureInfo.CurrentCulture, out var sonuc))
            return sonuc;

        return decimal.TryParse(temiz.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out sonuc)
            ? sonuc
            : 0;
    }

    private void SefImzaEkle_Click(object sender, RoutedEventArgs e)
    {
        ImzaDuzenlemesiniBaslat();
        SatinalmaDepo.Ayarlar.SefImzalari.Add(new ImzaAyari { Unvan = "Yeni Şef", Aktif = true });
        ImzaGridleriYenile();
        SatinalmaDepo.Kaydet();
    }

    private void SefImzaSil_Click(object sender, RoutedEventArgs e)
    {
        if (SefImzaGrid.SelectedItem is not ImzaAyari imza) return;

        ImzaDuzenlemesiniBaslat();
        SatinalmaDepo.Ayarlar.SefImzalari.Remove(imza);
        if (SatinalmaDepo.Ayarlar.SefImzalari.Count == 0
            && SatinalmaDepo.Ayarlar.YonetimImzalari.Count == 0)
            SatinalmaDepo.Ayarlar.ImzaAyarleriTemiz = true;
        ImzaGridleriYenile();
        SatinalmaDepo.Kaydet();
    }

    private void YonetimImzaEkle_Click(object sender, RoutedEventArgs e)
    {
        ImzaDuzenlemesiniBaslat();
        SatinalmaDepo.Ayarlar.YonetimImzalari.Add(new ImzaAyari
        {
            Unvan = "Yönetim / Proje Müdürü",
            Aktif = true
        });
        ImzaGridleriYenile();
        SatinalmaDepo.Kaydet();
    }

    private void YonetimImzaSil_Click(object sender, RoutedEventArgs e)
    {
        if (YonetimImzaGrid.SelectedItem is not ImzaAyari imza) return;

        ImzaDuzenlemesiniBaslat();
        SatinalmaDepo.Ayarlar.YonetimImzalari.Remove(imza);
        if (SatinalmaDepo.Ayarlar.SefImzalari.Count == 0
            && SatinalmaDepo.Ayarlar.YonetimImzalari.Count == 0)
            SatinalmaDepo.Ayarlar.ImzaAyarleriTemiz = true;
        ImzaGridleriYenile();
        SatinalmaDepo.Kaydet();
    }

    private void ImzaGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel)
            return;

        ImzaDuzenlemesiniBaslat();

        if (e.EditingElement is TextBox textBox)
        {
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
        }

        // CommitEdit burada çağrılmaz — CellEditEnding sırasında WPF çöker.
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                SatinalmaDepo.KaydetAyarlar();
                _ = BulutVeriSenkronu.AyarlariHemenGonderAsync();
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "Ayarlar.ImzaKaydet");
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    #endregion

    #region Veri yönetimi

    private void VeriDurumYenile_Click(object sender, RoutedEventArgs e) => VeriDurumlariniYenile();

    private void VeriKlasoruAc_Click(object sender, RoutedEventArgs e)
    {
        SatinalmaProKlasor.Olustur();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = SatinalmaProKlasor.Yol,
            UseShellExecute = true
        });
    }

    private async void ModulSifirla_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        if (sender is not Button { Tag: string dosyaAdi }) return;
        if (VeriDurumGrid.SelectedItem is not VeriKaydiDurumu secili &&
            string.IsNullOrEmpty(dosyaAdi))
            return;

        dosyaAdi = string.IsNullOrEmpty(dosyaAdi)
            ? (VeriDurumGrid.SelectedItem as VeriKaydiDurumu)?.DosyaAdi ?? ""
            : dosyaAdi;

        if (string.IsNullOrEmpty(dosyaAdi)) return;

        var tanim = SatinalmaProVeriKatalogu.TumKayitlar.FirstOrDefault(t => t.DosyaAdi == dosyaAdi);
        var ad = tanim?.ModulAdi ?? dosyaAdi;

        var sonuc = MessageBox.Show(
            $"{ad} verileri sıfırlanacak. Bu işlem geri alınamaz.\nDevam etmek istiyor musunuz?",
            "Modül Sıfırla",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (sonuc != MessageBoxResult.Yes) return;

        SatinalmaProYedeklemeServisi.ModulSifirla(dosyaAdi);
        AyarlariYukle();
        VeriDurumlariniYenile();

        if (string.Equals(dosyaAdi, "satinalma_talepler.json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await BildirimYoneticisi.GecersizleriSilAsync();
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "Ayarlar.ModulSifirla.BildirimTemizligi");
            }
        }

        if (OturumYoneticisi.GirisYapildi && OturumYoneticisi.Firestore is not null)
        {
            var anahtar = BulutVeriSenkronu.DosyaAdindanAnahtar(dosyaAdi);
            if (anahtar is not null)
            {
                try
                {
                    await BulutVeriSenkronu.AnahtarBulutaGonderAsync(anahtar);
                    MessageBox.Show(
                        $"{ad} sıfırlandı ve buluta kaydedildi.\nDiğer bilgisayarlar giriş yaptığında veya en geç ~25 sn içinde güncellenecek.",
                        UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"{ad} yerelde sıfırlandı ancak buluta yazılamadı:\n{ex.Message}",
                        UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }

        MessageBox.Show($"{ad} sıfırlandı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.ModulSifirla");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TumVerileriSifirla_Click(object sender, RoutedEventArgs e)
    {
        try
        {
        var sonuc = MessageBox.Show(
            "Firma bilgileri, logolar, imzalar ve tüm modül verileri sıfırlanacak.\n" +
            "Firebase ve Android ayarları korunur.\n" +
            "Önce yedek almanız önerilir.\n\nDevam etmek istiyor musunuz?",
            "Tüm Verileri Sıfırla",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (sonuc != MessageBoxResult.Yes) return;

        var onay = MessageBox.Show(
            "Son onay: Firma bilgileri, logolar, imzalar ve modül kayıtları kalıcı olarak silinip varsayılanlara dönecek.\n" +
            "Firebase yapılandırması, FCM anahtarı, google-services.json ve oturum bilgileri etkilenmeyecek.",
            "Emin misiniz?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (onay != MessageBoxResult.Yes) return;

        SatinalmaProYedeklemeServisi.TumVerileriSifirla();
        AyarlariYukle();
        VeriDurumlariniYenile();
        if (Application.Current.MainWindow is MainWindow mw)
            mw.Sidebar.Yenile();

        if (OturumYoneticisi.GirisYapildi && OturumYoneticisi.Firestore is not null)
        {
            try
            {
                await BulutVeriSenkronu.TumVerileriBulutaGonderAsync(sifirlamaModu: true);
                try
                {
                    await BildirimYoneticisi.SifirlamaSonrasiTemizleAsync();
                }
                catch (Exception ex)
                {
                    HataGunlugu.Kaydet(ex, "Ayarlar.TumVerileriSifirla.BildirimTemizligi");
                }
                MessageBox.Show(
                    "Firma ve modül verileri sıfırlandı ve buluta kaydedildi.\n" +
                    "Firebase ve Android ayarları korundu.\n" +
                    "Android ve diğer cihazlar yenileme/giriş sonrası boş listeyi görecek.",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Veriler yerelde sıfırlandı ancak buluta yazılamadı:\n{ex.Message}\n\nFirebase ayarları korundu.",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        MessageBox.Show(
            "Firma ve modül verileri sıfırlandı.\nFirebase ve Android ayarları korundu.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);

        try
        {
            await BildirimYoneticisi.SifirlamaSonrasiTemizleAsync();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.TumVerileriSifirla.BildirimTemizligi");
        }
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.TumVerileriSifirla");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void YedekAl_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Yedek Kaydet",
            Filter = "ZIP Dosyası (*.zip)|*.zip",
            FileName = $"SatinalmaPro_Yedek_{DateTime.Now:yyyyMMdd_HHmm}.zip"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            SatinalmaProYedeklemeServisi.Yedekle(dialog.FileName);
            MessageBox.Show($"Yedek oluşturuldu:\n{dialog.FileName}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yedek alınamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GeriYukle_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Yedek Seç",
            Filter = "ZIP Dosyası (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true) return;

        var sonuc = MessageBox.Show(
            "Mevcut veriler yedekteki dosyalarla değiştirilecek.\nDevam etmek istiyor musunuz?",
            "Geri Yükle",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (sonuc != MessageBoxResult.Yes) return;

        try
        {
            SatinalmaProYedeklemeServisi.GeriYukle(dialog.FileName);
            AyarlariYukle();
            VeriDurumlariniYenile();
            MessageBox.Show("Yedek başarıyla geri yüklendi.\nDeğişikliklerin tam yansıması için modülleri yeniden açın.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Geri yükleme başarısız:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    private void BulutPaneliniGuncelle()
    {
        TxtFirebaseYolu.Text = FirebaseAyarDeposu.UygulamaDosyaYolu;
        TxtGoogleServicesDurum.Text = FirebaseAyarDeposu.GoogleServicesMevcut
            ? "Android google-services.json: yüklü ✓ (APK derlemesi / yedek)"
            : "Android google-services.json: isteğe bağlı (v1.9.4 APK içinde zaten var)";
        TxtFcmServiceAccountDurum.Text = FirebaseAyarDeposu.FcmServiceAccountMevcut
            ? "FCM Service Account: yüklü ✓ (push gönderimi için gerekli)"
            : "FCM Service Account: henüz yüklenmedi — push çalışmaz";

        if (!FirebaseAyarDeposu.Ayarlar.Yapilandirildi)
        {
            TxtFirebaseUyari.Visibility = Visibility.Visible;
            TxtFirebaseUyari.Text =
                "⚠ Firebase yapılandırılmamış — uygulama yerel modda çalışır. Mobil senkron ve push bildirimleri devre dışı kalır.";
        }
        else
            TxtFirebaseUyari.Visibility = Visibility.Collapsed;

        var manifest = FirebaseAyarDeposu.Ayarlar.GuncellemeManifestUrl;
        var guncellemeMetni = string.IsNullOrWhiteSpace(manifest)
            ? $"Otomatik güncelleme: yapılandırılmamış · Mevcut sürüm: {UygulamaBilgisi.Versiyon}"
            : $"Otomatik güncelleme: aktif · Sürüm: {UygulamaBilgisi.Versiyon} · Manifest: {manifest}";
        TxtGuncellemeDurum.Text = guncellemeMetni;
        TxtGuncellemeDurum2.Text = guncellemeMetni;

        BtnBulutaYukle.Visibility = KullaniciYetkileri.AdminMi && OturumYoneticisi.BulutAktif
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void FcmServiceAccountYukle_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.AdminMi)
            return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Firebase Service Account JSON seçin",
            Filter = "JSON (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            FirebaseAyarDeposu.FcmServiceAccountKaydet(dialog.FileName);
            FirebaseAyarDeposu.Kaydet();
            MessageBox.Show(
                "Service Account JSON kaydedildi.\nPush bildirimleri artık çalışabilir.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            BulutPaneliniGuncelle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GoogleServicesYukle_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.AdminMi)
            return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "google-services.json seçin",
            Filter = "Firebase Android Config (google-services.json)|google-services.json|JSON (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            FirebaseAyarDeposu.GoogleServicesJsonKaydet(dialog.FileName);
            MessageBox.Show(
                "google-services.json kaydedildi.\nAndroid uygulaması bir sonraki derlemede kullanacak.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            BulutPaneliniGuncelle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BulutaYukle_Click(object sender, RoutedEventArgs e)
    {
        BtnBulutaYukle.IsEnabled = false;
        try
        {
            await BulutVeriSenkronu.TumVerileriBulutaGonderAsync();
            MessageBox.Show(
                "Tüm veriler ve logolar Firebase bulutuna yüklendi.\nDiğer bilgisayarlar giriş yaptığında aynı verileri görecek.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.BulutaYukle");
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            BtnBulutaYukle.IsEnabled = true;
        }
    }

    private void FirebaseKurulum_Click(object sender, RoutedEventArgs e) =>
        KurulumDosyasiAc("FIREBASE_KURULUM.txt");

    private void FirebaseAndroidKurulum_Click(object sender, RoutedEventArgs e) =>
        KurulumDosyasiAc("FIREBASE_ANDROID_CONSOLE.txt");

    private void GithubKurulum_Click(object sender, RoutedEventArgs e) =>
        KurulumDosyasiAc("GITHUB_RELEASES_KURULUM.txt");

    private void GithubSurumGuncelleme_Click(object sender, RoutedEventArgs e) =>
        KurulumDosyasiAc("GITHUB_SURUM_GUNCELLEME.txt");

    private static void KurulumDosyasiAc(string dosyaAdi)
    {
        var yol = Path.Combine(AppContext.BaseDirectory, dosyaAdi);
        if (!File.Exists(yol))
        {
            MessageBox.Show("Kurulum kılavuzu bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = yol,
            UseShellExecute = true
        });
    }
}
