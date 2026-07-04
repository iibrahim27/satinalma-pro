using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class FinansmanRaporlamaView : UserControl, IModulKlavyeKisayollari
{
    private FinansmanGenelOzet _ozet = new();
    private List<FinansmanModulOzeti> _modulOzetleri = [];
    private List<FinansmanHareketSatiri> _hareketler = [];
    private List<FinansmanAylikOzet> _aylikOzetler = [];
    private List<FinansmanVadeSatiri> _vadeler = [];
    private List<FinansmanGrupOzeti> _grupOzetleri = [];
    private readonly FiltreZamanlayici _filtreZamanlayici;

    public FinansmanRaporlamaView()
    {
        InitializeComponent();
        _filtreZamanlayici = new FiltreZamanlayici(RaporuYenile, 400);

        foreach (var tur in FinansmanTurleri.Tum)
            CmbRaporTuru.Items.Add(new ComboBoxItem { Content = tur });

        foreach (var modul in FinansmanModulleri.Tum)
            CmbModul.Items.Add(new ComboBoxItem { Content = modul });

        foreach (var tip in FinansmanHareketTipleri.Tum)
            CmbHareketTipi.Items.Add(new ComboBoxItem { Content = tip });

        CmbRaporTuru.SelectedIndex = 0;
        CmbModul.SelectedIndex = 0;
        CmbHareketTipi.SelectedIndex = 0;

        Loaded += (_, _) =>
        {
            KullaniciYetkileri.SekmeleriUygula(RaporTab, "Finansman Raporlama");
            KullaniciYetkileri.ModulErisiminiUygula(this, "Finansman Raporlama");
            RaporuYenile();
        };
    }

    private FinansmanFiltreleri FiltreOlustur() => new()
    {
        Baslangic = DpBaslangic.SelectedDate,
        Bitis = DpBitis.SelectedDate,
        RaporTuru = SeciliMetin(CmbRaporTuru),
        Modul = SeciliMetin(CmbModul),
        HareketTipi = SeciliMetin(CmbHareketTipi),
        Saha = SeciliMetin(CmbSaha)
    };

    private static string SeciliMetin(ComboBox combo) =>
        (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? combo.Text;

    private string FiltreMetni() =>
        FinansmanRaporlamaServisi.FiltreOzetiMetni(FiltreOlustur());

    private void FiltreDegisti(object sender, RoutedEventArgs e) => _filtreZamanlayici.Tetikle();

    private void FiltreleriTemizle_Click(object sender, RoutedEventArgs e)
    {
        DpBaslangic.SelectedDate = null;
        DpBitis.SelectedDate = null;
        CmbRaporTuru.SelectedIndex = 0;
        CmbModul.SelectedIndex = 0;
        CmbHareketTipi.SelectedIndex = 0;
        CmbSaha.SelectedIndex = 0;
        RaporuYenile();
    }

    private void Yenile_Click(object sender, RoutedEventArgs e) => RaporuYenile();

    public void KisayolYenile() => _filtreZamanlayici.Hemen();

    private void RaporuYenile()
    {
        var filtre = FiltreOlustur();
        var tur = filtre.RaporTuru;

        SahaCombosunuGuncelle();

        _ozet = FinansmanRaporlamaServisi.GenelOzet(filtre);
        _modulOzetleri = FinansmanRaporlamaServisi.ModulOzetleri(filtre);
        _aylikOzetler = FinansmanRaporlamaServisi.AylikOzetler(filtre);
        _vadeler = tur == FinansmanTurleri.BekleyenOdemeler
            ? FinansmanRaporlamaServisi.BekleyenOdemeler(filtre)
            : FinansmanRaporlamaServisi.VadeSatirlari(filtre);

        _hareketler = tur switch
        {
            FinansmanTurleri.GiderDetayi => FinansmanRaporlamaServisi.GiderSatirlari(filtre),
            FinansmanTurleri.GelirDetayi => FinansmanRaporlamaServisi.GelirSatirlari(filtre),
            _ => FinansmanRaporlamaServisi.TumHareketler(filtre)
        };

        _grupOzetleri = tur switch
        {
            FinansmanTurleri.SahaOzeti => FinansmanRaporlamaServisi.SahaOzeti(filtre),
            FinansmanTurleri.TedarikciOzeti => FinansmanRaporlamaServisi.TedarikciOzeti(filtre),
            _ => []
        };

        OzetKartlariniGuncelle();
        TabloyuGuncelle(tur);
    }

    private void SahaCombosunuGuncelle()
    {
        var secili = CmbSaha.SelectedIndex >= 0 ? SeciliMetin(CmbSaha) : "Tümü";
        CmbSaha.SelectionChanged -= FiltreDegisti;
        CmbSaha.Items.Clear();
        CmbSaha.Items.Add(new ComboBoxItem { Content = "Tümü" });

        foreach (var saha in FinansmanRaporlamaServisi.TumSahalar())
            CmbSaha.Items.Add(new ComboBoxItem { Content = saha });

        var bulundu = false;
        for (var i = 0; i < CmbSaha.Items.Count; i++)
        {
            if ((CmbSaha.Items[i] as ComboBoxItem)?.Content?.ToString() == secili)
            {
                CmbSaha.SelectedIndex = i;
                bulundu = true;
                break;
            }
        }

        if (!bulundu)
            CmbSaha.SelectedIndex = 0;

        CmbSaha.SelectionChanged += FiltreDegisti;
    }

    private void OzetKartlariniGuncelle()
    {
        OzetPanel.Children.Clear();

        OzetPanel.Children.Add(OzetKarti("Toplam Gider", _ozet.ToplamGider, "#EF4444"));
        OzetPanel.Children.Add(OzetKarti("Toplam Gelir", _ozet.ToplamGelir, "#22C55E"));
        OzetPanel.Children.Add(OzetKarti("Net Nakit", _ozet.NetNakit, _ozet.NetNakit >= 0 ? "#0891B2" : "#EF4444"));
        OzetPanel.Children.Add(OzetKarti("Bekleyen Ödeme", _ozet.BekleyenOdeme, "#F59E0B"));
        OzetPanel.Children.Add(OzetKarti("Geciken Ödeme", _ozet.GecikenOdeme, "#DC2626"));
        OzetPanel.Children.Add(OzetKarti("Gider Kayıt", _ozet.GiderKayitSayisi, "#64748B", para: false));
        OzetPanel.Children.Add(OzetKarti("Gelir Kayıt", _ozet.GelirKayitSayisi, "#64748B", para: false));
        OzetPanel.Children.Add(OzetKarti("Açık Vade", _ozet.VadeKayitSayisi, "#8B5CF6", para: false));
    }

    private static Border OzetKarti(string baslik, decimal tutar, string renkHex, bool para = true)
    {
        var renk = (Color)ColorConverter.ConvertFromString(renkHex)!;
        var border = new Border { Style = (Style)Application.Current.FindResource("StatChipStyle"), MinWidth = 140 };
        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = baslik,
            FontSize = 11,
            Foreground = (Brush)Application.Current.FindResource("InkMutedBrush")
        });
        panel.Children.Add(new TextBlock
        {
            Text = para ? $"₺{tutar:N0}" : tutar.ToString("N0", CultureInfo.CurrentCulture),
            FontSize = para ? 20 : 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(renk),
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
            case FinansmanTurleri.FinansalOzet:
            case FinansmanTurleri.ModulDagilimi:
                AnaGrid.ItemsSource = _modulOzetleri;
                ModulGrid.ItemsSource = _modulOzetleri;
                TxtKayitSayisi.Text = $"{_modulOzetleri.Sum(o => o.KayitSayisi)} kayıt";
                TxtRaporToplam.Text = $"₺{_modulOzetleri.Sum(o => o.ToplamTutar):N0}";
                if (tur == FinansmanTurleri.FinansalOzet)
                {
                    AnaGrid.Visibility = Visibility.Collapsed;
                    RaporTab.Visibility = Visibility.Visible;
                    RaporTab.SelectedItem = TabModul;
                    AylikGrid.ItemsSource = _aylikOzetler;
                    VadeGrid.ItemsSource = _vadeler;
                }
                break;

            case FinansmanTurleri.NakitAkisi:
                TabloGoster(TabAylik);
                AylikGrid.ItemsSource = _aylikOzetler;
                TxtKayitSayisi.Text = $"{_aylikOzetler.Count} ay";
                TxtRaporToplam.Text = $"₺{_aylikOzetler.Sum(a => a.Net):N0} net";
                break;

            case FinansmanTurleri.VadeTakvimi:
            case FinansmanTurleri.BekleyenOdemeler:
                TabloGoster(TabVade);
                VadeGrid.ItemsSource = _vadeler;
                TxtKayitSayisi.Text = $"{_vadeler.Count} vade";
                TxtRaporToplam.Text = $"₺{_vadeler.Sum(v => v.KdvDahilTutar > 0 ? v.KdvDahilTutar : v.Tutar):N0}";
                break;

            case FinansmanTurleri.SahaOzeti:
                ColGrupAdi.Header = "Saha";
                TabloGoster(TabGrup);
                GrupGrid.ItemsSource = _grupOzetleri;
                TxtKayitSayisi.Text = $"{_grupOzetleri.Sum(g => g.KayitSayisi)} kayıt";
                TxtRaporToplam.Text = $"₺{_grupOzetleri.Sum(g => g.GiderTutar):N0} gider";
                break;

            case FinansmanTurleri.TedarikciOzeti:
                ColGrupAdi.Header = "Tedarikçi";
                TabloGoster(TabGrup);
                GrupGrid.ItemsSource = _grupOzetleri;
                TxtKayitSayisi.Text = $"{_grupOzetleri.Sum(g => g.KayitSayisi)} kayıt";
                TxtRaporToplam.Text = $"₺{_grupOzetleri.Sum(g => g.GiderTutar):N0}";
                break;

            default:
                TabloGoster(TabHareket);
                HareketGrid.ItemsSource = _hareketler;
                TxtKayitSayisi.Text = $"{_hareketler.Count} kayıt";
                TxtRaporToplam.Text = tur == FinansmanTurleri.GelirDetayi
                    ? $"₺{_hareketler.Sum(h => h.Tutar):N0}"
                    : $"₺{_hareketler.Where(h => h.Tip == FinansmanHareketTipleri.Gider).Sum(h => h.Tutar):N0}";
                break;
        }
    }

    private void TabloGoster(TabItem tab)
    {
        AnaGrid.Visibility = Visibility.Collapsed;
        RaporTab.Visibility = Visibility.Visible;
        RaporTab.SelectedItem = tab;
    }

    private void PdfIndir_Click(object sender, RoutedEventArgs e)
    {
        var filtre = FiltreOlustur();
        FinansmanPdfOlusturucu.Indir(filtre, _ozet, _modulOzetleri, _hareketler, _aylikOzetler, _vadeler, _grupOzetleri, FiltreMetni());
    }

    private void PdfYazdir_Click(object sender, RoutedEventArgs e)
    {
        var filtre = FiltreOlustur();
        FinansmanPdfOlusturucu.Yazdir(filtre, _ozet, _modulOzetleri, _hareketler, _aylikOzetler, _vadeler, _grupOzetleri, FiltreMetni());
    }

    private void ExcelAktar_Click(object sender, RoutedEventArgs e)
    {
        FinansmanExcelService.DisaAktar(
            SeciliMetin(CmbRaporTuru), FiltreMetni(), _ozet, _modulOzetleri,
            _hareketler, _aylikOzetler, _vadeler, _grupOzetleri);
    }

    private void GelirSablon_Click(object sender, RoutedEventArgs e) =>
        FinansmanExcelService.GelirSablonKaydet();

    private void GelirExcelYukle_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Gelir Excel Dosyası Seç",
            Filter = "Excel Dosyası (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var kayitlar = FinansmanExcelService.GelirDosyadanOku(dialog.FileName);
            if (kayitlar.Count == 0)
            {
                MessageBox.Show("Dosyada geçerli gelir kaydı bulunamadı.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var kayit in kayitlar)
                FinansmanVeriDeposu.Gelirler.Add(kayit);

            FinansmanVeriDeposu.Kaydet();
            MessageBox.Show($"{kayitlar.Count} gelir kaydı içe aktarıldı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            RaporuYenile();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Excel okunamadı:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void YeniGelir_Click(object sender, RoutedEventArgs e)
    {
        var kayit = new FinansmanGelirKaydi();
        var pencere = new FinansmanGelirDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };

        if (pencere.ShowDialog() == true)
        {
            FinansmanVeriDeposu.Gelirler.Add(kayit);
            FinansmanVeriDeposu.Kaydet();
            RaporuYenile();
        }
    }

    private void GelirDuzenle_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
        GelirDuzenle_Click(sender, e);

    private void GelirDuzenle_Click(object sender, RoutedEventArgs e)
    {
        var satir = (HareketGrid.SelectedItem ?? AnaGrid.SelectedItem) as FinansmanHareketSatiri;
        if (satir?.Tip != FinansmanHareketTipleri.Gelir)
            return;

        var kayit = FinansmanVeriDeposu.Gelirler.FirstOrDefault(k =>
            k.Tarih == satir.Tarih &&
            k.Tutar == satir.Tutar &&
            (k.Aciklama == satir.Aciklama || k.Kaynak == satir.Tedarikci));

        if (kayit is null)
            return;

        var pencere = new FinansmanGelirDuzenleWindow(kayit) { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() == true)
            RaporuYenile();
    }

    private void GelirSil_Click(object sender, RoutedEventArgs e)
    {
        var satir = HareketGrid.SelectedItem as FinansmanHareketSatiri;
        if (satir?.Tip != FinansmanHareketTipleri.Gelir)
            return;

        var kayit = FinansmanVeriDeposu.Gelirler.FirstOrDefault(k =>
            k.Tarih == satir.Tarih && k.Tutar == satir.Tutar);

        if (kayit is null)
            return;

        if (MessageBox.Show("Bu gelir kaydını silmek istiyor musunuz?", UygulamaBilgisi.Ad,
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        FinansmanVeriDeposu.Gelirler.Remove(kayit);
        FinansmanVeriDeposu.Kaydet();
        RaporuYenile();
    }
}
