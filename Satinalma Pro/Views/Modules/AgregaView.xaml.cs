using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using Microsoft.Win32;

namespace SatinalmaPro.Views.Modules;

public partial class AgregaView : UserControl, IModulKlavyeKisayollari
{
    private readonly ICollectionView _gorunum;
    private readonly FiltreZamanlayici _filtreZamanlayici;
    private readonly ModulSayfalamaYoneticisi<AgregaKaydi> _sayfalama = new(25);
    private readonly FiltreZamanlayici _tarihZamanlayici;
    private List<string> _seciliAgregaCinsleri = [];
    private bool _filtreAcik;
    private double _filtrePanelYukseklik;
    private bool _yogunGorunum;
    private bool _tamEkran;
    private bool _arayuzHazir;
    private string? _grupAlani;

    private static readonly (string Baslik, string Alan)[] GrupSecenekleri =
    [
        ("Agrega Cinsi", "AgregaCinsi"),
        ("Agrega Türü", "AgregaTuru"),
        ("Tedarikçi", "Tedarikci"),
        ("Şantiye", "IndirildigiSaha")
    ];

    public ObservableCollection<AgregaKaydi> Kayitlar => ModulVeriDeposu.Agrega;

    public AgregaView()
    {
        InitializeComponent();
        DataContext = this;
        _filtreZamanlayici = new FiltreZamanlayici(FiltreYenile);
        _tarihZamanlayici = new FiltreZamanlayici(TarihFiltresiUygula);

        _gorunum = CollectionViewSource.GetDefaultView(Kayitlar);
        _gorunum.Filter = KayitFiltresi;
        AgregaGrid.ItemsSource = _sayfalama.SayfaKayitlari;
        ErpDataGridYardimcisi.PremiumGridAyarla(AgregaGrid);
        ModulSayfalamaYardimcisi.CubukBagla(_sayfalama, SayfalamaBar);
        CmbSayfaBoyutu.SelectionChanged += SayfaBoyutuDegisti;

        FiltreDurumunuYukle();
        VerileriHesapla();
        FiltreSecenekleriniGuncelle();
        SayfalamayiYenile();
        OzetGuncelle();
        _arayuzHazir = true;

        Loaded += AgregaView_Loaded;
        Unloaded += (_, _) => FiltreDurumunuKaydet();
    }

    private void FiltreDurumunuKaydet()
    {
        if (!_arayuzHazir)
            return;

        ModulFiltreDeposu.Kaydet("Agrega", ErpModulFiltreYardimcisi.Olustur(
            DpBaslangic, DpBitis, CmbTedarikci, _seciliAgregaCinsleri, CmbAgregaTuru, CmbSantiye,
            TxtTeslimAlanFilter, TxtIrsaliyeNoFilter, TxtGridArama,
            _filtreAcik, _filtrePanelYukseklik));
    }

    private void FiltreDurumunuYukle()
    {
        if (ModulFiltreDeposu.Oku<ErpModulFiltreDurumu>("Agrega") is not { } durum)
            return;

        ErpModulFiltreYardimcisi.Uygula(
            durum, DpBaslangic, DpBitis, CmbTedarikci, _seciliAgregaCinsleri, CmbAgregaTuru, CmbSantiye,
            TxtTeslimAlanFilter, TxtIrsaliyeNoFilter, TxtGridArama,
            ref _filtreAcik, ref _filtrePanelYukseklik);

        ErpModulFiltreYardimcisi.FiltrePanelGorunumunuUygula(
            FiltrePanelKap, _filtreAcik, _filtrePanelYukseklik, TxtFiltreToggle, IcoFiltreToggle);
        SecimMetniniGuncelle();
    }

