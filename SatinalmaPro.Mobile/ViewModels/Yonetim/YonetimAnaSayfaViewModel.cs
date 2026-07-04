using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Models.Yonetim;
using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile.ViewModels.Yonetim;

public partial class YonetimAnaSayfaViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;

    [ObservableProperty] private YonetimSayfaDurumu _sayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
    [ObservableProperty] private YonetimDashboardOzet? _ozet;
    [ObservableProperty] private List<YonetimTalepOgesi> _sonTalepler = [];
    [ObservableProperty] private string _kullaniciAdi = "";
    [ObservableProperty] private string _hataMesaji = "Veriler yüklenemedi.";

    public YonetimAnaSayfaViewModel(OturumServisi oturum)
    {
        _oturum = oturum;
        KullaniciAdi = oturum.KullaniciAdi;
    }

    [RelayCommand]
    public async Task YukleAsync()
    {
        SayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
        try
        {
            await Task.Delay(600);
            Ozet = await YonetimMockVeriServisi.DashboardGetirAsync();
            SonTalepler = (await YonetimMockVeriServisi.SonTaleplerGetirAsync()).ToList();
            SayfaDurumu = SonTalepler.Count == 0 ? YonetimSayfaDurumu.Bos : YonetimSayfaDurumu.Icerik;
        }
        catch (Exception ex)
        {
            HataMesaji = ex.Message;
            SayfaDurumu = YonetimSayfaDurumu.Hata;
        }
    }

    [RelayCommand]
    private async Task TalepDetayaGitAsync(YonetimTalepOgesi talep)
    {
        if (talep is null)
            return;
        await Shell.Current.GoToAsync($"yonetim-talep-detay?id={talep.Id}");
    }
}
