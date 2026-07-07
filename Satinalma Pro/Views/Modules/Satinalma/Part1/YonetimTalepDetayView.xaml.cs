using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public enum YonetimTalepDetayModu
{
    DirekOnaylanan,
    Reddedildi,
    Gecmis
}

public partial class YonetimTalepDetayView : UserControl
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private SatinalmaTalep? _talep;
    private YonetimTalepDetayModu _mod;

    public event Action? Geri;

    public YonetimTalepDetayView() => InitializeComponent();

    public void Yukle(SatinalmaTalep talep, YonetimTalepDetayModu mod)
    {
        _talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;
        _mod = mod;
        ArayuzuGuncelle();
    }

    private void ArayuzuGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        _talep = talep;
        TxtBaslik.Text = _mod switch
        {
            YonetimTalepDetayModu.DirekOnaylanan => $"Direk Onaylanan — {talep.TalepNo}",
            YonetimTalepDetayModu.Reddedildi => $"Red Verilen — {talep.TalepNo}",
            _ => $"Geçmiş — {talep.TalepNo}"
        };

        TxtOzet.Text = $"{talep.Tarih} · {talep.TalepEden} · {SatinalmaPart1DurumEtiketi.TeklifDurumu(talep)}";

        var ad = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd) ? "—" : talep.YonetimOnaylayanAd;
        var eposta = string.IsNullOrWhiteSpace(talep.YonetimOnaylayanEposta) ? "—" : talep.YonetimOnaylayanEposta;
        var tarih = string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi) ? "—" : talep.YonetimOnayTarihi;
        TxtOnaylayan.Text = _mod == YonetimTalepDetayModu.Reddedildi
            ? ""
            : $"Onaylayan: {ad} · {eposta} · {tarih}";
        TxtOnaylayan.Visibility = _mod == YonetimTalepDetayModu.Reddedildi
            ? Visibility.Collapsed
            : Visibility.Visible;

        TxtDurum.Text = _mod switch
        {
            YonetimTalepDetayModu.Reddedildi => string.IsNullOrWhiteSpace(talep.RedGerekcesi)
                ? "Talep reddedildi."
                : $"Red gerekçesi: {talep.RedGerekcesi}",
            YonetimTalepDetayModu.DirekOnaylanan => "Teklif süreci olmadan onaylanmış talep.",
            _ => talep.TeklifsizYonetimOnayi
                ? "Geçmiş teklifsiz onay kaydı."
                : "Geçmiş teklifli onay kaydı."
        };

        var teklifli = !talep.TeklifsizYonetimOnayi && (talep.Teklifler?.Count ?? 0) > 0;
        BtnKarsilastirmaPdf.Visibility = teklifli && _mod != YonetimTalepDetayModu.Reddedildi
            ? Visibility.Visible
            : Visibility.Collapsed;
        BtnOnayPdf.Visibility = _mod != YonetimTalepDetayModu.Reddedildi
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_mod == YonetimTalepDetayModu.Reddedildi)
        {
            KalemTablosu.ItemsSource = talep.Kalemler
                .OrderBy(k => k.SiraNo)
                .Select(k => new OnaylananKalemDetaySatiri
                {
                    Malzeme = k.Malzeme,
                    MiktarMetni = $"{k.Miktar.ToString("G", Tr)} {k.Birim}",
                    Firma = "—",
                    BirimFiyat = 0,
                    Toplam = 0
                })
                .ToList();
            return;
        }

        KalemTablosu.ItemsSource = SatinalmaDepo.OnaylananMalzemeleriOlustur()
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
    }

    private SatinalmaTalep? GuncelTalep() =>
        _talep is null ? null : SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == _talep.Id) ?? _talep;

    private void KarsilastirmaPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        SatinalmaPdfOlusturucu.KarsilastirmaYazdir(talep, SatinalmaDepo.Ayarlar, yonetimFormu: true);
    }

    private void OnayPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        SatinalmaPdfOlusturucu.YonetimOnayBelgesiYazdir(talep, SatinalmaDepo.Ayarlar);
    }

    private void Geri_Click(object sender, RoutedEventArgs e) => Geri?.Invoke();
}
