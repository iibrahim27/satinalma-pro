using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.ViewModels;

public partial class OnayBekleyenTaleplerViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;

    [ObservableProperty] private List<SatinalmaTalep> _talepler = [];
    [ObservableProperty] private bool _yukleniyor;

    public OnayBekleyenTaleplerViewModel(OturumServisi oturum) => _oturum = oturum;

    [RelayCommand]
    public async Task YukleAsync()
    {
        Yukleniyor = true;
        try
        {
            await _oturum.VerileriYenileAsync();
            Talepler = _oturum.Satinalma.OnayBekleyenTalepler()
                .OrderByDescending(t => t.TalepTuru == TalepTurleri.Acil)
                .ThenByDescending(t => t.Tarih)
                .ToList();
        }
        finally
        {
            Yukleniyor = false;
        }
    }
}
