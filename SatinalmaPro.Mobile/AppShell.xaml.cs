using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile;

public partial class AppShell : Shell
{
    private readonly OturumServisi _oturum;
    private readonly IServiceProvider _services;
    private string _sonRol = "";
    private string? _menuImzasi;
    private bool _menulerHazir;

    public AppShell(OturumServisi oturum, IServiceProvider services)
    {
        InitializeComponent();
        _oturum = oturum;
        _services = services;

        LblKullanici.Text = oturum.KullaniciAdi;
        LblRol.Text = oturum.Rol;
        _sonRol = oturum.Rol;

        // Yalnızca üst üste açılan (stack) alt sayfalar kayıtlı olmalı; flyout sekmeleri ShellContent ile tanımlı.
        Routing.RegisterRoute("talep-detay", typeof(Views.TalepDetayPage));
        Routing.RegisterRoute("talep-duzenle", typeof(Views.YeniTalepPage));
        Routing.RegisterRoute("teklif-onay-detay", typeof(Views.TeklifOnayDetayPage));
        Routing.RegisterRoute("onay-gecmisi-detay", typeof(Views.OnayGecmisiDetayPage));
        Routing.RegisterRoute("acil-onay", typeof(Views.AcilOnayPage));
        Routing.RegisterRoute("stok-aktar", typeof(Views.StokAktarPage));

        MenuleriOlustur();
        _menulerHazir = true;

        _oturum.Dinleyici.BildirimlerDegisti += BildirimRozetiniGuncelle;
        _oturum.OturumDegisti += OturumDegistiIsle;
        _oturum.VeriGuncellendi += BaglantiRozetiniGuncelle;
        BildirimRozetiniGuncelle();
        BaglantiRozetiniGuncelle();

        var bildirimTap = new TapGestureRecognizer();
        bildirimTap.Tapped += async (_, _) =>
        {
            if (_oturum.Dinleyici.OkunmamisSayisi <= 0)
                return;

            var route = RolRouteServisi.ErisilebilirRotalar(_oturum.Rol).Contains("bildirimler")
                ? "bildirimler"
                : RolRouteServisi.VarsayilanRoute(_oturum.Rol);
            await BildirimNavigasyonServisi.RouteGitAsync(route, _oturum);
        };
        LblBildirimSayi.GestureRecognizers.Add(bildirimTap);

        _ = Task.Run(async () =>
        {
            await Task.Delay(400);
            await BildirimNavigasyonServisi.BekleyenRouteIsleAsync();
        });

        var profil = new Button { Text = "Profil / Ayarlar", HorizontalOptions = LayoutOptions.Fill };
        profil.Clicked += async (_, _) => await ProfilSayfasinaGitAsync();

        var cikis = new Button { Text = "Çıkış Yap", HorizontalOptions = LayoutOptions.Fill };
        cikis.Clicked += (_, _) =>
        {
            _oturum.CikisYap();
            OturumYonlendirmeServisi.LoginSayfasinaGit(_services);
        };

        FlyoutFooter = new VerticalStackLayout
        {
            Padding = new Thickness(16),
            Spacing = 8,
            Children =
            {
                LblOfflineUyari,
                new Label
                {
                    Text = UygulamaBilgisi.AltBilgiMetni(AppInfo.VersionString),
                    FontSize = 10,
                    TextColor = Colors.Gray,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.WordWrap
                },
                profil,
                cikis
            }
        };
    }

    protected override bool OnBackButtonPressed()
    {
        if (GeriNavigasyonServisi.GeriDene())
            return true;

        return base.OnBackButtonPressed();
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        if (CurrentPage is ContentPage sayfa)
            SayfaYardimcisi.SurumAltBilgiEkle(sayfa);
    }

    private readonly Label LblOfflineUyari = new()
    {
        FontSize = 11,
        TextColor = Colors.OrangeRed,
        IsVisible = false,
        LineBreakMode = LineBreakMode.WordWrap
    };

    private void OturumDegistiIsle()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LblKullanici.Text = _oturum.KullaniciAdi;
            LblRol.Text = _oturum.Rol;

            var rol = _oturum.Rol;
            if (!_menulerHazir || rol == _sonRol)
                return;

