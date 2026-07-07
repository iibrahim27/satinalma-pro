using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Views.Controls;

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
    private readonly Dictionary<string, TextBlock> _stokKpiMetinleri = new(StringComparer.Ordinal);
    private string? _agacKategori;
    private string? _stokGrupAlani;
    private bool _stokYogunGorunum;
    private bool _stokTamEkran;
    private bool _arayuzHazir;
    private GridLength _solPanelGenislik;
    private GridLength _araBoslukGenislik;
    private GridLength _stokTabloSatirYukseklik;
    private int _stokTabloOrijinalSatir;

    private static readonly (string Baslik, string Alan)[] StokGrupSecenekleri =
    [
        ("Kategori", "Kategori"),
        ("Depo / Saha", "DepoSaha"),
        ("Durum", "DurumMetin"),
        ("Birim", "Birim")
    ];

    public ObservableCollection<StokKaydi> Kayitlar => UygulamaVeriDeposu.Stok;
    public ObservableCollection<StokHareketKaydi> Hareketler => UygulamaVeriDeposu.StokHareketleri;

    public StokYonetimiView()
    {
        InitializeComponent();
        DataContext = this;

        _filtreZamanlayici = new FiltreZamanlayici(StokFiltreYenile);
        _hareketFiltreZamanlayici = new FiltreZamanlayici(HareketFiltreYenile);

        _stokGorunum = CollectionViewSource.GetDefaultView(Kayitlar);
        _stokGorunum.Filter = StokFiltresi;
        StokGrid.ItemsSource = _stokSayfalama.SayfaKayitlari;
        ErpDataGridYardimcisi.PremiumGridAyarla(StokGrid);
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
        CmbDurum.Items.Add(new ComboBoxItem { Content = "Düşük" });
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
            UygulamaVeriDeposu.OrnekVeriyiYukle();
            SekmeYetkileriniUygula();
            StokAksiyonlariniAyarla();
            KullaniciYetkileri.ModulErisiminiUygula(this, "Stok Yönetimi");
            IlkGorunurSekmeyeGec();
            StokKpiOlustur();
            KategoriAgaciniGuncelle();
            TumunuYenile();
            _arayuzHazir = true;
        };
    }

    private void StokKpiOlustur()
    {
        StokKpiPanel.Children.Clear();
        _stokKpiMetinleri.Clear();
        var kpiler = new (string Baslik, string Deger, string Renk, string Ikon)[]
        {
            ("Toplam Stok Kalemi", "0", "#2563EB", "\uE8F1"),
            ("Toplam Stok Miktarı", "0", "#16A34A", "\uE7BF"),
            ("Toplam Stok Değeri", "₺0", "#14B8A6", "\uE8C7"),
            ("Kritik Stok", "0", "#DC2626", "\uE7BA"),
            ("Minimum Altı", "0", "#F59E0B", "\uE823"),
            ("Pasif Ürün", "0", "#64748B", "\uE9D9")
        };

        foreach (var kpi in kpiler)
        {
            var renk = (Color)ColorConverter.ConvertFromString(kpi.Renk)!;
            var degerTb = new TextBlock { Text = kpi.Deger, Style = (Style)FindResource("ErpKpiValue") };
            var kart = new Border { Style = (Style)FindResource("ErpKpiCard") };
            kart.Child = new StackPanel
            {
                Children =
                {
                    new Border
                    {
                        Width = 36, Height = 36, CornerRadius = new CornerRadius(10),
                        Background = new SolidColorBrush(renk) { Opacity = 0.12 },
                        Child = new TextBlock
                        {
                            Text = kpi.Ikon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 16,
                            Foreground = new SolidColorBrush(renk),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new TextBlock { Text = kpi.Baslik, Style = (Style)FindResource("ErpKpiLabel"), Margin = new Thickness(0, 10, 0, 0) },
                    degerTb
                }
            };
            _stokKpiMetinleri[kpi.Baslik] = degerTb;
            StokKpiPanel.Children.Add(kart);
        }
    }

    private void StokKpiGuncelle(string baslik, string deger)
    {
        if (_stokKpiMetinleri.TryGetValue(baslik, out var tb))
            tb.Text = deger;
    }

    private void KategoriAgaciniGuncelle()
    {
        var arama = TxtKategoriAra.Text.Trim();
        var gruplar = Kayitlar
            .GroupBy(k => string.IsNullOrWhiteSpace(k.Kategori) ? "Genel" : k.Kategori, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .ToList();

        KategoriAgaci.Items.Clear();
        var tumu = new TreeViewItem { Header = $"Tüm Kategoriler ({Kayitlar.Count})", Tag = "" };
        KategoriAgaci.Items.Add(tumu);

        foreach (var grup in gruplar)
        {
            if (!string.IsNullOrEmpty(arama) &&
                !grup.Key.Contains(arama, StringComparison.OrdinalIgnoreCase))
                continue;

            KategoriAgaci.Items.Add(new TreeViewItem
            {
                Header = $"{grup.Key} ({grup.Count()})",
                Tag = grup.Key
            });
        }

        tumu.IsSelected = _agacKategori is null;
    }

    private void KategoriAgaci_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (KategoriAgaci.SelectedItem is TreeViewItem oge)
        {
            _agacKategori = oge.Tag as string;
            if (_agacKategori == "")
                _agacKategori = null;
            StokFiltreYenile();
        }
    }

    private void KategoriAraDegisti(object sender, TextChangedEventArgs e) =>
        KategoriAgaciniGuncelle();

    #region Navigasyon

    private void SekmeYetkileriniUygula()
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        BtnNavDurum.Visibility = DesktopRoleTabManager.StockTabVisible(rol, StokRoutes.StokDurumu)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnNavHareket.Visibility = DesktopRoleTabManager.StockTabVisible(rol, StokRoutes.StokHareketleri)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnNavSayim.Visibility = DesktopRoleTabManager.StockTabVisible(rol, StokRoutes.StokSayim)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void StokAksiyonlariniAyarla()
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        BtnDepoTopluGiris.Visibility = KullaniciYetkileri.AdminMi ? Visibility.Visible : Visibility.Collapsed;
        BtnStokGiris.Visibility = DesktopRoleTabManager.StockTabVisible(rol, StokRoutes.StokGirisi)
            && DesktopRoleTabManager.StockCanWrite(rol)
            ? Visibility.Visible : Visibility.Collapsed;
        BtnStokCikis.Visibility = DesktopRoleTabManager.StockTabVisible(rol, StokRoutes.StokCikisi)
            && DesktopRoleTabManager.StockCanWrite(rol)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private static Visibility SekmeGorunur(string sekme) =>
        KullaniciYetkileri.SekmeGorebilir("Stok Yönetimi", sekme) ? Visibility.Visible : Visibility.Collapsed;

    private void NavAktif(Button aktif)
    {
        BtnNavDurum.Style = (Style)FindResource(aktif == BtnNavDurum ? "StokNavActiveStyle" : "ErpNavPill");
        BtnNavHareket.Style = (Style)FindResource(aktif == BtnNavHareket ? "StokNavActiveStyle" : "ErpNavPill");
        BtnNavSayim.Style = (Style)FindResource(aktif == BtnNavSayim ? "StokNavActiveStyle" : "ErpNavPill");
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

    public void BildirimdenAc(string? sekme)
    {
        var hedef = sekme?.Trim().ToLowerInvariant() switch
        {
            "stok-hareket" or "stok hareketleri" => BtnNavHareket,
            "stok-sayim" or "stok sayım" or "stok sayim" => BtnNavSayim,
            _ => BtnNavDurum
        };

        if (hedef.Visibility != Visibility.Visible)
            hedef = BtnNavDurum.Visibility == Visibility.Visible ? BtnNavDurum
                : BtnNavHareket.Visibility == Visibility.Visible ? BtnNavHareket
                : BtnNavSayim;

        if (hedef == BtnNavDurum)
            NavDurum_Click(hedef, new RoutedEventArgs());
        else if (hedef == BtnNavHareket)
            NavHareket_Click(hedef, new RoutedEventArgs());
        else
            NavSayim_Click(hedef, new RoutedEventArgs());
    }

    private void DepoTopluGiris_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.AdminMi)
        {
            MessageBox.Show("Bu işlem yalnızca admin kullanıcılar içindir.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pencere = new DepoTopluGirisWindow { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() == true)
            TumunuYenile();
    }

    private void StokGiris_Click(object sender, RoutedEventArgs e)
    {
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

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
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

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
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

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
        _stokGorunum.SortDescriptions.Clear();
        _stokGorunum.Refresh();

        var filtrelenmis = ModulSayfalamaYardimcisi.FiltrelenmisListe<StokKaydi>(_stokGorunum);
        if (string.IsNullOrEmpty(_stokGrupAlani))
        {
            _stokSayfalama.KaynakGuncelle(
                filtrelenmis, k => ModulSayfalamaYardimcisi.TarihSira(k.SonGuncelleme), ilkSayfayaDon: true);
        }
        else
        {
            _stokSayfalama.SiraliKaynakGuncelle(StokGruplanmisListe(filtrelenmis, _stokGrupAlani), ilkSayfayaDon: true);
        }

        StokSayfalamaBar.Guncelle(_stokSayfalama.GuncelSayfa, _stokSayfalama.ToplamSayfa, _stokSayfalama.ToplamKayit);
        StokGrupBilgiGuncelle(filtrelenmis);
        OzetGuncelle();
    }

    private static List<StokKaydi> StokGruplanmisListe(IEnumerable<StokKaydi> kaynak, string alan) =>
        kaynak
            .OrderBy(k => StokGrupAnahtari(k, alan), StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(k => ModulSayfalamaYardimcisi.TarihSira(k.SonGuncelleme))
            .ToList();

    private static string StokGrupAnahtari(StokKaydi kayit, string alan) => alan switch
    {
        "Kategori" => kayit.Kategori ?? "",
        "DepoSaha" => kayit.DepoSaha ?? "",
        "DurumMetin" => kayit.DurumMetin,
        "Birim" => kayit.Birim ?? "",
        _ => ""
    };

    private void StokGrupBilgiGuncelle(IReadOnlyList<StokKaydi> filtrelenmis)
    {
        if (string.IsNullOrEmpty(_stokGrupAlani))
        {
            TxtStokGrupBilgi.Visibility = Visibility.Collapsed;
            return;
        }

        var baslik = StokGrupSecenekleri.FirstOrDefault(g => g.Alan == _stokGrupAlani).Baslik;
        if (string.IsNullOrEmpty(baslik))
            baslik = _stokGrupAlani;

        var grupSayisi = filtrelenmis
            .Select(k => StokGrupAnahtari(k, _stokGrupAlani))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        TxtStokGrupBilgi.Text = $"· Gruplama: {baslik} ({grupSayisi} grup)";
        TxtStokGrupBilgi.Visibility = Visibility.Visible;
    }

    private void StokKolonlar_Click(object sender, RoutedEventArgs e) =>
        ErpDataGridYardimcisi.KolonSeciminiGoster(StokGrid, Window.GetWindow(this));

    private void StokGrupla_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement hedef)
            return;

        ErpDataGridYardimcisi.GruplaMenusunuGoster(hedef, StokGrupSecenekleri, _stokGrupAlani, alan =>
        {
            _stokGrupAlani = alan;
            StokFiltreYenile();
        });
    }

    private void StokFiltreOdak_Click(object sender, RoutedEventArgs e)
    {
        ErpDataGridYardimcisi.FiltrePanelineOdakla(StokFiltreKart);
        TxtArama.Focus();
    }

    private void StokYogunGorunum_Click(object sender, RoutedEventArgs e) =>
        ErpDataGridYardimcisi.YogunGorunumToggle(StokGrid, ref _stokYogunGorunum);

    private void StokTamEkran_Click(object sender, RoutedEventArgs e)
    {
        _stokTamEkran = !_stokTamEkran;

        if (_stokTamEkran)
        {
            _solPanelGenislik = PanelDurum.ColumnDefinitions[0].Width;
            _araBoslukGenislik = PanelDurum.ColumnDefinitions[1].Width;
            PanelDurum.ColumnDefinitions[0].Width = new GridLength(0);
            PanelDurum.ColumnDefinitions[1].Width = new GridLength(0);
            Grid.SetColumn(StokIcerikGrid, 0);
            Grid.SetColumnSpan(StokIcerikGrid, 3);

            _stokTabloOrijinalSatir = Grid.GetRow(StokTabloKart);
            _stokTabloSatirYukseklik = StokIcerikGrid.RowDefinitions[2].Height;
            StokKpiScroll.Visibility = Visibility.Collapsed;
            StokFiltreKart.Visibility = Visibility.Collapsed;
            StokIcerikGrid.RowDefinitions[0].Height = new GridLength(0);
            StokIcerikGrid.RowDefinitions[1].Height = new GridLength(0);
            Grid.SetRow(StokTabloKart, 0);
            Grid.SetRowSpan(StokTabloKart, 3);
            StokIcerikGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            BtnStokTamEkran.Content = "Çıkış";
        }
        else
        {
            PanelDurum.ColumnDefinitions[0].Width = _solPanelGenislik;
            PanelDurum.ColumnDefinitions[1].Width = _araBoslukGenislik;
            Grid.SetColumn(StokIcerikGrid, 2);
            Grid.SetColumnSpan(StokIcerikGrid, 1);

            StokKpiScroll.Visibility = Visibility.Visible;
            StokFiltreKart.Visibility = Visibility.Visible;
            StokIcerikGrid.RowDefinitions[0].Height = GridLength.Auto;
            StokIcerikGrid.RowDefinitions[1].Height = GridLength.Auto;
            Grid.SetRow(StokTabloKart, _stokTabloOrijinalSatir);
            Grid.SetRowSpan(StokTabloKart, 1);
            StokIcerikGrid.RowDefinitions[2].Height = _stokTabloSatirYukseklik;
            BtnStokTamEkran.Content = "Tam Ekran";
        }
    }

    private void StokSayfaBoyutuDegisti(object sender, SelectionChangedEventArgs e)
    {
        if (!_arayuzHazir)
            return;

        if (CmbStokSayfaBoyutu.SelectedItem is not ComboBoxItem { Tag: string tag })
            return;

        if (!int.TryParse(tag, out var boyut) || boyut == _stokSayfalama.SayfaBoyutu)
            return;

        if (string.IsNullOrEmpty(_stokGrupAlani))
            _stokSayfalama.SayfaBoyutunuAyarla(boyut, k => ModulSayfalamaYardimcisi.TarihSira(k.SonGuncelleme));
        else
            _stokSayfalama.SayfaBoyutunuDegistir(boyut, ilkSayfayaDon: true);

        StokSayfalamaBar.Guncelle(_stokSayfalama.GuncelSayfa, _stokSayfalama.ToplamSayfa, _stokSayfalama.ToplamKayit);
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

        if (_agacKategori is not null &&
            !kayit.Kategori.Equals(_agacKategori, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private void StokGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is not StokKaydi kayit)
            return;

        e.Row.Background = kayit.DurumMetin switch
        {
            "Tükendi" => new SolidColorBrush(Color.FromRgb(241, 245, 249)),
            "Kritik" => new SolidColorBrush(Color.FromRgb(254, 226, 226)),
            "Düşük" => new SolidColorBrush(Color.FromRgb(254, 243, 199)),
            _ => KategoriRenkleri.GetFirca(kayit.Kategori)
        };
    }

    private void StokGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void StokSatirMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: StokKaydi kayit })
            return;

        StokGrid.SelectedItem = kayit;
        var menu = new ContextMenu { PlacementTarget = sender as UIElement, Placement = PlacementMode.Bottom };
        EkleMenu(menu, "Görüntüle", () => KaydiDuzenle(kayit));
        EkleMenu(menu, "Düzenle", () => KaydiDuzenle(kayit));
        EkleMenu(menu, "Geçmişi Göster", () => StokGecmisiGoster(kayit));
        menu.Items.Add(new Separator());
        EkleMenu(menu, "Sil", () =>
        {
            StokGrid.SelectedItem = kayit;
            KayitSil_Click(sender, e);
        });
        menu.IsOpen = true;
    }

    private void StokGecmisiGoster(StokKaydi kayit)
    {
        var baslik = string.IsNullOrWhiteSpace(kayit.MalzemeAdi)
            ? "Stok kayıt özeti"
            : $"{kayit.MalzemeAdi} — kayıt özeti";

        var pencere = new ErpKayitGecmisiWindow(baslik, StokGecmisiSatirlari(kayit))
        {
            Owner = Window.GetWindow(this)
        };
        pencere.ShowDialog();
    }

    private static IEnumerable<string> StokGecmisiSatirlari(StokKaydi kayit)
    {
        yield return $"Stok adı: {kayit.MalzemeAdi}";
        yield return $"Kategori: {kayit.Kategori}";
        yield return $"Depo / saha: {kayit.DepoSaha}";
        yield return $"Mevcut miktar: {kayit.MevcutMiktar:N2} {kayit.Birim}";
        yield return $"Minimum stok: {kayit.MinimumStok:N2}";
        yield return $"Birim maliyet: ₺{kayit.BirimMaliyet:N2}";
        yield return $"Toplam değer: ₺{kayit.ToplamDeger:N2}";
        yield return $"Durum: {kayit.DurumRozetMetin}";
        yield return $"Son güncelleme: {kayit.SonGuncelleme}";
        if (!string.IsNullOrWhiteSpace(kayit.Aciklama))
            yield return $"Açıklama: {kayit.Aciklama}";
    }

    private static void EkleMenu(ContextMenu menu, string baslik, Action action)
    {
        var item = new MenuItem { Header = baslik };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private void StokGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (StokGrid.SelectedItem is StokKaydi kayit)
            KaydiDuzenle(kayit);
    }

    private void KayitDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

        if (StokGrid.SelectedItem is StokKaydi kayit)
            KaydiDuzenle(kayit);
    }

    private void KayitSil_Click(object sender, RoutedEventArgs e)
    {
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

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
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

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
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

        if (HareketGrid.SelectedItem is not StokHareketKaydi hareket)
            return;

        var pencere = new StokHareketDuzenleWindow(hareket) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        TumunuYenile();
    }

    private void HareketSil_Click(object sender, RoutedEventArgs e)
    {
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

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
        if (KullaniciYetkileri.StokYazmaIslemiEngellendi())
            return;

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
        KategoriAgaciniGuncelle();

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
        var toplamMiktar = gorunen.Sum(k => k.MevcutMiktar);
        var toplamDeger = gorunen.Sum(k => k.ToplamDeger);
        var kritik = Kayitlar.Count(k => k.DurumMetin == "Kritik");
        var minimumAlti = Kayitlar.Count(k => k.DurumMetin is "Kritik" or "Düşük");
        var pasif = Kayitlar.Count(k => k.DurumMetin == "Tükendi");

        StokKpiGuncelle("Toplam Stok Kalemi", Kayitlar.Count.ToString("N0"));
        StokKpiGuncelle("Toplam Stok Miktarı", $"{toplamMiktar:N1}");
        StokKpiGuncelle("Toplam Stok Değeri", $"₺{toplamDeger:N0}");
        StokKpiGuncelle("Kritik Stok", kritik.ToString("N0"));
        StokKpiGuncelle("Minimum Altı", minimumAlti.ToString("N0"));
        StokKpiGuncelle("Pasif Ürün", pasif.ToString("N0"));

        TxtStokTabloBaslik.Text = $"Stok Listesi ({gorunen.Count:N0} kayıt)";
        TxtAltKalem.Text = gorunen.Count.ToString("N0");
        TxtAltDeger.Text = $"₺{toplamDeger:N0}";
    }
}
