using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile.Models.Yonetim;

namespace SatinalmaPro.Mobile.ViewModels.Yonetim;

public partial class YonetimTeklifDetayViewModel : ObservableObject
{
    [ObservableProperty] private YonetimSayfaDurumu _sayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
    [ObservableProperty] private YonetimTeklifOgesi? _teklif;
    [ObservableProperty] private string _hataMesaji = "Teklif bulunamadı.";
    [ObservableProperty] private bool _islemYapiliyor;

    [RelayCommand]
    public async Task YukleAsync(string? id)
    {
        SayfaDurumu = YonetimSayfaDurumu.Yukleniyor;
        try
        {
            await Task.Delay(400);
            Teklif = await YonetimMockVeriServisi.TeklifGetirAsync(id ?? "");
            if (Teklif is null)
            {
                HataMesaji = "Teklif bulunamadı.";
                SayfaDurumu = YonetimSayfaDurumu.Hata;
                return;
            }
            SayfaDurumu = YonetimSayfaDurumu.Icerik;
        }
        catch (Exception ex)
        {
            HataMesaji = ex.Message;
            SayfaDurumu = YonetimSayfaDurumu.Hata;
        }
    }

    [RelayCommand]
    private async Task FirmayiOnaylaAsync(YonetimFirmaTeklifOgesi firma)
    {
        if (Teklif is null || firma is null || IslemYapiliyor)
            return;

        var onay = await Shell.Current.DisplayAlert("Onayla", $"{firma.FirmaAdi} firmasını onaylamak istiyor musunuz?", "Evet", "Hayır");
        if (!onay)
            return;

        IslemYapiliyor = true;
        await Task.Delay(800);
        IslemYapiliyor = false;
        await Shell.Current.DisplayAlert("Onaylandı", $"{Teklif.TalepNo} — {firma.FirmaAdi} onaylandı.", "Tamam");
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task TeklifleriReddetAsync()
    {
        if (Teklif is null || IslemYapiliyor)
            return;

        var onay = await Shell.Current.DisplayAlert("Reddet", "Tüm teklifleri reddetmek istiyor musunuz?", "Evet", "Hayır");
        if (!onay)
            return;

        IslemYapiliyor = true;
        await Task.Delay(800);
        IslemYapiliyor = false;
        await Shell.Current.DisplayAlert("Reddedildi", $"{Teklif.TalepNo} teklifleri reddedildi.", "Tamam");
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task SatinalmayaGeriGonderAsync()
    {
        if (Teklif is null || IslemYapiliyor)
            return;

        IslemYapiliyor = true;
        await Task.Delay(800);
        IslemYapiliyor = false;
        await Shell.Current.DisplayAlert("Gönderildi", $"{Teklif.TalepNo} satınalmaya geri gönderildi.", "Tamam");
        await Shell.Current.GoToAsync("..");
    }
}
