using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

public partial class TeklifOnayPage : ContentPage
{
    private readonly OturumServisi _oturum;

    public TeklifOnayPage(OturumServisi oturum)
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
        if (!await MobilSayfaKorumasi.RotaErisimAsync(this, _oturum, "teklif-onay"))
            return;

        await YukleAsync();
    }

    private async Task YukleAsync()
    {
        await _oturum.VerileriYenileAsync();
        Liste.ItemsSource = _oturum.Satinalma.YonetimTeklifOnayiBekleyenleri()
            .OrderByDescending(t => t.Tarih)
            .ToList();
    }

    private async void Liste_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SatinalmaTalep talep)
            return;

        Liste.SelectedItem = null;
        await BildirimNavigasyonServisi.RouteGitAsync($"teklif-onay-detay?id={talep.Id}", _oturum);
    }
}
