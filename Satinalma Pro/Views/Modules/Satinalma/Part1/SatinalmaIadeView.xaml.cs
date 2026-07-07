using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SatinalmaPro.Services;
using SatinalmaPro.Views.Modules;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class SatinalmaIadeView : UserControl
{
    private List<IadeSatirModel> _tumSatirlar = [];

    public SatinalmaIadeView()
    {
        InitializeComponent();
        BtnYeniIade.Visibility = KullaniciYetkileri.MalKabulVeStokAktarYapabilir()
            ? Visibility.Visible
            : Visibility.Collapsed;
        Loaded += (_, _) => Yenile();
    }

    public void Yenile() => _ = YenileAsync();

    public async Task YenileAsync()
    {
        if (OturumYoneticisi.BulutAktif && OturumYoneticisi.GirisYapildi)
            await IadeDeposu.YukleAsync();
        else
            IadeDeposu.YerelYukle();

        _tumSatirlar = SatinalmaMerkeziVeriServisi.Iadeler().ToList();
        KpiOlustur();
        TabloyuFiltrele();
    }

    public IReadOnlyList<IadeSatirModel> GorunenSatirlar() =>
        IadeGrid.ItemsSource as IReadOnlyList<IadeSatirModel>
        ?? (IadeGrid.ItemsSource as IEnumerable<IadeSatirModel>)?.ToList()
        ?? _tumSatirlar;

    public void ExcelDisAktar() =>
        SatinalmaPanosuExcelService.IadeListesiKaydet(GorunenSatirlar());

    public void PdfIndir() =>
        SatinalmaPanosuPdfOlusturucu.IadeListesiIndir(GorunenSatirlar());

    public void PdfYazdir() =>
        SatinalmaPanosuPdfOlusturucu.IadeListesiYazdir(GorunenSatirlar());

    private void YeniIade_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
        {
            MessageBox.Show(
                "İade işlemi yalnızca Satınalma rolü tarafından yapılabilir.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var pencere = new IadeOlusturWindow { Owner = Window.GetWindow(this) };
        if (pencere.ShowDialog() == true)
            Yenile();
    }

    private void KpiOlustur()
    {
        KpiPanel.Children.Clear();
        var toplam = _tumSatirlar.Count;
        var inceleme = _tumSatirlar.Count(s => s.Durum.Contains("nceleme", StringComparison.OrdinalIgnoreCase));
        var onay = _tumSatirlar.Count(s => s.Durum.Contains("Onay", StringComparison.OrdinalIgnoreCase));

        KpiEkle("Toplam İade", toplam.ToString(), "Kayıtlı iade", "#2563EB");
        KpiEkle("İncelemede", inceleme.ToString(), "Aktif süreç", "#F59E0B");
        KpiEkle("Onaylandı", onay.ToString(), "Tamamlanan", "#16A34A");
    }

    private void KpiEkle(string baslik, string deger, string alt, string renkHex)
    {
        var renk = (Color)ColorConverter.ConvertFromString(renkHex)!;
        var kart = new Border
        {
            Style = (Style)FindResource("SatModMiniKpiCard"),
            Margin = new Thickness(0, 0, 12, 0)
        };
        kart.Child = new StackPanel
        {
            Children =
            {
                new TextBlock
                {
                    Text = baslik, FontSize = 11, Foreground = (Brush)FindResource("InkMutedBrush")
                },
                new TextBlock
                {
                    Text = deger, FontSize = 22, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(renk), Margin = new Thickness(0, 4, 0, 0)
                },
                new TextBlock
                {
                    Text = alt, FontSize = 10, Foreground = (Brush)FindResource("InkMutedBrush"),
                    Margin = new Thickness(0, 2, 0, 0)
                }
            }
        };
        KpiPanel.Children.Add(kart);
    }

    private void TabloyuFiltrele()
    {
        var arama = TxtArama.Text.Trim();
        var liste = string.IsNullOrEmpty(arama)
            ? _tumSatirlar
            : _tumSatirlar.Where(s =>
                string.Join(" ", s.IadeNo, s.SiparisNo, s.Firma, s.Malzeme, s.Neden, s.Durum)
                    .Contains(arama, StringComparison.OrdinalIgnoreCase)).ToList();

        IadeGrid.ItemsSource = liste;
        var bos = liste.Count == 0;
        BosPanel.Visibility = bos ? Visibility.Visible : Visibility.Collapsed;
        IadeGrid.Visibility = bos ? Visibility.Collapsed : Visibility.Visible;
    }

    private void AramaDegisti(object sender, TextChangedEventArgs e) => TabloyuFiltrele();
}
