using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView
{
    private bool _menuModunda = true;

    private static readonly Dictionary<string, (string Renk, string Alt)> SekmeKartlari = new(StringComparer.Ordinal)
    {
        ["Taleplerim"] = ("#2980B9", "Talepleriniz"),
        ["Gelen Talepler"] = ("#E67E22", "Yönetim onayı"),
        ["Onay Bekleyen"] = ("#E67E22", "İşlemde"),
        ["Teklif Bekleyen"] = ("#8E44AD", "Teklif istendi"),
        ["Teklif Girişi"] = ("#2980B9", "Teklif girin"),
        ["Karşılaştırma"] = ("#8E44AD", "Yönetime gönder"),
        ["Teklif Onay"] = ("#8E44AD", "Onay bekliyor"),
        ["Onaylanan Talepler"] = ("#27AE60", "Onaylı"),
        ["Geçmiş Talepler"] = ("#1B3A5C", "Tamamlanan"),
        ["Geçmiş Teklifli Onaylar"] = ("#1B3A5C", "Teklifli geçmiş"),
        ["Red Talepler"] = ("#C0392B", "Reddedilen"),
        ["Alınan Malzemeler"] = ("#16A085", "Mal kabul"),
        ["Gelen Siparişler"] = ("#16A085", "Depoya giren")
    };

    private void MenuGoster()
    {
        _menuModunda = true;
        PanelSekmeMenu.Visibility = Visibility.Visible;
        PanelSekmeIcerik.Visibility = Visibility.Collapsed;
        BtnMenuyeDon.Visibility = Visibility.Collapsed;
        TxtAktifSekmeBaslik.Visibility = Visibility.Collapsed;
        SekmeMenusuYenile();
    }

    private void MenuyeDon_Click(object sender, RoutedEventArgs e) => MenuGoster();

    private void SekmeMenusuYenile()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(SekmeMenusuYenile);
            return;
        }

        SekmeMenuPanel.Children.Clear();

        foreach (var (ad, _) in _sekmePanelleri)
        {
            if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", ad))
                continue;

            var baslik = SekmeBasliklari.TryGetValue(ad, out var metin) ? metin : ad;
            var sayi = SatinalmaSekmeSayaclari.Say(ad);
            var (renk, alt) = SekmeKartlari.TryGetValue(ad, out var m) ? m : ("#F97316", "");
            SekmeMenuPanel.Children.Add(SekmeKartiOlustur(ad, baslik, sayi, renk, alt));
        }
    }

    private Border SekmeKartiOlustur(string sekmeAdi, string baslik, int sayi, string renkHex, string altMetin)
    {
        var renk = (Brush)new BrushConverter().ConvertFromString(renkHex)!;

        var sayiMetni = new TextBlock
        {
            Text = sayi.ToString(),
            FontSize = 28,
            FontWeight = FontWeights.Bold,
            Foreground = renk
        };

        var baslikMetni = new TextBlock
        {
            Text = baslik,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("InkBrush"),
            TextWrapping = TextWrapping.Wrap
        };

        var alt = new TextBlock
        {
            Text = altMetin,
            FontSize = 11,
            Foreground = (Brush)FindResource("InkMutedBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var cubuk = new Border
        {
            Height = 4,
            CornerRadius = new CornerRadius(2),
            Background = renk,
            Margin = new Thickness(0, 0, 0, 8)
        };

        var icerik = new StackPanel();
        icerik.Children.Add(cubuk);
        icerik.Children.Add(sayiMetni);
        icerik.Children.Add(baslikMetni);
        if (!string.IsNullOrWhiteSpace(altMetin))
            icerik.Children.Add(alt);

        var kart = new Border
        {
            Width = 200,
            MinHeight = 108,
            Margin = new Thickness(6),
            Padding = new Thickness(14, 12, 14, 12),
            Background = Brushes.White,
            BorderBrush = (Brush)FindResource("LineBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Cursor = Cursors.Hand,
            Tag = sekmeAdi,
            Child = icerik
        };

        kart.MouseEnter += (_, _) => kart.Background = new SolidColorBrush(Color.FromRgb(255, 247, 237));
        kart.MouseLeave += (_, _) => kart.Background = Brushes.White;
        kart.MouseLeftButtonUp += (_, e) =>
        {
            if (kart.Tag is string ad)
            {
                SekmeyeGec(ad);
                e.Handled = true;
            }
        };

        return kart;
    }

    private void IcerikGoster(string sekmeAdi)
    {
        _menuModunda = false;
        PanelSekmeMenu.Visibility = Visibility.Collapsed;
        PanelSekmeIcerik.Visibility = Visibility.Visible;
        BtnMenuyeDon.Visibility = Visibility.Visible;

        var baslik = SekmeBasliklari.TryGetValue(sekmeAdi, out var metin) ? metin : sekmeAdi;
        TxtAktifSekmeBaslik.Text = baslik;
        TxtAktifSekmeBaslik.Visibility = Visibility.Visible;
    }
}