            _sonRol = rol;
            MenuleriOlustur();
        });
    }

    private async Task ProfilSayfasinaGitAsync()
    {
        try
        {
            FlyoutIsPresented = false;
            await GoToAsync("//profil");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Profil navigasyon: {ex}");
            await DisplayAlert("Hata", "Profil sayfası açılamadı.", "Tamam");
        }
    }

    private void MenuleriOlustur()
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(MenuleriOlustur);
            return;
        }

        try
        {
            var menuler = RolNavigasyonu.Menuler(_oturum.Rol);
            var imza = string.Join("|", menuler.Select(m => m.Route));
            if (imza == _menuImzasi && Items.Count > 1)
                return;

            _menuImzasi = imza;

            while (Items.Count > 1)
                Items.RemoveAt(1);

            var admin = KullaniciRolleri.AdminMi(_oturum.Rol);

            if (admin)
            {
                foreach (var grup in menuler.Where(m => m.Route != "profil").GroupBy(m => m.Grup ?? "Diğer"))
                {
                    var flyoutItem = new FlyoutItem
                    {
                        Title = grup.Key,
                        FlyoutDisplayOptions = FlyoutDisplayOptions.AsMultipleItems
                    };

                    foreach (var menu in grup)
                    {
                        flyoutItem.Items.Add(new ShellContent
                        {
                            Title = MenuBasligi(menu),
                            Route = menu.Route,
                            ContentTemplate = new DataTemplate(() => SayfaOlustur(menu.Route))
                        });
                    }

                    Items.Add(flyoutItem);
                }
            }
            else
            {
                foreach (var menu in menuler.Where(m => m.Route != "profil"))
                {
                    Items.Add(new ShellContent
                    {
                        Title = MenuBasligi(menu),
                        Route = menu.Route,
                        ContentTemplate = new DataTemplate(() => SayfaOlustur(menu.Route))
                    });
                }
            }

            Items.Add(new ShellContent
            {
                Title = "👤 Profil / Ayarlar",
                Route = "profil",
                FlyoutItemIsVisible = false,
                ContentTemplate = new DataTemplate(() => _services.GetRequiredService<Views.ProfilPage>())
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Menü oluşturma: {ex}");
        }
    }

    private static string MenuBasligi(MenuOgesi menu) =>
        string.IsNullOrWhiteSpace(menu.Ikon) ? menu.Baslik : $"{menu.Ikon} {menu.Baslik}";

    private ContentPage SayfaOlustur(string route)
    {
        var sayfa = route switch
        {
            "taleplerim" => (ContentPage)_services.GetRequiredService<Views.TalepListPage>(),
            "yeni-talep" => _services.GetRequiredService<Views.YeniTalepPage>(),
            "onay-bekleyen" => _services.GetRequiredService<Views.OnayBekleyenTaleplerPage>(),
            "onaylanan-talepler" => _services.GetRequiredService<Views.OnaylananTaleplerPage>(),
            "gecmis-talepler" => _services.GetRequiredService<Views.GecmisTaleplerPage>(),
            "gecmis-teklifli-onaylar" => _services.GetRequiredService<Views.GecmisTeklifliOnaylarPage>(),
            "gelen-talepler" => _services.GetRequiredService<Views.GelenTaleplerPage>(),
            "teklif-bekleyen" => _services.GetRequiredService<Views.TeklifBekleyenPage>(),
            "onaylanan-teklifler" => _services.GetRequiredService<Views.YonetimOnaylananTekliflerPage>(),
            "red-talepler" => _services.GetRequiredService<Views.RedTaleplerPage>(),
            "onaylanan-malzemeler" => _services.GetRequiredService<Views.OnaylananMalzemelerPage>(),
            "teklif-gir" => _services.GetRequiredService<Views.TeklifGirisPage>(),
            "teklif-karsilastirma" => _services.GetRequiredService<Views.TeklifKarsilastirmaPage>(),
            "teklifsiz-firma-fiyat" => _services.GetRequiredService<Views.TeklifsizFirmaFiyatPage>(),
            "teklif-onay" => _services.GetRequiredService<Views.TeklifOnayPage>(),
            "onay-gecmisi" => _services.GetRequiredService<Views.OnayGecmisiPage>(),
            "stok-durum" => _services.GetRequiredService<Views.StokDurumPage>(),
            "stok-hareket" => _services.GetRequiredService<Views.StokHareketPage>(),
            "stok-giris" => _services.GetRequiredService<Views.StokGirisPage>(),
            "stok-cikis" => _services.GetRequiredService<Views.StokCikisPage>(),
            "stok-sayim" => _services.GetRequiredService<Views.StokSayimPage>(),
            "bildirimler" => _services.GetRequiredService<Views.BildirimPage>(),
            "profil" => _services.GetRequiredService<Views.ProfilPage>(),
            _ => _services.GetRequiredService<Views.AnaSayfaPage>()
        };
        SayfaYardimcisi.SurumAltBilgiEkle(sayfa);
        return sayfa;
    }

    private void BildirimRozetiniGuncelle()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var sayi = _oturum.Dinleyici.OkunmamisSayisi;
            LblBildirimSayi.IsVisible = sayi > 0;
            LblBildirimSayi.Text = sayi > 0 ? $"{sayi} okunmamış bildirim" : "";
        });
    }

    private void BaglantiRozetiniGuncelle()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LblOfflineUyari.IsVisible = _oturum.Depo.OfflineMod;
            if (_oturum.Depo.OfflineMod)
            {
                var son = _oturum.Depo.SonSenkronZamani?.ToString("dd.MM.yyyy HH:mm") ?? "—";
                LblOfflineUyari.Text = $"⚠ Bağlantı yok — önbellek ({son})";
            }
        });
    }
}
