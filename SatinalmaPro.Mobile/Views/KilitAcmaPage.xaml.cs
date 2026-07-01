using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.ViewModels;

namespace SatinalmaPro.Mobile.Views;

public partial class KilitAcmaPage : ContentPage
{
    private bool _hazirlikDenendi;
    private bool _shellAcildi;

    public KilitAcmaPage(KilitAcmaViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        SayfaYardimcisi.SurumAltBilgiEkle(this);
        vm.GirisBasarili += () => MainThread.BeginInvokeOnMainThread(ShellAc);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_hazirlikDenendi)
            return;

        _hazirlikDenendi = true;
        await Task.Delay(350);
        if (BindingContext is KilitAcmaViewModel vm)
            await vm.HazirlikCommand.ExecuteAsync(null);
    }

    private void ShellAc()
    {
        if (_shellAcildi)
            return;

        var services = IPlatformApplication.Current?.Services ?? MauiProgram.Services;
        if (services is null)
            return;

        _shellAcildi = true;
        OturumYonlendirmeServisi.ShellAc(services);
    }
}
