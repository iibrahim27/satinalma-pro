using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SharedTalepDurumlari = SatinalmaPro.Shared.Models.SatinalmaTalepDurumlari;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class SatinalmaPanosuView : UserControl
{
    public event Action<string>? RouteIstendi;
    public event Action<Guid>? TalepAcIstendi;
    public event Action? Degisti;

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private int _yenilemeSira;
    private IReadOnlyList<PanosuAylikHarcama> _aylik = [];
    private IReadOnlyList<PanosuKritikSatir> _kritik = [];

    public SatinalmaPanosuView()
    {
        InitializeComponent();
        TxtYilEtiket.Text = DateTime.Now.Year.ToString(Tr);
    }

    public void Yenile()
    {
        var sira = ++_yenilemeSira;

        Task.Run(() =>
        {
            try
            {
                var kpi = SatinalmaPanosuVeriServisi.DashboardUstKpi();
                var aylik = SatinalmaPanosuVeriServisi.AylikHarcamaSerisi();
                var durum = SatinalmaPanosuVeriServisi.TalepDurumDagilimi();
                var kritik = SatinalmaPanosuVeriServisi.KritikBekleyenTalepler(5);
                var kategori = SatinalmaPanosuVeriServisi.KategoriHarcamaDagilimi(4);

                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    if (sira != _yenilemeSira)
                        return;

                    try
                    {
                        TxtKpiOnay.Text = kpi.OnayBekleyen;
                        TxtKpiTeklif.Text = kpi.TeklifSurecinde;
                        TxtKpiSiparis.Text = kpi.SipariseDonusen;
                        TxtKpiHarcama.Text = kpi.BuAyHarcama;
                        TxtKpiGeciken.Text = kpi.Geciken;

                        _aylik = aylik;
                        CizAylikGrafik();
                        CizDonut(durum);
                        _kritik = kritik;
                        KritikListe.ItemsSource = kritik;
                        CizKategori(kategori);
                        TxtSonGuncelleme.Text = $"Son güncelleme: Bugün {DateTime.Now:HH:mm}";
                    }
                    catch (Exception ex)
                    {
                        HataGunlugu.Kaydet(ex, "SatinalmaPanosu.YenileUi");
                        MessageBox.Show(
                            $"Satınalma panosu yüklenemedi:\n{ex.Message}",
                            UygulamaBilgisi.Ad,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    HataGunlugu.Kaydet(ex, "SatinalmaPanosu.Yenile");
                    MessageBox.Show(
                        $"Satınalma panosu yüklenemedi:\n{ex.Message}",
                        UygulamaBilgisi.Ad,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
            }
        });
    }

    public IReadOnlyList<SatinalmaPanosuTalepSatir> GorunenSatirlar() =>
        SatinalmaPanosuVeriServisi.SonTalepler(50);

    public void ExcelDisAktar() =>
        SatinalmaPanosuExcelService.TalepListesiKaydet(GorunenSatirlar());

    public void PdfIndir() =>
        SatinalmaPanosuPdfOlusturucu.TalepListesiIndir(GorunenSatirlar());

    public void PdfYazdir() =>
        SatinalmaPanosuPdfOlusturucu.TalepListesiYazdir(GorunenSatirlar());

    private void Yenile_Click(object sender, RoutedEventArgs e) => Yenile();

    private void AylikCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => CizAylikGrafik();

    private void CizAylikGrafik()
    {
        AylikCanvas.Children.Clear();
        if (_aylik.Count == 0) return;

        var w = AylikCanvas.ActualWidth;
        var h = AylikCanvas.ActualHeight;
        if (w < 40 || h < 40) return;

        var maxHarcama = (double)_aylik.Max(x => x.Harcama);
        var maxTalep = (double)_aylik.Max(x => x.TalepSayisi);
        if (maxHarcama <= 0) maxHarcama = 1;
        if (maxTalep <= 0) maxTalep = 1;

        const double left = 8, right = 8, top = 10, bottom = 28;
        var plotW = w - left - right;
        var plotH = h - top - bottom;
        var n = _aylik.Count;
        var slot = plotW / n;
        var barW = Math.Max(10, slot * 0.42);

        var teal = BrushHex("#07858E");
        var blue = BrushHex("#246FE5");
        var muted = BrushHex("#94A3B8");

        var line = new Polyline
        {
            Stroke = blue,
            StrokeThickness = 2.2,
            StrokeLineJoin = PenLineJoin.Round
        };

        for (var i = 0; i < n; i++)
        {
            var item = _aylik[i];
            var cx = left + slot * i + slot / 2;
            var barH = (double)item.Harcama / maxHarcama * plotH;
            var bar = new Rectangle
            {
                Width = barW,
                Height = Math.Max(2, barH),
                Fill = teal,
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(bar, cx - barW / 2);
            Canvas.SetTop(bar, top + plotH - bar.Height);
            AylikCanvas.Children.Add(bar);

            var ly = top + plotH - item.TalepSayisi / maxTalep * plotH;
            line.Points.Add(new Point(cx, ly));

            var lbl = new TextBlock
            {
                Text = item.Etiket,
                FontSize = 11,
                Foreground = muted,
                Width = slot,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(lbl, left + slot * i);
            Canvas.SetTop(lbl, h - 22);
            AylikCanvas.Children.Add(lbl);
        }

        AylikCanvas.Children.Add(line);

        foreach (Point p in line.Points)
        {
            var dot = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = blue,
                Stroke = Brushes.White,
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(dot, p.X - 3.5);
            Canvas.SetTop(dot, p.Y - 3.5);
            AylikCanvas.Children.Add(dot);
        }
    }

    private void CizDonut(IReadOnlyList<PanosuDurumDilimi> dilimler)
    {
        DonutCanvas.Children.Clear();
        DonutLegend.Children.Clear();
        var toplam = dilimler.Sum(d => d.Adet);
        TxtDonutToplam.Text = toplam.ToString("N0", Tr);
        if (dilimler.Count == 0 || toplam == 0)
            return;

        const double cx = 80, cy = 80, r = 62, inner = 40;
        var start = -90.0;

        foreach (var d in dilimler)
        {
            if (d.Adet <= 0) continue;
            var sweep = d.Adet * 360.0 / toplam;
            DonutCanvas.Children.Add(DonutDilim(cx, cy, r, inner, start, sweep, BrushHex(d.RenkHex)));
            start += sweep;

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            row.Children.Add(new Border
            {
                Width = 10,
                Height = 10,
                CornerRadius = new CornerRadius(2),
                Background = BrushHex(d.RenkHex),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(new TextBlock
            {
                Text = $"{d.Etiket}  {d.Adet}  (%{d.Yuzde:0})",
                FontSize = 12,
                Foreground = BrushHex("#10233F"),
                VerticalAlignment = VerticalAlignment.Center
            });
            DonutLegend.Children.Add(row);
        }
    }

    private void CizKategori(IReadOnlyList<PanosuKategoriHarcama> kategoriler)
    {
        KategoriPanel.Children.Clear();
        if (kategoriler.Count == 0)
        {
            KategoriPanel.Children.Add(new TextBlock
            {
                Text = "Henüz kategori harcaması yok.",
                FontSize = 12,
                Foreground = BrushHex("#607089"),
                Margin = new Thickness(0, 8, 0, 0)
            });
            return;
        }

        var max = kategoriler.Max(k => k.Tutar);
        if (max <= 0) max = 1;

        foreach (var k in kategoriler)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var ad = new TextBlock
            {
                Text = k.Etiket,
                FontSize = 12,
                Foreground = BrushHex("#10233F"),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            row.Children.Add(ad);

            var barHost = new Grid { Height = 10, Margin = new Thickness(8, 0, 12, 0) };
            barHost.Children.Add(new Border
            {
                Background = BrushHex("#EEF2F6"),
                CornerRadius = new CornerRadius(5)
            });
            var fill = new Border
            {
                Background = BrushHex("#07858E"),
                CornerRadius = new CornerRadius(5),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 8
            };
            barHost.Children.Add(fill);
            var oran = (double)(k.Tutar / max);
            barHost.SizeChanged += (_, _) =>
            {
                fill.Width = Math.Max(8, barHost.ActualWidth * oran);
            };
            Grid.SetColumn(barHost, 1);
            row.Children.Add(barHost);

            var metin = new TextBlock
            {
                Text = $"{k.Tutar.ToString("C0", Tr)}  %{k.Yuzde.ToString("0.#", Tr)}",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushHex("#10233F"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(metin, 2);
            row.Children.Add(metin);

            KategoriPanel.Children.Add(row);
        }
    }

    private static Path DonutDilim(double cx, double cy, double r, double inner,
        double startDeg, double sweepDeg, Brush fill)
    {
        var start = startDeg * Math.PI / 180.0;
        var end = (startDeg + sweepDeg) * Math.PI / 180.0;
        var large = sweepDeg > 180 ? 1 : 0;

        var x1 = cx + r * Math.Cos(start);
        var y1 = cy + r * Math.Sin(start);
        var x2 = cx + r * Math.Cos(end);
        var y2 = cy + r * Math.Sin(end);
        var ix1 = cx + inner * Math.Cos(end);
        var iy1 = cy + inner * Math.Sin(end);
        var ix2 = cx + inner * Math.Cos(start);
        var iy2 = cy + inner * Math.Sin(start);

        var fig = new PathFigure { StartPoint = new Point(x1, y1), IsClosed = true };
        fig.Segments.Add(new ArcSegment(new Point(x2, y2), new Size(r, r), 0, large == 1, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(new Point(ix1, iy1), true));
        fig.Segments.Add(new ArcSegment(new Point(ix2, iy2), new Size(inner, inner), 0, large == 1, SweepDirection.Counterclockwise, true));
        return new Path { Fill = fill, Data = new PathGeometry([fig]) };
    }

    private static Brush BrushHex(string hex) =>
        (Brush)new BrushConverter().ConvertFromString(hex)!;

    private void KpiOnay_Click(object sender, MouseButtonEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.YonetimTeklifGirilen);

    private void KpiTeklif_Click(object sender, MouseButtonEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.SatinalmaTeklifIstenen);

    private void KpiSiparis_Click(object sender, MouseButtonEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.SatinalmaSiparis);

    private void KpiGeciken_Click(object sender, MouseButtonEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.YonetimGelenTalepler);

    private void TumKritik_Click(object sender, RoutedEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.YonetimGelenTalepler);

    private void Kritik_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (KritikListe.SelectedItem is PanosuKritikSatir satir)
            TalepAcIstendi?.Invoke(satir.Id);
    }

    private void HizliYeniTalep_Click(object sender, MouseButtonEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.SatinalmaTalep);

    private void HizliTeklifIstemi_Click(object sender, MouseButtonEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.YonetimGelenTalepler);

    private void HizliTeklifOnay_Click(object sender, MouseButtonEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.YonetimTeklifGirilen);

    private void HizliKarsilastirma_Click(object sender, MouseButtonEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.SatinalmaKarsilastirma);

    public static string TalepIcinRoute(SatinalmaTalep talep) => talep.Durum switch
    {
        SharedTalepDurumlari.YonetimOnayinda => SatinalmaPart1Menusu.YonetimGelenTalepler,
        SharedTalepDurumlari.TeklifGirisi => SatinalmaPart1Menusu.SatinalmaTeklifIstenen,
        SharedTalepDurumlari.Karsilastirma => SatinalmaPart1Menusu.SatinalmaKarsilastirma,
        SharedTalepDurumlari.Onaylandi => SatinalmaPart1Menusu.SatinalmaOnaylanan,
        SharedTalepDurumlari.SiparisOlusturuldu => SatinalmaPart1Menusu.SatinalmaSiparis,
        SharedTalepDurumlari.Reddedildi => SatinalmaPart1Menusu.YonetimRedVerilen,
        _ => SatinalmaPart1Menusu.SatinalmaTalepler
    };
}
