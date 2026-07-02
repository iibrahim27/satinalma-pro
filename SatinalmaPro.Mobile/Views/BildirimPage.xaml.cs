using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class BildirimPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private List<BildirimKaydi> _liste = [];

    public BildirimPage(OturumServisi oturum)
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
        if (!await MobilSayfaKorumasi.RotaErisimAsync(this, _oturum, "bildirimler"))
            return;
        await Yukle();
    }

    private async Task Yukle()
    {
        await _oturum.VerileriYenileAsync();
        _liste = _oturum.Bildirimler.KullaniciBildirimleri(_oturum.Depo.AktifKullanici).ToList();
        Liste.ItemsSource = _liste;
    }

    private async void Liste_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is BildirimKaydi bildirim)
        {
            Liste.SelectedItem = null;
            await _oturum.Bildirimler.OkunduIsaretleAsync(bildirim);
            await BildirimNavigasyonServisi.BildirimdenGitAsync(bildirim, _oturum);
        }
    }

    private async void Temizle_Clicked(object? sender, EventArgs e)
    {
        if (_oturum.Depo.AktifKullanici is not { } kullanici)
            return;

        var onay = await DisplayAlert(
            "Bildirimleri Temizle",
            "Onay bekleyen teklif bildirimleri korunur. Diğer bildirimler silinecek. Devam edilsin mi?",
            "Temizle",
            "İptal");

        if (!onay)
            return;

        await _oturum.Bildirimler.TemizleAsync(kullanici);
        await Yukle();
    }

    private async void TumunuOkundu_Clicked(object? sender, EventArgs e)
    {
        if (_oturum.Depo.AktifKullanici is not { } kullanici)
            return;

        await _oturum.Bildirimler.TumunuOkunduIsaretleAsync(kullanici);
        await Yukle();
    }
}
