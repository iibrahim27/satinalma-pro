using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;
using SatinalmaPro.Views.Modules.Satinalma;
using SatinalmaPro.Views.Modules.Satinalma.Part1;
using SatinalmaPro.Views;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaShellView : UserControl, IModulKlavyeKisayollari
{
    public event Action? StokModuluIstendi;

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

    public SatinalmaShellView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            try
            {
                SatinalmaMasaustuSifirlama.IlkAcilistaUygula();
                SatinalmaDepo.Yukle();

                var ad = KullaniciYetkileri.AktifKullaniciAdi() ?? "Kullanıcı";
                var rol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
                TxtKullanici.Text = $"{ad} · {rol}";

                NavigasyonuOlustur();
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
        Unloaded += (_, _) =>
        {
            SatinalmaDepo.TaleplerGuncellendi -= IcerikYenile;
            OturumYoneticisi.OturumDegisti -= OturumDegistiIsle;
        };

        OturumYoneticisi.OturumDegisti += OturumDegistiIsle;
    }

    private void OturumDegistiIsle()
    {
        if (!IsLoaded)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            var rol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
            var ad = KullaniciYetkileri.AktifKullaniciAdi() ?? "Kullanıcı";
            TxtKullanici.Text = $"{ad} · {rol}";

            NavigasyonuOlustur();

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
            "satinalma-teklif-duzeltme" or "teklif-duzeltme"
                => SatinalmaPart1Menusu.SatinalmaTeklifDuzeltme,
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

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        foreach (var grup in SatinalmaPart1Menusu.MenuGruplari(rol))
        {
            if (!string.IsNullOrWhiteSpace(grup.Baslik))
            {
                NavPanel.Children.Add(new TextBlock
                {
                    Text = grup.Baslik,
                    Style = (Style?)TryFindResource("SatModSideNavGroup")
                        ?? Application.Current.TryFindResource("SatModSideNavGroup") as Style
                });
            }

            foreach (var menu in grup.Ogeler)
                NavPanel.Children.Add(NavOgesi(menu.Route, menu.Baslik));
        }

        BtnYeniTalep.Visibility = SatinalmaPart1Menusu.TalepAcabilir(rol)
            ? Visibility.Visible
            : Visibility.Collapsed;

        NavRozetleriniGuncelle();
        AktifNavGuncelle(_aktifRoute);
    }

    private Button NavOgesi(string route, string baslik)
    {
        var btn = new Button
        {
            Style = (Style?)TryFindResource("SatModSideNavItem")
                ?? Application.Current.TryFindResource("SatModSideNavItem") as Style,
            Tag = route,
            ToolTip = baslik
        };
        btn.Click += (_, _) => RouteAc(route, null);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var metin = new TextBlock
        {
            Text = baslik,
            Style = (Style?)TryFindResource("SatModSideNavLabel")
                ?? Application.Current.TryFindResource("SatModSideNavLabel") as Style
        };
        Grid.SetColumn(metin, 0);
        grid.Children.Add(metin);

        var rozetCer = new Border
        {
            Style = (Style?)TryFindResource("SatModSideNavBadge")
                ?? Application.Current.TryFindResource("SatModSideNavBadge") as Style
        };
        var rozet = new TextBlock
        {
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(225, 29, 72)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        rozetCer.Child = rozet;
        Grid.SetColumn(rozetCer, 1);
        grid.Children.Add(rozetCer);
        _navRozetleri[route] = rozet;

        btn.Content = grid;
        _navButtons[route] = btn;
        return btn;
    }

    private void RouteAc(string route, Guid? talepId)
    {
        _aktifRoute = route;
        var (baslik, aciklama) = SatinalmaPart1Menusu.Baslik(route);
        TxtBaslik.Text = baslik;
        TxtAciklama.Text = aciklama;
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
            _yonetimDetay ??= OlusturYonetimDetay();
            _yonetimDetay.Yukle(talep, YonetimTalepDetayModu.Gecmis);
            IcerikAlani.Content = _yonetimDetay;
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
            if (!KullaniciRolleri.SatinalmaTeklifGirebilir(OturumYoneticisi.AktifKullanici?.Rol))
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
        var normalLabel = (Style?)TryFindResource("SatModSideNavLabel")
            ?? Application.Current.TryFindResource("SatModSideNavLabel") as Style;
        var aktifLabel = (Style?)TryFindResource("SatModSideNavLabelActive")
            ?? Application.Current.TryFindResource("SatModSideNavLabelActive") as Style;

        foreach (var (id, btn) in _navButtons)
        {
            btn.Tag = id == route ? "Active" : id;

            if (btn.Content is Grid grid && grid.Children[0] is TextBlock metin)
                metin.Style = id == route ? aktifLabel : normalLabel;
        }
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
                var sayi = sayaclar.GetValueOrDefault(route);
                var gorunur = sayi > 0;
                metin.Text = sayi > 99 ? "99+" : sayi.ToString();
                metin.Visibility = gorunur ? Visibility.Visible : Visibility.Collapsed;

                if (metin.Parent is Border cerceve)
                    cerceve.Visibility = gorunur ? Visibility.Visible : Visibility.Collapsed;
            }
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
    }

    private void Yenile_Click(object sender, RoutedEventArgs e) => KisayolYenile();
}
