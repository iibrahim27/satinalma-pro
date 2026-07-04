using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Models.Yonetim;

namespace SatinalmaPro.Mobile.ViewModels.Yonetim;

public partial class YonetimTalepDetayViewModel : ObservableObject
{
    [ObservableProperty] private YonetimSayfaDurumu _sayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
    [ObservableProperty] private YonetimTalepOgesi? _talep;
    [ObservableProperty] private string _hataMesaji = "Talep bulunamadı.";
    [ObservableProperty] private bool _islemYapiliyor;

    public bool AcilAksiyonlar => Talep?.AcilMi == true && Talep.Durum == YonetimTalepDurum.Bekleyen;
    public bool NormalAksiyonlar => Talep?.AcilMi == false && Talep?.Durum == YonetimTalepDurum.Bekleyen;
    public bool AksiyonAktif => !IslemYapiliyor;

    [RelayCommand]
    public async Task YukleAsync(string? id)
    {
        SayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
        try
        {
            await Task.Delay(400);
            Talep = await YonetimMockVeriServisi.TalepGetirAsync(id ?? "");
            if (Talep is null)
            {
                HataMesaji = "Talep bulunamadı.";
                SayfaDurumu = YonetimSayfaDurumu.Hata;
                return;
            }
            SayfaDurumu = YonetimSayfaDurumu.Icerik;
            OnPropertyChanged(nameof(AcilAksiyonlar));
            OnPropertyChanged(nameof(NormalAksiyonlar));
            OnPropertyChanged(nameof(AksiyonAktif));
        }
        catch (Exception ex)
        {
            HataMesaji = ex.Message;
            SayfaDurumu = YonetimSayfaDurumu.Hata;
        }
    }

    partial void OnIslemYapiliyorChanged(bool value) => OnPropertyChanged(nameof(AksiyonAktif));

    [RelayCommand]
    private async Task AcilAlimiOnaylaAsync()
    {
        if (Talep is null || IslemYapiliyor)
            return;
        IslemYapiliyor = true;
        await Task.Delay(800);
        IslemYapiliyor = false;
        await Shell.Current.DisplayAlert("Onaylandı", $"{Talep.TalepNo} acil alım olarak onaylandı.", "Tamam");
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task TeklifIsteAsync()
    {
        if (Talep is null || IslemYapiliyor)
            return;
        IslemYapiliyor = true;
        await Task.Delay(800);
        IslemYapiliyor = false;
        await Shell.Current.DisplayAlert("Teklif İstendi", $"{Talep.TalepNo} satınalmaya teklif toplama için gönderildi.", "Tamam");
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task DogrudanOnaylaAsync()
    {
        if (Talep is null || IslemYapiliyor)
            return;
        IslemYapiliyor = true;
        await Task.Delay(800);
        IslemYapiliyor = false;
        await Shell.Current.DisplayAlert("Onaylandı", $"{Talep.TalepNo} doğrudan onaylandı.", "Tamam");
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ReddetAsync()
    {
        if (Talep is null || IslemYapiliyor)
            return;

        var onay = await Shell.Current.DisplayAlert("Reddet", $"{Talep.TalepNo} talebini reddetmek istediğinize emin misiniz?", "Evet", "Hayır");
        if (!onay)
            return;

        IslemYapiliyor = true;
        await Task.Delay(800);
        IslemYapiliyor = false;
        await Shell.Current.DisplayAlert("Reddedildi", $"{Talep.TalepNo} reddedildi.", "Tamam");
        await Shell.Current.GoToAsync("..");
    }
}