    private void AgregaView_Loaded(object sender, RoutedEventArgs e)
    {
        var hazirdi = _arayuzHazir;
        _arayuzHazir = false;
        FiltreDurumunuYukle();
        if (hazirdi)
        {
            FiltreSecenekleriniGuncelle();
            FiltreYenile();
        }

        _arayuzHazir = true;

        Dispatcher.BeginInvoke(() =>
        {
            FiltrePanelAnimasyonYardimcisi.YukseklikOlcul(
                FiltrePanelKap, AnaIcerikGrid.ActualWidth, out _filtrePanelYukseklik);
            if (_filtreAcik)
                ErpModulFiltreYardimcisi.FiltrePanelGorunumunuUygula(
                    FiltrePanelKap, true, _filtrePanelYukseklik, TxtFiltreToggle, IcoFiltreToggle);
        }, System.Windows.Threading.DispatcherPriority.Loaded);

        KullaniciYetkileri.ModulErisiminiUygula(this, "Agrega");
    }

    private void FiltrePaneli_Toggle(object sender, RoutedEventArgs e)
    {
        FiltrePanelAnimasyonYardimcisi.Toggle(
            FiltrePanelKap, AnaIcerikGrid.ActualWidth, ref _filtreAcik, ref _filtrePanelYukseklik);

        TxtFiltreToggle.Text = _filtreAcik ? "Filtreleri Gizle" : "Filtreleri Göster";
        IcoFiltreToggle.Text = _filtreAcik ? "\uE70E" : "\uE70D";
        FiltreDurumunuKaydet();
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
            var yeniKayitlar = AgregaExcelService.DosyadanOku(dialog.FileName);
            if (yeniKayitlar.Count == 0)
            {
                MessageBox.Show("Dosyada aktarılacak kayıt bulunamadı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ModulVeriDeposu.BeginBatch();
            try
            {
                foreach (var kayit in yeniKayitlar)
                    Kayitlar.Add(kayit);
            }
            finally
            {
                ModulVeriDeposu.EndBatch();
            }

            VeriGuncellendi();
            MessageBox.Show($"{yeniKayitlar.Count} kayıt içe aktarıldı.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Excel okunamadı:\n{ex.Message}", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SablonIndir_Click(object sender, RoutedEventArgs e) =>
        AgregaExcelService.SablonKaydet();

    private void PdfYazdir_Click(object sender, RoutedEventArgs e) =>
        AgregaPdfOlusturucu.Yazdir(GorunenKayitlar(), FiltreOzetiMetni());

    private void PdfIndir_Click(object sender, RoutedEventArgs e) =>
        AgregaPdfOlusturucu.Indir(GorunenKayitlar(), FiltreOzetiMetni());

    private void YeniKayit_Click(object sender, RoutedEventArgs e)
    {
        var yeni = new AgregaKaydi
        {
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            Birim = "Ton",
            AgregaTuru = "Mıcır"
        };

        var pencere = new AgregaDuzenleWindow(yeni) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        Kayitlar.Add(yeni);
        VeriGuncellendi();
    }

    private void DisaAktar_Click(object sender, RoutedEventArgs e) =>
        AgregaExcelService.ListeyiKaydet(GorunenKayitlar(), "Agrega.xlsx");

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();

    private void FiltreAra_Click(object sender, RoutedEventArgs e) => FiltreYenile();

    private void FiltreUygula_Click(object sender, RoutedEventArgs e) => FiltreYenile();

    private void FiltreDegisti(object sender, RoutedEventArgs e)
    {
        if (!_arayuzHazir)
            return;

        if (sender is TextBox)
            _filtreZamanlayici.Tetikle();
        else if (sender is DatePicker)
            _tarihZamanlayici.Tetikle();
        else
            FiltreYenile();
    }

    private void TarihFiltresiUygula()
    {
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void TedarikciDegisti(object sender, RoutedEventArgs e)
    {
        CinsleriSenkronize();
        SecimMetniniGuncelle();
        FiltreYenile();
    }

    private void AgregaCinsiSec_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new RaporCokluSecWindow(
            "Agrega Cinsi Seç",
            "Seçili tarih aralığı ve tedarikçiye göre listelenir. Birden fazla agrega cinsi seçebilirsiniz.",
            MevcutAgregaCinsleri(),
            _seciliAgregaCinsleri)
        {
            Owner = Window.GetWindow(this)
        };

        if (pencere.ShowDialog() == true)
        {
            _seciliAgregaCinsleri = pencere.Secilenler.ToList();
            SecimMetniniGuncelle();
            FiltreYenile();
        }
    }

    private void FiltreleriTemizle_Click(object sender, RoutedEventArgs e)
    {
        ModulFiltreDeposu.Sil("Agrega");
        TxtGridArama.Text = "";
        DpBaslangic.SelectedDate = null;
        DpBitis.SelectedDate = null;
        CmbTedarikci.SelectedIndex = 0;
        CmbAgregaTuru.SelectedIndex = 0;
        CmbSantiye.SelectedIndex = 0;
        TxtTeslimAlanFilter.Text = "";
        TxtIrsaliyeNoFilter.Text = "";
        _seciliAgregaCinsleri = [];
        SecimMetniniGuncelle();
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void FiltreYenile()
    {
        SayfalamayiYenile(ilkSayfayaDon: true);
        OzetGuncelle();
        AktifFiltreSayisiniGuncelle();
        FiltreDurumunuKaydet();
    }

    private void SayfalamayiYenile(bool ilkSayfayaDon = false)
    {
        ErpModulTabloYardimcisi.SayfalamayiUygula(
            _sayfalama, _gorunum, k => ModulSayfalamaYardimcisi.TarihSira(k.Tarih),
            SayfalamaBar, _grupAlani, string.IsNullOrEmpty(_grupAlani) ? null : k => AgregaGrupAnahtari(k, _grupAlani!), ilkSayfayaDon);

        var filtrelenmis = ModulSayfalamaYardimcisi.FiltrelenmisListe<AgregaKaydi>(_gorunum);
        ErpModulTabloYardimcisi.GrupBilgiGuncelle(
            TxtGrupBilgi, _grupAlani, GrupSecenekleri,
            filtrelenmis.Select(k => AgregaGrupAnahtari(k, _grupAlani!)));
    }

    private static string AgregaGrupAnahtari(AgregaKaydi kayit, string alan) => alan switch
    {
        "AgregaCinsi" => kayit.AgregaCinsi ?? "",
        "AgregaTuru" => kayit.AgregaTuru ?? "",
        "Tedarikci" => kayit.Tedarikci ?? "",
        "IndirildigiSaha" => kayit.IndirildigiSaha ?? "",
        _ => ""
    };

    public void KisayolYenile()
    {
        VerileriHesapla();
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void VeriGuncellendi()
    {
        VerileriHesapla();
        FiltreSecenekleriniGuncelle();
        FiltreYenile();
    }

    private void VerileriHesapla()
    {
        foreach (var kayit in Kayitlar)
            kayit.ToplamTutariHesapla();

        AgregaArtisHesaplayici.Hesapla(Kayitlar);
    }

    private string AramaMetni() => TxtGridArama.Text.Trim();

    private bool KayitFiltresi(object item)
    {
        if (item is not AgregaKaydi kayit)
            return false;

        var arama = AramaMetni();
        if (!string.IsNullOrEmpty(arama))
        {
            var metin = string.Join(" ",
                kayit.Tarih, kayit.IrsaliyeNo, kayit.SiparisNo, kayit.AgregaTuru, kayit.AgregaCinsi,
                kayit.Tedarikci, kayit.IndirildigiSaha, kayit.TeslimAlan, kayit.Aciklama);

            if (!metin.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (!TarihUygunMu(kayit.Tarih))
            return false;

        var tedarikci = ComboDegeri(CmbTedarikci);
        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü" &&
            !kayit.Tedarikci.Equals(tedarikci, StringComparison.OrdinalIgnoreCase))
            return false;

        if (_seciliAgregaCinsleri.Count > 0 &&
            !_seciliAgregaCinsleri.Any(c => c.Equals(kayit.AgregaCinsi, StringComparison.CurrentCultureIgnoreCase)))
            return false;

        var sinif = ComboDegeri(CmbAgregaTuru);
        if (!string.IsNullOrEmpty(sinif) && sinif != "Tümü" &&
            !kayit.AgregaTuru.Equals(sinif, StringComparison.OrdinalIgnoreCase))
            return false;

        var santiye = ComboDegeri(CmbSantiye);
        if (!string.IsNullOrEmpty(santiye) && santiye != "Tümü" &&
            !kayit.IndirildigiSaha.Equals(santiye, StringComparison.OrdinalIgnoreCase))
            return false;

        var teslimAlan = TxtTeslimAlanFilter.Text.Trim();
        if (!string.IsNullOrEmpty(teslimAlan) &&
            !kayit.TeslimAlan.Contains(teslimAlan, StringComparison.OrdinalIgnoreCase))
            return false;

        var irsaliyeNo = TxtIrsaliyeNoFilter.Text.Trim();
        if (!string.IsNullOrEmpty(irsaliyeNo) &&
            !kayit.IrsaliyeNo.Contains(irsaliyeNo, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private bool TarihUygunMu(string tarih)
    {
        if (DpBaslangic.SelectedDate is not DateTime && DpBitis.SelectedDate is not DateTime)
            return true;

        if (!DateTime.TryParseExact(tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return false;

        if (DpBaslangic.SelectedDate is DateTime b && dt.Date < b.Date)
            return false;

        if (DpBitis.SelectedDate is DateTime bit && dt.Date > bit.Date)
            return false;

        return true;
    }

    private IEnumerable<AgregaKaydi> TarihFiltreliKayitlar() =>
        Kayitlar.Where(k => TarihUygunMu(k.Tarih));

    private IEnumerable<AgregaKaydi> TedarikciFiltreliKayitlar()
    {
        var kayitlar = TarihFiltreliKayitlar();
        var tedarikci = ComboDegeri(CmbTedarikci);

        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü")
            kayitlar = kayitlar.Where(k => k.Tedarikci.Equals(tedarikci, StringComparison.OrdinalIgnoreCase));

        return kayitlar;
    }

    private List<string> MevcutAgregaCinsleri() =>
        TedarikciFiltreliKayitlar()
            .Select(k => k.AgregaCinsi)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private void CinsleriSenkronize()
    {
        if (_seciliAgregaCinsleri.Count == 0)
            return;

        var gecerli = new HashSet<string>(MevcutAgregaCinsleri(), StringComparer.CurrentCultureIgnoreCase);
        _seciliAgregaCinsleri = _seciliAgregaCinsleri.Where(c => gecerli.Contains(c)).ToList();
    }

    private void SecimMetniniGuncelle()
    {
        TxtAgregaCinsiSecim.Text = _seciliAgregaCinsleri.Count switch
        {
            0 => "Tümü",
            1 => KisaMetin(_seciliAgregaCinsleri[0], 22),
            _ => $"{_seciliAgregaCinsleri.Count} seçili"
        };
    }

    private static string KisaMetin(string metin, int max) =>
        metin.Length <= max ? metin : metin[..(max - 1)] + "…";

    private static string? ComboDegeri(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private void FiltreSecenekleriniGuncelle()
    {
        var seciliTedarikci = ComboDegeri(CmbTedarikci);
        var seciliSinif = ComboDegeri(CmbAgregaTuru);
        var seciliSantiye = ComboDegeri(CmbSantiye);

        ComboDoldur(CmbTedarikci, TarihFiltreliKayitlar().Select(k => k.Tedarikci), seciliTedarikci);
        ComboDoldur(CmbAgregaTuru, TarihFiltreliKayitlar().Select(k => k.AgregaTuru), seciliSinif);
        ComboDoldur(CmbSantiye, TarihFiltreliKayitlar().Select(k => k.IndirildigiSaha), seciliSantiye);

        CinsleriSenkronize();
        SecimMetniniGuncelle();
    }

    private static void ComboDoldur(ComboBox combo, IEnumerable<string> degerler, string? secili)
    {
        combo.Items.Clear();
        combo.Items.Add(new ComboBoxItem { Content = "Tümü" });

        foreach (var d in degerler
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.CurrentCultureIgnoreCase)
                     .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase))
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

    private void AktifFiltreSayisiniGuncelle()
    {
        var sayi = 0;
        if (DpBaslangic.SelectedDate is not null) sayi++;
        if (DpBitis.SelectedDate is not null) sayi++;
        if (ComboDegeri(CmbTedarikci) is { } t && t != "Tümü") sayi++;
        if (_seciliAgregaCinsleri.Count > 0) sayi++;
        if (ComboDegeri(CmbAgregaTuru) is { } s && s != "Tümü") sayi++;
        if (ComboDegeri(CmbSantiye) is { } sh && sh != "Tümü") sayi++;
        if (!string.IsNullOrWhiteSpace(TxtTeslimAlanFilter.Text)) sayi++;
        if (!string.IsNullOrWhiteSpace(TxtIrsaliyeNoFilter.Text)) sayi++;
        if (!string.IsNullOrWhiteSpace(TxtGridArama.Text)) sayi++;
        TxtAktifFiltreSayisi.Text = sayi.ToString();
    }

    private void AgregaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void AgregaGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is AgregaKaydi kayit)
            e.Row.Background = AgregaRenkleri.GetFirca(kayit.AgregaTuru);

        if (_yogunGorunum)
            e.Row.Height = 36;
    }

    private void AgregaGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SeciliKayit() is { } kayit)
            KaydiDuzenle(kayit);
    }

    private void SatirIslem_Tikla(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AgregaKaydi kayit })
            return;

        AgregaGrid.SelectedItem = kayit;

        var menu = new ContextMenu
        {
            PlacementTarget = sender as UIElement,
            Placement = PlacementMode.Bottom
        };
        var duzenle = new MenuItem { Header = "Düzenle" };
        duzenle.Click += (_, _) => KaydiDuzenle(kayit);
        var sil = new MenuItem { Header = "Sil" };
        sil.Click += (_, _) => KayitSil(kayit);
        menu.Items.Add(duzenle);
        menu.Items.Add(new Separator());
        menu.Items.Add(sil);
        menu.IsOpen = true;
    }

    private void MenuDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliKayit() is { } kayit)
            KaydiDuzenle(kayit);
    }

    private void MenuSil_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliKayit() is { } kayit)
            KayitSil(kayit);
    }

    private void KayitSil(AgregaKaydi kayit)
    {
        if (MessageBox.Show($"{kayit.IrsaliyeNo} irsaliye numaralı kaydı silmek istiyor musunuz?", "Kayıt Sil",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Kayitlar.Remove(kayit);
        VeriGuncellendi();
    }

    private AgregaKaydi? SeciliKayit() =>
        AgregaGrid.SelectedItem as AgregaKaydi;

    private void KaydiDuzenle(AgregaKaydi kayit)
    {
        var pencere = new AgregaDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        kayit.Tarih = TarihYardimcisi.Normalize(kayit.Tarih);
        VeriGuncellendi();
    }

    private string FiltreOzetiMetni()
    {
        var parcalar = new List<string>();

        if (DpBaslangic.SelectedDate is DateTime bas)
            parcalar.Add($"Başlangıç: {bas:dd.MM.yyyy}");
        if (DpBitis.SelectedDate is DateTime bit)
            parcalar.Add($"Bitiş: {bit:dd.MM.yyyy}");

        var tedarikci = ComboDegeri(CmbTedarikci);
        if (!string.IsNullOrEmpty(tedarikci) && tedarikci != "Tümü")
            parcalar.Add($"Tedarikçi: {tedarikci}");

        if (_seciliAgregaCinsleri.Count > 0)
            parcalar.Add($"Agrega cinsi: {string.Join(", ", _seciliAgregaCinsleri)}");

        var tur = ComboDegeri(CmbAgregaTuru);
        if (!string.IsNullOrEmpty(tur) && tur != "Tümü")
            parcalar.Add($"Agrega türü: {tur}");

        var arama = AramaMetni();
        if (!string.IsNullOrEmpty(arama))
            parcalar.Add($"Arama: {arama}");

        return parcalar.Count == 0 ? "Tüm kayıtlar" : string.Join(" · ", parcalar);
    }

    private List<AgregaKaydi> GorunenKayitlar() =>
        _gorunum.Cast<AgregaKaydi>().ToList();

    private void OzetGuncelle()
    {
        var gorunen = GorunenKayitlar();
        var toplamTon = gorunen.Sum(k => k.Miktar);
        var toplamTutar = gorunen.Sum(k => k.ToplamTutar);

        TxtToplamKayit.Text = gorunen.Count.ToString("N0", CultureInfo.GetCultureInfo("tr-TR"));
        TxtToplamKayitAlt.Text = gorunen.Count == Kayitlar.Count
            ? "Bu filtreleme için"
            : $"{gorunen.Count} / {Kayitlar.Count} kayıt";

        TxtToplamMiktar.Text = $"{toplamTon:N1} Ton";
        TxtToplamTutar.Text = $"₺{toplamTutar:N0}";
        TxtTeslimEdilen.Text = gorunen.Count.ToString("N0", CultureInfo.GetCultureInfo("tr-TR"));

        TxtGridBaslik.Text = $"Agrega Listesi ({gorunen.Count:N0} kayıt)";
        TxtFooterKayit.Text = $"Toplam {gorunen.Count:N0} kayıt";
        TxtFooterTon.Text = $"{toplamTon:N2} Ton";
        TxtFooterTutar.Text = $"₺{toplamTutar:N2}";
    }

    private void SayfaBoyutuDegisti(object sender, SelectionChangedEventArgs e)
    {
        if (!_arayuzHazir || SayfalamaBar is null)
            return;

        if (CmbSayfaBoyutu.SelectedItem is not ComboBoxItem { Tag: string tag })
            return;

        if (!int.TryParse(tag, out var boyut) || boyut == _sayfalama.SayfaBoyutu)
            return;

        ErpModulTabloYardimcisi.SayfaBoyutuDegistir(
            _sayfalama, boyut, k => ModulSayfalamaYardimcisi.TarihSira(k.Tarih), _grupAlani, SayfalamaBar);
    }

    private void Kolonlar_Click(object sender, RoutedEventArgs e) =>
        ErpModulTabloYardimcisi.Kolonlar(AgregaGrid, Window.GetWindow(this));

    private void Grupla_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement hedef)
            return;

        ErpModulTabloYardimcisi.Grupla(hedef, GrupSecenekleri, _grupAlani, alan =>
        {
            _grupAlani = alan;
            SayfalamayiYenile(ilkSayfayaDon: true);
            OzetGuncelle();
        });
    }

    private void FiltreOdak_Click(object sender, RoutedEventArgs e)
    {
        if (!_filtreAcik)
            FiltrePaneli_Toggle(sender, e);

        ErpModulTabloYardimcisi.FiltreOdakla(FiltreBaslikBar);
    }

    private void YogunGorunum_Click(object sender, RoutedEventArgs e) =>
        ErpModulTabloYardimcisi.Yogun(AgregaGrid, ref _yogunGorunum);

    private void TamEkran_Click(object sender, RoutedEventArgs e) =>
        ErpModulTabloYardimcisi.TamEkran(AnaIcerikGrid, TabloKart, 4, [0, 1, 2, 3], ref _tamEkran, BtnTamEkran);
}
