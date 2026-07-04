using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;

    [ObservableProperty] private string _panelBasligi = "Panel";
    [ObservableProperty] private string _altBaslik = "";
    [ObservableProperty] private List<DashboardKart> _kartlar = [];
    [ObservableProperty] private List<DashboardAktivite> _sonAktivite = [];
    [ObservableProperty] private bool _yukleniyor;
    [ObservableProperty] private string _sonGuncelleme = "";

    public DashboardViewModel(OturumServisi oturum)
    {
        _oturum = oturum;
        _oturum.VeriGuncellendi += () => MainThread.BeginInvokeOnMainThread(Guncelle);
        _oturum.Dinleyici.BildirimlerDegisti += () => MainThread.BeginInvokeOnMainThread(Guncelle);
    }

    [RelayCommand]
    public async Task SayfaAcildiAsync()
    {
        if (Yukleniyor)
            return;

        Yukleniyor = true;
        try
        {
            await _oturum.VerileriYenileAsync();
        }
        catch
        {
            // Önbellekteki verilerle devam et
        }
        finally
        {
            Yukleniyor = false;
            Guncelle();
        }
    }

    [RelayCommand]
    private async Task KartaGitAsync(DashboardKart kart)
    {
        if (string.IsNullOrWhiteSpace(kart.Route))
            return;

        try
        {
            await _oturum.VerileriYenileAsync();
        }
        catch
        {
            // mevcut veriyle devam
        }

        Guncelle();
        await BildirimNavigasyonServisi.RouteGitAsync(kart.Route, _oturum);
    }

    [RelayCommand]
    private async Task AktiviteyeGitAsync(DashboardAktivite aktivite)
    {
        if (aktivite.TalepId is { } id)
            await BildirimNavigasyonServisi.RouteGitAsync($"talep-detay?id={id}", _oturum);
        else if (!string.IsNullOrWhiteSpace(aktivite.Route))
            await BildirimNavigasyonServisi.RouteGitAsync(aktivite.Route, _oturum);
    }

    private void Guncelle()
    {
        if (!_oturum.GirisYapildi)
            return;

        var ozet = DashboardServisi.Olustur(
            _oturum.Depo,
            _oturum.Satinalma,
            _oturum.Rol,
            _oturum.Dinleyici.OkunmamisSayisi);

        PanelBasligi = ozet.PanelBasligi;
        AltBaslik = ozet.AltBaslik;
        Kartlar = ozet.Kartlar;
        SonAktivite = ozet.SonAktivite;
        SonGuncelleme = DateTime.Now.ToString("HH:mm");
    }
}
