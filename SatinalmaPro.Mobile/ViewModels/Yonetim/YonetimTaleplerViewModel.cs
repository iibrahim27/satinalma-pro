using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Models.Yonetim;

namespace SatinalmaPro.Mobile.ViewModels.Yonetim;

public partial class YonetimTaleplerViewModel : ObservableObject
{
    [ObservableProperty] private YonetimSayfaDurumu _sayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
    [ObservableProperty] private List<YonetimTalepOgesi> _talepler = [];
    [ObservableProperty] private List<YonetimFiltreOgesi> _filtreler = [];
    [ObservableProperty] private string _aramaMetni = "";
    [ObservableProperty] private string _hataMesaji = "Veriler yüklenemedi.";
    [ObservableProperty] private string _seciliFiltre = "tumu";

    public YonetimTaleplerViewModel()
    {
        Filtreler = YonetimMockVeriServisi.VarsayilanFiltreler().ToList();
    }

    [RelayCommand]
    public async Task YukleAsync()
    {
        SayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
        try
        {
            await Task.Delay(500);
            var liste = await YonetimMockVeriServisi.TaleplerGetirAsync(SeciliFiltre, AramaMetni);
            Talepler = liste.ToList();
            SayfaDurumu = Talepler.Count == 0 ? YonetimSayfaDurumu.Bos : YonetimSayfaDurumu.Icerik;
        }
        catch (Exception ex)
        {
            HataMesaji = ex.Message;
            SayfaDurumu = YonetimSayfaDurumu.Hata;
        }
    }

    [RelayCommand]
    private async Task FiltreSecAsync(YonetimFiltreOgesi filtre)
    {
        if (filtre is null)
            return;

        SeciliFiltre = filtre.Anahtar;
        foreach (var f in Filtreler)
            f.Secili = f.Anahtar == filtre.Anahtar;
        OnPropertyChanged(nameof(Filtreler));
        await YukleAsync();
    }

    partial void OnAramaMetniChanged(string value) => _ = YukleAsync();

    [RelayCommand]
    private async Task TalepDetayaGitAsync(YonetimTalepOgesi talep)
    {
        if (talep is null)
            return;
        await Shell.Current.GoToAsync($"yonetim-talep-detay?id={talep.Id}");
    }
}
