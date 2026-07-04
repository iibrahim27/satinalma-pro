using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SatinalmaPro.Mobile;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.ViewModels;

public partial class YonetimOnayViewModel : ObservableObject
{
    private readonly OturumServisi _oturum;

    [ObservableProperty] private List<SatinalmaTalep> _talepler = [];
    [ObservableProperty] private SatinalmaTalep? _seciliTalep;
    [ObservableProperty] private bool _yukleniyor;
    [ObservableProperty] private string _redGerekcesi = "";

    public YonetimOnayViewModel(OturumServisi oturum) => _oturum = oturum;

    [RelayCommand]
    public async Task YukleAsync()
    {
        Yukleniyor = true;
        try
        {
            await _oturum.VerileriYenileAsync();
            Talepler = _oturum.Satinalma.YonetimTalepleri()
                .OrderByDescending(t => t.TalepTuru == TalepTurleri.Acil)
                .ThenByDescending(t => t.Tarih)
                .ToList();
        }
        finally
        {
            Yukleniyor = false;
        }
    }

    [RelayCommand]
    private async Task OnaylaAsync(SatinalmaTalep talep)
    {
        if (talep.TalepTuru == TalepTurleri.Acil)
        {
            await BildirimNavigasyonServisi.RouteGitAsync($"acil-onay?talepId={talep.Id}");
            return;
        }

        Yukleniyor = true;
        try
        {
            var onay = await ShellGuvenli.DisplayAlertAsync(
                "Teklifsiz Onay",
                $"{talep.TalepNo} teklif olmadan onaylanacak.\nFirma ve birim fiyat satınalma tarafından sonradan girilecek.\n\nDevam?",
                "Onayla", "İptal");
            if (!onay) return;

            await _oturum.Satinalma.YonetimOnaylaAsync(talep, teklifIste: false);
            await YukleAsync();
            await ShellGuvenli.DisplayAlertAsync("Onaylandı", $"{talep.TalepNo} onaylandı.", "Tamam");
            await BildirimNavigasyonServisi.RouteGitAsync("//gecmis-talepler");
        }
        catch (Exception ex)
        {
            await ShellGuvenli.DisplayAlertAsync("Hata", ex.Message, "Tamam");
        }
        finally
        {
            Yukleniyor = false;
        }
    }

    [RelayCommand]
    private async Task TeklifIsteAsync(SatinalmaTalep talep)
    {
        if (talep.TalepTuru == TalepTurleri.Acil)
        {
            await ShellGuvenli.DisplayAlertAsync("Uyarı", "Acil taleplerde teklif istenemez.", "Tamam");
            return;
        }

        Yukleniyor = true;
        try
        {
            await _oturum.Satinalma.YonetimOnaylaAsync(talep, teklifIste: true);
            await YukleAsync();
            await ShellGuvenli.DisplayAlertAsync("Gönderildi", "Satınalmaya teklif girişi için bildirim gönderildi.", "Tamam");
            await BildirimNavigasyonServisi.RouteGitAsync("//gelen-talepler");
        }
        catch (Exception ex)
        {
            await ShellGuvenli.DisplayAlertAsync("Hata", ex.Message, "Tamam");
        }
        finally
        {
            Yukleniyor = false;
        }
    }

    [RelayCommand]
    private async Task ReddetAsync(SatinalmaTalep talep)
    {
        var secim = await ShellGuvenli.DisplayActionSheetAsync(
            "Red işlemi",
            "İptal",
            null,
            "Sebepli Reddet",
            "Sebepsiz Reddet");

        if (secim is null or "İptal")
            return;

        string? gerekce = "";
        if (secim == "Sebepli Reddet")
        {
            gerekce = await ShellGuvenli.DisplayPromptAsync("Red Gerekçesi", "Red sebebini yazın:", "Reddet", "İptal");
            if (gerekce is null)
                return;
        }

        Yukleniyor = true;
        try
        {
            await _oturum.Satinalma.YonetimReddetAsync(talep, gerekce);
            await YukleAsync();
            await BildirimNavigasyonServisi.RouteGitAsync("//red-talepler");
        }
        catch (Exception ex)
        {
            await ShellGuvenli.DisplayAlertAsync("Hata", ex.Message, "Tamam");
        }
        finally
        {
            Yukleniyor = false;
        }
    }
}
