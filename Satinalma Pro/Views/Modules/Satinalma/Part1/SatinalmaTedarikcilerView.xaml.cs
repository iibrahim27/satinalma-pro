using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class SatinalmaTedarikcilerView : UserControl
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private List<TedarikciPerformansModel> _tumSatirlar = [];

    public SatinalmaTedarikcilerView()
    {
        InitializeComponent();
        Loaded += (_, _) => Yenile();
    }

    public void Yenile()
    {
        _tumSatirlar = SatinalmaMerkeziVeriServisi.TedarikciPerformans().ToList();
        KpiOlustur();
        TabloyuFiltrele();
    }

    public IReadOnlyList<TedarikciPerformansModel> GorunenSatirlar() =>
        TedarikciGrid.ItemsSource as IReadOnlyList<TedarikciPerformansModel>
        ?? (TedarikciGrid.ItemsSource as IEnumerable<TedarikciPerformansModel>)?.ToList()
        ?? _tumSatirlar;

    public void ExcelDisAktar() =>
        SatinalmaPanosuExcelService.TedarikciListesiKaydet(GorunenSatirlar());

    public void PdfIndir() =>
        SatinalmaPanosuPdfOlusturucu.TedarikciListesiIndir(GorunenSatirlar());

    public void PdfYazdir() =>
        SatinalmaPanosuPdfOlusturucu.TedarikciListesiYazdir(GorunenSatirlar());

    private void KpiOlustur()
    {
        KpiPanel.Children.Clear();
        var firma = _tumSatirlar.Count;
        var siparis = _tumSatirlar.Sum(s => s.ToplamSiparis);
        var tutar = _tumSatirlar.Sum(s => s.ToplamTutar);
        var ortPuan = firma == 0 ? 0 : (int)Math.Round(_tumSatirlar.Average(s => s.PerformansPuani));

        KpiEkle("Tedarikçi", firma.ToString("N0", Tr), "Aktif firma", "#2563EB");
        KpiEkle("Toplam Sipariş", siparis.ToString("N0", Tr), "Tamamlanan", "#0891B2");
        KpiEkle("Toplam Tutar", $"₺{tutar:N0}", "Sipariş hacmi", "#8B5CF6");
        KpiEkle("Ort. Performans", ortPuan.ToString("N0", Tr), "Puan ortalaması", "#16A34A");
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
                    Text = deger, FontSize = 20, FontWeight = FontWeights.Bold,
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
            : _tumSatirlar.Where(s => s.Firma.Contains(arama, StringComparison.OrdinalIgnoreCase)).ToList();

        TedarikciGrid.ItemsSource = liste;
    }

    private void AramaDegisti(object sender, TextChangedEventArgs e) => TabloyuFiltrele();
}
