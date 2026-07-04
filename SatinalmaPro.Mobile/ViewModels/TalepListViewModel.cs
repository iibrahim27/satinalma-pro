using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.ViewModels;

public partial class TalepListViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;

    [ObservableProperty] private List<SatinalmaTalep> _talepler = [];
    [ObservableProperty] private bool _yukleniyor;

    public TalepListViewModel(OturumServisi oturum) => _oturum = oturum;

    [RelayCommand]
    public async Task YukleAsync()
    {
        Yukleniyor = true;
        try
        {
            await _oturum.VerileriYenileAsync();
            var uid = _oturum.Depo.AktifKullanici?.Uid ?? "";
            var ad = _oturum.Depo.AktifKullanici?.AdSoyad;
            Talepler = _oturum.Depo.Talepler
                .Where(t => SatinalmaPro.Shared.Helpers.SatinalmaTalepKuyrugu.TaleplerimListesindeGoster(t, uid, ad, _oturum.Rol))
                .OrderByDescending(t => t.Tarih)
                .ToList();
        }
        finally
        {
            Yukleniyor = false;
        }
    }
}
