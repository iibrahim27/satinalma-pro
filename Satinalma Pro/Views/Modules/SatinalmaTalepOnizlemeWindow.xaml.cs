using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaTalepOnizlemeWindow : Window
{
    private SatinalmaTalep _talep;

    public event Action<SatinalmaTalep>? DuzenleIstendi;
    public event Action<SatinalmaTalep>? OnaylaIstendi;
    public event Action<SatinalmaTalep>? TeklifIsteIstendi;

    public SatinalmaTalepOnizlemeWindow(SatinalmaTalep talep)
    {
        _talep = talep;
        InitializeComponent();
        Goster(talep);
    }

    public void Goster(SatinalmaTalep talep)
    {
        _talep = talep;
        Title = $"Talep Önizleme — {talep.TalepNo}";
        TxtTalepNo.Text = talep.TalepNo;

        var etiket = TalepListeSatiri.DurumEtiketiOlustur(talep);
        TxtDurum.Text = etiket;
        var (_, _, rozetArka, rozetYazi) = TalepDurumRenkleri.Fircalar(etiket);
        DurumRozeti.Background = rozetArka;
        TxtDurum.Foreground = rozetYazi;

        TxtTarih.Text = talep.Tarih;
        TxtTalepEden.Text = string.IsNullOrWhiteSpace(talep.TalepEden) ? "—" : talep.TalepEden;
        TxtKalemSayisi.Text = talep.KalemSayisiMetni;
        TxtAciklama.Text = string.IsNullOrWhiteSpace(talep.TalepAciklamasi) ? "—" : talep.TalepAciklamasi;
        KalemTablosu.ItemsSource = (talep.Kalemler ?? []).OrderBy(k => k.SiraNo).ToList();

        EkBilgiIcerik.Children.Clear();
        var ekSatirlar = EkSatirlariOlustur(talep).ToList();
        if (ekSatirlar.Count > 0)
        {
            foreach (var satir in ekSatirlar)
                EkBilgiIcerik.Children.Add(satir);
            EkBilgiPanel.Visibility = Visibility.Visible;
        }
        else
        {
            EkBilgiPanel.Visibility = Visibility.Collapsed;
        }

        var duzenlenebilir = KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(talep);
        BtnDuzenle.Visibility = duzenlenebilir ? Visibility.Visible : Visibility.Collapsed;

        YonetimButonlariniGuncelle(talep);
    }

    private void YonetimButonlariniGuncelle(SatinalmaTalep talep)
    {
        var yetki = KullaniciYetkileri.YonetimKararVerebilir();
        var gelenTalep = SatinalmaTabFiltreleri.GelenTalepler(talep);
        var teklifIstenebilir = SatinalmaPro.Shared.Helpers.SatinalmaIsAkisi.TeklifIstenebilir(
            talep.TalepTuru,
            talep.Durum,
            SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep));

        var goster = yetki && gelenTalep;
        BtnOnayla.Visibility = goster ? Visibility.Visible : Visibility.Collapsed;
        BtnOnayla.IsEnabled = goster;
        BtnTeklifIste.Visibility = goster && teklifIstenebilir ? Visibility.Visible : Visibility.Collapsed;
        BtnTeklifIste.IsEnabled = goster && teklifIstenebilir;
    }

    private static IEnumerable<TextBlock> EkSatirlariOlustur(SatinalmaTalep talep)
    {
        var teklifSayisi = talep.Teklifler?.Count ?? 0;
        if (teklifSayisi > 0)
            yield return MetinSatiri($"Teklif: {teklifSayisi} firma teklifi girildi");

        if (!string.IsNullOrWhiteSpace(talep.SiparisNo))
            yield return MetinSatiri($"Sipariş No: {talep.SiparisNo}");

        if (!string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd))
        {
            var onay = $"Onaylayan: {talep.YonetimOnaylayanAd}";
            if (!string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi))
                onay += $" · {talep.YonetimOnayTarihi}";
            yield return MetinSatiri(onay);
        }

        if (!string.IsNullOrWhiteSpace(talep.RedGerekcesi))
            yield return MetinSatiri($"Red gerekçesi: {talep.RedGerekcesi}", "#B91C1C");
    }

    private static TextBlock MetinSatiri(string metin, string? renk = null) =>
        new()
        {
            Text = metin,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 6),
            TextWrapping = TextWrapping.Wrap,
            Foreground = renk is null
                ? Brushes.Black
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString(renk)!)
        };

    private void Kapat_Click(object sender, RoutedEventArgs e) => Close();

    private void Duzenle_Click(object sender, RoutedEventArgs e)
    {
        DuzenleIstendi?.Invoke(_talep);
        Close();
    }

    private void Onayla_Click(object sender, RoutedEventArgs e)
    {
        OnaylaIstendi?.Invoke(_talep);
        Close();
    }

    private void TeklifIste_Click(object sender, RoutedEventArgs e)
    {
        TeklifIsteIstendi?.Invoke(_talep);
        Close();
    }

    private void Pdf_Click(object sender, RoutedEventArgs e) =>
        SatinalmaPdfOlusturucu.TalepFormuYazdir(_talep, SatinalmaDepo.Ayarlar);
}
