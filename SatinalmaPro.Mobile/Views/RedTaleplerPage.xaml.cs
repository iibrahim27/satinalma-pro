using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class RedTaleplerPage : ContentPage
{
    private readonly OturumServisi _oturum;

    public RedTaleplerPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
        RefreshView.Command = new Command(async () =>
        {
            await YukleAsync();
            RefreshView.IsRefreshing = false;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await YukleAsync();
    }

    private async Task YukleAsync()
    {
        await _oturum.VerileriYenileAsync();
        Liste.ItemsSource = _oturum.Satinalma.YonetimReddedilenleri()
            .OrderByDescending(t => t.Tarih)
            .ToList();
    }

    private async void Liste_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SatinalmaTalep talep)
            return;

        Liste.SelectedItem = null;
        await BildirimNavigasyonServisi.RouteGitAsync($"talep-detay?id={talep.Id}", _oturum);
    }
}
