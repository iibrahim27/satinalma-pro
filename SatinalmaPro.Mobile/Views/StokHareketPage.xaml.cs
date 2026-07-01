using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile.Views;

public partial class StokHareketPage : ContentPage
{
    private readonly OturumServisi _oturum;

    public StokHareketPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
        RefreshView.Command = new Command(async () =>
        {
            await Yukle();
            RefreshView.IsRefreshing = false;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await MobilSayfaKorumasi.StokHareketErisimAsync(this, _oturum))
            return;

        await Yukle();
    }

    private async Task Yukle()
    {
        await _oturum.VerileriYenileAsync();
        Liste.ItemsSource = _oturum.Depo.StokHareketleri
            .OrderByDescending(h => h.Tarih)
            .Take(500)
            .ToList();
    }
}
