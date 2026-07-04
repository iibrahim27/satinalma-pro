using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class StokGirisPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private StokIslemSatirYonetici _satirlar = null!;
    private string _tarih = "";
    private string _belgeNo = "";

    public StokGirisPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
        _satirlar = new StokIslemSatirYonetici(
            SatirlarPanel,
            arama => _oturum.Depo.MalzemeAdiOneriAra(arama));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await MobilSayfaKorumasi.StokGirisErisimAsync(this, _oturum))
            return;

        _tarih = DateTime.Now.ToString("dd.MM.yyyy");
        _belgeNo = _oturum.Depo.YeniBelgeNo("GR");
        LblTarih.Text = $"Tarih: {_tarih}";
        LblBelgeNo.Text = $"Belge: {_belgeNo}";

        if (_satirlar.SatirSayisi == 0)
            _satirlar.SatirEkle();
    }

    private void SatirEkle_Clicked(object sender, EventArgs e) => _satirlar.SatirEkle();

    private async void Kaydet_Clicked(object sender, EventArgs e)
    {
        var teslimEdilen = TxtTeslimEdilen.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(teslimEdilen))
        {
            await DisplayAlert("Uyarı", "Teslim edilen adını girin.", "Tamam");
            return;
        }

        var satirlar = _satirlar.GecerliSatirlar()
            .Select(s => OlusturSatirKaydi(s.Malzeme, s.Miktar))
            .ToList();

        if (satirlar.Count == 0)
        {
            await DisplayAlert("Uyarı", "En az bir malzeme ve miktar girin.", "Tamam");
            return;
        }

        try
        {
            await _oturum.Stok.GirisYapAsync(
                _tarih, satirlar, _belgeNo, _oturum.KullaniciAdi, teslimEdilen);

            await DisplayAlert("Kaydedildi", $"{satirlar.Count} kalem stok girişi tamamlandı.", "Tamam");

            _satirlar.Temizle();
            TxtTeslimEdilen.Text = "";
            _belgeNo = _oturum.Depo.YeniBelgeNo("GR");
            LblBelgeNo.Text = $"Belge: {_belgeNo}";
            _satirlar.SatirEkle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private StokIslemSatirKaydi OlusturSatirKaydi(string malzeme, double miktar)
    {
        var mevcut = _oturum.Depo.Stok.FirstOrDefault(s =>
            s.MalzemeAdi.Equals(malzeme, StringComparison.CurrentCultureIgnoreCase));

        return new StokIslemSatirKaydi
        {
            Malzeme = malzeme,
            Miktar = miktar,
            Birim = string.IsNullOrWhiteSpace(mevcut?.Birim) ? "Adet" : mevcut!.Birim,
            Kategori = mevcut?.Kategori ?? "",
            DepoSaha = string.IsNullOrWhiteSpace(mevcut?.DepoSaha) ? "Depo" : mevcut!.DepoSaha,
            BirimFiyat = mevcut?.BirimMaliyet ?? 0
        };
    }
}
