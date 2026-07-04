using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

public partial class YonetimOnaylananTekliflerPage : ContentPage
{
    private readonly OturumServisi _oturum;

    public YonetimOnaylananTekliflerPage(OturumServisi oturum)
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
        if (!await MobilSayfaKorumasi.RotaErisimAsync(this, _oturum, "onaylanan-teklifler"))
            return;
        await YukleAsync();
    }

    private async Task YukleAsync()
    {
        await _oturum.VerileriYenileAsync();
        Liste.ItemsSource = _oturum.Satinalma.YonetimOnaylananTeklifleri()
            .OrderByDescending(t => t.YonetimOnayTarihi)
            .Select(t => new OnayGecmisiListeOgesi { Talep = t })
            .ToList();
    }

    private async void Liste_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not OnayGecmisiListeOgesi oge)
            return;

        Liste.SelectedItem = null;
        await BildirimNavigasyonServisi.RouteGitAsync($"onay-gecmisi-detay?id={oge.Talep.Id}", _oturum);
    }
}
