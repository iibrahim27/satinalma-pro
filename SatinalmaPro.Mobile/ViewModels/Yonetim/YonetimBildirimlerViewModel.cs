using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Models.Yonetim;

namespace SatinalmaPro.Mobile.ViewModels.Yonetim;

public partial class YonetimBildirimlerViewModel : ObservableObject
{
    [ObservableProperty] private YonetimSayfaDurumu _sayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
    [ObservableProperty] private List<YonetimBildirimOgesi> _bildirimler = [];
    [ObservableProperty] private string _hataMesaji = "Veriler yüklenemedi.";

    public int OkunmamisSayisi => Bildirimler.Count(b => !b.Okundu);
    public bool OkunmamisVar => OkunmamisSayisi > 0;

    [RelayCommand]
    public async Task YukleAsync()
    {
        SayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
        try
        {
            await Task.Delay(400);
            Bildirimler = (await YonetimMockVeriServisi.BildirimlerGetirAsync()).ToList();
            SayfaDurumu = Bildirimler.Count == 0 ? YonetimSayfaDurumu.Bos : YonetimSayfaDurumu.Icerik;
            OnPropertyChanged(nameof(OkunmamisSayisi));
            OnPropertyChanged(nameof(OkunmamisVar));
        }
        catch (Exception ex)
        {
            HataMesaji = ex.Message;
            SayfaDurumu = YonetimSayfaDurumu.Hata;
        }
    }

    [RelayCommand]
    private async Task BildirimeTiklaAsync(YonetimBildirimOgesi bildirim)
    {
        if (bildirim is null)
            return;

        await Shell.Current.DisplayAlert(bildirim.Baslik, bildirim.Mesaj, "Tamam");
    }
}
