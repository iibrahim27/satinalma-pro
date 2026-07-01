using System.Globalization;
using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

[QueryProperty(nameof(TalepId), "talepId")]
[QueryProperty(nameof(KalemId), "kalemId")]
public partial class StokAktarPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private string _talepId = "";
    private string _kalemId = "";
    private OnaylananMalzemeSatiri? _satir;

    public string TalepId
    {
        get => _talepId;
        set { _talepId = value; _ = YukleAsync(); }
    }

    public string KalemId
    {
        get => _kalemId;
        set { _kalemId = value; _ = YukleAsync(); }
    }

    public StokAktarPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!MobilYetkiServisi.MalKabulVeStokAktarYapabilir(_oturum.Rol))
        {
            await DisplayAlert("Yetki", "Stoğa aktarım yalnızca Satınalma rolü tarafından yapılabilir.", "Tamam");
            await ShellGuvenli.GoToAsync("..");
        }
    }

    private async Task YukleAsync()
    {
        if (!Guid.TryParse(_talepId, out var talepGuid) || !Guid.TryParse(_kalemId, out var kalemGuid))
            return;

        await _oturum.VerileriYenileAsync();
        _satir = _oturum.Satinalma.OnaylananMalzemeleriOlustur()
            .FirstOrDefault(s => s.TalepId == talepGuid && s.KalemId == kalemGuid);

        if (_satir is null)
        {
            await DisplayAlert("Hata", "Malzeme bulunamadı.", "Tamam");
            await ShellGuvenli.GoToAsync("..");
            return;
        }

        if (_oturum.Satinalma.StogaDahaOnceAktarildi(_satir))
        {
            await DisplayAlert("Uyarı",
                $"{_satir.Malzeme} daha önce stoğa aktarılmış.\nBelge: {_satir.AktarimBelgeNo()}",
                "Tamam");
            await ShellGuvenli.GoToAsync("..");
            return;
        }

        LblBaslik.Text = _satir.Malzeme;
        LblOzet.Text = $"{_satir.Firma} · {_satir.TalepNo}\nSipariş: {_satir.SiparisMiktari:N2} {_satir.Birim} · Kabul: {_satir.KabulEdilenMiktar:N2}";

        var varsayilan = _satir.KalanMiktar > 0 ? _satir.KalanMiktar : _satir.KabulEdilenMiktar;
        if (varsayilan <= 0)
            varsayilan = _satir.SiparisMiktari;

        TxtMiktar.Text = varsayilan.ToString("N2", CultureInfo.CurrentCulture);
        TxtTeslimAlan.Text = _oturum.KullaniciAdi;
        LblBelgeNo.Text = $"Belge no: {_satir.AktarimBelgeNo()}";
    }

    private async void Aktar_Clicked(object sender, EventArgs e)
    {
        if (_satir is null)
            return;

        if (!double.TryParse(TxtMiktar.Text?.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var miktar) || miktar <= 0)
        {
            await DisplayAlert("Uyarı", "Geçerli bir miktar girin.", "Tamam");
            return;
        }

        var depo = TxtDepo.Text?.Trim() ?? "";
        var kategori = TxtKategori.Text?.Trim() ?? "";
        var teslimAlan = TxtTeslimAlan.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(depo))
        {
            await DisplayAlert("Uyarı", "Depo / saha girin.", "Tamam");
            return;
        }

        if (string.IsNullOrWhiteSpace(teslimAlan))
        {
            await DisplayAlert("Uyarı", "Teslim alan kişiyi girin.", "Tamam");
            return;
        }

        try
        {
            BtnAktar.IsEnabled = false;
            await _oturum.Satinalma.StogaAktarAsync(_satir, miktar, kategori, depo, teslimAlan);
            await DisplayAlert("Aktarıldı",
                $"{_satir.Malzeme} stoğa kaydedildi.\n{miktar:N2} {_satir.Birim} → {depo}",
                "Tamam");
            await ShellGuvenli.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
        finally
        {
            BtnAktar.IsEnabled = true;
        }
    }

    private async void Iptal_Clicked(object sender, EventArgs e) =>
        await ShellGuvenli.GoToAsync("..");
}
