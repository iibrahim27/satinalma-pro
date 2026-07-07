using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using Microsoft.Win32;

namespace SatinalmaPro.Views.Modules;

public partial class AkaryakitView : UserControl, IModulKlavyeKisayollari
{
    private readonly ICollectionView _gorunum;
    private readonly FiltreZamanlayici _filtreZamanlayici;
    private readonly ModulSayfalamaYoneticisi<AkaryakitKaydi> _sayfalama = new();
    private bool _yogunGorunum;
    private bool _tamEkran;
    private bool _arayuzHazir;
    private string? _grupAlani;

    private static readonly (string Baslik, string Alan)[] GrupSecenekleri =
    [
        ("Kayıt Tipi", "KayitTipi"),
        ("Araç Tipi", "AracTipi"),
        ("Yakıt Türü", "YakitTuru"),
        ("Plaka", "PlakaVeyaKod"),
        ("Saha", "Saha")
    ];

    public ObservableCollection<AkaryakitKaydi> Kayitlar => ModulVeriDeposu.Akaryakit;

    public AkaryakitView()
    {
        InitializeComponent();
        DataContext = this;
        _filtreZamanlayici = new FiltreZamanlayici(FiltreYenile);

        _gorunum = CollectionViewSource.GetDefaultView(Kayitlar);
        _gorunum.Filter = KayitFiltresi;
        YakitGrid.ItemsSource = _sayfalama.SayfaKayitlari;
        ErpDataGridYardimcisi.PremiumGridAyarla(YakitGrid);
        ModulSayfalamaYardimcisi.CubukBagla(_sayfalama, SayfalamaBar);

        VerileriHesapla();
        FiltreCombolariGuncelle();
        SayfalamayiYenile();
        OzetGuncelle();
        AdminExcelYetkisiniUygula();
        _arayuzHazir = true;
    }

