using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class RaporlamalarView : UserControl, IModulKlavyeKisayollari
{
    private List<RaporModulOzeti> _modulOzetleri = [];
    private List<RaporDetaySatiri> _detaySatirlari = [];
    private List<RaporGrupOzeti> _grupOzetleri = [];
    private List<string> _seciliKategoriler = [];
    private List<string> _seciliMalzemeler = [];
    private readonly FiltreZamanlayici _filtreZamanlayici;

    public RaporlamalarView()
    {
        InitializeComponent();
        _filtreZamanlayici = new FiltreZamanlayici(RaporuYenile, 400);

        foreach (var tur in RaporTurleri.Tum)
            CmbRaporTuru.Items.Add(new ComboBoxItem { Content = tur });

        foreach (var modul in RaporModulleri.Tum)
            CmbModul.Items.Add(new ComboBoxItem { Content = modul });

        CmbRaporTuru.SelectedIndex = 0;
        CmbModul.SelectedIndex = 0;

        Loaded += (_, _) =>
        {
            KullaniciYetkileri.SekmeleriUygula(RaporTab, "Raporlamalar");
            KullaniciYetkileri.ModulErisiminiUygula(this, "Raporlamalar");
            RaporuYenile();
        };
    }

    private RaporFiltreleri FiltreOlustur() => new()
    {
        Baslangic = DpBaslangic.SelectedDate,
        Bitis = DpBitis.SelectedDate,
        Modul = SeciliModul(),
        RaporTuru = SeciliRaporTuru(),
        Kategoriler = _seciliKategoriler,
        Malzemeler = _seciliMalzemeler
    };

    private string SeciliRaporTuru() =>
        (CmbRaporTuru.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? RaporTurleri.GenelOzet;

    private string SeciliModul() =>
        (CmbModul.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? RaporModulleri.Tumu;

    private string FiltreMetni() =>
        RaporlamaServisi.FiltreOzetiMetni(FiltreOlustur());

    private void FiltreDegisti(object sender, RoutedEventArgs e) => _filtreZamanlayici.Tetikle();

    private void FiltreleriTemizle_Click(object sender, RoutedEventArgs e)
    {
        DpBaslangic.SelectedDate = null;
        DpBitis.SelectedDate = null;
        CmbRaporTuru.SelectedIndex = 0;
        CmbModul.SelectedIndex = 0;
        _seciliKategoriler = [];
        _seciliMalzemeler = [];
        SecimMetinleriniGuncelle();
        RaporuYenile();
    }

    private void KategoriSec_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new RaporCokluSecWindow(
            "Kategori Seç",
            "Birden fazla kategori seçebilirsiniz. Seçim yoksa tüm kategoriler dahil edilir.",
            RaporlamaServisi.TumKategoriler(),
            _seciliKategoriler)
        {
            Owner = Window.GetWindow(this)
        };

        if (pencere.ShowDialog() == true)
        {
            _seciliKategoriler = pencere.Secilenler.ToList();
            MalzemeleriKategoriyeGoreSenkronize();
            SecimMetinleriniGuncelle();
            RaporuYenile();
        }
    }

    private void MalzemeSec_Click(object sender, RoutedEventArgs e)
    {
        var malzemeler = RaporlamaServisi.TumMalzemeler(_seciliKategoriler);
        var aciklama = _seciliKategoriler.Count > 0
            ? $"Seçili {(_seciliKategoriler.Count == 1 ? "kategori" : "kategoriler")} içindeki malzemeler listelenir. Birden fazla seçebilirsiniz."
            : "Birden fazla malzeme veya hizmet seçebilirsiniz. Seçim yoksa tümü dahil edilir.";

        var pencere = new RaporCokluSecWindow(
            "Malzeme / Hizmet Seç",
            aciklama,
            malzemeler,
            _seciliMalzemeler)
        {
            Owner = Window.GetWindow(this)
        };

        if (pencere.ShowDialog() == true)
        {
            _seciliMalzemeler = pencere.Secilenler.ToList();
            SecimMetinleriniGuncelle();
            RaporuYenile();
        }
    }

    private void MalzemeleriKategoriyeGoreSenkronize()
    {
        if (_seciliKategoriler.Count == 0 || _seciliMalzemeler.Count == 0)
            return;

        var gecerli = new HashSet<string>(
            RaporlamaServisi.TumMalzemeler(_seciliKategoriler),
            StringComparer.CurrentCultureIgnoreCase);

        _seciliMalzemeler = _seciliMalzemeler
            .Where(m => gecerli.Contains(m))
            .ToList();
    }

    private void SecimMetinleriniGuncelle()
    {
        TxtKategoriSecim.Text = _seciliKategoriler.Count switch
        {
            0 => "Tümü",
            1 => _seciliKategoriler[0],
            _ => $"{_seciliKategoriler.Count} seçili"
        };

        TxtMalzemeSecim.Text = _seciliMalzemeler.Count switch
        {
            0 => "Tümü",
            1 => KisaMetin(_seciliMalzemeler[0], 22),
            _ => $"{_seciliMalzemeler.Count} seçili"
        };
    }

    private static string KisaMetin(string metin, int max) =>
        metin.Length <= max ? metin : metin[..(max - 1)] + "…";

    private void Yenile_Click(object sender, RoutedEventArgs e) => RaporuYenile();

    private void RaporuYenile()
    {
        var filtre = FiltreOlustur();
        var tur = filtre.RaporTuru;

        _modulOzetleri = RaporlamaServisi.ModulOzetleri(filtre);
        _detaySatirlari = RaporlamaServisi.DetaySatirlari(filtre);
        _grupOzetleri = tur switch
        {
            RaporTurleri.TedarikciOzeti => RaporlamaServisi.TedarikciOzeti(filtre),
            RaporTurleri.SahaOzeti => RaporlamaServisi.SahaOzeti(filtre),
            RaporTurleri.KategoriOzeti => RaporlamaServisi.KategoriOzeti(filtre),
            _ => []
        };

        ModulKartlariniGuncelle();
        TabloyuGuncelle(tur);
    }

    public void KisayolYenile() => _filtreZamanlayici.Hemen();

    private void ModulKartlariniGuncelle()
    {
        ModulOzetPanel.Children.Clear();
        var genelToplam = _modulOzetleri.Sum(o => o.ToplamTutar);

        foreach (var ozet in _modulOzetleri)
            ModulOzetPanel.Children.Add(OzetKartiOlustur(ozet, genelToplam));

        ModulOzetPanel.Children.Add(GenelToplamKarti(genelToplam));
    }

    private static Border OzetKartiOlustur(RaporModulOzeti ozet, decimal genelToplam)
    {
        var renk = (Color)ColorConverter.ConvertFromString(ozet.Renk)!;
        var pay = genelToplam > 0 ? ozet.ToplamTutar / genelToplam * 100m : 0;

        var border = new Border
        {
            Style = (Style)Application.Current.FindResource("StatChipStyle"),
            MinWidth = 150
        };

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = ozet.ModulAdi,
            FontSize = 11,
            Foreground = (Brush)Application.Current.FindResource("InkMutedBrush"),
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"₺{ozet.ToplamTutar:N0}",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(renk),
            Margin = new Thickness(0, 2, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{ozet.KayitSayisi} kayıt · %{pay:N0}",
            FontSize = 10,
            Foreground = (Brush)Application.Current.FindResource("InkMutedBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        });

        border.Child = panel;
        return border;
    }

    private static Border GenelToplamKarti(decimal genelToplam)
    {
        var border = new Border
        {
            Style = (Style)Application.Current.FindResource("StatChipStyle"),
            MinWidth = 160,
            Margin = new Thickness(0)
        };

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "GENEL TOPLAM",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("InkMutedBrush")
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"₺{genelToplam:N0}",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(8, 145, 178)),
            Margin = new Thickness(0, 2, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Filtrelenmiş toplam",
            FontSize = 10,
            Foreground = (Brush)Application.Current.FindResource("InkMutedBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        });

        border.Child = panel;
        return border;
    }

    private void TabloyuGuncelle(string tur)
    {
        TxtRaporBaslik.Text = tur;
        AnaGrid.Visibility = Visibility.Visible;
        RaporTab.Visibility = Visibility.Collapsed;

        switch (tur)
        {
            case RaporTurleri.GenelOzet:
                AnaGrid.ItemsSource = _modulOzetleri;
                TxtKayitSayisi.Text = $"{_modulOzetleri.Sum(o => o.KayitSayisi)} kayıt";
                TxtRaporToplam.Text = $"₺{_modulOzetleri.Sum(o => o.ToplamTutar):N0}";
                break;

            case RaporTurleri.TedarikciOzeti:
                GrupTablosuGoster("Tedarikçi / Hizmet Veren");
                break;

            case RaporTurleri.SahaOzeti:
                GrupTablosuGoster("Saha");
                break;

            case RaporTurleri.KategoriOzeti:
                GrupTablosuGoster("Kategori");
                break;

            default:
                DetayTablosuGoster(tur);
                break;
        }
    }

    private void GrupTablosuGoster(string grupBaslik)
    {
        AnaGrid.Visibility = Visibility.Collapsed;
        RaporTab.Visibility = Visibility.Visible;
        RaporTab.SelectedItem = TabGrupOzet;
        ColGrupAdi.Header = grupBaslik;
        GrupOzetGrid.ItemsSource = _grupOzetleri;
        TxtKayitSayisi.Text = $"{_grupOzetleri.Sum(g => g.KayitSayisi)} kayıt";
        TxtRaporToplam.Text = $"₺{_grupOzetleri.Sum(g => g.ToplamTutar):N0}";
    }

    private void DetayTablosuGoster(string tur)
    {
        AnaGrid.Visibility = Visibility.Collapsed;
        RaporTab.Visibility = Visibility.Visible;
        RaporTab.SelectedItem = TabDetay;
        DetayGrid.ItemsSource = _detaySatirlari;
        TxtKayitSayisi.Text = $"{_detaySatirlari.Count} kayıt";
        TxtRaporToplam.Text = $"₺{_detaySatirlari.Sum(d => d.Tutar):N0}";

        if (tur == RaporTurleri.SatinalmaTalepleri)
            TxtRaporBaslik.Text = "Satınalma — Onaylı Kalemler";
    }

    private void PdfIndir_Click(object sender, RoutedEventArgs e)
    {
        var filtre = FiltreOlustur();
        RaporlamaPdfOlusturucu.Indir(
            filtre, _modulOzetleri, _detaySatirlari, _grupOzetleri, FiltreMetni());
    }

    private void PdfYazdir_Click(object sender, RoutedEventArgs e)
    {
        var filtre = FiltreOlustur();
        RaporlamaPdfOlusturucu.Yazdir(
            filtre, FiltreMetni(), _modulOzetleri, _detaySatirlari, _grupOzetleri);
    }

    private void ExcelAktar_Click(object sender, RoutedEventArgs e) =>
        RaporlamaExcelService.DisaAktar(
            SeciliRaporTuru(), FiltreMetni(),
            _modulOzetleri, _detaySatirlari, _grupOzetleri);
}
