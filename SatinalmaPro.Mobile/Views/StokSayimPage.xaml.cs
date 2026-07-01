using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;

namespace SatinalmaPro.Mobile.Views;

public partial class StokSayimPage : ContentPage
{
    private readonly OturumServisi _oturum;

    public StokSayimPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await MobilSayfaKorumasi.StokSayimErisimAsync(this, _oturum))
            return;

        await _oturum.VerileriYenileAsync();
        PickerMalzeme.ItemsSource = _oturum.Stok.MalzemeListesi().ToList();
        PickerDepo.ItemsSource = _oturum.Stok.DepoListesi().ToList();
        GuncelleMevcut();
    }

    private void Picker_Changed(object sender, EventArgs e) => GuncelleMevcut();

    private void GuncelleMevcut()
    {
        if (PickerMalzeme.SelectedIndex < 0 || PickerDepo.SelectedIndex < 0)
        {
            LblMevcut.Text = "";
            return;
        }

        var malzeme = PickerMalzeme.SelectedItem?.ToString() ?? "";
        var depo = PickerDepo.SelectedItem?.ToString() ?? "";
        var stok = _oturum.Stok.StokBul(malzeme, depo);
        LblMevcut.Text = stok is null
            ? "Stok kaydı bulunamadı."
            : $"Mevcut: {stok.MevcutMiktar:N2} {stok.Birim}";
    }

    private async void Kaydet_Clicked(object sender, EventArgs e)
    {
        if (PickerMalzeme.SelectedItem is not string malzeme ||
            PickerDepo.SelectedItem is not string depo ||
            !double.TryParse(TxtSayim.Text?.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var miktar))
        {
            await DisplayAlert("Uyarı", "Malzeme, depo ve sayım miktarı zorunludur.", "Tamam");
            return;
        }

        try
        {
            var islemYapan = _oturum.KullaniciAdi;
            await _oturum.Stok.SayimYapAsync(malzeme, depo, miktar, islemYapan);
            await DisplayAlert("Kaydedildi", "Stok sayımı kaydedildi.", "Tamam");
            GuncelleMevcut();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }
}
