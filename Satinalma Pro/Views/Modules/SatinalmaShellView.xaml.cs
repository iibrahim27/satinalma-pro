using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Controls.Dashboard;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaShellView : UserControl, IModulKlavyeKisayollari
{
    private readonly SatinalmaOverviewView _overview;
    private readonly SatinalmaView _operasyon;
    private readonly Dictionary<string, Button> _navButtons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (Border Rozet, TextBlock Metin)> _navRozetleri = new(StringComparer.Ordinal);
    private string _aktifId = SatinalmaNavYapisi.GenelBakisId;

    public SatinalmaShellView()
    {
        InitializeComponent();

        _overview = new SatinalmaOverviewView();
        _overview.SekmeIstendi += SekmeAc;

        _operasyon = new SatinalmaView { ShellModunda = true };

        Loaded += (_, _) =>
        {
            NavigasyonuOlustur();
            GenelBakisaGec();
        };

        SatinalmaDepo.TaleplerGuncellendi += NavRozetleriniGuncelle;
        Unloaded += (_, _) => SatinalmaDepo.TaleplerGuncellendi -= NavRozetleriniGuncelle;
    }

    public void KisayolYenile()
    {
        _overview.Yenile();
        _operasyon.KisayolYenile();
        NavRozetleriniGuncelle();
    }

    public bool EscapeTusunuIsle() => _operasyon.EscapeTusunuIsle();

    public void BildirimdenAc(Guid? talepId, int adim = 0, string sekme = "talepler")
    {
        sekme = MasaustuRolHaritasi.SatinalmaRouteSlug(sekme) ?? sekme;
        var hedef = sekme switch
        {
            "teklifler" or "teklif-bekleyen" => "Teklif Bekleyen",
            "teklif-gir" or "teklif-giris" => "Teklif Girişi",
            "gelen-talepler" => "Gelen Talepler",
            "teklif-onay" => "Teklif Onay",
            "onaylar" or "onaylanan" or "onaylanan-talepler" => "Onaylanan Talepler",
            "onay-bekleyen" or "bekleyen" => "Onay Bekleyen",
            "gecmis-talepler" or "onay-gecmisi" => "Geçmiş Talepler",
            "gecmis-teklifli-onaylar" => "Geçmiş Teklifli Onaylar",
            "siparisler" or "alinan-malzemeler" or "onaylanan-malzemeler" => "Alınan Malzemeler",
            "teklif-karsilastirma" or "karsilastirma" =>
                KullaniciYetkileri.YonetimOnayModu() ? "Teklif Onay" : "Karşılaştırma",
            "red" or "reddedilen" or "red-talepler" => "Red Talepler",
            _ => MasaustuRolHaritasi.SatinalmaSekmeNormalize(sekme)
        };

        if (string.IsNullOrWhiteSpace(hedef) || hedef == SatinalmaNavYapisi.GenelBakisId)
            hedef = "Taleplerim";

        NavigasyonSec(hedef, talepId, adim);
    }

    public void SekmeAc(string sekmeAdi) => NavigasyonSec(sekmeAdi, null, 0);

    private void NavigasyonuOlustur()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();
        _navRozetleri.Clear();

        foreach (var grup in SatinalmaNavYapisi.TumGruplar)
        {
            var gorunen = grup.Ogeler.Any(o =>
                o.Id == SatinalmaNavYapisi.GenelBakisId
                || (o.SekmeAdi is not null && KullaniciYetkileri.SekmeGorebilir("Satınalma", o.SekmeAdi)));

            if (!gorunen)
                continue;

            NavPanel.Children.Add(new TextBlock
            {
                Text = grup.Baslik.ToUpperInvariant(),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = AppTheme.SecondaryTextBrush,
                Margin = new Thickness(12, 14, 0, 8)
            });

            foreach (var oge in grup.Ogeler)
            {
                if (oge.SekmeAdi is not null && !KullaniciYetkileri.SekmeGorebilir("Satınalma", oge.SekmeAdi))
                    continue;

                NavPanel.Children.Add(NavButonuOlustur(oge));
            }
        }

        NavRozetleriniGuncelle();
    }

    private Button NavButonuOlustur(SatinalmaNavOge oge)
    {
        var btn = new Button
        {
            Style = (Style?)TryFindResource("DashNavButtonStyle")
                ?? Application.Current.TryFindResource("DashNavButtonStyle") as Style,
            Tag = oge.Id,
            Margin = new Thickness(0, 0, 0, 2)
        };
        btn.Click += (_, _) => NavigasyonSec(oge.Id, null, 0);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var icon = new IconControl
        {
            Kind = oge.Icon,
            IconSize = 17,
            StrokeBrush = AppTheme.SecondaryTextBrush,
            Margin = new Thickness(8, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(icon);

        var metin = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        metin.Children.Add(new TextBlock
        {
            Text = oge.Baslik,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = AppTheme.TextBrush
        });
        Grid.SetColumn(metin, 1);
        grid.Children.Add(metin);

        if (oge.SekmeAdi is not null)
        {
            var rozet = new Border
            {
                MinWidth = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = AppTheme.TintBrush(AppTheme.Primary, 40),
                Padding = new Thickness(6, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(6, 0, 8, 0)
            };
            var rozetMetin = new TextBlock
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = AppTheme.PrimaryBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            rozet.Child = rozetMetin;
            Grid.SetColumn(rozet, 2);
            grid.Children.Add(rozet);
            _navRozetleri[oge.Id] = (rozet, rozetMetin);
        }

        btn.Content = grid;
        _navButtons[oge.Id] = btn;
        return btn;
    }

    private void NavigasyonSec(string id, Guid? talepId, int adim)
    {
        if (id != SatinalmaNavYapisi.GenelBakisId && SatinalmaNavYapisi.Bul(id)?.SekmeAdi is { } sekme)
            id = sekme;

        if (id == SatinalmaNavYapisi.GenelBakisId)
        {
            GenelBakisaGec();
            return;
        }

        if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", id))
            return;

        _aktifId = id;
        AktifNavGuncelle();

        var oge = SatinalmaNavYapisi.Bul(id);
        TxtBaslik.Text = oge?.Baslik ?? id;
        TxtAciklama.Text = oge?.Aciklama ?? "";
        BtnYeniTalep.Visibility = id == "Taleplerim" ? Visibility.Visible : Visibility.Collapsed;

        IcerikAlani.Content = _operasyon;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            _operasyon.SekmeAc(id);
            if (talepId.HasValue)
            {
                var slug = MasaustuRolHaritasi.SatinalmaRouteSlug(id) ?? "talepler";
                _operasyon.BildirimdenAc(talepId, adim, slug);
            }
        });
    }

    private void GenelBakisaGec()
    {
        _aktifId = SatinalmaNavYapisi.GenelBakisId;
        AktifNavGuncelle();
        TxtBaslik.Text = "Genel Bakış";
        TxtAciklama.Text = "Satınalma süreçlerinizin özeti";
        BtnYeniTalep.Visibility = Visibility.Collapsed;
        IcerikAlani.Content = _overview;
        _overview.Yenile();
    }

    private void AktifNavGuncelle()
    {
        foreach (var (id, btn) in _navButtons)
            btn.Tag = id == _aktifId ? "Active" : null;
    }

    private void NavRozetleriniGuncelle()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(NavRozetleriniGuncelle);
            return;
        }

        foreach (var (id, (rozet, metin)) in _navRozetleri)
        {
            var oge = SatinalmaNavYapisi.Bul(id);
            if (oge?.SekmeAdi is null)
                continue;

            var sayi = SatinalmaSekmeSayaclari.Say(oge.SekmeAdi);
            rozet.Visibility = sayi > 0 ? Visibility.Visible : Visibility.Collapsed;
            metin.Text = sayi > 99 ? "99+" : sayi.ToString();
        }

        if (IcerikAlani.Content is SatinalmaOverviewView)
            _overview.Yenile();
    }

    private void BtnYeniTalep_Click(object sender, RoutedEventArgs e)
    {
        NavigasyonSec("Taleplerim", null, 0);
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            _operasyon.YeniTalepBaslat());
    }

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();
}
