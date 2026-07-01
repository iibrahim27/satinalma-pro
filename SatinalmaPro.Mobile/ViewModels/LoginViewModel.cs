using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Mobile.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;

    [ObservableProperty] private string _eposta = "";
    [ObservableProperty] private string _sifre = "";
    [ObservableProperty] private bool _yukleniyor;
    [ObservableProperty] private string _hata = "";
    [ObservableProperty] private bool _firebaseYapilandirildi;

    public event Action? GirisBasarili;

    public LoginViewModel(OturumServisi oturum)
    {
        _oturum = oturum;
        _oturum.AyarlariYenile();
        FirebaseYapilandirildi = oturum.Ayarlar.Yapilandirildi;
    }

    [RelayCommand]
    private async Task GirisYapAsync()
    {
        if (Yukleniyor)
            return;

        Hata = "";
        Yukleniyor = true;
        try
        {
            await _oturum.GirisYapAsync(Eposta, Sifre);
            await GuvenliGirisDeposu.EpostaIpucuAyarlaAsync(Eposta);
            GirisBasarili?.Invoke();
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
