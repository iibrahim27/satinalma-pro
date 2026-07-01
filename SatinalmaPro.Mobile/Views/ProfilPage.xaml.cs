using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile.Views;

public partial class ProfilPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private readonly IBiyometrikKimlikServisi _biyometrik;
    private bool _swYukleniyor;

    public ProfilPage(OturumServisi oturum, IBiyometrikKimlikServisi biyometrik)
    {
        InitializeComponent();
        _oturum = oturum;
        _biyometrik = biyometrik;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = GuvenliGorunumYukleAsync();
    }

    private async Task GuvenliGorunumYukleAsync()
    {
        try
        {
            var k = _oturum.Depo.AktifKullanici;
            LblAd.Text = k?.AdSoyad ?? _oturum.KullaniciAdi;
            LblEposta.Text = k?.Eposta ?? _oturum.Auth.Eposta ?? "—";
            LblRol.Text = $"Rol: {_oturum.Rol}";

            OfflinePanel.IsVisible = _oturum.Depo.OfflineMod || !_oturum.Depo.SonSenkronBasarili;
            if (OfflinePanel.IsVisible)
            {
                var son = _oturum.Depo.SonSenkronZamani?.ToString("dd.MM.yyyy HH:mm") ?? "—";
                LblBaglanti.Text = _oturum.Depo.OfflineMod
                    ? $"Çevrimdışı mod — son senkron: {son}"
                    : $"Son senkron başarısız: {_oturum.Depo.SonSenkronHata}";
            }

            await GuvenliGirisPaneliniYenileAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Profil görünüm: {ex.Message}");
        }
    }

    private async Task GuvenliGirisPaneliniYenileAsync()
    {
        var bioKullanilabilir = await _biyometrik.KullanilabilirMiAsync();
        var bioAktif = await GuvenliGirisDeposu.BiyometrikAktifMiAsync();
        var pinAktif = await GuvenliGirisDeposu.PinAyarliMiAsync();

        _swYukleniyor = true;
        SwBiyometrik.IsEnabled = bioKullanilabilir;
        SwBiyometrik.IsToggled = bioAktif && bioKullanilabilir;
        _swYukleniyor = false;

        LblBiyometrikDurum.Text = bioKullanilabilir
            ? bioAktif ? "Biyometrik giriş açık" : "Biyometrik giriş kapalı"
            : "Bu cihazda biyometrik doğrulama desteklenmiyor.";

        BtnPinKaldir.IsVisible = pinAktif;
        LblPinDurum.Text = pinAktif ? "PIN ayarlı" : "PIN ayarlanmadı";
    }

    private async void Biyometrik_Toggled(object sender, ToggledEventArgs e)
    {
        if (_swYukleniyor)
            return;

        if (e.Value)
        {
            if (!await _biyometrik.KullanilabilirMiAsync())
            {
                await DisplayAlert("Uyarı", "Biyometrik doğrulama kullanılamıyor.", "Tamam");
                await GuvenliGirisPaneliniYenileAsync();
                return;
            }

            if (!await _biyometrik.DogrulaAsync("Biyometrik girişi etkinleştirmek için doğrulayın"))
            {
                await GuvenliGirisPaneliniYenileAsync();
                return;
            }

            await GuvenliGirisDeposu.BiyometrikAyarlaAsync(true);
            await GuvenliGirisDeposu.EpostaIpucuAyarlaAsync(LblEposta.Text);
        }
        else
        {
            await GuvenliGirisDeposu.BiyometrikAyarlaAsync(false);
        }

        await GuvenliGirisPaneliniYenileAsync();
    }

    private async void PinAyarla_Clicked(object sender, EventArgs e)
    {
        var pin = await DisplayPromptAsync("PIN Ayarla", "4-6 haneli PIN girin:", "Kaydet", "İptal",
            keyboard: Keyboard.Numeric, maxLength: 6);
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
            return;

        var tekrar = await DisplayPromptAsync("PIN Tekrar", "PIN'i tekrar girin:", "Kaydet", "İptal",
            keyboard: Keyboard.Numeric, maxLength: 6);
        if (pin != tekrar)
        {
            await DisplayAlert("Uyarı", "PIN'ler eşleşmiyor.", "Tamam");
            return;
        }

        await GuvenliGirisDeposu.PinAyarlaAsync(pin);
        await GuvenliGirisDeposu.EpostaIpucuAyarlaAsync(LblEposta.Text);
        await DisplayAlert("Kaydedildi", "PIN ayarlandı.", "Tamam");
        await GuvenliGirisPaneliniYenileAsync();
    }

    private async void PinKaldir_Clicked(object sender, EventArgs e)
    {
        var onay = await DisplayAlert("PIN Kaldır", "PIN kaldırılsın mı?", "Kaldır", "İptal");
        if (!onay)
            return;

        await GuvenliGirisDeposu.PinKaldirAsync();
        await GuvenliGirisPaneliniYenileAsync();
    }

    private async void SifreGuncelle_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtMevcutSifre.Text) ||
            string.IsNullOrWhiteSpace(TxtYeniSifre.Text))
        {
            await DisplayAlert("Uyarı", "Mevcut ve yeni şifre zorunludur.", "Tamam");
            return;
        }

        if (TxtYeniSifre.Text != TxtYeniSifreTekrar.Text)
        {
            await DisplayAlert("Uyarı", "Yeni şifreler eşleşmiyor.", "Tamam");
            return;
        }

        try
        {
            await _oturum.Auth.SifreDegistirAsync(TxtMevcutSifre.Text, TxtYeniSifre.Text);
            TxtMevcutSifre.Text = "";
            TxtYeniSifre.Text = "";
            TxtYeniSifreTekrar.Text = "";
            await DisplayAlert("Güncellendi", "Şifreniz değiştirildi.", "Tamam");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }
}
