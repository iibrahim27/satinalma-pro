using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Mobile.Views.Controls;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

[QueryProperty(nameof(TalepId), "id")]
public partial class YeniTalepPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private SatinalmaTalep _talep = null!;
    private readonly List<KalemSatir> _satirlar = [];
    private string _talepId = "";

    public string TalepId
    {
        get => _talepId;
        set => _talepId = value ?? "";
    }

    public YeniTalepPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _oturum.VerileriYenileAsync();
        PickerTur.ItemsSource = TalepTurleri.Tum.Select(TalepTurleri.TurkceAd).ToList();

        var konum = Shell.Current?.CurrentState?.Location?.OriginalString ?? "";
        var duzenlemeModu = konum.Contains("talep-duzenle", StringComparison.OrdinalIgnoreCase)
                            && Guid.TryParse(_talepId, out _);

        if (!duzenlemeModu && !KullaniciRolleri.TalepOlusturabilir(_oturum.Rol))
        {
            await DisplayAlert("Yetki", "Talep oluşturma yetkiniz yok.", "Tamam");
            await Shell.Current.GoToAsync("//main");
            return;
        }

        if (!duzenlemeModu)
        {
            _talepId = "";
            YeniFormuHazirla();
            return;
        }

        if (!Guid.TryParse(_talepId, out var id))
        {
            YeniFormuHazirla();
            return;
        }

        var mevcut = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        if (mevcut is null)
        {
            await DisplayAlert("Hata", "Talep bulunamadı.", "Tamam");
            await Shell.Current.GoToAsync("..");
            return;
        }

        var uid = _oturum.Depo.AktifKullanici?.Uid;
        if (!MobilYetkiServisi.TalepDuzenleyebilir(_oturum.Rol, mevcut, uid, _oturum.KullaniciAdi))
        {
            await DisplayAlert("Yetki", "Bu talep düzenlenemez.", "Tamam");
            await Shell.Current.GoToAsync("..");
            return;
        }

        _talep = mevcut;
        Title = "Talep Düzenle";
        TxtAciklama.Text = _talep.TalepAciklamasi;
        PickerTur.SelectedItem = TalepTurleri.TurkceAd(_talep.TalepTuru);
        KalemlerPanel.Clear();
        _satirlar.Clear();
        foreach (var kalem in _talep.Kalemler.OrderBy(k => k.SiraNo))
            KalemEkle(kalem.Malzeme, kalem.Miktar, kalem.Birim);

        BtnGonder.IsVisible = SatinalmaTalepYardimcisi.FormDuzenlenebilir(_talep);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        var konum = Shell.Current?.CurrentState?.Location?.OriginalString ?? "";
        if (konum.Contains("talep-duzenle", StringComparison.OrdinalIgnoreCase))
            _talepId = "";
    }

    private void YeniFormuHazirla()
    {
        KalemlerPanel.Clear();
        _satirlar.Clear();
        TxtAciklama.Text = "";
        _talep = _oturum.Satinalma.YeniTalepOlustur();
        Title = "Yeni Talep";
        PickerTur.SelectedIndex = 1;
        KalemEkle();
        BtnGonder.IsVisible = true;
    }

    private void KalemEkle_Clicked(object sender, EventArgs e) => KalemEkle();

    private void KalemEkle(string malzeme = "", double miktar = 0, string birim = "Adet")
    {
        var satir = new KalemSatir();
        _satirlar.Add(satir);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(80),
                new ColumnDefinition(80)
            },
            ColumnSpacing = 8
        };

        var malzemeGiris = new MalzemeOneriGirisView { Text = malzeme };
        malzemeGiris.OneriKaynaginiAyarla(arama => _oturum.Depo.MalzemeAdiOneriAra(arama));

        var miktarEntry = new Entry
        {
            Placeholder = "Miktar",
            Keyboard = Keyboard.Numeric,
            Text = miktar > 0 ? miktar.ToString(System.Globalization.CultureInfo.InvariantCulture) : ""
        };
        var birimEntry = new Entry { Placeholder = "Birim", Text = birim };

        satir.Malzeme = malzemeGiris;
        satir.Miktar = miktarEntry;
        satir.Birim = birimEntry;

        grid.Add(malzemeGiris, 0);
        grid.Add(miktarEntry, 1);
        grid.Add(birimEntry, 2);
        KalemlerPanel.Add(grid);
    }

    private void KalemleriTopla()
    {
        _talep.TalepAciklamasi = TxtAciklama.Text ?? "";
        var secilenTur = PickerTur.SelectedItem?.ToString() ?? "";
        _talep.TalepTuru = secilenTur.StartsWith("Acil", StringComparison.OrdinalIgnoreCase)
            ? TalepTurleri.Acil
            : TalepTurleri.Normal;
        _talep.Kalemler.Clear();

        var sira = 1;
        foreach (var satir in _satirlar)
        {
            if (string.IsNullOrWhiteSpace(satir.Malzeme.Text))
                continue;

            _talep.Kalemler.Add(new SatinalmaTalepKalemi
            {
                SiraNo = sira++,
                Malzeme = satir.Malzeme.Text.Trim(),
                Miktar = double.TryParse(satir.Miktar.Text?.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var m) ? m : 0,
                Birim = string.IsNullOrWhiteSpace(satir.Birim.Text) ? "Adet" : satir.Birim.Text.Trim()
            });
        }
    }

    private async void Kaydet_Clicked(object sender, EventArgs e)
    {
        KalemleriTopla();
        try
        {
            await _oturum.Satinalma.TalepKaydetAsync(_talep);
            await DisplayAlert("Kaydedildi", "Talep kaydedildi.", "Tamam");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async void Gonder_Clicked(object sender, EventArgs e)
    {
        KalemleriTopla();
        if (_talep.Kalemler.Count == 0)
        {
            await DisplayAlert("Uyarı", "En az bir kalem girin.", "Tamam");
            return;
        }

        try
        {
            await _oturum.Satinalma.TalepKaydetAsync(_talep);
            await _oturum.Satinalma.YonetimeGonderAsync(_talep);
            await DisplayAlert("Gönderildi", "Talep imza sürecine gönderildi.", "Tamam");
            await Shell.Current.GoToAsync("//taleplerim");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private sealed class KalemSatir
    {
        public MalzemeOneriGirisView Malzeme { get; set; } = null!;
        public Entry Miktar { get; set; } = null!;
        public Entry Birim { get; set; } = null!;
    }
}
