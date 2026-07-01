using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.ViewModels;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class TalepListPage : ContentPage
{
    private readonly TalepListViewModel _vm;

    public TalepListPage(TalepListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        RefreshView.Command = new Command(async () =>
        {
            await _vm.YukleCommand.ExecuteAsync(null);
            RefreshView.IsRefreshing = false;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Yukle();
    }

    private async Task Yukle()
    {
        Yukleniyor.IsVisible = Yukleniyor.IsRunning = true;
        await _vm.YukleCommand.ExecuteAsync(null);
        Liste.ItemsSource = _vm.Talepler;
        Yukleniyor.IsVisible = Yukleniyor.IsRunning = false;
    }

    private async void Yenile_Clicked(object sender, EventArgs e) => await Yukle();

    private async void Liste_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SatinalmaTalep talep)
        {
            Liste.SelectedItem = null;
            await BildirimNavigasyonServisi.RouteGitAsync($"talep-detay?id={talep.Id}");
        }
    }
}
