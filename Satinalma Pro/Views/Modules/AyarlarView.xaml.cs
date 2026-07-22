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
using SatinalmaPro.Views.Modules.Satinalma.Part1;
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
            // Salt-okunur gezgini Ekle/Düzenle/Sil'i kapatmasın — ayar yazabilen kullanıcıda aç.
            ImzaButonlariniAktifEt();
        };
    }

    private void ImzaButonlariniAktifEt()
    {
        if (!KullaniciYetkileri.ModulYazabilir("Ayarlar"))
            return;

        foreach (var btn in new[]
                 {
                     BtnSefImzaEkle, BtnSefImzaDuzenle, BtnSefImzaSil,
                     BtnYonetimImzaEkle, BtnYonetimImzaDuzenle, BtnYonetimImzaSil
                 })
        {
            if (btn is null) continue;
            btn.IsEnabled = true;
            btn.Opacity = 1;
            btn.Cursor = System.Windows.Input.Cursors.Hand;
        }
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
        if (id == "satinalma")
            ImzaButonlariniAktifEt();
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
        ImzaDuzenlemeleriniKaydet();
        UygulamaAyarDeposu.Kaydet();
        SatinalmaDepo.Kaydet();
        _ = BulutVeriSenkronu.AyarlariHemenGonderAsync();

        DegisiklikTemizle();
        KpiKartlariniGuncelle();
        MessageBox.Show("Tüm ayarlar kaydedildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>İmza listelerini diske yazar ve buluta hemen gönderir.</summary>
    private void ImzaDuzenlemeleriniKaydet()
    {
        ImzaDuzenlemesiniBaslat();
        if ((SatinalmaDepo.Ayarlar.SefImzalari?.Count ?? 0) > 0
            || (SatinalmaDepo.Ayarlar.YonetimImzalari?.Count ?? 0) > 0)
        {
            SatinalmaDepo.Ayarlar.ImzaAyarleriTemiz = false;
        }

        SatinalmaDepo.KaydetAyarlar();
        _ = BulutVeriSenkronu.AyarlariHemenGonderAsync();
        DegisiklikIsaretle();
    }

    private Window? SahipPencere() => Window.GetWindow(this);

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

    private void ImzaGridleriYenile(ImzaAyari? sefSecili = null, ImzaAyari? yonetimSecili = null)
    {
        SatinalmaDepo.ImzalariHazirla(SatinalmaDepo.Ayarlar);
        var oncekiSef = sefSecili ?? SefImzaOlarak(SefImzaGrid.SelectedItem) ?? SefImzaOlarak(SefImzaGrid.CurrentItem);
        var oncekiYonetim = yonetimSecili ?? SefImzaOlarak(YonetimImzaGrid.SelectedItem)
            ?? SefImzaOlarak(YonetimImzaGrid.CurrentItem);

        SefImzaGrid.ItemsSource = null;
        YonetimImzaGrid.ItemsSource = null;
        SefImzaGrid.ItemsSource = SatinalmaDepo.Ayarlar.SefImzalari;
        YonetimImzaGrid.ItemsSource = SatinalmaDepo.Ayarlar.YonetimImzalari;

        if (oncekiSef is not null && SatinalmaDepo.Ayarlar.SefImzalari.Contains(oncekiSef))
            SefImzaGrid.SelectedItem = oncekiSef;
        if (oncekiYonetim is not null && SatinalmaDepo.Ayarlar.YonetimImzalari.Contains(oncekiYonetim))
            YonetimImzaGrid.SelectedItem = oncekiYonetim;
    }

    private static ImzaAyari? SefImzaOlarak(object? o) => o as ImzaAyari;

    /// <summary>
    /// Cell seçiminde SelectedItem boş kalabiliyor; CurrentItem / tek satır yedekleri kullan.
    /// </summary>
    private static ImzaAyari? SeciliImzaAl(DataGrid grid, IList<ImzaAyari>? liste)
    {
        if (SefImzaOlarak(grid.SelectedItem) is { } secili)
            return secili;
        if (SefImzaOlarak(grid.CurrentItem) is { } guncel)
            return guncel;
        if (grid.SelectedCells.Count > 0 && SefImzaOlarak(grid.SelectedCells[0].Item) is { } hucre)
            return hucre;
        if (liste is { Count: 1 })
            return liste[0];
        return null;
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
        try
        {
            ImzaButonlariniAktifEt();
            var yeni = ImzaDuzenleDialog.Goster(SahipPencere(), "Şef İmzası Ekle",
                new ImzaAyari { Unvan = "Şef", Aktif = true });
            if (yeni is null) return;

            SatinalmaDepo.Ayarlar.SefImzalari ??= [];
            ImzaDuzenlemesiniBaslat();
            SatinalmaDepo.Ayarlar.ImzaAyarleriTemiz = false;
            SatinalmaDepo.Ayarlar.SefImzalari.Add(yeni);
            ImzaGridleriYenile(sefSecili: yeni);
            ImzaDuzenlemeleriniKaydet();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.SefImzaEkle");
            MessageBox.Show($"İmza eklenemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SefImzaDuzenle_Click(object sender, RoutedEventArgs e) => SefImzaDuzenle();

    private void SefImzaGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        SefImzaDuzenle();

    private void SefImzaDuzenle()
    {
        try
        {
            ImzaButonlariniAktifEt();
            var imza = SeciliImzaAl(SefImzaGrid, SatinalmaDepo.Ayarlar.SefImzalari);
            if (imza is null)
            {
                MessageBox.Show("Düzenlemek için listeden bir satıra tıklayıp seçin.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SefImzaGrid.SelectedItem = imza;
            var guncel = ImzaDuzenleDialog.Goster(SahipPencere(), "Şef İmzası Düzenle", imza);
            if (guncel is null) return;

            imza.Unvan = guncel.Unvan;
            imza.AdSoyad = guncel.AdSoyad;
            imza.Aktif = guncel.Aktif;
            ImzaGridleriYenile(sefSecili: imza);
            ImzaDuzenlemeleriniKaydet();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.SefImzaDuzenle");
            MessageBox.Show($"İmza düzenlenemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SefImzaSil_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ImzaButonlariniAktifEt();
            var imza = SeciliImzaAl(SefImzaGrid, SatinalmaDepo.Ayarlar.SefImzalari);
            if (imza is null)
            {
                MessageBox.Show("Silmek için listeden bir satıra tıklayıp seçin.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var onay = MessageBox.Show(
                $"«{imza.Unvan}» imzasını silmek istiyor musunuz?",
                "İmza Sil", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes) return;

            ImzaDuzenlemesiniBaslat();
            SatinalmaDepo.Ayarlar.SefImzalari.Remove(imza);
            if (SatinalmaDepo.Ayarlar.SefImzalari.Count == 0
                && SatinalmaDepo.Ayarlar.YonetimImzalari.Count == 0)
                SatinalmaDepo.Ayarlar.ImzaAyarleriTemiz = true;
            ImzaGridleriYenile();
            ImzaDuzenlemeleriniKaydet();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.SefImzaSil");
            MessageBox.Show($"İmza silinemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void YonetimImzaEkle_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ImzaButonlariniAktifEt();
            var yeni = ImzaDuzenleDialog.Goster(SahipPencere(), "Yönetim İmzası Ekle",
                new ImzaAyari { Unvan = "Yönetim / Proje Müdürü", Aktif = true });
            if (yeni is null) return;

            SatinalmaDepo.Ayarlar.YonetimImzalari ??= [];
            ImzaDuzenlemesiniBaslat();
            SatinalmaDepo.Ayarlar.ImzaAyarleriTemiz = false;
            SatinalmaDepo.Ayarlar.YonetimImzalari.Add(yeni);
            ImzaGridleriYenile(yonetimSecili: yeni);
            ImzaDuzenlemeleriniKaydet();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.YonetimImzaEkle");
            MessageBox.Show($"İmza eklenemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void YonetimImzaDuzenle_Click(object sender, RoutedEventArgs e) => YonetimImzaDuzenle();

    private void YonetimImzaGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        YonetimImzaDuzenle();

    private void YonetimImzaDuzenle()
    {
        try
        {
            ImzaButonlariniAktifEt();
            var imza = SeciliImzaAl(YonetimImzaGrid, SatinalmaDepo.Ayarlar.YonetimImzalari);
            if (imza is null)
            {
                MessageBox.Show("Düzenlemek için listeden bir satıra tıklayıp seçin.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            YonetimImzaGrid.SelectedItem = imza;
            var guncel = ImzaDuzenleDialog.Goster(SahipPencere(), "Yönetim İmzası Düzenle", imza);
            if (guncel is null) return;

            imza.Unvan = guncel.Unvan;
            imza.AdSoyad = guncel.AdSoyad;
            imza.Aktif = guncel.Aktif;
            ImzaGridleriYenile(yonetimSecili: imza);
            ImzaDuzenlemeleriniKaydet();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.YonetimImzaDuzenle");
            MessageBox.Show($"İmza düzenlenemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void YonetimImzaSil_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ImzaButonlariniAktifEt();
            var imza = SeciliImzaAl(YonetimImzaGrid, SatinalmaDepo.Ayarlar.YonetimImzalari);
            if (imza is null)
            {
                MessageBox.Show("Silmek için listeden bir satıra tıklayıp seçin.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var onay = MessageBox.Show(
                $"«{imza.Unvan}» imzasını silmek istiyor musunuz?",
                "İmza Sil", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes) return;

            ImzaDuzenlemesiniBaslat();
            SatinalmaDepo.Ayarlar.YonetimImzalari.Remove(imza);
            if (SatinalmaDepo.Ayarlar.SefImzalari.Count == 0
                && SatinalmaDepo.Ayarlar.YonetimImzalari.Count == 0)
                SatinalmaDepo.Ayarlar.ImzaAyarleriTemiz = true;
            ImzaGridleriYenile();
            ImzaDuzenlemeleriniKaydet();
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.YonetimImzaSil");
            MessageBox.Show($"İmza silinemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

        var satinalmaModulu =
            dosyaAdi.Equals("satinalma_talepler.json", StringComparison.OrdinalIgnoreCase)
            || dosyaAdi.Equals("satinalma_ayarlar.json", StringComparison.OrdinalIgnoreCase);

        // Satınalma: istemci merge yarışına bırakma — sunucu otoriter temizler.
        if (satinalmaModulu && OturumYoneticisi.GirisYapildi && OturumYoneticisi.Firestore is not null)
        {
            BulutVeriSenkronu.SifirlamaKapisiniAc();
            try
            {
                var sunucu = await KiraciVeriSifirlamaServisi.SifirlaAsync("satinalma");
                SatinalmaDepo.AyarlariSifirla();
                SatinalmaDepo.Ayarlar.VeriSifirlamaUtc = sunucu.VeriSifirlamaUtc;
                SatinalmaDepo.TumTalepleriSifirla();
                SatinalmaDepo.Kaydet();
                BildirimDeposu.KiraciDegisti();

                AyarlariYukle();
                VeriDurumlariniYenile();
                if (Application.Current.MainWindow is MainWindow mwSat)
                    mwSat.Sidebar.Yenile();

                MessageBox.Show(
                    $"Satınalma verileri (talepler + bildirimler) sıfırlandı.\n" +
                    $"Android yenilemede boş listeyi görecek.\nInbox: {sunucu.InboxesCleared}/{sunucu.UsersProcessed}",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "Ayarlar.ModulSifirla.Satinalma");
                MessageBox.Show(
                    $"Satınalma sıfırlanamadı:\n{ex.Message}",
                    UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                BulutVeriSenkronu.SifirlamaKapisiniKapat();
            }
        }

        SatinalmaProYedeklemeServisi.ModulSifirla(dosyaAdi);
        AyarlariYukle();
        VeriDurumlariniYenile();

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
            "Talepler, stok, bildirimler, medya ve tüm modül verileri sıfırlanacak\n" +
            "(masaüstü + Android + tüm kullanıcı bildirimleri).\n" +
            "Kullanıcı hesapları ve Firebase yapılandırması korunur.\n" +
            "Önce yedek almanız önerilir.\n\nDevam etmek istiyor musunuz?",
            "Tüm Verileri Sıfırla",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (sonuc != MessageBoxResult.Yes) return;

        var onay = MessageBox.Show(
            "Son onay: Tüm operasyonel veriler kalıcı olarak silinecek.\n" +
            "Firebase yapılandırması, kullanıcı hesapları ve oturum bilgileri etkilenmeyecek.",
            "Emin misiniz?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (onay != MessageBoxResult.Yes) return;

        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)
        {
            SatinalmaProYedeklemeServisi.TumVerileriSifirla();
            AyarlariYukle();
            VeriDurumlariniYenile();
            if (Application.Current.MainWindow is MainWindow mwOffline)
                mwOffline.Sidebar.Yenile();
            MessageBox.Show(
                "Firma ve modül verileri yerelde sıfırlandı.\nBuluta yazmak için giriş yapıp tekrar sıfırlayın.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        BulutVeriSenkronu.SifirlamaKapisiniAc();
        try
        {
            // 1) Sunucu otoriter temizler (tüm inbox + veri + procurement_requests).
            var sunucu = await KiraciVeriSifirlamaServisi.SifirlaAsync();

            // 2) Yerel bellek/disk — Planla/merge yarışı olmadan.
            SatinalmaProYedeklemeServisi.TumVerileriSifirla(bulutaPlanlama: false);
            SatinalmaDepo.Ayarlar.VeriSifirlamaUtc = sunucu.VeriSifirlamaUtc;
            SatinalmaDepo.Kaydet();
            BildirimDeposu.KiraciDegisti();

            // 3) Boş bulutu birleştirmeden uygula.
            await BulutVeriSenkronu.BuluttanYukleAsync();

            AyarlariYukle();
            VeriDurumlariniYenile();
            if (Application.Current.MainWindow is MainWindow mw)
                mw.Sidebar.Yenile();

            MessageBox.Show(
                "Tüm operasyonel veriler (talepler, stok, bildirimler, medya) sıfırlandı.\n" +
                $"Android ve diğer cihazlar yenilemede boş listeyi görecek.\n" +
                $"Kullanıcı inbox temizliği: {sunucu.InboxesCleared}/{sunucu.UsersProcessed}",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, "Ayarlar.TumVerileriSifirla");
            MessageBox.Show(
                $"Sistem sıfırlanamadı:\n{ex.Message}",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BulutVeriSenkronu.SifirlamaKapisiniKapat();
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
