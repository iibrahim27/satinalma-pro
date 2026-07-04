using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.ViewModels;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class OnaylananTaleplerPage : ContentPage
{
    private readonly OnaylananTaleplerViewModel _vm;
    private readonly OturumServisi _oturum;

    public OnaylananTaleplerPage(OnaylananTaleplerViewModel vm, OturumServisi oturum)
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
        if (!await MobilSayfaKorumasi.RotaErisimAsync(this, _oturum, "onaylanan-talepler"))
            return;
        await _vm.YukleCommand.ExecuteAsync(null);
        Liste.ItemsSource = _vm.Talepler;
    }

    private async void Liste_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SatinalmaTalep talep)
        {
            Liste.SelectedItem = null;
            await BildirimNavigasyonServisi.RouteGitAsync($"onay-gecmisi-detay?id={talep.Id}");
        }
    }
}
