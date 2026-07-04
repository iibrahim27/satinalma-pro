using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class StokDurumPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private List<StokKaydi> _tum = [];

    public StokDurumPage(OturumServisi oturum)
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
        if (!await MobilSayfaKorumasi.StokDurumErisimAsync(this, _oturum))
            return;

        await Yukle();
    }

    private async Task Yukle()
    {
        await _oturum.VerileriYenileAsync();
        _tum = _oturum.Depo.Stok.OrderBy(s => s.MalzemeAdi).ToList();
        var kritik = _tum.Count(s => s.DurumMetin is "Kritik" or "Tükendi");
        LblKritikOzet.Text = kritik > 0
            ? $"⚠ {kritik} kalem kritik veya tükenmiş (min. stok altı)"
            : "Kritik stok uyarısı yok";
        Filtrele();
    }

    private void AramaBar_TextChanged(object sender, TextChangedEventArgs e) => Filtrele();

    private void Filtrele()
    {
        var metin = AramaBar.Text ?? "";
        Liste.ItemsSource = string.IsNullOrWhiteSpace(metin)
            ? _tum
            : _tum.Where(s => s.MalzemeAdi.Contains(metin, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
