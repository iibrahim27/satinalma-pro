using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
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

    private List<SatinalmaPanosuTalepSatir> _tumSatirlar = [];
    private string _aktifTab = "Tumu";
    private int _yenilemeSira;

    public SatinalmaPanosuView()
    {
        InitializeComponent();
    }

    public void Yenile()
    {
        var sira = ++_yenilemeSira;

        Task.Run(() =>
        {
            try
            {
                var adimlar = SatinalmaPanosuVeriServisi.WorkflowAdimlari();
                var kpis = SatinalmaPanosuVeriServisi.OzetKpi();
                var satirlar = SatinalmaPanosuVeriServisi.SonTalepler(50).ToList();
                var aylik = SatinalmaPanosuVeriServisi.AylikSatinalma();
                var kategori = SatinalmaPanosuVeriServisi.KategoriDagilimi();

                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    if (sira != _yenilemeSira)
                        return;

                    try
                    {
                        WorkflowOlustur(adimlar);
                        KpiOlustur(kpis);
                        _tumSatirlar = satirlar;
                        TabloyuFiltrele();
                        AylikGrafik.Bagla(aylik);
                        KategoriGrafik.Bagla(kategori);
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
        TalepGrid.ItemsSource as IReadOnlyList<SatinalmaPanosuTalepSatir>
        ?? (TalepGrid.ItemsSource as IEnumerable<SatinalmaPanosuTalepSatir>)?.ToList()
        ?? _tumSatirlar;

    public void ExcelDisAktar() =>
        SatinalmaPanosuExcelService.TalepListesiKaydet(GorunenSatirlar());

    public void PdfIndir() =>
        SatinalmaPanosuPdfOlusturucu.TalepListesiIndir(GorunenSatirlar());

    public void PdfYazdir() =>
        SatinalmaPanosuPdfOlusturucu.TalepListesiYazdir(GorunenSatirlar());

    private void WorkflowOlustur(IReadOnlyList<SatinalmaWorkflowAdim> adimlar)
    {
        WorkflowPanel.Children.Clear();

        for (var i = 0; i < adimlar.Count; i++)
        {
            if (i > 0)
                WorkflowPanel.Children.Add(Oklar());

            var adim = adimlar[i];
            var kart = new Border
            {
                Style = KaynakStili("SatModWorkflowCard"),
                Tag = adim.Route,
                Width = 168,
                Margin = new Thickness(0, 0, 0, 0)
            };
            kart.MouseLeftButtonUp += (_, _) =>
            {
                if (!string.IsNullOrEmpty(adim.Route))
                    RouteIstendi?.Invoke(adim.Route);
            };

            var renk = (Color)ColorConverter.ConvertFromString(adim.RenkHex)!;
            var ikonZemin = new SolidColorBrush(renk) { Opacity = 0.12 };

            kart.Child = new StackPanel
            {
                Children =
                {
                    new Grid
                    {
                        Children =
                        {
                            new Border
                            {
                                Width = 36, Height = 36, CornerRadius = new CornerRadius(10),
                                Background = ikonZemin, HorizontalAlignment = HorizontalAlignment.Left,
                                Child = new TextBlock
                                {
                                    Text = adim.Ikon, FontFamily = new FontFamily("Segoe MDL2 Assets"),
                                    FontSize = 16, Foreground = new SolidColorBrush(renk),
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center
                                }
                            },
                            new TextBlock
                            {
                                Text = adim.Adet.ToString(), FontSize = 22, FontWeight = FontWeights.Bold,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Foreground = KaynakFircasi("DashTextBrush")

                            }
                        }
                    },
                    new TextBlock
                    {
                        Text = adim.Baslik, FontSize = 12, FontWeight = FontWeights.SemiBold,
                        Foreground = KaynakFircasi("DashTextBrush"), Margin = new Thickness(0, 10, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = adim.SonHareket, FontSize = 10,
                        Foreground = KaynakFircasi("InkMutedBrush"),
                        Margin = new Thickness(0, 6, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis
                    }
                }
            };

            WorkflowPanel.Children.Add(kart);
        }
    }

    private static TextBlock Oklar() => new()
    {
        Text = "→",
        FontSize = 18,
        Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(8, 0, 8, 0)
    };

    private void KpiOlustur(IReadOnlyList<SatinalmaPanosuOzetKpi> kpis)
    {
        KpiPanel.Children.Clear();
        foreach (var kpi in kpis)
        {
            var renk = (Color)ColorConverter.ConvertFromString(kpi.RenkHex)!;
            var kart = new Border { Style = KaynakStili("SatModMiniKpiCard"), Margin = new Thickness(0, 0, 8, 8) };
            kart.Child = new StackPanel
            {
                Children =
                {
                    new Border
                    {
                        Width = 32, Height = 32, CornerRadius = new CornerRadius(8),
                        Background = new SolidColorBrush(renk) { Opacity = 0.12 },
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Child = new TextBlock
                        {
                            Text = kpi.Ikon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14,
                            Foreground = new SolidColorBrush(renk),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    },
                    new TextBlock
                    {
                        Text = kpi.Baslik, FontSize = 11, Foreground = KaynakFircasi("InkMutedBrush"),
                        Margin = new Thickness(0, 8, 0, 0)
                    },
                    new TextBlock
                    {
                        Text = kpi.Deger, FontSize = 18, FontWeight = FontWeights.Bold,
                        Foreground = KaynakFircasi("DashTextBrush"), Margin = new Thickness(0, 2, 0, 0)
                    },
                    new TextBlock
                    {
                        Text = kpi.Alt, FontSize = 10, Foreground = KaynakFircasi("InkMutedBrush"),
                        Margin = new Thickness(0, 2, 0, 0)
                    }
                }
            };
            KpiPanel.Children.Add(kart);
        }
    }

    private void TabloyuFiltrele()
    {
        var arama = TxtArama.Text.Trim();
        IEnumerable<SatinalmaPanosuTalepSatir> liste = _tumSatirlar;

        liste = _aktifTab switch
        {
            "Bekleyen" => liste.Where(s => s.Durum is "Bekliyor" or "Karşılaştırılıyor"),
            "Teklif" => liste.Where(s => s.Durum == "Teklif Geldi"),
            "Onay" => liste.Where(s => s.Durum == "Onaylandı"),
            "Siparis" => liste.Where(s => s.Durum == "Sipariş"),
            _ => liste
        };

        if (!string.IsNullOrEmpty(arama))
        {
            liste = liste.Where(s =>
                string.Join(" ", s.TalepNo, s.TalepEden, s.Santiye, s.Malzeme, s.Kategori, s.Durum)
                    .Contains(arama, StringComparison.OrdinalIgnoreCase));
        }

        TalepGrid.ItemsSource = liste.ToList();
    }

    private void AramaDegisti(object sender, TextChangedEventArgs e) => TabloyuFiltrele();

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag)
            return;

        _aktifTab = tag;
        foreach (var b in new[] { TabTumu, TabBekleyen, TabTeklif, TabOnay, TabSiparis })
            b.Style = (Style)FindResource("SatModTabButton");

        btn.Style = (Style)FindResource("SatModTabButtonActive");
        TabloyuFiltrele();
    }

    private void TumTalepler_Click(object sender, RoutedEventArgs e) =>
        RouteIstendi?.Invoke(SatinalmaPart1Menusu.SatinalmaTalepler);

    private void TalepGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void TalepGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TalepGrid.SelectedItem is SatinalmaPanosuTalepSatir satir)
            TalepAcIstendi?.Invoke(satir.Id);
    }

    private void SatirMenu_Tikla(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SatinalmaPanosuTalepSatir satir })
            return;

        var menu = new ContextMenu { PlacementTarget = sender as UIElement, Placement = PlacementMode.Bottom };
        Ekle(menu, "Talebi Aç", () => TalepAcIstendi?.Invoke(satir.Id));
        Ekle(menu, "Teklifleri Gör", () => RouteIstendi?.Invoke(SatinalmaPart1Menusu.SatinalmaTeklifGirilen));
        Ekle(menu, "Karşılaştır", () => RouteIstendi?.Invoke(SatinalmaPart1Menusu.SatinalmaKarsilastirma));
        Ekle(menu, "Sipariş Oluştur", () => RouteIstendi?.Invoke(SatinalmaPart1Menusu.SatinalmaSiparis));
        menu.Items.Add(new Separator());
        Ekle(menu, "PDF", () => TalepPdfAc(satir.Id));
        Ekle(menu, "Yazdır", () => TalepPdfAc(satir.Id));
        Ekle(menu, "Sil", () => TalepSil(satir.Id));
        menu.IsOpen = true;
    }

    private static void TalepPdfAc(Guid talepId)
    {
        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talepId);
        if (talep is null)
            return;

        SatinalmaPdfOlusturucu.TalepFormuYazdir(talep, SatinalmaDepo.Ayarlar);
    }

    private async void TalepSil(Guid talepId)
    {
        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talepId);
        if (talep is null)
            return;

        if (!KullaniciYetkileri.SatinalmaTalepSilebilir(talep))
        {
            MessageBox.Show("Bu talebi silme yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var onay = MessageBox.Show($"{talep.TalepNo} silinsin mi?", UygulamaBilgisi.Ad,
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaPart1Servisi.SilAsync(talep);
            Yenile();
            Degisti?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Style KaynakStili(string anahtar) =>
        (Style)(Application.Current.TryFindResource(anahtar)
            ?? throw new InvalidOperationException($"Stil bulunamadı: {anahtar}"));

    private static Brush KaynakFircasi(string anahtar) =>
        Application.Current.TryFindResource(anahtar) as Brush ?? Brushes.Black;

    private static void Ekle(ContextMenu menu, string baslik, Action action)
    {
        var item = new MenuItem { Header = baslik };
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

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
