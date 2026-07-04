using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile.ViewModels.Yonetim;

public partial class YonetimProfilViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;

    [ObservableProperty] private string _kullaniciAdi = "";
    [ObservableProperty] private string _eposta = "";
    [ObservableProperty] private string _rol = "";
    [ObservableProperty] private bool _bildirimlerAcik = true;
    [ObservableProperty] private bool _islemYapiliyor;

    public YonetimProfilViewModel(OturumServisi oturum)
    {
        _oturum = oturum;
        KullaniciAdi = oturum.KullaniciAdi;
        Eposta = oturum.Depo.AktifKullanici?.Eposta ?? "—";
        Rol = oturum.Rol;
    }

    [RelayCommand]
    private async Task SifreDegistirAsync()
    {
        await Shell.Current.DisplayAlert("Şifre Değiştir", "Bu özellik backend bağlantısı sonrası aktif olacaktır.", "Tamam");
    }

    [RelayCommand]
    private async Task CikisYapAsync()
    {
        var onay = await Shell.Current.DisplayAlert("Çıkış Yap", "Oturumu kapatmak istiyor musunuz?", "Evet", "Hayır");
        if (!onay)
            return;

        _oturum.CikisYap();
        OturumYonlendirmeServisi.LoginSayfasinaGit(MauiProgram.Services!);
    }
}
