using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Procurement.Detail;
using TalepProRuntime = SatinalmaPro.Shared.Helpers.TalepProRuntime;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Views.Modules.Satinalma;
using SatinalmaPro.Views.Modules.Satinalma.Part1;
using SatinalmaPro.Views;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaShellView : UserControl, IModulKlavyeKisayollari
{
    public event Action? StokModuluIstendi;
    public event Action? OturumKapatIstendi;

    private readonly Dictionary<string, Button> _navButtons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TextBlock> _navRozetleri = new(StringComparer.Ordinal);

    private TalepFormView? _talepForm;
    private TalepListesiView? _liste;
    private OnaylananTalepListesiView? _onaylananListe;
    private SiparisVerilenTalepListesiView? _siparisListe;
    private MalKabulTalepListesiView? _malKabulListe;
    private GelenTalepDetayView? _gelenDetay;
    private YonetimTalepDetayView? _yonetimDetay;
    private OnaylananTalepDetayView? _onaylananDetay;
    private SiparisVerilenTalepDetayView? _siparisDetay;
    private MalKabulTalepDetayView? _malKabulDetay;
    private TeklifGirisView? _teklifGiris;
    private SatinalmaBosSekmeView? _bosSekme;
    private SatinalmaPanosuView? _panosu;
    private SatinalmaIadeView? _iade;
    private SatinalmaTedarikcilerView? _tedarikciler;

    private string _aktifRoute = SatinalmaPart1Menusu.SatinalmaPanosu;
    private bool _rozetGuncelleniyor;
    private DispatcherTimer? _rozetZamanlayici;
    private int _panoYenilemeSira;
    private bool _menuDaraltildi;
    private readonly Dictionary<string, Border> _navRozetCerceveleri = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Border> _navAccentBarlari = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TextBlock> _navMetinleri = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TextBlock> _navIkonlari = new(StringComparer.Ordinal);

    public SatinalmaShellView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            try
            {
                TalepProRuntime.EtkinlestirGerekirse();
                SatinalmaMasaustuSifirlama.IlkAcilistaUygula();
                SatinalmaDepo.Yukle();

                KullaniciPaneliniGuncelle();
                NavigasyonuOlustur();
                BildirimRozetiniGuncelle();
                CikisButonlariniGuncelle();
                var rol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
                var ilkRoute = SatinalmaPart1Menusu.IlkRoute(rol);
                Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    () => RouteAc(ilkRoute, null));
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "SatinalmaShell.Loaded");
                MessageBox.Show(
                    $"Satınalma modülü açılamadı:\n{ex.Message}",
                    UygulamaBilgisi.Ad,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };

        SatinalmaDepo.TaleplerGuncellendi += IcerikYenile;
        BildirimYoneticisi.BildirimlerDegisti += BildirimRozetiniGuncelle;
        Unloaded += (_, _) =>
        {
            SatinalmaDepo.TaleplerGuncellendi -= IcerikYenile;
            BildirimYoneticisi.BildirimlerDegisti -= BildirimRozetiniGuncelle;
            OturumYoneticisi.OturumDegisti -= OturumDegistiIsle;
        };

        OturumYoneticisi.OturumDegisti += OturumDegistiIsle;
    }

    private void KullaniciPaneliniGuncelle()
    {
        var ad = OturumYoneticisi.AktifKullanici?.AdSoyad
                 ?? KullaniciYetkileri.AktifKullaniciAdi()
                 ?? "Kullanıcı";
        var rol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
        var rolEtiket = string.IsNullOrWhiteSpace(rol) ? "Satınalma" : KullaniciRolleri.GorunenAd(rol);
        TxtKullaniciAd.Text = ad;
        TxtKullaniciRol.Text = rolEtiket;
        var bas = AvatarBasHarfler(ad);
        TxtAvatar.Text = bas;
        TxtHeaderAvatar.Text = bas;
    }

    private static string AvatarBasHarfler(string ad)
    {
        var parcalar = ad.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parcalar.Length == 0) return "?";
        if (parcalar.Length == 1) return parcalar[0][..Math.Min(2, parcalar[0].Length)].ToLowerInvariant();
        return $"{char.ToLowerInvariant(parcalar[0][0])}{char.ToLowerInvariant(parcalar[^1][0])}";
    }

    private void BildirimRozetiniGuncelle()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(BildirimRozetiniGuncelle, DispatcherPriority.Background);
            return;
        }

        var sayi = BildirimYoneticisi.OkunmamisSayisi;
        var gorunur = sayi > 0;
        TxtBildirimSayi.Text = sayi > 99 ? "99+" : sayi.ToString();
        BildirimRozet.Visibility = gorunur ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OturumDegistiIsle()
    {
        if (!IsLoaded)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            var rol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
            KullaniciPaneliniGuncelle();
            NavigasyonuOlustur();
            BildirimRozetiniGuncelle();
            CikisButonlariniGuncelle();

            if (!_navButtons.ContainsKey(_aktifRoute))
            {
                var ilk = SatinalmaPart1Menusu.IlkRoute(rol);
                RouteAc(ilk, null);
            }
            else
            {
                RouteAc(_aktifRoute, null);
            }
        });
    }

    public void KisayolYenile() => IcerikYenile();

    private void IcerikYenile()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(IcerikYenile, DispatcherPriority.Background);
            return;
        }

        NavRozetleriniGuncelle();

        if (_panosu is not null && IcerikAlani.Content == _panosu)
            PanoYenilemeyiPlanla();
        if (_iade is not null && IcerikAlani.Content == _iade)
            _iade.Yenile();
        if (_tedarikciler is not null && IcerikAlani.Content == _tedarikciler)
            _tedarikciler.Yenile();
        if (_liste is not null && IcerikAlani.Content == _liste && SatinalmaPart1Menusu.ListeRoute(_aktifRoute))
            _liste.Yenile();
        if (_onaylananListe is not null && IcerikAlani.Content == _onaylananListe)
            _onaylananListe.Yenile();
        if (_siparisListe is not null && IcerikAlani.Content == _siparisListe)
            _siparisListe.Yenile();
        if (_malKabulListe is not null && IcerikAlani.Content == _malKabulListe)
            _malKabulListe.Yenile();
    }

    public bool EscapeTusunuIsle()
    {
        if (IcerikAlani.Content is GelenTalepDetayView or TalepFormView or TeklifGirisView
            or OnaylananTalepDetayView or SiparisVerilenTalepDetayView or MalKabulTalepDetayView
            or YonetimTalepDetayView)
        {
            ListeyeDon();
            return true;
        }

        return false;
    }

    public void BildirimdenAc(Guid? talepId, int adim = 0, string sekme = "taleplerim")
    {
        if (sekme is "teklif-onay" or "teklif-onay-detay" or "teklif-onay-pencere")
        {
            if (talepId is { } onayId)
                YonetimTeklifIncelemeWindow.Goster(Window.GetWindow(this), onayId);
            else
                RouteAc(SatinalmaPart1Menusu.YonetimTeklifGirilen, null);
            NavRozetleriniGuncelle();
            return;
        }

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        sekme = MasaustuRolHaritasi.SatinalmaRouteSlug(sekme) ?? sekme;
        var route = sekme switch
        {
            "gelen-talepler" or "yonetim" or "yonetim-gelen-talepler"
                => SatinalmaPart1Menusu.YonetimGelenTalepler,
            "satinalma-teklif-istenen" or "teklif-istenen"
                => SatinalmaPart1Menusu.SatinalmaTeklifIstenen,
            "satinalma-karsilastirma" or "karsilastirma" or "teklif-karsilastirma"
                => SatinalmaPart1Menusu.SatinalmaKarsilastirma,
            "teklif-gir" or "teklif-giris"
                => SatinalmaPart1Menusu.SatinalmaTeklifIstenen,
            "satinalma-teklif-girilen" or "teklif-girilen"
                => SatinalmaPart1Menusu.SatinalmaTeklifGirilen,
            // Eski bildirim deep-link'leri → Teklif İstemi Yapılanlar
            "satinalma-teklif-duzeltme" or "teklif-duzeltme"
                => SatinalmaPart1Menusu.SatinalmaTeklifIstenen,
            "yonetim-teklif-girilen"
                => SatinalmaPart1Menusu.YonetimTeklifGirilen,
            "teklifsiz-firma-fiyat"
                => SatinalmaPart1Menusu.SatinalmaOnaylanan,
            "alinan-malzemeler" or "onaylanan-malzemeler" or "satinalma-siparis"
                => SatinalmaPart1Menusu.SatinalmaSiparis,
            "satinalma-mal-kabul" or "mal-kabul"
                => SatinalmaPart1Menusu.SatinalmaMalKabul,
            "satinalma-onaylanan" or "onaylanan-teklifler"
                => SatinalmaPart1Menusu.SatinalmaOnaylanan,
            "satinalma-onay-gecmisi" or "onay-gecmisi-satinalma"
                => SatinalmaPart1Menusu.SatinalmaOnayGecmisi,
            "yonetim-onay-gecmisi"
                => SatinalmaPart1Menusu.YonetimOnayGecmisi,
            "yonetim-onaylanan-teklifler" or "onaylanan-teklifler-yonetim"
                => SatinalmaPart1Menusu.YonetimOnaylananTeklifler,
            "gecmis-talepler" or "yonetim-gecmis"
                => SatinalmaPart1Menusu.YonetimGecmis,
            "red-talepler" or "yonetim-red"
                => SatinalmaPart1Menusu.YonetimRedVerilen,
            "teklif-bekleyen" or "yonetim-teklif-bekleyen"
                => SatinalmaTeklifBekleyenRoute(rol),
            _ when adim is 1 or 2 => SatinalmaPart1Menusu.YonetimGelenTalepler,
            _ => SatinalmaPart1Menusu.SatinalmaTalepler
        };

        route = SatinalmaPart1Menusu.BildirimRouteDonustur(route, rol);
        RouteAc(route, talepId);
    }

    private static string SatinalmaTeklifBekleyenRoute(string? rol)
    {
        rol = KullaniciRolleri.Normalize(rol);
        if (rol is KullaniciRolleri.Satinalma)
            return SatinalmaPart1Menusu.SatinalmaTeklifIstenen;

        if (rol is KullaniciRolleri.Yonetim)
            return SatinalmaPart1Menusu.YonetimTeklifBekleyen;

        return SatinalmaPart1Menusu.SatinalmaTeklifIstenen;
    }

    private void NavigasyonuOlustur()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();
        _navRozetleri.Clear();
        _navRozetCerceveleri.Clear();
        _navAccentBarlari.Clear();
        _navMetinleri.Clear();
        _navIkonlari.Clear();

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var muted = BrushKaynak("SecondaryTextBrush", Color.FromRgb(0x60, 0x70, 0x89));
        foreach (var grup in SatinalmaPart1Menusu.MenuGruplari(rol))
        {
            if (!string.IsNullOrWhiteSpace(grup.Baslik))
            {
                NavPanel.Children.Add(new TextBlock
                {
                    Text = grup.Baslik.ToUpperInvariant(),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = muted,
                    Margin = new Thickness(14, 14, 8, 6),
                    Visibility = _menuDaraltildi ? Visibility.Collapsed : Visibility.Visible,
                    Tag = "NavGroup"
                });
            }

            foreach (var menu in grup.Ogeler)
            {
                if (SatinalmaPart1Menusu.TalepProMenudeGizle(menu.Route))
                    continue;
                NavPanel.Children.Add(NavOgesi(menu.Route, menu.Baslik));
            }
        }

        BtnYeniTalep.Visibility = SatinalmaPart1Menusu.TalepAcabilir(rol)
            ? Visibility.Visible
            : Visibility.Collapsed;

        NavRozetleriniGuncelle();
        AktifNavGuncelle(_aktifRoute);
        MenuDaraltmaUygula();
    }

    private Button NavOgesi(string route, string baslik)
    {
        var rozet = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        var rozetCer = new Border
        {
            Background = BrushKaynak("DangerBrush", Color.FromRgb(0xE8, 0x3F, 0x45)),
            CornerRadius = new CornerRadius(9),
            MinWidth = 18,
            Height = 18,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(5, 0, 5, 0),
            Child = rozet,
            Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center
        };
        _navRozetleri[route] = rozet;
        _navRozetCerceveleri[route] = rozetCer;

        var navy = BrushKaynak("NavyTextBrush", Color.FromRgb(0x10, 0x23, 0x3F));
        var mutedIcon = BrushKaynak("SecondaryTextBrush", Color.FromRgb(0x60, 0x70, 0x89));

        var ikon = new TextBlock
        {
            Text = NavIkon(route),
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Foreground = mutedIcon,
            Width = 20,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _navIkonlari[route] = ikon;

        var metin = new TextBlock
        {
            Text = baslik,
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Foreground = navy,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 160
        };
        _navMetinleri[route] = metin;

        var sol = new StackPanel { Orientation = Orientation.Horizontal };
        sol.Children.Add(ikon);
        sol.Children.Add(metin);

        var icerik = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(rozetCer, Dock.Right);
        icerik.Children.Add(rozetCer);
        icerik.Children.Add(sol);

        var accent = new Border
        {
            Width = 3,
            CornerRadius = new CornerRadius(2),
            Background = Brushes.Transparent,
            Margin = new Thickness(0, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        _navAccentBarlari[route] = accent;

        var satir = new Grid();
        satir.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        satir.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(accent, 0);
        Grid.SetColumn(icerik, 1);
        satir.Children.Add(accent);
        satir.Children.Add(icerik);

        var shell = new Border
        {
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 9, 10, 9),
            Child = satir
        };

        var btn = new Button
        {
            Content = shell,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(8, 1, 8, 1),
            Cursor = Cursors.Hand,
            Tag = route,
            ToolTip = baslik,
            Focusable = true,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        var t = new ControlTemplate(typeof(Button));
        var f = new FrameworkElementFactory(typeof(ContentPresenter));
        t.VisualTree = f;
        btn.Template = t;
        btn.Click += (_, _) => RouteAc(route, null);
        btn.MouseEnter += (_, _) =>
        {
            if (!string.Equals(btn.Tag as string, "Active", StringComparison.Ordinal)
                && shell.Background == Brushes.Transparent)
                shell.Background = BrushKaynak("RowHoverBrush", Color.FromRgb(0xF3, 0xF6, 0xF9));
        };
        btn.MouseLeave += (_, _) =>
        {
            if (!string.Equals(btn.Tag as string, "Active", StringComparison.Ordinal))
                shell.Background = Brushes.Transparent;
        };

        _navButtons[route] = btn;
        return btn;
    }

    private Brush BrushKaynak(string key, Color fallback) =>
        TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);

    private static string NavIkon(string route) => route switch
    {
        SatinalmaPart1Menusu.SatinalmaPanosu => "\uE80F",
        SatinalmaPart1Menusu.SatinalmaTalep => "\uE710",
        SatinalmaPart1Menusu.SatinalmaTalepler => "\uE8A5",
        SatinalmaPart1Menusu.YonetimGelenTalepler => "\uE8F1",
        SatinalmaPart1Menusu.SatinalmaTeklifIstenen => "\uE8FD",
        SatinalmaPart1Menusu.SatinalmaTeklifGirilen => "\uE70F",
        SatinalmaPart1Menusu.YonetimTeklifGirilen => "\uE8FB",
        SatinalmaPart1Menusu.SatinalmaKarsilastirma => "\uE9D2",
        SatinalmaPart1Menusu.SatinalmaOnaylanan or SatinalmaPart1Menusu.YonetimOnaylananTeklifler => "\uE73E",
        SatinalmaPart1Menusu.SatinalmaOnayGecmisi or SatinalmaPart1Menusu.YonetimOnayGecmisi => "\uE81C",
        SatinalmaPart1Menusu.YonetimRedVerilen => "\uE711",
        SatinalmaPart1Menusu.SatinalmaSiparis => "\uE7BF",
        SatinalmaPart1Menusu.SatinalmaMalKabul => "\uE7B8",
        SatinalmaPart1Menusu.SatinalmaIade => "\uE72C",
        SatinalmaPart1Menusu.SatinalmaTedarikciler => "\uE716",
        _ => "\uE8A5"
    };

    private void RouteAc(string route, Guid? talepId)
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        if (!DesktopRoleTabManager.RouteVisible(rol, route))
        {
            route = SatinalmaPart1Menusu.IlkRoute(rol);
            talepId = null;
        }

        _aktifRoute = route;
        var (baslik, aciklama) = SatinalmaPart1Menusu.Baslik(route);
        TxtBaslik.Text = baslik;
        TxtAciklama.Text = aciklama;
        TxtBreadcrumb.Text = SatinalmaPart1Menusu.Breadcrumb(route);
        AktifNavGuncelle(route);

        if (SatinalmaPart1Menusu.PanosuRoute(route))
        {
            _panosu ??= OlusturPanosu();
            IcerikAlani.Content = _panosu;
            PanoYenilemeyiPlanla();
            return;
        }

        if (SatinalmaPart1Menusu.StokRoute(route))
        {
            StokModuluIstendi?.Invoke();
            return;
        }

        if (SatinalmaPart1Menusu.TalepFormuRoute(route))
        {
            _talepForm ??= OlusturTalepForm();
            _talepForm.YeniTalep();
            IcerikAlani.Content = _talepForm;
            return;
        }

        if (SatinalmaPart1Menusu.OnaylananListeRoute(route))
        {
            _onaylananListe ??= OlusturOnaylananListe();
            _onaylananListe.Goster(route);
            IcerikAlani.Content = _onaylananListe;

            if (talepId is { } onayId)
            {
                var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == onayId);
                if (talep is not null)
                    OnaylananTalepSecildi(talep);
            }

            return;
        }

        if (SatinalmaPart1Menusu.SiparisListeRoute(route))
        {
            _siparisListe ??= OlusturSiparisListe();
            _siparisListe.Yenile();
            IcerikAlani.Content = _siparisListe;

            if (talepId is { } siparisId)
            {
                var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == siparisId);
                if (talep is not null)
                    SiparisTalepSecildi(talep);
            }

            return;
        }

        if (SatinalmaPart1Menusu.MalKabulListeRoute(route))
        {
            _malKabulListe ??= OlusturMalKabulListe();
            _malKabulListe.Yenile();
            IcerikAlani.Content = _malKabulListe;

            if (talepId is { } mkId)
            {
                var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == mkId);
                if (talep is not null)
                    MalKabulTalepSecildi(talep);
            }

            return;
        }

        if (SatinalmaPart1Menusu.IadeRoute(route))
        {
            _iade ??= new SatinalmaIadeView();
            _iade.Yenile();
            IcerikAlani.Content = _iade;
            return;
        }

        if (SatinalmaPart1Menusu.TedarikciRoute(route))
        {
            _tedarikciler ??= new SatinalmaTedarikcilerView();
            _tedarikciler.Yenile();
            IcerikAlani.Content = _tedarikciler;
            return;
        }

        if (SatinalmaPart1Menusu.ListeRoute(route))
        {
            _liste ??= OlusturListe();
            _liste.Goster(route);
            IcerikAlani.Content = _liste;

            if (talepId is { } id)
            {
                var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == id);
                if (talep is not null)
                    TalepSecildi(talep);
            }

            return;
        }

        _bosSekme ??= new SatinalmaBosSekmeView();
        _bosSekme.Goster(baslik, aciklama);
        IcerikAlani.Content = _bosSekme;
    }

    private SatinalmaPanosuView OlusturPanosu()
    {
        var pano = new SatinalmaPanosuView();
        pano.RouteIstendi += r => RouteAc(r, null);
        pano.TalepAcIstendi += id =>
        {
            var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == id);
            if (talep is not null)
            {
                var route = SatinalmaPanosuView.TalepIcinRoute(talep);
                RouteAc(route, id);
            }
        };
        pano.Degisti += IcerikYenile;
        return pano;
    }

    private TalepFormView OlusturTalepForm()
    {
        var form = new TalepFormView();
        form.Degisti += IcerikYenile;
        form.KapatIstendi += ListeyeDon;
        return form;
    }

    private TalepListesiView OlusturListe()
    {
        var liste = new TalepListesiView();
        liste.TalepSecildi += TalepSecildi;
        return liste;
    }

    private OnaylananTalepListesiView OlusturOnaylananListe()
    {
        var liste = new OnaylananTalepListesiView();
        liste.TalepSecildi += OnaylananTalepSecildi;
        liste.Degisti += IcerikYenile;
        return liste;
    }

    private void OnaylananTalepSecildi(SatinalmaTalep talep)
    {
        _onaylananDetay ??= OlusturOnaylananDetay();
        _onaylananDetay.Yukle(talep, _aktifRoute);
        IcerikAlani.Content = _onaylananDetay;
    }

    private OnaylananTalepDetayView OlusturOnaylananDetay()
    {
        var detay = new OnaylananTalepDetayView();
        detay.Geri += ListeyeDon;
        detay.Degisti += IcerikYenile;
        detay.Yonlendir += route => RouteAc(route, detay.AktifTalep?.Id);
        return detay;
    }

    private SiparisVerilenTalepListesiView OlusturSiparisListe()
    {
        var liste = new SiparisVerilenTalepListesiView();
        liste.TalepSecildi += SiparisTalepSecildi;
        liste.OnaylananlaraGitIstendi += () => RouteAc(SatinalmaPart1Menusu.SatinalmaOnaylanan, null);
        liste.MalKabuleGitIstendi += () => RouteAc(SatinalmaPart1Menusu.SatinalmaMalKabul, null);
        return liste;
    }

    private void SiparisTalepSecildi(SatinalmaTalep talep)
    {
        _siparisDetay ??= OlusturSiparisDetay();
        _siparisDetay.Yukle(talep);
        IcerikAlani.Content = _siparisDetay;
    }

    private SiparisVerilenTalepDetayView OlusturSiparisDetay()
    {
        var detay = new SiparisVerilenTalepDetayView();
        detay.Geri += ListeyeDon;
        detay.Degisti += IcerikYenile;
        return detay;
    }

    private MalKabulTalepListesiView OlusturMalKabulListe()
    {
        var liste = new MalKabulTalepListesiView();
        liste.TalepSecildi += MalKabulTalepSecildi;
        liste.SiparislereGitIstendi += () => RouteAc(SatinalmaPart1Menusu.SatinalmaSiparis, null);
        return liste;
    }

    private void MalKabulTalepSecildi(SatinalmaTalep talep)
    {
        _malKabulDetay ??= OlusturMalKabulDetay();
        _malKabulDetay.Yukle(talep);
        IcerikAlani.Content = _malKabulDetay;
    }

    private MalKabulTalepDetayView OlusturMalKabulDetay()
    {
        var detay = new MalKabulTalepDetayView();
        detay.Geri += ListeyeDon;
        return detay;
    }

    private void TalepSecildi(SatinalmaTalep talep)
    {
        if (_aktifRoute == SatinalmaPart1Menusu.YonetimGelenTalepler)
        {
            _gelenDetay ??= OlusturGelenDetay();
            _gelenDetay.Yukle(talep);
            IcerikAlani.Content = _gelenDetay;
            return;
        }

        if (_aktifRoute == SatinalmaPart1Menusu.YonetimTeklifBekleyen)
        {
            // Teklif bekleyen: acil alıma çevir + onay / red (Gelen detay aksiyonları).
            _gelenDetay ??= OlusturGelenDetay();
            _gelenDetay.Yukle(talep);
            IcerikAlani.Content = _gelenDetay;
            return;
        }

        if (SatinalmaPart1Menusu.YonetimTeklifIncelemeRoute(_aktifRoute))
        {
            if (YonetimTeklifIncelemeWindow.Goster(Window.GetWindow(this), talep))
                NavRozetleriniGuncelle();
            return;
        }

        if (SatinalmaPart1Menusu.YonetimArsivListeRoute(_aktifRoute))
        {
            _yonetimDetay ??= OlusturYonetimDetay();
            _yonetimDetay.Yukle(talep, SatinalmaPart1Menusu.YonetimDetayModu(_aktifRoute));
            IcerikAlani.Content = _yonetimDetay;
            return;
        }

        if (SatinalmaPart1Menusu.TeklifGirisRoute(_aktifRoute))
        {
            ProcurementTalepAdapter.StatusSenkronizeEt(talep);
            var rol = OturumYoneticisi.AktifKullanici?.Rol;
            var ui = PurchaseRequestDetailServisi.UiDurumu(
                talep, rol, PurchaseRequestDetailScreen.ManagementQuoteReview);

            // Yönetime gönderilmiş teklif incelemesi: onay / red / revize (Yönetim + Satınalma)
            if (ui.Screen == PurchaseRequestDetailScreen.ManagementQuoteReview
                && PurchaseRequestDetailPresenter.CanQuoteDecide(rol)
                && (ui.VisibleActions.Count > 0 || ui.ShowPerQuoteApproveButtons)
                && _aktifRoute is not SatinalmaPart1Menusu.SatinalmaTeklifIstenen)
            {
                if (YonetimTeklifIncelemeWindow.Goster(Window.GetWindow(this), talep))
                    NavRozetleriniGuncelle();
                return;
            }

            if (!KullaniciRolleri.SatinalmaTeklifGirebilir(rol))
            {
                _yonetimDetay ??= OlusturYonetimDetay();
                _yonetimDetay.Yukle(talep, YonetimTalepDetayModu.Gecmis);
                IcerikAlani.Content = _yonetimDetay;
                return;
            }

            _teklifGiris ??= OlusturTeklifGiris();
            _teklifGiris.Yukle(talep, TeklifModu(_aktifRoute));
            IcerikAlani.Content = _teklifGiris;
            return;
        }

        if (_aktifRoute == SatinalmaPart1Menusu.SatinalmaTalepler)
        {
            _talepForm ??= OlusturTalepForm();
            var duzenlenebilir = SatinalmaPart1Servisi.Duzenlenebilir(talep);
            _talepForm.Yukle(talep, duzenlenebilir);
            IcerikAlani.Content = _talepForm;
            return;
        }

        MessageBox.Show(
            $"Talep: {talep.TalepNo}\nTalep durumu: {SatinalmaPart1DurumEtiketi.TalepDurumu(talep)}\nTeklif durumu: {SatinalmaPart1DurumEtiketi.TeklifDurumu(talep)}",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static TeklifGirisModu TeklifModu(string route) => route switch
    {
        SatinalmaPart1Menusu.SatinalmaKarsilastirma => TeklifGirisModu.Karsilastirma,
        SatinalmaPart1Menusu.SatinalmaTeklifDuzeltme => TeklifGirisModu.Karsilastirma,
        SatinalmaPart1Menusu.SatinalmaTeklifGirilen => TeklifGirisModu.YonetimeGonderildi,
        _ => TeklifGirisModu.TeklifIstenen
    };

    private TeklifGirisView OlusturTeklifGiris()
    {
        var view = new TeklifGirisView();
        view.Geri += ListeyeDon;
        view.Degisti += IcerikYenile;
        view.Yonlendir += route => RouteAc(route, null);
        return view;
    }

    private GelenTalepDetayView OlusturGelenDetay()
    {
        var detay = new GelenTalepDetayView();
        detay.Geri += ListeyeDon;
        detay.Degisti += IcerikYenile;
        detay.Yonlendir += route => RouteAc(route, null);
        return detay;
    }

    private YonetimTalepDetayView OlusturYonetimDetay()
    {
        var detay = new YonetimTalepDetayView();
        detay.Geri += ListeyeDon;
        return detay;
    }

    private void ListeyeDon()
    {
        if (_liste is null)
            RouteAc(SatinalmaPart1Menusu.IlkRoute(OturumYoneticisi.AktifKullanici?.Rol), null);
        else if (SatinalmaPart1Menusu.ListeRoute(_aktifRoute))
            RouteAc(_aktifRoute, null);
        else if (SatinalmaPart1Menusu.OnaylananListeRoute(_aktifRoute))
            RouteAc(_aktifRoute, null);
        else if (SatinalmaPart1Menusu.SiparisListeRoute(_aktifRoute))
            RouteAc(_aktifRoute, null);
        else if (SatinalmaPart1Menusu.MalKabulListeRoute(_aktifRoute))
            RouteAc(_aktifRoute, null);
        else if (SatinalmaPart1Menusu.TeklifGirisRoute(_aktifRoute))
            RouteAc(_aktifRoute, null);
        else if (SatinalmaPart1Menusu.TalepFormuRoute(_aktifRoute))
            RouteAc(SatinalmaPart1Menusu.SatinalmaTalepler, null);
        else
            RouteAc(_aktifRoute, null);
    }

    private void AktifNavGuncelle(string route)
    {
        var aktifBg = BrushKaynak("PrimaryTealLightBrush", Color.FromRgb(0xE8, 0xF7, 0xF7));
        var aktifFg = BrushKaynak("PrimaryTealDarkBrush", Color.FromRgb(0x04, 0x6C, 0x75));
        var navy = BrushKaynak("NavyTextBrush", Color.FromRgb(0x10, 0x23, 0x3F));
        var muted = BrushKaynak("SecondaryTextBrush", Color.FromRgb(0x60, 0x70, 0x89));
        var cream = BrushKaynak("BadgeCreamBrush", Color.FromRgb(0xF5, 0xE6, 0xA8));
        var danger = BrushKaynak("DangerBrush", Color.FromRgb(0xE8, 0x3F, 0x45));

        foreach (var (id, btn) in _navButtons)
        {
            var aktif = id == route;
            btn.Tag = aktif ? "Active" : id;
            if (btn.Content is not Border shell) continue;

            shell.Background = aktif ? aktifBg : Brushes.Transparent;

            if (_navMetinleri.TryGetValue(id, out var metin))
            {
                metin.Foreground = aktif ? aktifFg : navy;
                metin.FontWeight = aktif ? FontWeights.SemiBold : FontWeights.Medium;
            }

            if (_navIkonlari.TryGetValue(id, out var ikon))
                ikon.Foreground = aktif ? aktifFg : muted;

            if (_navAccentBarlari.TryGetValue(id, out var accent))
                accent.Background = aktif ? aktifFg : Brushes.Transparent;

            if (_navRozetCerceveleri.TryGetValue(id, out var rozetCer)
                && _navRozetleri.TryGetValue(id, out var rozet)
                && rozetCer.Visibility == Visibility.Visible)
            {
                if (aktif)
                {
                    rozetCer.Background = cream;
                    rozet.Foreground = navy;
                }
                else
                {
                    rozetCer.Background = danger;
                    rozet.Foreground = Brushes.White;
                }
            }
        }
    }

    private void BtnMenuDaralt_Click(object sender, RoutedEventArgs e)
    {
        _menuDaraltildi = !_menuDaraltildi;
        MenuDaraltmaUygula();
    }

    private void MenuDaraltmaUygula()
    {
        ColNav.Width = new GridLength(_menuDaraltildi ? 72 : 268);
        PanelMarkaMetin.Visibility = _menuDaraltildi ? Visibility.Collapsed : Visibility.Visible;
        PanelKullaniciMetin.Visibility = _menuDaraltildi ? Visibility.Collapsed : Visibility.Visible;
        BtnMenuDaralt.Content = _menuDaraltildi ? "\uE76C" : "\uE76B";
        BtnMenuDaralt.ToolTip = _menuDaraltildi ? "Menüyü genişlet" : "Menüyü daralt";

        foreach (var child in NavPanel.Children)
        {
            if (child is TextBlock { Tag: "NavGroup" } grup)
                grup.Visibility = _menuDaraltildi ? Visibility.Collapsed : Visibility.Visible;
        }

        foreach (var (route, metin) in _navMetinleri)
            metin.Visibility = _menuDaraltildi ? Visibility.Collapsed : Visibility.Visible;

        foreach (var (route, rozet) in _navRozetleri)
        {
            if (_navRozetCerceveleri.TryGetValue(route, out var cer))
            {
                if (_menuDaraltildi)
                    cer.Visibility = Visibility.Collapsed;
                else if (rozet.Visibility == Visibility.Visible)
                    cer.Visibility = Visibility.Visible;
            }
        }
    }

    private void BtnSatinalmaProDon_Click(object sender, RoutedEventArgs e) =>
        UygulamaKoordinasyonu.SatinalmaProModulAc(null);

    private void BtnCikis_Click(object sender, RoutedEventArgs e) =>
        OturumKapatIstendi?.Invoke();

    private void CikisButonlariniGuncelle()
    {
        var gorunur = OturumKapatmaServisi.CikisButonuGorunur
            ? Visibility.Visible
            : Visibility.Collapsed;
        BtnCikis.Visibility = gorunur;
        BtnCikisAlt.Visibility = gorunur;
        BtnCikisUst.Visibility = gorunur;
    }

    private void PanoYenilemeyiPlanla()
    {
        if (_panosu is null)
            return;

        var sira = ++_panoYenilemeSira;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            if (sira != _panoYenilemeSira || _panosu is null || IcerikAlani.Content != _panosu)
                return;

            _panosu.Yenile();
        });
    }

    private void NavRozetleriniGuncelle()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(NavRozetleriniGuncelle, DispatcherPriority.Background);
            return;
        }

        if (_navRozetleri.Count == 0)
            return;

        _rozetZamanlayici ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _rozetZamanlayici.Stop();
        _rozetZamanlayici.Tick -= RozetZamanlayiciTik;
        _rozetZamanlayici.Tick += RozetZamanlayiciTik;
        _rozetZamanlayici.Start();
    }

    private void RozetZamanlayiciTik(object? sender, EventArgs e)
    {
        _rozetZamanlayici?.Stop();
        RozetleriUygula();
    }

    private void RozetleriUygula()
    {
        if (_rozetGuncelleniyor || _navRozetleri.Count == 0)
            return;

        _rozetGuncelleniyor = true;
        try
        {
            var routes = _navRozetleri.Keys.ToList();
            var sayaclar = SatinalmaPart1Filtreleri.RozetSayilari(routes);

            foreach (var (route, metin) in _navRozetleri)
            {
                var sayi = SatinalmaPart1Menusu.BekleyenRozetGoster(route)
                    ? sayaclar.GetValueOrDefault(route)
                    : 0;
                var gorunur = sayi > 0 && !_menuDaraltildi;
                metin.Text = sayi > 99 ? "99+" : sayi.ToString();
                metin.Visibility = sayi > 0 ? Visibility.Visible : Visibility.Collapsed;

                if (_navRozetCerceveleri.TryGetValue(route, out var cerceve))
                    cerceve.Visibility = gorunur ? Visibility.Visible : Visibility.Collapsed;
                else if (metin.Parent is Border eski)
                    eski.Visibility = gorunur ? Visibility.Visible : Visibility.Collapsed;
            }

            AktifNavGuncelle(_aktifRoute);
        }
        finally
        {
            _rozetGuncelleniyor = false;
        }
    }

    private void BtnYeniTalep_Click(object sender, RoutedEventArgs e) =>
        RouteAc(SatinalmaPart1Menusu.SatinalmaTalep, null);

    private void Excel_Click(object sender, RoutedEventArgs e)
    {
        switch (IcerikAlani.Content)
        {
            case SatinalmaPanosuView pano:
                pano.ExcelDisAktar();
                return;
            case SatinalmaIadeView iade:
                iade.ExcelDisAktar();
                return;
            case SatinalmaTedarikcilerView tedarikci:
                tedarikci.ExcelDisAktar();
                return;
            case TalepFormView:
                SatinalmaPanosuExcelService.TalepListesiKaydet(
                    SatinalmaPanosuVeriServisi.SonTalepler(200), "Satinalma_Talepler.xlsx");
                return;
            default:
                SatinalmaPanosuExcelService.TalepListesiKaydet(
                    SatinalmaPanosuVeriServisi.SonTalepler(200), "Satinalma_Talepler.xlsx");
                return;
        }
    }

    private void Pdf_Click(object sender, RoutedEventArgs e)
    {
        switch (IcerikAlani.Content)
        {
            case SatinalmaPanosuView pano:
                pano.PdfIndir();
                return;
            case SatinalmaIadeView iade:
                iade.PdfIndir();
                return;
            case SatinalmaTedarikcilerView tedarikci:
                tedarikci.PdfIndir();
                return;
            case TalepFormView form when form.AktifTalep is { } talep:
                SatinalmaPdfOlusturucu.TalepFormuYazdir(talep, SatinalmaDepo.Ayarlar);
                return;
            default:
                SatinalmaPanosuPdfOlusturucu.TalepListesiIndir(SatinalmaPanosuVeriServisi.SonTalepler(200));
                return;
        }
    }

    private void Yazdir_Click(object sender, RoutedEventArgs e)
    {
        switch (IcerikAlani.Content)
        {
            case SatinalmaPanosuView pano:
                pano.PdfYazdir();
                return;
            case SatinalmaIadeView iade:
                iade.PdfYazdir();
                return;
            case SatinalmaTedarikcilerView tedarikci:
                tedarikci.PdfYazdir();
                return;
            case TalepFormView form when form.AktifTalep is { } talep:
                SatinalmaPdfOlusturucu.TalepFormuYazdir(talep, SatinalmaDepo.Ayarlar);
                return;
            default:
                SatinalmaPanosuPdfOlusturucu.TalepListesiYazdir(SatinalmaPanosuVeriServisi.SonTalepler(200));
                return;
        }
    }

    private void Bildirim_Click(object sender, RoutedEventArgs e)
    {
        var pencere = new BildirimlerWindow { Owner = Window.GetWindow(this) };
        pencere.ShowDialog();
        BildirimRozetiniGuncelle();
    }

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();
}