    private void AdminExcelYetkisiniUygula()
    {
        var admin = KullaniciYetkileri.AdminMi;
        BtnExcelYukle.Visibility = admin ? Visibility.Visible : Visibility.Collapsed;
        BtnSablonIndir.Visibility = admin ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool AdminExcelEngellendi()
    {
        if (KullaniciYetkileri.AdminMi)
            return false;

        MessageBox.Show(
            "Geçmiş veri Excel yükleme ve şablon indirme yalnızca admin kullanıcılar içindir.",
            UygulamaBilgisi.Ad,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return true;
    }

    private void ExcelYukle_Click(object sender, RoutedEventArgs e)
    {
        if (AdminExcelEngellendi())
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
            var yeniKayitlar = AkaryakitExcelService.DosyadanOku(dialog.FileName);
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

    private void SablonIndir_Click(object sender, RoutedEventArgs e)
    {
        if (AdminExcelEngellendi())
            return;

        AkaryakitExcelService.SablonKaydet();
    }

    private void PdfIndir_Click(object sender, RoutedEventArgs e) =>
        AkaryakitPdfOlusturucu.Indir(GorunenKayitlar(), "Akaryakıt Takip Raporu", FiltreOzetiMetni());

    private void PdfYazdir_Click(object sender, RoutedEventArgs e) =>
        AkaryakitPdfOlusturucu.Yazdir(GorunenKayitlar(), "Akaryakıt Takip Raporu", FiltreOzetiMetni());

    private void YeniKayit_Click(object sender, RoutedEventArgs e)
    {
        var yeni = new AkaryakitKaydi
        {
            Tarih = DateTime.Now.ToString("dd.MM.yyyy"),
            Birim = "Lt",
            AracTipi = "Araç",
            KayitTipi = "Dağıtılan",
            YakitTuru = "Motorin"
        };

        var pencere = new AkaryakitDuzenleWindow(yeni) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        Kayitlar.Add(yeni);
        VeriGuncellendi();
    }

    private void DisaAktar_Click(object sender, RoutedEventArgs e) =>
        AkaryakitExcelService.ListeyiKaydet(GorunenKayitlar(), "Akaryakit.xlsx");

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
        DpBaslangic.SelectedDate = null;
        DpBitis.SelectedDate = null;
        CmbKayitTipi.SelectedIndex = 0;
        CmbAracTipi.SelectedIndex = 0;
        CmbYakitTuru.SelectedIndex = 0;
        CmbPlaka.SelectedIndex = 0;
        FiltreYenile();
    }

    private void FiltreYenile()
    {
        SayfalamayiYenile(ilkSayfayaDon: true);
        OzetGuncelle();
    }

    private void SayfalamayiYenile(bool ilkSayfayaDon = false)
    {
        ErpModulTabloYardimcisi.SayfalamayiUygula(
            _sayfalama, _gorunum, k => ModulSayfalamaYardimcisi.TarihSira(k.Tarih),
            SayfalamaBar, _grupAlani, string.IsNullOrEmpty(_grupAlani) ? null : k => AkaryakitGrupAnahtari(k, _grupAlani!), ilkSayfayaDon);

        var filtrelenmis = ModulSayfalamaYardimcisi.FiltrelenmisListe<AkaryakitKaydi>(_gorunum);
        ErpModulTabloYardimcisi.GrupBilgiGuncelle(
            TxtGrupBilgi, _grupAlani, GrupSecenekleri,
            filtrelenmis.Select(k => AkaryakitGrupAnahtari(k, _grupAlani!)));
    }

    private static string AkaryakitGrupAnahtari(AkaryakitKaydi kayit, string alan) => alan switch
    {
        "KayitTipi" => kayit.KayitTipi ?? "",
        "AracTipi" => kayit.AracTipi ?? "",
        "YakitTuru" => kayit.YakitTuru ?? "",
        "PlakaVeyaKod" => kayit.PlakaVeyaKod ?? "",
        "Saha" => kayit.Saha ?? "",
        _ => ""
    };

    private void Kolonlar_Click(object sender, RoutedEventArgs e) =>
        ErpModulTabloYardimcisi.Kolonlar(YakitGrid, Window.GetWindow(this));

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

    private void FiltreOdak_Click(object sender, RoutedEventArgs e) =>
        ErpModulTabloYardimcisi.FiltreOdakla(FiltreKart);

    private void YogunGorunum_Click(object sender, RoutedEventArgs e) =>
        ErpModulTabloYardimcisi.Yogun(YakitGrid, ref _yogunGorunum);

    private void TamEkran_Click(object sender, RoutedEventArgs e) =>
        ErpModulTabloYardimcisi.TamEkran(AnaIcerikGrid, TabloKart, 5, [0, 1, 2, 3, 4], ref _tamEkran, BtnTamEkran);

    private void SayfaBoyutuDegisti(object sender, SelectionChangedEventArgs e)
    {
        if (!_arayuzHazir)
            return;

        if (CmbSayfaBoyutu.SelectedItem is not ComboBoxItem { Tag: string tag })
            return;

        if (!int.TryParse(tag, out var boyut) || boyut == _sayfalama.SayfaBoyutu)
            return;

        ErpModulTabloYardimcisi.SayfaBoyutuDegistir(
            _sayfalama, boyut, k => ModulSayfalamaYardimcisi.TarihSira(k.Tarih), _grupAlani, SayfalamaBar);
    }

    public void KisayolYenile()
    {
        VerileriHesapla();
        FiltreCombolariGuncelle();
        FiltreYenile();
    }

    private void VeriGuncellendi()
    {
        VerileriHesapla();
        FiltreCombolariGuncelle();
        FiltreYenile();
    }

    private void VerileriHesapla()
    {
        foreach (var kayit in Kayitlar)
            kayit.ToplamTutariHesapla();

        AkaryakitTuketimHesaplayici.Hesapla(Kayitlar);
    }

    private bool KayitFiltresi(object item)
    {
        if (item is not AkaryakitKaydi kayit)
            return false;

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
        {
            var metin = string.Join(" ",
                kayit.KayitTipi, kayit.Tarih, kayit.AracTipi, kayit.PlakaVeyaKod, kayit.AracMakineAdi,
                kayit.YakitTuru, kayit.Tedarikci, kayit.Istasyon, kayit.TeslimAlan,
                kayit.SoforOperator, kayit.Saha, kayit.Aciklama);

            if (!metin.Contains(arama, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (DpBaslangic.SelectedDate is DateTime || DpBitis.SelectedDate is DateTime)
        {
            if (!DateTime.TryParseExact(kayit.Tarih, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tarih))
                return false;

            if (DpBaslangic.SelectedDate is DateTime b && tarih.Date < b.Date)
                return false;

            if (DpBitis.SelectedDate is DateTime bit && tarih.Date > bit.Date)
                return false;
        }

        var aracTipi = ComboDegeri(CmbAracTipi);
        if (!string.IsNullOrEmpty(aracTipi) && aracTipi != "Tümü" &&
            !kayit.AracTipi.Equals(aracTipi, StringComparison.OrdinalIgnoreCase))
            return false;

        var yakit = ComboDegeri(CmbYakitTuru);
        if (!string.IsNullOrEmpty(yakit) && yakit != "Tümü" &&
            !kayit.YakitTuru.Equals(yakit, StringComparison.OrdinalIgnoreCase))
            return false;

        var kayitTipi = ComboDegeri(CmbKayitTipi);
        if (!string.IsNullOrEmpty(kayitTipi) && kayitTipi != "Tümü" &&
            !kayit.KayitTipi.Equals(kayitTipi, StringComparison.OrdinalIgnoreCase))
            return false;

        var plaka = ComboDegeri(CmbPlaka);
        if (!string.IsNullOrEmpty(plaka) && plaka != "Tümü" &&
            !kayit.PlakaVeyaKod.Equals(plaka, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static string? ComboDegeri(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private void FiltreCombolariGuncelle()
    {
        var seciliKayitTipi = ComboDegeri(CmbKayitTipi);
        var seciliTip = ComboDegeri(CmbAracTipi);
        var seciliYakit = ComboDegeri(CmbYakitTuru);
        var seciliPlaka = ComboDegeri(CmbPlaka);

        CmbKayitTipi.Items.Clear();
        CmbAracTipi.Items.Clear();
        CmbYakitTuru.Items.Clear();
        CmbPlaka.Items.Clear();

        CmbKayitTipi.Items.Add(new ComboBoxItem { Content = "Tümü" });
        CmbAracTipi.Items.Add(new ComboBoxItem { Content = "Tümü" });
        CmbYakitTuru.Items.Add(new ComboBoxItem { Content = "Tümü" });
        CmbPlaka.Items.Add(new ComboBoxItem { Content = "Tümü" });

        foreach (var kt in Kayitlar.Select(k => k.KayitTipi).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
            CmbKayitTipi.Items.Add(new ComboBoxItem { Content = kt });

        foreach (var t in Kayitlar.Select(k => k.AracTipi).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
            CmbAracTipi.Items.Add(new ComboBoxItem { Content = t });

        foreach (var y in Kayitlar.Select(k => k.YakitTuru).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
            CmbYakitTuru.Items.Add(new ComboBoxItem { Content = y });

        foreach (var p in FiloPlakaServisi.AktifPlakalar()
                     .Concat(Kayitlar.Select(k => k.PlakaVeyaKod))
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(s => s))
            CmbPlaka.Items.Add(new ComboBoxItem { Content = p });

        CmbKayitTipi.SelectedIndex = IndexBul(CmbKayitTipi, seciliKayitTipi);
        CmbAracTipi.SelectedIndex = IndexBul(CmbAracTipi, seciliTip);
        CmbYakitTuru.SelectedIndex = IndexBul(CmbYakitTuru, seciliYakit);
        CmbPlaka.SelectedIndex = IndexBul(CmbPlaka, seciliPlaka);
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

    private void YakitGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        KayitButonlariniGuncelle();

    private void KayitDuzenle_Click(object sender, RoutedEventArgs e) => MenuDuzenle_Click(sender, e);

    private void KayitSil_Click(object sender, RoutedEventArgs e) => MenuSil_Click(sender, e);

    private void KayitButonlariniGuncelle()
    {
        var secili = SeciliKayit() is not null;
        BtnKayitDuzenle.IsEnabled = secili;
        BtnKayitSil.IsEnabled = secili;
    }

    private void YakitGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is AkaryakitKaydi kayit)
            e.Row.Background = AkaryakitRenkleri.GetFirca(kayit.AracTipi);
    }

    private void YakitGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SeciliKayit() is { } kayit)
            KaydiDuzenle(kayit);
    }

    private void MenuDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliKayit() is { } kayit)
            KaydiDuzenle(kayit);
    }

    private void MenuSil_Click(object sender, RoutedEventArgs e)
    {
        if (SeciliKayit() is not { } kayit)
            return;

        var ozet = kayit.AlinanKayit
            ? $"{kayit.Tarih} — {kayit.Miktar:N1} {kayit.Birim} alınan"
            : kayit.PlakaVeyaKod;
        if (MessageBox.Show($"{ozet} için kaydı silmek istiyor musunuz?", "Kayıt Sil",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Kayitlar.Remove(kayit);
        VeriGuncellendi();
    }

    private AkaryakitKaydi? SeciliKayit() =>
        YakitGrid.SelectedItem as AkaryakitKaydi;

    private void KaydiDuzenle(AkaryakitKaydi kayit)
    {
        var pencere = new AkaryakitDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() != true)
            return;

        VeriGuncellendi();
    }

    private List<AkaryakitKaydi> GorunenKayitlar() =>
        _gorunum.Cast<AkaryakitKaydi>().ToList();

    private string FiltreOzetiMetni()
    {
        var parcalar = new List<string>();

        if (DpBaslangic.SelectedDate is DateTime baslangic)
            parcalar.Add($"Başlangıç: {baslangic:dd.MM.yyyy}");

        if (DpBitis.SelectedDate is DateTime bitis)
            parcalar.Add($"Bitiş: {bitis:dd.MM.yyyy}");

        var kayitTipi = ComboDegeri(CmbKayitTipi);
        if (!string.IsNullOrEmpty(kayitTipi) && kayitTipi != "Tümü")
            parcalar.Add($"Kayıt tipi: {kayitTipi}");

        var aracTipi = ComboDegeri(CmbAracTipi);
        if (!string.IsNullOrEmpty(aracTipi) && aracTipi != "Tümü")
            parcalar.Add($"Araç tipi: {aracTipi}");

        var yakit = ComboDegeri(CmbYakitTuru);
        if (!string.IsNullOrEmpty(yakit) && yakit != "Tümü")
            parcalar.Add($"Yakıt: {yakit}");

        var plaka = ComboDegeri(CmbPlaka);
        if (!string.IsNullOrEmpty(plaka) && plaka != "Tümü")
            parcalar.Add($"Plaka: {plaka}");

        var arama = TxtArama.Text.Trim();
        if (!string.IsNullOrEmpty(arama))
            parcalar.Add($"Arama: {arama}");

        return parcalar.Count == 0 ? "Tüm kayıtlar" : string.Join(" · ", parcalar);
    }

    private void OzetGuncelle()
    {
        var gorunen = GorunenKayitlar();

        var gelenLt = Kayitlar.Where(k => k.AlinanKayit).Sum(k => k.Miktar);
        var dagitilanLt = Kayitlar.Where(k => !k.AlinanKayit).Sum(k => k.Miktar);
        var stokLt = gelenLt - dagitilanLt;
        var gelenKayit = Kayitlar.Count(k => k.AlinanKayit);
        var dagitilanKayit = Kayitlar.Count(k => !k.AlinanKayit);

        TxtGelenAkaryakit.Text = $"{gelenLt:N0} Lt";
        TxtGelenKayit.Text = $"{gelenKayit} alım kaydı";
        TxtDagitilanAkaryakit.Text = $"{dagitilanLt:N0} Lt";
        TxtDagitilanKayit.Text = $"{dagitilanKayit} dağıtım kaydı";
        TxtStokAkaryakit.Text = $"{stokLt:N0} Lt";
        TxtStokDurum.Text = stokLt < 0
            ? "Eksiye düşmüş — kontrol edin"
            : "Gelen − dağıtılan";

        TxtToplamKayit.Text = Kayitlar.Count.ToString();
        TxtToplamTutar.Text = $"₺{gorunen.Where(k => k.AlinanKayit).Sum(k => k.ToplamTutar):N0}";
        TxtFiltrelenen.Text = gorunen.Count == Kayitlar.Count
            ? "Tümü"
            : $"{gorunen.Count} / {Kayitlar.Count} kayıt";
    }
}

