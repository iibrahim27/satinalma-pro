using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class TeklifKarsilastirmaPage : ContentPage
{
    private readonly OturumServisi _oturum;

    public TeklifKarsilastirmaPage(OturumServisi oturum)
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
        if (!await MobilSayfaKorumasi.RotaErisimAsync(this, _oturum, "teklif-karsilastirma"))
            return;
        await YukleAsync();
    }

    private async Task YukleAsync()
    {
        await _oturum.VerileriYenileAsync();
        Liste.ItemsSource = _oturum.Satinalma.KarsilastirmaBekleyenleri()
            .OrderByDescending(t => t.Tarih)
            .ToList();
    }

    private async void YonetimeGonder_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: SatinalmaTalep talep })
            return;

        var onay = await DisplayAlert(
            "Yönetime Gönder",
            $"{talep.TalepNo} teklifleri yönetim onayına gönderilecek.\nDevam?",
            "Gönder", "İptal");
        if (!onay)
            return;

        try
        {
            await _oturum.Satinalma.YonetimeTeklifOnayGonderAsync(talep);
            _ = _oturum.Dinleyici.SenkronizeVeGosterAsync();
            await DisplayAlert("Gönderildi", "Yönetim kullanıcılarına bildirim gönderildi.", "Tamam");
            await YukleAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }
}
