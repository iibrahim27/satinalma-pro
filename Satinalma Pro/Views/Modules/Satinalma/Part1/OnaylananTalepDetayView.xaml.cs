using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Views;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class OnaylananTalepDetayView : UserControl
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private SatinalmaTalep? _talep;
    private string? _listeRoute;

    public event Action? Geri;
    public event Action? Degisti;

    public OnaylananTalepDetayView() => InitializeComponent();

    public void Yukle(SatinalmaTalep talep, string? listeRoute = null)
    {
        _listeRoute = listeRoute;
        _talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;
        ArayuzuGuncelle();
    }

    private void ArayuzuGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        _talep = talep;
        var gecmisModu = SatinalmaPart1Menusu.OnayGecmisiRoute(_listeRoute ?? "");
        TxtBaslik.Text = gecmisModu
            ? $"Geçmiş Onay — {talep.TalepNo}"
            : $"Onaylanan Talep — {talep.TalepNo}";
        TxtOzet.Text = $"{talep.Tarih} · {talep.TalepEden} · {SatinalmaPart1DurumEtiketi.TeklifDurumu(talep)}";

        var onayTuru = talep.TeklifsizYonetimOnayi && !talep.HerhangiKalemOnayli ? "Teklifsiz" : "Teklifli";
        var ad = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd) ? "—" : talep.YonetimOnaylayanAd;
        var eposta = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanEposta) ? "—" : talep.YonetimOnaylayanEposta;
        var tarih = string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi) ? "—" : talep.YonetimOnayTarihi;
        TxtOnaylayan.Text = $"Onay türü: {onayTuru} · Onaylayan: {ad} · {eposta} · {tarih}";

        var teklifsizBekliyor = talep.TeklifsizFirmaFiyatBekliyor;
        TxtDurum.Text = gecmisModu
            ? $"Kayıt durumu: {SatinalmaPart1DurumEtiketi.TalepDurumu(talep)} — {SatinalmaPart1DurumEtiketi.TeklifDurumu(talep)}"
            : teklifsizBekliyor
                ? "Teklifsiz onay — sipariş öncesi firma ve birim fiyatı girilmelidir."
                : "Onaylandı — firmaya sipariş verilebilir.";

        var satinalmaModu = !gecmisModu
            && KullaniciRolleri.Normalize(OturumYoneticisi.AktifKullanici?.Rol)
                is KullaniciRolleri.Satinalma or KullaniciRolleri.Admin;

        BtnFirmaFiyat.Visibility = teklifsizBekliyor && satinalmaModu
            ? Visibility.Visible
            : Visibility.Collapsed;
        BtnSiparisVer.Visibility = satinalmaModu ? Visibility.Visible : Visibility.Collapsed;
        BtnSiparisVer.IsEnabled = !teklifsizBekliyor && satinalmaModu;

        KalemTablosu.ItemsSource = OnaylananKalemSatirlari(talep);
        TeklifGecmisiniGoster(talep);
    }

    private static List<OnaylananKalemDetaySatiri> OnaylananKalemSatirlari(SatinalmaTalep talep)
    {
        var depodan = SatinalmaDepo.OnaylananMalzemeleriOlustur()
            .Where(s => s.TalepId == talep.Id)
            .Select(s => new OnaylananKalemDetaySatiri
            {
                Malzeme = s.Malzeme,
                MiktarMetni = $"{s.SiparisMiktari.ToString("G", Tr)} {s.Birim}",
                Firma = string.IsNullOrWhiteSpace(s.Firma) ? "—" : s.Firma,
                BirimFiyat = s.BirimFiyati,
                Toplam = s.ToplamTutar
            })
            .ToList();

        if (depodan.Count > 0)
            return depodan;

        // Arşiv / edge durumlar: depo filtresi kaçırsa talepten doğrudan göster
        foreach (var teklif in talep.Teklifler ?? [])
            teklif.FiyatlariHesapla(talep.Kalemler);

        return (talep.Kalemler ?? [])
            .Where(k => !string.IsNullOrWhiteSpace(k.Malzeme))
            .OrderBy(k => k.SiraNo)
            .Select(k =>
            {
                var teklif = talep.KalemOnayTeklifi(k);
                var fiyat = teklif?.Fiyatlar?.FirstOrDefault(f => f.KalemId == k.Id);
                return new OnaylananKalemDetaySatiri
                {
                    Malzeme = k.Malzeme,
                    MiktarMetni = $"{k.Miktar.ToString("G", Tr)} {k.Birim}",
                    Firma = string.IsNullOrWhiteSpace(teklif?.FirmaAdi) ? "—" : teklif!.FirmaAdi,
                    BirimFiyat = fiyat?.TlBirimFiyat(teklif?.UsdKuru ?? 0, teklif?.EurKuru ?? 0) ?? 0,
                    Toplam = fiyat?.ToplamTutar ?? 0
                };
            })
            .ToList();
    }

    private void TeklifGecmisiniGoster(SatinalmaTalep talep)
    {
        foreach (var teklif in talep.Teklifler ?? [])
            teklif.FiyatlariHesapla(talep.Kalemler);

        var satirlar = (talep.Teklifler ?? [])
            .OrderByDescending(t => t.Onaylandi)
            .ThenBy(t => t.FirmaAdi, StringComparer.OrdinalIgnoreCase)
            .Select(t => new OnaylananTeklifGecmisSatiri(t))
            .ToList();

        var goster = satirlar.Count > 0;
        TxtTeklifBaslik.Visibility = goster ? Visibility.Visible : Visibility.Collapsed;
        TeklifTablosu.Visibility = goster ? Visibility.Visible : Visibility.Collapsed;
        TeklifTablosu.ItemsSource = satirlar;
    }

    private SatinalmaTalep? GuncelTalep() =>
        _talep is null ? null : SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == _talep.Id) ?? _talep;

    private void FirmaFiyat_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        talep.Kalemler ??= [];
        var satirlar = talep.Kalemler
            .OrderBy(k => k.SiraNo)
            .Select(k => new TeklifsizFirmaFiyatSatiri(k))
            .ToList();

        var pencere = new TeklifsizFirmaFiyatWindow(talep, satirlar)
        {
            Owner = Window.GetWindow(this)
        };

        if (pencere.ShowDialog() != true)
            return;

        SatinalmaDepo.TeklifsizFirmaFiyatKaydet(talep, satirlar);
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);
        ArayuzuGuncelle();
        Degisti?.Invoke();
    }

    private void KarsilastirmaPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        if ((talep.Teklifler?.Count ?? 0) == 0)
        {
            MessageBox.Show("Bu talep teklifsiz onaylı — karşılaştırma PDF yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaPdfOlusturucu.KarsilastirmaYazdir(talep, SatinalmaDepo.Ayarlar, yonetimFormu: true);
    }

    private void OnayPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        SatinalmaPdfOlusturucu.YonetimOnayBelgesiYazdir(talep, SatinalmaDepo.Ayarlar);
    }

    private async void SiparisVer_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        await SatinalmaPart1Servisi.SiparisVerAsync(talep);
        Degisti?.Invoke();
        Geri?.Invoke();
    }

    private void Geri_Click(object sender, RoutedEventArgs e) => Geri?.Invoke();
}
