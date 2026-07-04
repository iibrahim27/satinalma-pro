using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Models.Yonetim;

namespace SatinalmaPro.Mobile.ViewModels.Yonetim;

public partial class YonetimTekliflerViewModel : ObservableObject
{
    [ObservableProperty] private YonetimSayfaDurumu _sayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
    [ObservableProperty] private List<YonetimTeklifOgesi> _teklifler = [];
    [ObservableProperty] private string _hataMesaji = "Veriler yüklenemedi.";

    [RelayCommand]
    public async Task YukleAsync()
    {
        SayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
        try
        {
            await Task.Delay(500);
            Teklifler = (await YonetimMockVeriServisi.TekliflerGetirAsync()).ToList();
            SayfaDurumu = Teklifler.Count == 0 ? YonetimSayfaDurumu.Bos : YonetimSayfaDurumu.Icerik;
        }
        catch (Exception ex)
        {
            HataMesaji = ex.Message;
            SayfaDurumu = YonetimSayfaDurumu.Hata;
        }
    }

    [RelayCommand]
    private async Task TeklifDetayaGitAsync(YonetimTeklifOgesi teklif)
    {
        if (teklif is null)
            return;
        await Shell.Current.GoToAsync($"yonetim-teklif-detay?id={teklif.Id}");
    }
}
