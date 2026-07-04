using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.ViewModels;

namespace SatinalmaPro.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private bool _shellAcildi;

    public LoginPage(OturumServisi oturum, LoginViewModel vm)
    {
        InitializeComponent();
        _oturum = oturum;
        BindingContext = vm;
        SayfaYardimcisi.SurumAltBilgiEkle(this);
        vm.GirisBasarili += () => MainThread.BeginInvokeOnMainThread(GirisSonrasiShellAc);
        FirebaseUyarisiniGuncelle(vm);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await FirebaseAyarServisi.PaketDosyalariniHazirlaAsync();
        _oturum.AyarlariYenile();
        if (BindingContext is LoginViewModel vm)
            FirebaseUyarisiniGuncelle(vm);
    }

    private void FirebaseUyarisiniGuncelle(LoginViewModel vm)
    {
        vm.FirebaseYapilandirildi = _oturum.Ayarlar.Yapilandirildi;
        LblFirebaseUyari.IsVisible = !vm.FirebaseYapilandirildi;
    }

    private void GirisSonrasiShellAc()
    {
        if (_shellAcildi)
            return;

        if (Application.Current?.Windows.FirstOrDefault()?.Page is AppShell)
        {
            _shellAcildi = true;
            return;
        }

        var services = IPlatformApplication.Current?.Services ?? MauiProgram.Services;
        if (services is null)
            return;

        _shellAcildi = true;
        OturumYonlendirmeServisi.ShellAc(services);
    }
}
