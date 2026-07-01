using SatinalmaPro.Mobile.ViewModels;

namespace SatinalmaPro.Mobile.Views;

public partial class AnaSayfaPage : ContentPage
{
    private readonly DashboardViewModel _vm;

    public AnaSayfaPage(DashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = _vm.SayfaAcildiCommand.ExecuteAsync(null);
    }
}
