using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

public sealed class OnayGecmisiListeOgesi
{
    public required SatinalmaTalep Talep { get; init; }
    public string TalepNo => Talep.TalepNo;
    public string TalepAciklamasi => string.IsNullOrWhiteSpace(Talep.TalepAciklamasi) ? Talep.TalepEden : Talep.TalepAciklamasi;
    public string OnayTipi => SatinalmaMobilServisi.OnayTipiMetni(Talep);
    public string OnayTarihi => Talep.YonetimOnayTarihi;
    public string Firma
    {
        get
        {
            var teklif = Talep.OnaylananTeklif;
            if (teklif is null)
                return Talep.TeklifsizYonetimOnayi ? "Teklifsiz onay" : "";
            teklif.FiyatlariHesapla(Talep.Kalemler);
            return $"{teklif.FirmaAdi} · {teklif.GenelToplam:N2} ₺";
        }
    }
}

public partial class OnayGecmisiPage : ContentPage
{
    private readonly OturumServisi _oturum;

    public OnayGecmisiPage(OturumServisi oturum)
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
        if (!await MobilSayfaKorumasi.RotaErisimAsync(this, _oturum, "onay-gecmisi"))
            return;
        await YukleAsync();
    }

    private async Task YukleAsync()
    {
        await _oturum.VerileriYenileAsync();
        var uid = _oturum.Depo.AktifKullanici?.Uid ?? "";
        Liste.ItemsSource = _oturum.Satinalma.YonetimOnayGecmisi(uid)
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
