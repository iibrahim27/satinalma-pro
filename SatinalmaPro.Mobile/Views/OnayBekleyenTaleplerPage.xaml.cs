using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.ViewModels;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

public partial class OnayBekleyenTaleplerPage : ContentPage
{
    private readonly OnayBekleyenTaleplerViewModel _vm;
    private readonly OturumServisi _oturum;

    public OnayBekleyenTaleplerPage(OnayBekleyenTaleplerViewModel vm, OturumServisi oturum)
    {
        InitializeComponent();
        _vm = vm;
        _oturum = oturum;        RefreshView.Command = new Command(async () =>
        {
            await _vm.YukleCommand.ExecuteAsync(null);
            Liste.ItemsSource = _vm.Talepler;
            RefreshView.IsRefreshing = false;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.YukleCommand.ExecuteAsync(null);
        Liste.ItemsSource = _vm.Talepler;
    }

    private async void Liste_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SatinalmaTalep talep)
            return;

        Liste.SelectedItem = null;

        var rol = KullaniciRolleri.Normalize(_oturum.Rol);
        if (rol == KullaniciRolleri.Yonetim)
        {
            if (SatinalmaTalepYardimcisi.YonetimTeklifKarariBekliyor(talep))
                await BildirimNavigasyonServisi.RouteGitAsync($"teklif-onay-detay?id={talep.Id}", _oturum);
            else if (SatinalmaTalepKuyrugu.YonetimTalepler(talep))
            {
                if (talep.TalepTuru == TalepTurleri.Acil)
                    await BildirimNavigasyonServisi.RouteGitAsync($"acil-onay?talepId={talep.Id}", _oturum);
                else
                    await BildirimNavigasyonServisi.RouteGitAsync("//gelen-talepler", _oturum);
            }
            else
                await BildirimNavigasyonServisi.RouteGitAsync("//gelen-talepler", _oturum);
            return;
        }

        await BildirimNavigasyonServisi.RouteGitAsync($"talep-detay?id={talep.Id}", _oturum);
    }
}