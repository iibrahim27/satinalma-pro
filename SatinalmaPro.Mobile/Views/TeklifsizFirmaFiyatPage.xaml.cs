using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class TeklifsizFirmaFiyatPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private List<SatinalmaTalep> _talepler = [];
    private SatinalmaTalep? _secili;
    private readonly List<(SatinalmaTalepKalemi Kalem, Entry Firma, Entry Fiyat)> _satirlar = [];

    public TeklifsizFirmaFiyatPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await YukleAsync();
    }

    private async Task YukleAsync()
    {
        await _oturum.VerileriYenileAsync();
        _talepler = _oturum.Satinalma.TeklifsizFirmaFiyatBekleyenleri().ToList();
        PickerTalep.ItemsSource = _talepler.Select(TalepEtiketi).ToList();
        if (_talepler.Count == 0)
            FormPanel.IsVisible = false;
    }

    private void PickerTalep_Changed(object sender, EventArgs e)
    {
        var idx = PickerTalep.SelectedIndex;
        if (idx < 0 || idx >= _talepler.Count)
        {
            FormPanel.IsVisible = false;
            return;
        }

        _secili = _talepler[idx];
        KalemPanel.Clear();
        _satirlar.Clear();

        foreach (var kalem in _secili.Kalemler.OrderBy(k => k.SiraNo))
        {
            KalemPanel.Add(new Label
            {
                Text = $"{kalem.Malzeme} — {kalem.Miktar:N2} {kalem.Birim}",
                FontAttributes = FontAttributes.Bold,
                TextColor = TemaKaynaklari.BirincilMetin
            });
            var firma = MobilGirisYardimcisi.GirisOlustur("Firma adı");
            var fiyat = MobilGirisYardimcisi.GirisOlustur("Birim fiyat", Keyboard.Numeric);
            KalemPanel.Add(MobilGirisYardimcisi.Cercevele(firma));
            KalemPanel.Add(MobilGirisYardimcisi.Cercevele(fiyat));
            _satirlar.Add((kalem, firma, fiyat));
        }

        FormPanel.IsVisible = true;
    }

    private static string TalepEtiketi(SatinalmaTalep t)
    {
        var tur = t.TalepTuru == TalepTurleri.Acil ? "ACİL" : "Teklifsiz";
        return $"{t.TalepNo} · {tur} · {t.TalepEden}";
    }

    private async void Kaydet_Clicked(object sender, EventArgs e)
    {
        if (_secili is null)
            return;

        var girdiler = new List<TeklifsizFirmaFiyatGirdisi>();
        foreach (var (kalem, firma, fiyat) in _satirlar)
        {
            if (!decimal.TryParse(fiyat.Text?.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var bf) || bf <= 0)
            {
                await DisplayAlert("Uyarı", $"'{kalem.Malzeme}' için geçerli birim fiyat girin.", "Tamam");
                return;
            }

            if (string.IsNullOrWhiteSpace(firma.Text))
            {
                await DisplayAlert("Uyarı", $"'{kalem.Malzeme}' için firma adı girin.", "Tamam");
                return;
            }

            girdiler.Add(new TeklifsizFirmaFiyatGirdisi
            {
                KalemId = kalem.Id,
                FirmaAdi = firma.Text.Trim(),
                BirimFiyat = bf
            });
        }

        try
        {
            await _oturum.Satinalma.TeklifsizFirmaFiyatKaydetAsync(_secili, girdiler);
            await DisplayAlert("Kaydedildi", "Firma ve fiyatlar kaydedildi.", "Tamam");
            await YukleAsync();
            FormPanel.IsVisible = false;
            PickerTalep.SelectedIndex = -1;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }
}
