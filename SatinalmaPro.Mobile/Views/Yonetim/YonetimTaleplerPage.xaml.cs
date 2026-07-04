using SatinalmaPro.Mobile.ViewModels.Yonetim;

namespace SatinalmaPro.Mobile.Views.Yonetim;

public partial class YonetimTaleplerPage : ContentPage
{
    private readonly YonetimTaleplerViewModel _vm;

    public YonetimTaleplerPage(YonetimTaleplerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        DurumGorunumu.TekrarDeneTiklandi += (_, _) => _ = _vm.YukleCommand.ExecuteAsync(null);
        RefreshView.Refreshing += async (_, _) =>
        {
            await _vm.YukleCommand.ExecuteAsync(null);
            RefreshView.IsRefreshing = false;
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.YukleCommand.ExecuteAsync(null);
    }
}
