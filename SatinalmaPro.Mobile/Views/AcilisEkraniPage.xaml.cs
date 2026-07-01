using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class AcilisEkraniPage : ContentPage
{
    private const int KarsilamaSuresiMs = 3500;

    private bool _basladi;

    public AcilisEkraniPage()
    {
        InitializeComponent();
        SayfaYardimcisi.SurumAltBilgiEkle(this);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_basladi)
            return;

        _basladi = true;
        await YukleVeDevamEtAsync();
    }

    private async Task YukleVeDevamEtAsync()
    {
        try
        {
            Ilerle(0, "Uygulama yükleniyor...", "Başlatılıyor...");
            await Task.Delay(120);

            Ilerle(3, "Uygulama yükleniyor...", "Bulut ayarları kontrol ediliyor...");
            await FirebaseAyarServisi.PaketDosyalariniHazirlaAsync();

            var guncellemeUygulandi = await MobilGuncellemeServisi.KontrolEtVeUygulaAsync(
                (durum, yuzde) => MainThread.BeginInvokeOnMainThread(() =>
                    Ilerle(yuzde,
                        yuzde >= 90 ? "Güncelleniyor..." : "Güncelleme kontrol ediliyor...",
                        durum)));

            if (guncellemeUygulandi)
            {
                Ilerle(100, "Kurulum bekleniyor", "Android kurulum ekranında «Yükle»ye basın, ardından uygulamayı yeniden açın.");
                return;
            }

            var services = IPlatformApplication.Current?.Services ?? MauiProgram.Services;
            if (services is null)
                return;

            await OturumYonlendirmeServisi.SplashSonrasiYonlendirAsync(
                services,
                Ilerle,
                KarsilamaGosterAsync);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Acilis: {ex}");
            Ilerle(100, "Hazır", "Giriş ekranına geçiliyor...");
            await Task.Delay(400);

            var services = IPlatformApplication.Current?.Services ?? MauiProgram.Services;
            if (services is not null)
                OturumYonlendirmeServisi.LoginSayfasinaGit(services);
        }
    }

    private async Task KarsilamaGosterAsync(OturumServisi oturum)
    {
        var rol = KullaniciRolleri.Normalize(oturum.Rol);
        var ad = string.IsNullOrWhiteSpace(oturum.KullaniciAdi)
            ? oturum.Depo.AktifKullanici?.AdSoyad?.Trim() ?? ""
            : oturum.KullaniciAdi.Trim();

        if (string.IsNullOrWhiteSpace(ad))
            ad = "Kullanıcı";

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            LblKarsilamaRol.Text = rol;
            LblKarsilamaAd.Text = ad;
            YuklemeLayout.IsVisible = false;
            KarsilamaLayout.IsVisible = true;
        });

        await Task.Delay(KarsilamaSuresiMs);
    }

    private void Ilerle(double yuzde, string baslik, string durum)
    {
        var oran = Math.Clamp(yuzde, 0, 100) / 100.0;
        PrgCubuk.Progress = oran;
        TxtYuzde.Text = $"{(int)Math.Clamp(yuzde, 0, 100)}%";
        TxtBaslik.Text = baslik;
        TxtDurum.Text = durum;
    }
}