using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.ViewModels;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

public partial class GelenTaleplerPage : ContentPage
{
    private readonly YonetimOnayViewModel _vm;
    private readonly OturumServisi _oturum;

    public GelenTaleplerPage(YonetimOnayViewModel vm, OturumServisi oturum)
    {
        InitializeComponent();
        _vm = vm;
        _oturum = oturum;
        RefreshView.Command = new Command(async () =>
        {
            await _vm.YukleCommand.ExecuteAsync(null);
            Liste.ItemsSource = _vm.Talepler;
            RefreshView.IsRefreshing = false;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await MobilSayfaKorumasi.RotaErisimAsync(this, _oturum, "gelen-talepler"))
            return;

        await _vm.YukleCommand.ExecuteAsync(null);
        Liste.ItemsSource = _vm.Talepler;
    }

    private async void Onayla_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SatinalmaTalep talep)
        {
            if (talep.TeklifGirilmis)
            {
                await BildirimNavigasyonServisi.RouteGitAsync($"teklif-onay-detay?id={talep.Id}", _oturum);
                return;
            }

            if (talep.TalepTuru == TalepTurleri.Acil)
                await BildirimNavigasyonServisi.RouteGitAsync($"acil-onay?talepId={talep.Id}");
            else
                await _vm.OnaylaCommand.ExecuteAsync(talep);
        }
        Liste.ItemsSource = _vm.Talepler;
    }

    private async void TeklifIste_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SatinalmaTalep talep)
        {
            if (talep.TeklifGirilmis)
            {
                await BildirimNavigasyonServisi.RouteGitAsync($"teklif-onay-detay?id={talep.Id}", _oturum);
                return;
            }

            await _vm.TeklifIsteCommand.ExecuteAsync(talep);
        }
        Liste.ItemsSource = _vm.Talepler;
    }

    private async void Reddet_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SatinalmaTalep talep)
            await _vm.ReddetCommand.ExecuteAsync(talep);
        Liste.ItemsSource = _vm.Talepler;
    }
}
