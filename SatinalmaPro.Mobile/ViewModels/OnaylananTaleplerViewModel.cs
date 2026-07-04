using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.ViewModels;

public partial class OnaylananTaleplerViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;

    [ObservableProperty] private List<SatinalmaTalep> _talepler = [];
    [ObservableProperty] private bool _yukleniyor;

    public OnaylananTaleplerViewModel(OturumServisi oturum) => _oturum = oturum;

    [RelayCommand]
    public async Task YukleAsync()
    {
        Yukleniyor = true;
        try
        {
            await _oturum.VerileriYenileAsync();
            Talepler = _oturum.Satinalma.OnaylanmisTalepler()
                .OrderByDescending(t => t.Tarih)
                .ThenByDescending(t => t.TalepNo)
                .ToList();
        }
        finally
        {
            Yukleniyor = false;
        }
    }
}
