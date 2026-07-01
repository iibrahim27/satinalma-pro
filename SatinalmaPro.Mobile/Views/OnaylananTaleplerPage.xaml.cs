using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.ViewModels;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class OnaylananTaleplerPage : ContentPage
{
    private readonly OnaylananTaleplerViewModel _vm;

    public OnaylananTaleplerPage(OnaylananTaleplerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
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
