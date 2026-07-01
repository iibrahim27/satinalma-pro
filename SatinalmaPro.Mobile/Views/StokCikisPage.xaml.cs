using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class StokCikisPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private StokIslemSatirYonetici _satirlar = null!;
    private string _tarih = "";
    private string _belgeNo = "";

    public StokCikisPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
        _satirlar = new StokIslemSatirYonetici(
            SatirlarPanel,
            arama => _oturum.Stok.StokMalzemeAra(arama, sadeceMevcutStok: true));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await MobilSayfaKorumasi.StokCikisErisimAsync(this, _oturum))
            return;

        _tarih = DateTime.Now.ToString("dd.MM.yyyy");
        _belgeNo = _oturum.Depo.YeniBelgeNo("CK");
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
            await DisplayAlert("Uyarı", "Teslim edilen (alan) kişinin adını girin.", "Tamam");
            return;
        }

        var kayitlar = new List<StokIslemSatirKaydi>();
        foreach (var (malzeme, miktar) in _satirlar.GecerliSatirlar())
        {
            var stok = StokKaydiniBul(malzeme);
            if (stok is null)
            {
                await DisplayAlert("Uyarı", $"'{malzeme}' stokta bulunamadı.", "Tamam");
                return;
            }

            if (miktar > stok.MevcutMiktar)
            {
                await DisplayAlert("Uyarı",
                    $"'{malzeme}' için yetersiz stok. Mevcut: {stok.MevcutMiktar:N2} {stok.Birim}",
                    "Tamam");
                return;
            }

            kayitlar.Add(new StokIslemSatirKaydi
            {
                Malzeme = malzeme,
                Miktar = miktar,
                DepoSaha = stok.DepoSaha,
                Birim = stok.Birim,
                Kategori = stok.Kategori
            });
        }

        if (kayitlar.Count == 0)
        {
            await DisplayAlert("Uyarı", "En az bir malzeme ve miktar girin.", "Tamam");
            return;
        }

        try
        {
            await _oturum.Stok.CikisYapAsync(
                _tarih, kayitlar, _belgeNo, _oturum.KullaniciAdi, teslimEdilen);

            await DisplayAlert("Kaydedildi", $"{kayitlar.Count} kalem stok çıkışı tamamlandı.", "Tamam");

            _satirlar.Temizle();
            TxtTeslimEdilen.Text = "";
            _belgeNo = _oturum.Depo.YeniBelgeNo("CK");
            LblBelgeNo.Text = $"Belge: {_belgeNo}";
            _satirlar.SatirEkle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private StokKaydi? StokKaydiniBul(string malzeme) =>
        _oturum.Depo.Stok
            .Where(s => s.MalzemeAdi.Equals(malzeme.Trim(), StringComparison.CurrentCultureIgnoreCase)
                        && s.MevcutMiktar > 0)
            .OrderByDescending(s => s.MevcutMiktar)
            .FirstOrDefault();
}
