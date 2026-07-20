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
using SatinalmaPro.Views.Controls;
using Microsoft.Win32;

namespace SatinalmaPro.Views.Modules;

public partial class AlinanMalzemelerView : UserControl, IModulKlavyeKisayollari
{
    private readonly ICollectionView _gorunum;
    private readonly FiltreZamanlayici _filtreZamanlayici;
    private readonly ModulSayfalamaYoneticisi<AlinanMalzemeKaydi> _sayfalama = new();
    private bool _hesaplamaBekliyor;
    private bool _filtreAcik;
    private bool _tamEkran;
    private bool _yogunGorunum;
    private bool _arayuzHazir;
    private string? _grupAlani;
    private readonly Dictionary<string, TextBlock> _kpiMetinleri = new(StringComparer.Ordinal);

    private static readonly (string Baslik, string Alan)[] GrupSecenekleri =
    [
        ("Kategori", "Kategori"),
        ("Tedarikçi", "Tedarikci"),
        ("Şantiye", "IndirildigiSaha"),
        ("Teslim Alan", "TeslimAlan")
    ];

    public ObservableCollection<AlinanMalzemeKaydi> Kayitlar => UygulamaVeriDeposu.AlinanMalzemeler;

    public AlinanMalzemelerView()
    {
        InitializeComponent();
        DataContext = this;
        _filtreZamanlayici = new FiltreZamanlayici(FiltreYenile);

        _gorunum = CollectionViewSource.GetDefaultView(Kayitlar);
        _gorunum.Filter = KayitFiltresi;
        MalzemeGrid.ItemsSource = _sayfalama.SayfaKayitlari;
        ErpDataGridYardimcisi.PremiumGridAyarla(MalzemeGrid);
        ModulSayfalamaYardimcisi.CubukBagla(_sayfalama, SayfalamaBar);

        MalzemeFiltre.OneriKaynaginiAyarla(MalzemeAdiOneriServisi.Ara);
        MalzemeFiltre.MetinYazildi += (_, _) => _filtreZamanlayici.Tetikle();

        Loaded += (_, _) =>
        {
            UygulamaVeriDeposu.OrnekVeriyiYukle();
            KullaniciYetkileri.ModulErisiminiUygula(this, "Alınan Malzemeler");
            MalzemeKategoriDeposu.KayitlardanSenkronizeEt();
            FiltreCombolariGuncelle();
            KpiOlustur();
            SayfalamayiYenile();
            OzetGuncelle();
            _arayuzHazir = true;
            _ = HesaplamalariArkaPlandaYapAsync();
        };
    }

    private async Task HesaplamalariArkaPlandaYapAsync()
    {
        if (_hesaplamaBekliyor)
            return;

        _hesaplamaBekliyor = true;
        try
        {
            var kayitlar = Kayitlar.ToList();
            await Task.Run(() =>
            {
                foreach (var kayit in kayitlar)
                    kayit.ToplamTutariHesapla();
                AlinanMalzemeArtisHesaplayici.Hesapla(kayitlar);
            });

            await Dispatcher.InvokeAsync(() =>
            {
                SayfalamayiYenile();
                OzetGuncelle();
            });
        }
        finally
        {
            _hesaplamaBekliyor = false;
        }
    }

    #region UI — KPI & Filtre paneli

