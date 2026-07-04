using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Mobile.ViewModels;

public partial class KilitAcmaViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;
    private readonly IBiyometrikKimlikServisi _biyometrik;

    [ObservableProperty] private string _pinKodu = "";
    [ObservableProperty] private string _hata = "";
    [ObservableProperty] private bool _yukleniyor;
    [ObservableProperty] private bool _biyometrikKullanilabilir;
    [ObservableProperty] private bool _pinAktif;
    [ObservableProperty] private string _epostaIpucu = "";

    public event Action? GirisBasarili;

    public KilitAcmaViewModel(OturumServisi oturum, IBiyometrikKimlikServisi biyometrik)
    {
        _oturum = oturum;
        _biyometrik = biyometrik;
    }

    [RelayCommand]
    private async Task BiyometrikGirisAsync()
    {
        if (Yukleniyor)
            return;

        Hata = "";
        try
        {
            if (!await _biyometrik.KullanilabilirMiAsync())
            {
                Hata = "Biyometrik doğrulama kullanılamıyor.";
                return;
            }

            if (!await _biyometrik.DogrulaAsync("Uygulamayı açmak için doğrulayın"))
                return;

            await OturumuAcAsync();
        }
        catch (Exception ex)
        {
            Hata = AgHataMesaji.Turkcele(ex.Message);
        }
    }

    [RelayCommand]
    private async Task PinGirisAsync()
    {
        if (Yukleniyor || string.IsNullOrWhiteSpace(PinKodu))
        {
            Hata = "PIN girin.";
            return;
        }

        Hata = "";
        try
        {
            if (!await GuvenliGirisDeposu.PinDogrulaAsync(PinKodu))
            {
                Hata = "PIN hatalı.";
                PinKodu = "";
                return;
            }

            await OturumuAcAsync();
        }
        catch (Exception ex)
        {
            Hata = AgHataMesaji.Turkcele(ex.Message);
        }
    }

    [RelayCommand]
    private async Task HazirlikAsync()
    {
        BiyometrikKullanilabilir = await _biyometrik.KullanilabilirMiAsync();
        PinAktif = await GuvenliGirisDeposu.PinAyarliMiAsync();
        EpostaIpucu = await GuvenliGirisDeposu.EpostaIpucuAlAsync() ?? "";

        if (BiyometrikKullanilabilir && await GuvenliGirisDeposu.BiyometrikAktifMiAsync())
            await BiyometrikGirisAsync();
    }

    private async Task OturumuAcAsync()
    {
        if (Yukleniyor)
            return;

        Yukleniyor = true;
        try
        {
            if (await _oturum.KayitliOturumuDeneAsync())
                GirisBasarili?.Invoke();
            else
                Hata = "Oturum süresi doldu. Çıkış yapıp tekrar giriş yapın.";
        }
        catch (Exception ex)
        {
            Hata = AgHataMesaji.Turkcele(ex.Message);
        }
        finally
        {
            Yukleniyor = false;
        }
    }
}
