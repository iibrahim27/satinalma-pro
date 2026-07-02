using System.Globalization;
using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Mobile.Views;

public partial class TeklifGirisPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private List<SatinalmaTalep> _talepler = [];
    private SatinalmaTalep? _secili;
    private SatinalmaTeklif _teklif = new();
    private Guid? _duzenlenenTeklifId;
    private Guid? _sonSeciliTalepId;
    private bool _pickerGuncelleniyor;
    private readonly List<KalemFiyatSatir> _fiyatSatirlari = [];

    public TeklifGirisPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
        MobilGirisYardimcisi.AndroidGirisHazirla(TxtFirma);
        MobilGirisYardimcisi.AndroidGirisHazirla(TxtMarka);
        MobilGirisYardimcisi.AndroidGirisHazirla(TxtVade);
        MobilGirisYardimcisi.AndroidGirisHazirla(TxtTeslim);
        MobilGirisYardimcisi.AndroidGirisHazirla(TxtUsdKuru);
        MobilGirisYardimcisi.AndroidGirisHazirla(TxtEurKuru);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await MobilSayfaKorumasi.RotaErisimAsync(this, _oturum, "teklif-gir"))
            return;
        await SayfayiYenileAsync();
    }

    private async Task SayfayiYenileAsync()
    {
        var korunanTalepId = _secili?.Id;
        var duzenlenenId = _duzenlenenTeklifId;

        await _oturum.VerileriYenileAsync();
        BaglantiBanneriniGuncelle();
        _talepler = _oturum.Satinalma.TeklifGirisiBekleyenleri().ToList();

        _pickerGuncelleniyor = true;
        PickerTalep.ItemsSource = _talepler.Select(t => $"{t.TalepNo} — {t.TalepEden}").ToList();
        _pickerGuncelleniyor = false;

        if (korunanTalepId is not null)
        {
            var idx = _talepler.FindIndex(t => t.Id == korunanTalepId);
            if (idx >= 0)
            {
                _pickerGuncelleniyor = true;
                PickerTalep.SelectedIndex = idx;
                _secili = _talepler[idx];
                _sonSeciliTalepId = _secili.Id;
                _pickerGuncelleniyor = false;

                MevcutTeklifleriGoster();
                if (duzenlenenId is not null)
                    TeklifDuzenlemeyeAl(duzenlenenId.Value);
                else if (FormPanel.IsVisible)
                    FormuDoldur(duzenleme: false);
                return;
            }
        }
    }

    private void PickerTalep_Changed(object sender, EventArgs e)
    {
        if (_pickerGuncelleniyor)
            return;

        var idx = PickerTalep.SelectedIndex;
        if (idx < 0 || idx >= _talepler.Count)
        {
            _sonSeciliTalepId = null;
            FormPanel.IsVisible = false;
            MevcutTekliflerPanel.IsVisible = false;
            OneriPanel.IsVisible = false;
            return;
        }

        var yeni = _talepler[idx];
        if (_sonSeciliTalepId == yeni.Id)
            return;

        _sonSeciliTalepId = yeni.Id;
        _secili = yeni;
        DuzenleIptal();
        MevcutTeklifleriGoster();
        YeniTeklifFormunuHazirla();
    }

    private void BaglantiBanneriniGuncelle()
    {
        OfflineBanner.IsVisible = _oturum.Depo.OfflineMod;
        if (_oturum.Depo.OfflineMod)
        {
            var son = _oturum.Depo.SonSenkronZamani?.ToString("dd.MM.yyyy HH:mm") ?? "—";
            LblOffline.Text = $"Bağlantı yok — önbellekten gösteriliyor (son senkron: {son})";
        }
    }

    private void MevcutTeklifleriGoster()
    {
        TeklifListesi.Clear();
        if (_secili is null || _secili.Teklifler.Count == 0)
        {
            MevcutTekliflerPanel.IsVisible = false;
            OneriPanel.IsVisible = false;
            return;
        }

        MevcutTekliflerPanel.IsVisible = true;
        _secili.SatinalmaOnerisiMigrasyonu();
        var onerilen = _secili.OnerilenTeklif();
        foreach (var teklif in _secili.Teklifler.OrderBy(t => t.FirmaAdi))
        {
            teklif.FiyatlariHesapla(_secili.Kalemler);
            var oneriMi = onerilen is not null && teklif.Id == onerilen.Id;
            var satir = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                },
                Padding = new Thickness(0, 4)
            };

            var firmaMetin = oneriMi
                ? $"{teklif.FirmaAdi} — {teklif.GenelToplam:N2} ₺ · Vade {teklif.VadeGunu} gün ★ Öneri"
                : $"{teklif.FirmaAdi} — {teklif.GenelToplam:N2} ₺ · Vade {teklif.VadeGunu} gün";
            satir.Add(new Label
            {
                Text = firmaMetin,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = oneriMi ? TemaKaynaklari.OneriCerceve : TemaKaynaklari.BirincilMetin,
                FontAttributes = oneriMi ? FontAttributes.Bold : FontAttributes.None
            }, 0);

            if (!oneriMi)
            {
                var oneriYap = new Button
                {
                    Text = "Öner",
                    BackgroundColor = Colors.Transparent,
                    TextColor = TemaKaynaklari.OneriCerceve,
                    FontSize = 13,
                    Padding = new Thickness(8, 0)
                };
                var teklifIdOneri = teklif.Id;
                oneriYap.Clicked += async (_, _) => await OneriSecAsync(teklifIdOneri);
                satir.Add(oneriYap, 1);
            }
            else
            {
                satir.Add(new BoxView { WidthRequest = 0 }, 1);
            }

            var duzenle = new Button
            {
                Text = "Düzenle",
                BackgroundColor = Colors.Transparent,
                TextColor = TemaKaynaklari.VurguMetin,
                FontSize = 13,
                Padding = new Thickness(8, 0)
            };
            var teklifId = teklif.Id;
            duzenle.Clicked += (_, _) => TeklifDuzenlemeyeAl(teklifId);
            satir.Add(duzenle, 2);

            var sil = new Button
            {
                Text = "Sil",
                BackgroundColor = Colors.Transparent,
                TextColor = TemaKaynaklari.Tehlike,
                FontSize = 13,
                Padding = 0
            };
            sil.Clicked += async (_, _) => await TeklifSilAsync(teklifId);
            satir.Add(sil, 3);
            TeklifListesi.Add(satir);
        }

        OneriPaneliniGuncelle();
    }

    private void OneriPaneliniGuncelle()
    {
        if (_secili is null || _secili.Teklifler.Count == 0)
        {
            OneriPanel.IsVisible = false;
            return;
        }

        OneriPanel.IsVisible = true;
        var onerilen = _secili.OnerilenTeklif();
        if (_secili.SatinalmaOnerisiElleSecildi && onerilen is not null)
        {
            LblOneriAciklama.Text =
                $"Elle seçildi: {onerilen.FirmaAdi} — {onerilen.GenelToplam:N2} ₺ (KDV dahil). " +
                "Otomatiğe dönerseniz sistem en uygun fiyatlı teklifi önerir.";
            BtnOneriOtomatik.IsVisible = true;
        }
        else if (onerilen is not null)
        {
            LblOneriAciklama.Text =
                $"Sistem önerisi (en uygun fiyat): {onerilen.FirmaAdi} — {onerilen.GenelToplam:N2} ₺ (KDV dahil). " +
                "Farklı bir teklif için listeden Öner'e basın.";
            BtnOneriOtomatik.IsVisible = false;
        }
        else
        {
            LblOneriAciklama.Text = "Öneri için en az bir teklif girin.";
            BtnOneriOtomatik.IsVisible = false;
        }
    }

    private async Task OneriSecAsync(Guid teklifId)
    {
        if (_secili is null)
            return;

        try
        {
            await _oturum.Satinalma.SatinalmaOnerisiSecAsync(_secili, teklifId);
            await TalepYenileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async void OneriOtomatik_Clicked(object sender, EventArgs e)
    {
        if (_secili is null)
            return;

        try
        {
            await _oturum.Satinalma.SatinalmaOnerisiOtomatigeAlAsync(_secili);
            await TalepYenileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private void TeklifDuzenlemeyeAl(Guid teklifId)
    {
        if (_secili is null)
            return;

        var kaynak = _secili.Teklifler.FirstOrDefault(t => t.Id == teklifId);
        if (kaynak is null)
            return;

        _duzenlenenTeklifId = teklifId;
        _teklif = new SatinalmaTeklif
        {
            Id = kaynak.Id,
            FirmaAdi = kaynak.FirmaAdi,
            Marka = kaynak.Marka,
            VadeGunu = kaynak.VadeGunu,
            TeslimSuresi = kaynak.TeslimSuresi,
            OdemeSekli = kaynak.OdemeSekli,
            KdvOrani = kaynak.KdvOrani,
            Aciklama = kaynak.Aciklama,
            UsdKuru = kaynak.UsdKuru,
            EurKuru = kaynak.EurKuru,
            Onaylandi = kaynak.Onaylandi,
            Fiyatlar = kaynak.Fiyatlar.Select(f => new SatinalmaTeklifFiyati
            {
                KalemId = f.KalemId,
                Marka = f.Marka,
                ParaBirimi = f.ParaBirimi,
                BirimFiyat = f.BirimFiyat,
                KdvOrani = f.KdvOrani
            }).ToList()
        };

        FormuDoldur(duzenleme: true);
    }

    private void YeniTeklifFormunuHazirla()
    {
        if (_secili is null)
        {
            FormPanel.IsVisible = false;
            return;
        }

        if (_duzenlenenTeklifId is not null)
            return;

        _teklif = new SatinalmaTeklif
        {
            UsdKuru = _oturum.Depo.Ayarlar.VarsayilanUsdKuru,
            EurKuru = _oturum.Depo.Ayarlar.VarsayilanEurKuru
        };
        _oturum.Satinalma.TeklifFiyatlariniHazirla(_secili, _teklif);
        FormuDoldur(duzenleme: false);
    }

    private void FormuDoldur(bool duzenleme)
    {
        if (_secili is null)
            return;

        LblFormBaslik.Text = duzenleme ? "Teklif düzenle" : "Yeni teklif";
        BtnKaydet.Text = duzenleme ? "Değişiklikleri Kaydet" : "Teklifi Kaydet";
        BtnDuzenleIptal.IsVisible = duzenleme;

        TxtFirma.Text = _teklif.FirmaAdi;
        TxtMarka.Text = _teklif.Marka;
        TxtVade.Text = _teklif.VadeGunu > 0 ? _teklif.VadeGunu.ToString(CultureInfo.InvariantCulture) : "";
        TxtTeslim.Text = _teklif.TeslimSuresi;
        TxtUsdKuru.Text = _teklif.UsdKuru > 0 ? _teklif.UsdKuru.ToString(CultureInfo.InvariantCulture) : "";
        TxtEurKuru.Text = _teklif.EurKuru > 0 ? _teklif.EurKuru.ToString(CultureInfo.InvariantCulture) : "";

        FiyatPanel.Clear();
        _fiyatSatirlari.Clear();
        foreach (var kalem in _secili.Kalemler.OrderBy(k => k.SiraNo))
        {
            var fiyat = _teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
            var satir = new KalemFiyatSatir(kalem, fiyat);
            FiyatPanel.Add(satir.Baslik);
            FiyatPanel.Add(satir.ParaPicker);
            FiyatPanel.Add(satir.FiyatKutusu);
            FiyatPanel.Add(satir.MarkaKutusu);
            FiyatPanel.Add(satir.KdvKutusu);
            _fiyatSatirlari.Add(satir);
        }

        FormPanel.IsVisible = true;
    }

    private void DuzenleIptal()
    {
        _duzenlenenTeklifId = null;
        BtnDuzenleIptal.IsVisible = false;
    }

    private void DuzenleIptal_Clicked(object sender, EventArgs e)
    {
        DuzenleIptal();
        YeniTeklifFormunuHazirla();
    }

    private async Task TeklifSilAsync(Guid teklifId)
    {
        if (_secili is null)
            return;

        var onay = await DisplayAlert("Teklif Sil", "Bu teklif silinsin mi?", "Sil", "İptal");
        if (!onay)
            return;

        try
        {
            await _oturum.Satinalma.TeklifSilAsync(_secili, teklifId);
            await TalepYenileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async Task TalepYenileAsync()
    {
        var seciliId = _secili?.Id;
        await SayfayiYenileAsync();

        if (seciliId is null)
            return;

        var idx = _talepler.FindIndex(t => t.Id == seciliId);
        if (idx < 0)
        {
            FormPanel.IsVisible = false;
            MevcutTekliflerPanel.IsVisible = false;
            OneriPanel.IsVisible = false;
            _pickerGuncelleniyor = true;
            PickerTalep.SelectedIndex = -1;
            _pickerGuncelleniyor = false;
            _sonSeciliTalepId = null;
            return;
        }

        DuzenleIptal();
        MevcutTeklifleriGoster();
        YeniTeklifFormunuHazirla();
    }

    private async void Kaydet_Clicked(object sender, EventArgs e)
    {
        if (_secili is null || string.IsNullOrWhiteSpace(TxtFirma.Text))
        {
            await DisplayAlert("Uyarı", "Talep ve firma adı zorunludur.", "Tamam");
            return;
        }

        _teklif.FirmaAdi = TxtFirma.Text.Trim();
        _teklif.Marka = TxtMarka.Text ?? "";
        _teklif.VadeGunu = int.TryParse(TxtVade.Text, out var v) ? v : 0;
        _teklif.TeslimSuresi = TxtTeslim.Text ?? "";
        _teklif.UsdKuru = ParseDecimal(TxtUsdKuru.Text);
        _teklif.EurKuru = ParseDecimal(TxtEurKuru.Text);

        foreach (var satir in _fiyatSatirlari)
        {
            var fiyat = _teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == satir.Kalem.Id);
            if (fiyat is null)
                continue;

            fiyat.BirimFiyat = ParseDecimal(satir.FiyatEntry.Text);
            fiyat.ParaBirimi = satir.ParaPicker.SelectedItem?.ToString() ?? ParaBirimleri.Try;
            fiyat.Marka = satir.MarkaEntry.Text ?? "";
            fiyat.KdvOrani = double.TryParse(satir.KdvEntry.Text?.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var kdv) ? kdv : _teklif.KdvOrani;
        }

        try
        {
            if (_duzenlenenTeklifId is not null)
            {
                await _oturum.Satinalma.TeklifGuncelleAsync(_secili, _teklif);
                await DisplayAlert("Güncellendi", "Teklif güncellendi.", "Tamam");
            }
            else
            {
                await _oturum.Satinalma.TeklifEkleAsync(_secili, _teklif);
                await DisplayAlert("Kaydedildi", "Teklif kaydedildi.", "Tamam");
            }

            await TalepYenileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private static decimal ParseDecimal(string? metin) =>
        decimal.TryParse(metin?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;

    private sealed class KalemFiyatSatir
    {
        public SatinalmaTalepKalemi Kalem { get; }
        public Label Baslik { get; }
        public Picker ParaPicker { get; }
        public Entry FiyatEntry { get; }
        public Entry MarkaEntry { get; }
        public Entry KdvEntry { get; }
        public View FiyatKutusu { get; }
        public View MarkaKutusu { get; }
        public View KdvKutusu { get; }

        public KalemFiyatSatir(SatinalmaTalepKalemi kalem, SatinalmaTeklifFiyati? fiyat)
        {
            Kalem = kalem;
            Baslik = new Label { Text = $"{kalem.Malzeme} ({kalem.Miktar:N2} {kalem.Birim})" };
            ParaPicker = new Picker
            {
                Title = "Para birimi",
                ItemsSource = ParaBirimleri.Tum.ToList(),
                SelectedItem = fiyat?.ParaBirimi ?? ParaBirimleri.Try
            };
            FiyatEntry = MobilGirisYardimcisi.GirisOlustur(
                "Birim fiyat",
                Keyboard.Numeric,
                fiyat?.BirimFiyat > 0 ? fiyat.BirimFiyat.ToString(CultureInfo.InvariantCulture) : "");
            MarkaEntry = MobilGirisYardimcisi.GirisOlustur(
                "Kalem markası",
                text: fiyat?.Marka ?? "");
            KdvEntry = MobilGirisYardimcisi.GirisOlustur(
                "KDV %",
                Keyboard.Numeric,
                (fiyat?.KdvOrani ?? 20).ToString(CultureInfo.InvariantCulture));
            FiyatKutusu = MobilGirisYardimcisi.Cercevele(FiyatEntry);
            MarkaKutusu = MobilGirisYardimcisi.Cercevele(MarkaEntry);
            KdvKutusu = MobilGirisYardimcisi.Cercevele(KdvEntry);
        }
    }
}