    private void KpiOlustur()
    {
        KpiPanel.Children.Clear();
        _kpiMetinleri.Clear();
        var kpiler = new (string Baslik, string Deger, string Alt, string Renk, string Ikon)[]
        {
            ("Toplam Kayıt", "0", "Modüldeki tüm kayıtlar", "#2563EB", "\uE8F1"),
            ("Toplam Tutar", "₺0", "Genel toplam tutar", "#8B5CF6", "\uE8C7"),
            ("Aylık Ortalama Tutar", "₺0", "Ay bazında ortalama", "#16A34A", "\uE787"),
            ("Filtreye Göre Tutar", "₺0", "Aktif filtre sonucu", "#0891B2", "\uE71C"),
            ("En Çok Alınan Malzeme", "—", "Kalem sayısına göre", "#F59E0B", "\uE7BF")
        };

        foreach (var kpi in kpiler)
        {
            var renk = (Color)ColorConverter.ConvertFromString(kpi.Renk)!;
            var degerTb = new TextBlock
            {
                Text = kpi.Deger,
                Style = (Style)FindResource("ErpKpiValue"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = kpi.Deger
            };
            var kart = new Border { Style = (Style)FindResource("ErpKpiCard") };
            kart.Child = new StackPanel
            {
                Children =
                {
                    new Border
                    {
                        Width = 36, Height = 36, CornerRadius = new CornerRadius(10),
                        Background = new SolidColorBrush(renk) { Opacity = 0.12 },
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Child = new TextBlock
                        {
                            Text = kpi.Ikon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 16,
                            Foreground = new SolidColorBrush(renk),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new TextBlock { Text = kpi.Baslik, Style = (Style)FindResource("ErpKpiLabel"), Margin = new Thickness(0, 10, 0, 0) },
                    degerTb,
                    new TextBlock { Text = kpi.Alt, Style = (Style)FindResource("ErpKpiHint") }
                }
            };
            _kpiMetinleri[kpi.Baslik] = degerTb;
            KpiPanel.Children.Add(kart);
        }
    }

    private void KpiGuncelle(string baslik, string deger)
    {
        if (_kpiMetinleri.TryGetValue(baslik, out var tb))
            tb.Text = deger;
    }

    private void FiltreToggle_Click(object sender, RoutedEventArgs e) =>
        ErpDataGridYardimcisi.FiltrePaneliToggle(FilterIcerik, BtnFiltreToggle, ref _filtreAcik);

    private void FiltreYenile_Click(object sender, RoutedEventArgs e) => FiltreYenile();

    private void Kolonlar_Click(object sender, RoutedEventArgs e) =>
        ErpDataGridYardimcisi.KolonSeciminiGoster(MalzemeGrid, Window.GetWindow(this));

    private void Grupla_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement hedef)
            return;

        ErpDataGridYardimcisi.GruplaMenusunuGoster(hedef, GrupSecenekleri, _grupAlani, alan =>
        {
            _grupAlani = alan;
            SayfalamayiYenile(ilkSayfayaDon: true);
            OzetGuncelle();
        });
    }

    private void FiltreOdak_Click(object sender, RoutedEventArgs e)
    {
        if (!_filtreAcik)
            ErpDataGridYardimcisi.FiltrePaneliToggle(FilterIcerik, BtnFiltreToggle, ref _filtreAcik);

        ErpDataGridYardimcisi.FiltrePanelineOdakla(FiltreBaslikKart);
        TxtArama.Focus();
    }

    private void YogunGorunum_Click(object sender, RoutedEventArgs e) =>
        ErpDataGridYardimcisi.YogunGorunumToggle(MalzemeGrid, ref _yogunGorunum);

    private void TamEkran_Click(object sender, RoutedEventArgs e) =>
        ErpDataGridYardimcisi.TabloTamEkranToggle(AnaIcerikGrid, TabloKart, 4, [0, 1, 2, 3], ref _tamEkran, BtnTamEkran);

    private void SayfaBoyutuDegisti(object sender, SelectionChangedEventArgs e)
    {
        if (!_arayuzHazir)
            return;

        if (CmbSayfaBoyutu.SelectedItem is not ComboBoxItem { Tag: string tag })
            return;

        if (!int.TryParse(tag, out var boyut) || boyut == _sayfalama.SayfaBoyutu)
            return;

        if (string.IsNullOrEmpty(_grupAlani))
            _sayfalama.SayfaBoyutunuAyarla(boyut, k => ModulSayfalamaYardimcisi.TarihSira(k.Tarih));
        else
            _sayfalama.SayfaBoyutunuDegistir(boyut, ilkSayfayaDon: true);

        SayfalamaBar.Guncelle(_sayfalama.GuncelSayfa, _sayfalama.ToplamSayfa, _sayfalama.ToplamKayit);
    }

    #endregion

    #region Araç çubuğu

    private void ExcelYukle_Click(object sender, RoutedEventArgs e)
    {
        if (KullaniciYetkileri.YazmaIslemiEngellendi("Alınan Malzemeler"))
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Excel Dosyası Seç",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var yeniKayitlar = AlinanMalzemeExcelService.DosyadanOku(dialog.FileName);
            if (yeniKayitlar.Count == 0)
            {
                MessageBox.Show("Dosyada aktarılacak kayıt bulunamadı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var secim = MessageBox.Show(
                $"{yeniKayitlar.Count} satır okundu.\n\n" +
                "Evet = Boş tedarikçileri Excel'den doldur (çift kayıt oluşturmaz)\n" +
                "Hayır = Yeni kayıt olarak ekle\n" +
                "İptal = Vazgeç\n\n" +
                "Öneri: Daha önce yüklediğiniz kayıtların tedarikçisi boşsa «Evet» seçin. Modül sıfırlamayın.",
                "Excel Yükleme",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (secim == MessageBoxResult.Cancel)
                return;

            if (secim == MessageBoxResult.Yes)
            {
                ModulVeriDeposu.BeginBatch();
                int guncellenen;
                try
                {
                    foreach (var kayit in yeniKayitlar)
                        kayit.Tarih = TarihYardimcisi.Normalize(kayit.Tarih);

                    guncellenen = AlinanMalzemeExcelService.BosTedarikcileriGuncelle(Kayitlar, yeniKayitlar);
                }
                finally
                {
                    ModulVeriDeposu.EndBatch();
                }

                VeriGuncellendi();
                MessageBox.Show(
                    guncellenen > 0
                        ? $"{guncellenen} kaydın tedarikçi bilgisi Excel'den güncellendi."
                        : "Eşleşen ve tedarikçisi boş kayıt bulunamadı. Excel'de tarih / malzeme / miktar / birim fiyat aynı olmalı.",
                    UygulamaBilgisi.Ad,
                    MessageBoxButton.OK,
                    guncellenen > 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
                return;
            }

            ModulVeriDeposu.BeginBatch();
            var stokGirisSayisi = 0;
            try
            {
                foreach (var kayit in yeniKayitlar)
                {
                    kayit.Tarih = TarihYardimcisi.Normalize(kayit.Tarih);
                    Kayitlar.Add(kayit);
                    if (AlinanMalzemeAktarimServisi.ExcelKayittanStokGiris(kayit) is not null)
                        stokGirisSayisi++;
                }
            }
            finally
            {
                ModulVeriDeposu.EndBatch();
            }

            ModulVeriDeposu.KaydetAlinanMalzemeler();
            ModulVeriDeposu.KaydetStok();
            ModulVeriDeposu.KaydetStokHareketleri();

            VeriGuncellendi();
            MessageBox.Show(
                $"{yeniKayitlar.Count} kayıt içe aktarıldı.\n{stokGirisSayisi} kalem için stok giriş hareketi oluşturuldu (alınan malzeme tarihine göre).",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Excel okunamadı:\n{ex.Message}", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SablonIndir_Click(object sender, RoutedEventArgs e) =>
        AlinanMalzemeExcelService.SablonKaydet();

    private void PdfIndir_Click(object sender, RoutedEventArgs e) =>
        AlinanMalzemePdfOlusturucu.Indir(GorunenKayitlar(), FiltreOzetiMetni());

    private void PdfYazdir_Click(object sender, RoutedEventArgs e) =>
        AlinanMalzemePdfOlusturucu.Yazdir(GorunenKayitlar(), FiltreOzetiMetni());

    private void YeniKayit_Click(object sender, RoutedEventArgs e)
    {
        var yeni = new AlinanMalzemeKaydi
        {
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            Birim = "Ton"
        };

        var pencere = new AlinanMalzemeDuzenleWindow(yeni) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        Kayitlar.Add(yeni);
        VeriGuncellendi();
    }

    private void DisaAktar_Click(object sender, RoutedEventArgs e) =>
        AlinanMalzemeExcelService.ListeyiKaydet(GorunenKayitlar(), "AlinanMalzemeler.xlsx");

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();

    #endregion

    #region Filtreler

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
        DpBaslangic.SelectedDate = null;
        DpBitis.SelectedDate = null;
        MalzemeFiltre.MetniTemizle();
        CmbKategori.SelectedIndex = 0;
        CmbTedarikci.SelectedIndex = 0;
        CmbSantiye.SelectedIndex = 0;
        CmbTeslimAlan.SelectedIndex = 0;
        FiltreYenile();
    }

    private void FiltreYenile()
    {
        SayfalamayiYenile(ilkSayfayaDon: true);
        OzetGuncelle();
    }

    private void SayfalamayiYenile(bool ilkSayfayaDon = false)
    {
        _gorunum.SortDescriptions.Clear();
        _gorunum.Refresh();

        var filtrelenmis = ModulSayfalamaYardimcisi.FiltrelenmisListe<AlinanMalzemeKaydi>(_gorunum);
        if (string.IsNullOrEmpty(_grupAlani))
        {
            _sayfalama.KaynakGuncelle(filtrelenmis, k => ModulSayfalamaYardimcisi.TarihSira(k.Tarih), ilkSayfayaDon);
        }
        else
        {
            _sayfalama.SiraliKaynakGuncelle(GruplanmisListe(filtrelenmis, _grupAlani), ilkSayfayaDon);
        }

        SayfalamaBar.Guncelle(_sayfalama.GuncelSayfa, _sayfalama.ToplamSayfa, _sayfalama.ToplamKayit);
        GrupBilgiGuncelle(filtrelenmis);
    }

    private static List<AlinanMalzemeKaydi> GruplanmisListe(IEnumerable<AlinanMalzemeKaydi> kaynak, string alan) =>
        kaynak
            .OrderBy(k => GrupAnahtari(k, alan), StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(k => ModulSayfalamaYardimcisi.TarihSira(k.Tarih))
            .ToList();

    private static string GrupAnahtari(AlinanMalzemeKaydi kayit, string alan) => alan switch
    {
        "Kategori" => kayit.Kategori ?? "",
        "Tedarikci" => kayit.Tedarikci ?? "",
        "IndirildigiSaha" => kayit.IndirildigiSaha ?? "",
        "TeslimAlan" => kayit.TeslimAlan ?? "",
        _ => ""
    };

    private void GrupBilgiGuncelle(IReadOnlyList<AlinanMalzemeKaydi> filtrelenmis)
    {
        if (string.IsNullOrEmpty(_grupAlani))
        {
            TxtGrupBilgi.Visibility = Visibility.Collapsed;
            return;
        }

        var baslik = GrupSecenekleri.FirstOrDefault(g => g.Alan == _grupAlani).Baslik;
        if (string.IsNullOrEmpty(baslik))
            baslik = _grupAlani;

        var grupSayisi = filtrelenmis
            .Select(k => GrupAnahtari(k, _grupAlani))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        TxtGrupBilgi.Text = $"· Gruplama: {baslik} ({grupSayisi} grup)";
        TxtGrupBilgi.Visibility = Visibility.Visible;
    }

    public void KisayolYenile()
    {
        MalzemeAdiOneriServisi.OnbellekSifirla();
        FiltreCombolariGuncelle();
        _ = HesaplamalariArkaPlandaYapAsync();
        FiltreYenile();
    }

    private void VeriGuncellendi()
    {
        MalzemeAdiOneriServisi.OnbellekSifirla();
        MalzemeKategoriDeposu.KayitlardanSenkronizeEt();
        foreach (var kayit in Kayitlar)
            kayit.Tarih = TarihYardimcisi.Normalize(kayit.Tarih);

        FiltreCombolariGuncelle();
        _ = HesaplamalariArkaPlandaYapAsync();
        FiltreYenile();
    }

    private bool KayitFiltresi(object item)
    {
        if (item is not AlinanMalzemeKaydi kayit)
            return false;

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama) && !AramaEslesir(kayit, arama))
            return false;

        if (DpBaslangic.SelectedDate is DateTime || DpBitis.SelectedDate is DateTime)
        {
            if (!DateTime.TryParseExact(kayit.Tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarih))
                return false;

            if (DpBaslangic.SelectedDate is DateTime b && tarih.Date < b.Date)
                return false;

            if (DpBitis.SelectedDate is DateTime bit && tarih.Date > bit.Date)
                return false;
        }

        var malzeme = (MalzemeFiltre.Metin ?? "").Trim();
        if (!string.IsNullOrEmpty(malzeme) &&
            !kayit.MalzemeHizmet.Contains(malzeme, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!ComboEslesir(CmbKategori, kayit.Kategori))
            return false;

        if (!ComboEslesir(CmbTedarikci, kayit.Tedarikci))
            return false;

        if (!ComboEslesir(CmbSantiye, kayit.IndirildigiSaha))
            return false;

        if (!ComboEslesir(CmbTeslimAlan, kayit.TeslimAlan))
            return false;

        return true;
    }

    private static bool ComboEslesir(ComboBox combo, string? deger)
    {
        var secim = ComboDegeri(combo);
        return string.IsNullOrEmpty(secim) || secim == "Tümü"
            || (deger ?? "").Equals(secim, StringComparison.OrdinalIgnoreCase);
    }

    private static bool AramaEslesir(AlinanMalzemeKaydi kayit, string arama) =>
        (kayit.Tarih?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.FaturaNo?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.Kategori?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.Tedarikci?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.IndirildigiSaha?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.TeslimAlan?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.Aciklama?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (kayit.MalzemeHizmet?.Contains(arama, StringComparison.OrdinalIgnoreCase) ?? false);

    private static string? ComboDegeri(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private void FiltreCombolariGuncelle()
    {
        var seciliKategori = ComboDegeri(CmbKategori);
        var seciliTedarikci = ComboDegeri(CmbTedarikci);
        var seciliSantiye = ComboDegeri(CmbSantiye);
        var seciliTeslim = ComboDegeri(CmbTeslimAlan);

        ComboDoldur(CmbKategori, MalzemeKategoriDeposu.TumListe(), seciliKategori);
        ComboDoldur(CmbTedarikci, Kayitlar.Select(k => k.Tedarikci), seciliTedarikci);
        ComboDoldur(CmbSantiye, Kayitlar.Select(k => k.IndirildigiSaha), seciliSantiye);
        ComboDoldur(CmbTeslimAlan, Kayitlar.Select(k => k.TeslimAlan), seciliTeslim);
    }

    private static void ComboDoldur(ComboBox combo, IEnumerable<string> degerler, string? secili)
    {
        combo.Items.Clear();
        combo.Items.Add(new ComboBoxItem { Content = "Tümü" });
        foreach (var d in degerler.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
            combo.Items.Add(new ComboBoxItem { Content = d });
        combo.SelectedIndex = IndexBul(combo, secili);
    }

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

    #endregion

    #region Tablo işlemleri

    private void MalzemeGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void MalzemeGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!KullaniciYetkileri.ModulYazabilir("Alınan Malzemeler"))
            return;

        if (SeciliKayit() is { } kayit)
            KaydiDuzenle(kayit);
    }

    private void MalzemeGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is AlinanMalzemeKaydi kayit)
            e.Row.Background = KategoriRenkleri.GetFirca(kayit.Kategori);
    }

    private void SatirMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AlinanMalzemeKaydi kayit })
            return;

        MalzemeGrid.SelectedItem = kayit;
        var menu = new ContextMenu { PlacementTarget = sender as UIElement, Placement = PlacementMode.Bottom };
        EkleMenu(menu, "Görüntüle", () =>
        {
            var pencere = new AlinanMalzemeDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
            pencere.ShowDialog();
        });
        EkleMenu(menu, "Düzenle", () => KaydiDuzenle(kayit));
        EkleMenu(menu, "Kopyala", () => KaydiKopyala(kayit));
        EkleMenu(menu, "Geçmişi Göster", () => GecmisiGoster(kayit));
        menu.Items.Add(new Separator());
        EkleMenu(menu, "PDF", () => AlinanMalzemePdfOlusturucu.Indir([kayit], kayit.MalzemeHizmet));
        EkleMenu(menu, "Yazdır", () => AlinanMalzemePdfOlusturucu.Yazdir([kayit], kayit.MalzemeHizmet));
        EkleMenu(menu, "Sil", () => KaydiSil(kayit));
        menu.IsOpen = true;
    }

    private void GecmisiGoster(AlinanMalzemeKaydi kayit)
    {
        var baslik = string.IsNullOrWhiteSpace(kayit.MalzemeHizmet)
            ? "Kayıt geçmişi"
            : $"{kayit.MalzemeHizmet} — kayıt özeti";

        var pencere = new ErpKayitGecmisiWindow(baslik, KayitGecmisiSatirlari(kayit))
        {
            Owner = Window.GetWindow(this)
        };
        pencere.ShowDialog();
    }

    private static IEnumerable<string> KayitGecmisiSatirlari(AlinanMalzemeKaydi kayit)
    {
        yield return $"Tarih: {kayit.Tarih}";
        yield return $"İrsaliye / Fatura No: {(string.IsNullOrWhiteSpace(kayit.FaturaNo) ? "—" : kayit.FaturaNo)}";
        yield return $"Malzeme: {kayit.MalzemeHizmet}";
        yield return $"Kategori: {kayit.Kategori}";
        yield return $"Miktar: {kayit.Miktar:N2} {kayit.Birim}";
        yield return $"Birim fiyat: ₺{kayit.BirimFiyati:N2}";
        yield return $"Toplam: ₺{kayit.ToplamTutar:N2}";
        yield return $"Tedarikçi: {kayit.Tedarikci}";
        yield return $"Şantiye: {kayit.IndirildigiSaha}";
        yield return $"Teslim alan: {(string.IsNullOrWhiteSpace(kayit.TeslimAlan) ? "—" : kayit.TeslimAlan)}";
        if (!string.IsNullOrWhiteSpace(kayit.Aciklama))
            yield return $"Açıklama: {kayit.Aciklama}";
        if (kayit.SatinalmaTalepId is Guid talepId)
            yield return $"Satınalma talebi: {talepId}";
    }

    private static void EkleMenu(ContextMenu menu, string baslik, Action action)
    {
        var item = new MenuItem { Header = baslik };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private void KaydiKopyala(AlinanMalzemeKaydi kayit)
    {
        if (KullaniciYetkileri.YazmaIslemiEngellendi("Alınan Malzemeler"))
            return;

        var kopya = kayit.Kopyala();
        kopya.FaturaNo = "";
        Kayitlar.Add(kopya);
        VeriGuncellendi();
    }

    private void KaydiSil(AlinanMalzemeKaydi kayit)
    {
        if (KullaniciYetkileri.YazmaIslemiEngellendi("Alınan Malzemeler"))
            return;

        if (MessageBox.Show($"{kayit.FaturaNo} numaralı kaydı silmek istiyor musunuz?", "Kayıt Sil",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Kayitlar.Remove(kayit);
        VeriGuncellendi();
    }

    private AlinanMalzemeKaydi? SeciliKayit() =>
        MalzemeGrid.SelectedItem as AlinanMalzemeKaydi;

    private void KaydiDuzenle(AlinanMalzemeKaydi kayit)
    {
        if (KullaniciYetkileri.YazmaIslemiEngellendi("Alınan Malzemeler"))
            return;

        var pencere = new AlinanMalzemeDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        VeriGuncellendi();
    }

    #endregion

    private List<AlinanMalzemeKaydi> GorunenKayitlar() =>
        _gorunum.Cast<AlinanMalzemeKaydi>().ToList();

    private string FiltreOzetiMetni()
    {
        var parcalar = new List<string>();

        if (DpBaslangic.SelectedDate is DateTime baslangic)
            parcalar.Add($"Başlangıç: {baslangic:dd.MM.yyyy}");
        if (DpBitis.SelectedDate is DateTime bitis)
            parcalar.Add($"Bitiş: {bitis:dd.MM.yyyy}");

        var malzeme = (MalzemeFiltre.Metin ?? "").Trim();
        if (!string.IsNullOrEmpty(malzeme))
            parcalar.Add($"Malzeme: {malzeme}");

        var kategori = ComboDegeri(CmbKategori);
        if (!string.IsNullOrEmpty(kategori) && kategori != "Tümü")
            parcalar.Add($"Kategori: {kategori}");

        var tedarikci = ComboDegeri(CmbTedarikci);
        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü")
            parcalar.Add($"Tedarikçi: {tedarikci}");

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
            parcalar.Add($"Arama: {arama}");

        return parcalar.Count == 0 ? "Tüm kayıtlar" : string.Join(" · ", parcalar);
    }

    private void OzetGuncelle()
    {
        var tumu = Kayitlar.ToList();
        var gorunen = GorunenKayitlar();
        var gorunenSayi = gorunen.Count;
        var toplamMiktar = gorunen.Sum(k => k.Miktar);
        var toplamTutarTumu = tumu.Sum(k => k.ToplamTutar);
        var filtreTutar = gorunen.Sum(k => k.ToplamTutar);

        var aylikToplamlar = tumu
            .Select(k => (Kayit: k, Tarih: TarihOku(k.Tarih)))
            .Where(x => x.Tarih.HasValue)
            .GroupBy(x => (x.Tarih!.Value.Year, x.Tarih!.Value.Month))
            .Select(g => g.Sum(x => x.Kayit.ToplamTutar))
            .ToList();
        var aylikOrtalama = aylikToplamlar.Count > 0 ? aylikToplamlar.Average() : 0m;

        var enCokGrup = tumu
            .Where(k => !string.IsNullOrWhiteSpace(k.MalzemeHizmet))
            .GroupBy(k => k.MalzemeHizmet.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Ad = g.Key, Adet = g.Count() })
            .OrderByDescending(x => x.Adet)
            .FirstOrDefault();

        var enCokMalzeme = enCokGrup?.Ad ?? "—";
        var enCokAdet = enCokGrup?.Adet ?? 0;

        KpiGuncelle("Toplam Kayıt", tumu.Count.ToString("N0"));
        KpiGuncelle("Toplam Tutar", $"₺{toplamTutarTumu:N0}");
        KpiGuncelle("Aylık Ortalama Tutar", $"₺{aylikOrtalama:N0}");
        KpiGuncelle("Filtreye Göre Tutar", $"₺{filtreTutar:N0}");
        KpiGuncelle("En Çok Alınan Malzeme", enCokMalzeme);
        if (_kpiMetinleri.TryGetValue("En Çok Alınan Malzeme", out var enCokTb))
            enCokTb.ToolTip = enCokAdet > 0 ? $"{enCokMalzeme} ({enCokAdet:N0} kalem)" : enCokMalzeme;
        if (_kpiMetinleri.TryGetValue("Filtreye Göre Tutar", out var filtreTb))
            filtreTb.ToolTip = $"{gorunenSayi:N0} kayıt · ₺{filtreTutar:N2}";

        TxtTabloBaslik.Text = $"Malzeme Listesi ({gorunenSayi:N0} kayıt)";
        TxtAltKayit.Text = gorunenSayi.ToString("N0");
        TxtAltMiktar.Text = $"{toplamMiktar:N1}";
        TxtAltTutar.Text = $"₺{filtreTutar:N0}";
    }

    private static DateTime? TarihOku(string? tarih)
    {
        if (string.IsNullOrWhiteSpace(tarih))
            return null;

        if (DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return DateTime.TryParse(tarih, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt) ? dt : null;
    }
}
