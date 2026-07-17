using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

public partial class OnaylananMalzemelerPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private List<OnaylananMalzemeSatiri> _liste = [];

    public OnaylananMalzemelerPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
        RefreshView.Command = new Command(async () =>
        {
            await Yukle();
            RefreshView.IsRefreshing = false;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await MobilSayfaKorumasi.AlinanMalzemeErisimAsync(this, _oturum))
            return;

        MalKabulYetkisiUygula();
        await Yukle();
    }

    private void MalKabulYetkisiUygula()
    {
        var yapabilir = MobilYetkiServisi.MalKabulVeStokAktarYapabilir(_oturum.Rol);
        Liste.IsEnabled = true;
        _malKabulYapabilir = yapabilir;
    }

    private bool _malKabulYapabilir;

    private async Task Yukle()
    {
        await _oturum.VerileriYenileAsync();
        _liste = _oturum.Satinalma.OnaylananMalzemeleriOlustur()
            .OrderByDescending(s => s.Tarih)
            .ThenBy(s => s.TalepNo)
            .ToList();
        ListeyiBagla();
    }

    private void ListeyiBagla()
    {
        var arama = TxtArama.Text?.Trim();
        var gorunenler = string.IsNullOrWhiteSpace(arama)
            ? _liste
            : _liste.Where(s => string.Join(" ", s.TalepNo, s.SiparisNo, s.Malzeme, s.Firma, s.KabulDurumu)
                .Contains(arama, StringComparison.OrdinalIgnoreCase)).ToList();

        var topluKabulBilgileri = _liste
            .Where(s => !s.SiparisTamamlandi && s.KalanMiktar > 0.0001)
            .GroupBy(s => (s.TalepId, s.TeklifId))
            .ToDictionary(
                g => g.Key,
                g => (IlkKalemId: g.First().KalemId, KalemSayisi: g.Count()));

        Liste.ItemsSource = gorunenler
            .Select(s =>
            {
                var topluKabulYapilabilir = topluKabulBilgileri.TryGetValue((s.TalepId, s.TeklifId), out var bilgi)
                    && bilgi.KalemSayisi > 1
                    && bilgi.IlkKalemId == s.KalemId;
                return new OnaylananMalzemeGorunum(
                    s,
                    _malKabulYapabilir,
                    topluKabulYapilabilir,
                    topluKabulYapilabilir ? bilgi.KalemSayisi : 0);
            })
            .ToList();
    }

    private void AramaDegisti(object sender, TextChangedEventArgs e) => ListeyiBagla();

    private async void MalKabul_Clicked(object sender, EventArgs e)
    {
        if (!_malKabulYapabilir)
        {
            await DisplayAlert("Yetki", "Mal kabul işlemi yalnızca Satınalma rolü tarafından yapılabilir.", "Tamam");
            return;
        }

        if (sender is not Button { CommandParameter: OnaylananMalzemeGorunum gorunum })
            return;

        var satir = gorunum.Satir;

        var varsayilan = satir.KalanMiktar > 0 ? satir.KalanMiktar : satir.SiparisMiktari;
        var miktarStr = await DisplayPromptAsync(
            "Mal Kabul",
            $"{satir.Malzeme}\nSipariş: {satir.SiparisMiktari:N2} {satir.Birim}\nKabul: {satir.KabulEdilenMiktar:N2} {satir.Birim}",
            "Kaydet", "İptal",
            keyboard: Keyboard.Numeric,
            initialValue: varsayilan.ToString("N2"));

        if (string.IsNullOrWhiteSpace(miktarStr) || !double.TryParse(miktarStr, out var miktar) || miktar <= 0)
            return;

        try
        {
            await _oturum.Satinalma.MalKabulAsync(satir.TalepId, satir.KalemId, miktar);
            await DisplayAlert("Kaydedildi", $"Mal kabul: {miktar:N2} {satir.Birim}", "Tamam");
            await Yukle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async void Tamamla_Clicked(object sender, EventArgs e)
    {
        if (!_malKabulYapabilir)
        {
            await DisplayAlert("Yetki", "Bu işlem yalnızca Satınalma rolü tarafından yapılabilir.", "Tamam");
            return;
        }

        if (sender is not Button { CommandParameter: OnaylananMalzemeGorunum gorunum })
            return;

        var satir = gorunum.Satir;

        var onay = await DisplayAlert(
            "Siparişi Tamamla",
            $"{satir.Malzeme} tamamlandı olarak işaretlenecek.\nKabul: {satir.KabulEdilenMiktar:N2} / {satir.SiparisMiktari:N2} {satir.Birim}",
            "Tamamla", "İptal");

        if (!onay)
            return;

        try
        {
            await _oturum.Satinalma.SiparisTamamlaAsync(satir.TalepId, satir.KalemId);
            await DisplayAlert("Tamamlandı", "Sipariş kalemi tamamlandı.", "Tamam");
            await Yukle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async void TopluMalKabul_Clicked(object sender, EventArgs e)
    {
        if (!_malKabulYapabilir)
        {
            await DisplayAlert("Yetki", "Mal kabul işlemi yalnızca Satınalma rolü tarafından yapılabilir.", "Tamam");
            return;
        }

        if (sender is not Button { CommandParameter: OnaylananMalzemeGorunum gorunum })
            return;

        var onay = await DisplayAlert(
            "Tüm Kalemleri Kabul Et",
            $"{gorunum.TopluKabulOzet} kalan {gorunum.TopluKabulKalemSayisi:N0} kalemin tamamı kabul edilecek. Devam edilsin mi?",
            "Kabul Et",
            "İptal");
        if (!onay)
            return;

        try
        {
            var kabulEdilenKalemSayisi = await _oturum.Satinalma.TumKalemleriMalKabulEtAsync(
                gorunum.Satir.TalepId,
                gorunum.Satir.TeklifId);
            await DisplayAlert("Kaydedildi", $"{kabulEdilenKalemSayisi:N0} kalemin mal kabulü tamamlandı.", "Tamam");
            await Yukle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async void StogaAktar_Clicked(object sender, EventArgs e)
    {
        if (!_malKabulYapabilir)
        {
            await DisplayAlert("Yetki", "Stoğa aktarım yalnızca Satınalma rolü tarafından yapılabilir.", "Tamam");
            return;
        }

        if (sender is not Button { CommandParameter: OnaylananMalzemeGorunum gorunum })
            return;

        var satir = gorunum.Satir;

        if (_oturum.Satinalma.StogaDahaOnceAktarildi(satir))
        {
            await DisplayAlert("Uyarı",
                $"{satir.Malzeme} daha önce stoğa aktarılmış.\nBelge: {satir.AktarimBelgeNo()}",
                "Tamam");
            return;
        }

        if (string.IsNullOrWhiteSpace(satir.SiparisNo))
        {
            var siparisAta = await DisplayAlert(
                "Sipariş No",
                "Bu talep için sipariş numarası yok. Otomatik oluşturulsun mu?",
                "Evet", "Hayır");
            if (siparisAta)
            {
                try
                {
                    await _oturum.Satinalma.SiparisNoAtaAsync(satir.TalepId);
                    await Yukle();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hata", ex.Message, "Tamam");
                    return;
                }
            }
        }

        try
        {
            await ShellGuvenli.GoToAsync(
                $"stok-aktar?talepId={satir.TalepId}&kalemId={satir.KalemId}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async void PdfPaylas_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: OnaylananMalzemeGorunum gorunum })
            return;

        var satir = gorunum.Satir;

        try
        {
            IsBusy = true;
            await MobilBelgePaylasServisi.PdfOlusturVePaylasAsync(
                () => MobilPdfOlusturucu.OnaylananMalzemePdf(satir),
                $"{satir.TalepNo}_{satir.Malzeme}_siparis.pdf",
                "Sipariş PDF");
        }
        catch (Exception ex)
        {
            await DisplayAlert("PDF Hatası", ex.Message, "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed class OnaylananMalzemeGorunum(
    OnaylananMalzemeSatiri satir,
    bool islemYapabilir,
    bool topluKabulYapilabilir,
    int topluKabulKalemSayisi)
{
    public OnaylananMalzemeSatiri Satir { get; } = satir;
    public bool IslemYapabilir { get; } = islemYapabilir;
    public bool TopluKabulYapabilir { get; } = islemYapabilir && topluKabulYapilabilir;
    public int TopluKabulKalemSayisi { get; } = topluKabulKalemSayisi;
    public string TopluKabulMetni => $"Kalan {TopluKabulKalemSayisi:N0} kalemi kabul et";
    public string TopluKabulOzet => string.IsNullOrWhiteSpace(Satir.Firma)
        ? $"{TalepNo} talebindeki"
        : $"{Satir.Firma} / {TalepNo} teklifindeki";
    public string Malzeme => Satir.Malzeme;
    public string Firma => Satir.Firma;
    public string TalepNo => Satir.TalepNo;
    public string SiparisNo => Satir.SiparisNo;
    public string VadeOzeti => Satir.VadeOzeti;
    public string KabulDurumu => Satir.KabulDurumu;
    public string MiktarOzeti => Satir.MiktarOzeti;
    public string FiyatOzeti => Satir.FiyatOzeti;
}
