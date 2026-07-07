using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Procurement.Detail;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class YonetimTeklifIncelemeWindow : Window
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private SatinalmaTalep? _talep;
    private bool _degisti;
    private bool _secimAktif;

    public YonetimTeklifIncelemeWindow()
    {
        InitializeComponent();
    }

    public static bool Goster(Window? owner, SatinalmaTalep talep)
    {
        var guncel = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;
        var pencere = new YonetimTeklifIncelemeWindow
        {
            Owner = owner ?? Application.Current.MainWindow
        };
        pencere.Yukle(guncel);
        pencere.ShowDialog();
        return pencere._degisti;
    }

    public static bool Goster(Window? owner, Guid talepId)
    {
        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talepId);
        if (talep is null)
        {
            MessageBox.Show("Talep bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        return Goster(owner, talep);
    }

    private void Yukle(SatinalmaTalep talep)
    {
        _talep = talep;
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        foreach (var teklif in talep.Teklifler)
            teklif.FiyatlariHesapla(talep.Kalemler);

        TxtBaslik.Text = $"Teklif İnceleme — {talep.TalepNo}";
        TxtOzet.Text =
            $"{talep.Tarih} · {talep.TalepEden} · {SatinalmaPart1DurumEtiketi.TeklifDurumu(talep)}";

        if (!string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu))
        {
            TxtDuzeltmeNotu.Text = $"Satınalma notu / düzeltme: {talep.TeklifDuzeltmeNotu}";
            DuzeltmeNotuPanel.Visibility = Visibility.Visible;
        }
        else
        {
            DuzeltmeNotuPanel.Visibility = Visibility.Collapsed;
        }

        var oneri = talep.SatinalmaKalemOnerisiElleSecildi && SatinalmaOneriYardimcisi.HerhangiKalemOnerili(talep)
            ? SatinalmaOneriYardimcisi.OneriMetni(talep)
            : talep.OnerilenTeklif() is { } oneriTeklif
                ? talep.SatinalmaOnerisiElleSecildi
                    ? $"Satınalma önerisi: {oneriTeklif.FirmaAdi} — KDV Hariç: {oneriTeklif.AraToplam.ToString("N2", Tr)} ₺ | KDV Dahil: {oneriTeklif.GenelToplam.ToString("N2", Tr)} ₺ (elle seçildi)"
                    : $"Satınalma önerisi: {oneriTeklif.FirmaAdi} — KDV Hariç: {oneriTeklif.AraToplam.ToString("N2", Tr)} ₺ | KDV Dahil: {oneriTeklif.GenelToplam.ToString("N2", Tr)} ₺ (en uygun fiyat)"
                : null;

        if (!string.IsNullOrWhiteSpace(oneri))
        {
            TxtOneriBanner.Text = oneri;
            OneriBanner.Visibility = Visibility.Visible;
        }
        else
        {
            OneriBanner.Visibility = Visibility.Collapsed;
        }

        KararButonlariniGuncelle();
        TeklifAksiyonPaneliniYukle();
        KarsilastirmaTablosunuYukle();
        SecimOzetiniGuncelle();
    }

    private void TeklifAksiyonPaneliniYukle()
    {
        TeklifAksiyonPanel.Children.Clear();
        var talep = GuncelTalep();
        if (talep is null)
            return;

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var ui = PurchaseRequestDetailServisi.UiDurumu(
            talep, rol, PurchaseRequestDetailScreen.ManagementQuoteReview);

        if (!ui.ShowQuotesList)
        {
            TeklifAksiyonPanel.Visibility = Visibility.Collapsed;
            return;
        }

        TeklifAksiyonPanel.Visibility = Visibility.Visible;
        TeklifAksiyonPanel.Children.Add(new TextBlock
        {
            Text = "Teklifler",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var satir in PurchaseRequestDetailServisi.TeklifSatirlari(talep, rol))
        {
            var satirPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 6)
            };

            satirPanel.Children.Add(new TextBlock
            {
                Text = satir.FirmName,
                Width = 280,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            });

            if (satir.CanApprove)
            {
                var btn = new Button
                {
                    Content = ui.LabelFor(PurchaseRequestDetailAction.ApproveQuote),
                    Style = (Style)FindResource("PrimaryToolbarButtonStyle"),
                    Padding = new Thickness(14, 6, 14, 6),
                    Tag = satir.QuoteId
                };
                btn.Click += FirmaOnayla_Click;
                satirPanel.Children.Add(btn);
            }

            TeklifAksiyonPanel.Children.Add(satirPanel);
        }
    }

    private async void FirmaOnayla_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string quoteId)
            return;

        var talep = GuncelTalep();
        if (talep is null)
            return;

        var firma = talep.Teklifler?.FirstOrDefault(t => t.Id.ToString() == quoteId)?.FirmaAdi ?? "Firma";
        var onay = MessageBox.Show(
            $"{firma} teklifi onaylansın mı?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            var rol = OturumYoneticisi.AktifKullanici?.Rol;
            await PurchaseRequestDetailServisi.UygulaAsync(
                talep,
                PurchaseRequestDetailAction.ApproveQuote,
                rol,
                quoteId: quoteId);

            _degisti = true;
            MessageBox.Show($"{talep.TalepNo} onaylandı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void KararButonlariniGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var ui = PurchaseRequestDetailServisi.UiDurumu(
            talep, rol, PurchaseRequestDetailScreen.ManagementQuoteReview);

        var onaylanabilir = SatinalmaPart1OnayYardimcisi.TeklifOnaylanabilir(talep);
        _secimAktif = onaylanabilir && ui.ShowPerQuoteApproveButtons;

        BtnOnayla.Visibility = onaylanabilir && ui.ShowPerQuoteApproveButtons
            ? Visibility.Visible
            : Visibility.Collapsed;

        BtnReddet.Visibility = ui.IsActionVisible(PurchaseRequestDetailAction.RejectEntireRequest)
            ? Visibility.Visible
            : Visibility.Collapsed;
        BtnReddet.Content = ui.LabelFor(PurchaseRequestDetailAction.RejectEntireRequest);

        BtnGeriGonder.Visibility = ui.IsActionVisible(PurchaseRequestDetailAction.SendQuotesForRevision)
            ? Visibility.Visible
            : Visibility.Collapsed;
        BtnGeriGonder.Content = ui.LabelFor(PurchaseRequestDetailAction.SendQuotesForRevision);

        BtnOneriyiUygula.Visibility = onaylanabilir && talep.OnerilenTeklif() is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void KarsilastirmaTablosunuYukle()
    {
        var talep = GuncelTalep();
        KarsilastirmaGrid.Children.Clear();
        KarsilastirmaGrid.RowDefinitions.Clear();
        KarsilastirmaGrid.ColumnDefinitions.Clear();

        if (talep is null || talep.Teklifler.Count == 0)
            return;

        var teklifler = talep.Teklifler
            .Where(t => !string.IsNullOrWhiteSpace(t.FirmaAdi))
            .OrderBy(t => t.FirmaAdi)
            .ToList();
        if (teklifler.Count == 0)
            return;

        var onerilen = talep.OnerilenTeklif();
        var kalemler = talep.Kalemler.OrderBy(k => k.SiraNo).ToList();

        KarsilastirmaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        foreach (var _ in teklifler)
            KarsilastirmaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(128) });

        var satir = 0;
        KarsilastirmaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        HucreEkle(0, satir, "Malzeme / Miktar", kalin: true, baslik: true);
        for (var c = 0; c < teklifler.Count; c++)
        {
            var teklif = teklifler[c];
            var oneriMi = onerilen is not null && teklif.Id == onerilen.Id;
            var baslik = oneriMi ? $"{teklif.FirmaAdi}\n★ Öneri" : teklif.FirmaAdi;
            HucreEkle(c + 1, satir, baslik, kalin: true, baslik: true, oneri: oneriMi);
        }

        satir++;
        KarsilastirmaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        HucreEkle(0, satir, "Birim fiyatı", kalin: true, baslik: true);
        for (var c = 0; c < teklifler.Count; c++)
        {
            var oneriMi = onerilen is not null && teklifler[c].Id == onerilen.Id;
            HucreEkle(c + 1, satir, "Birim Fiyat", kalin: true, baslik: true, oneri: oneriMi);
        }

        foreach (var kalem in kalemler)
        {
            satir++;
            KarsilastirmaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var fiyatlar = teklifler.Select(t =>
                t.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id)).ToList();
            var enDusuk = fiyatlar
                .Where(f => f is not null && f.ToplamTutar > 0)
                .MinBy(f => f!.ToplamTutar)?.ToplamTutar;

            HucreEkle(0, satir, $"{kalem.Malzeme}\n{kalem.Miktar.ToString("N2", Tr)} {kalem.Birim}");

            for (var c = 0; c < teklifler.Count; c++)
            {
                var teklif = teklifler[c];
                var fiyat = fiyatlar[c];
                var metin = fiyat is null
                    ? "—"
                    : ParaBirimleri.BirimFiyatGosterim(fiyat.BirimFiyat, fiyat.ParaBirimi, teklif.UsdKuru, teklif.EurKuru);
                var oneriMi = onerilen is not null && teklif.Id == onerilen.Id;
                var enDusukMu = fiyat is not null && fiyat.ToplamTutar > 0 && fiyat.ToplamTutar == enDusuk;
                var seciliMi = kalem.OnaylananTeklifId == teklif.Id;
                HucreEkle(c + 1, satir, metin, kalem, teklif, oneri: oneriMi, enDusuk: enDusukMu, secili: seciliMi);
            }
        }

        satir++;
        KarsilastirmaGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        HucreEkle(0, satir, "ARA TOPLAM\n(KDV Hariç)", kalin: true, baslik: true);
        for (var c = 0; c < teklifler.Count; c++)
        {
            var teklif = teklifler[c];
            var oneriMi = onerilen is not null && teklif.Id == onerilen.Id;
            HucreEkle(c + 1, satir, teklif.AraToplam.ToString("N2", Tr) + " ₺", kalin: true, baslik: true, oneri: oneriMi);
        }
    }

    private void HucreEkle(
        int kolon,
        int satir,
        string metin,
        SatinalmaTalepKalemi? kalem = null,
        SatinalmaTeklif? teklif = null,
        bool kalin = false,
        bool baslik = false,
        bool oneri = false,
        bool enDusuk = false,
        bool secili = false)
    {
        Brush arkaPlan = baslik
            ? YonetimTeklifKarsilastirmaRenkleri.BaslikArkaPlan
            : YonetimTeklifKarsilastirmaRenkleri.HucreArkaPlan;

        Brush cerceve = YonetimTeklifKarsilastirmaRenkleri.Cerceve;
        var cerceveKalinlik = 1.0;

        if (oneri)
        {
            arkaPlan = YonetimTeklifKarsilastirmaRenkleri.OneriArkaPlan;
            cerceve = YonetimTeklifKarsilastirmaRenkleri.OneriCerceve;
        }
        else if (enDusuk && !baslik)
        {
            arkaPlan = YonetimTeklifKarsilastirmaRenkleri.EnDusukArkaPlan;
        }

        if (secili)
        {
            arkaPlan = YonetimTeklifKarsilastirmaRenkleri.SeciliArkaPlan;
            cerceve = YonetimTeklifKarsilastirmaRenkleri.SeciliCerceve;
            cerceveKalinlik = 2.0;
        }

        var border = new Border
        {
            BorderBrush = cerceve,
            BorderThickness = new Thickness(cerceveKalinlik),
            Background = arkaPlan,
            Padding = new Thickness(6, 5, 6, 5),
            Margin = new Thickness(1),
            Child = new TextBlock
            {
                Text = metin,
                TextWrapping = TextWrapping.Wrap,
                FontSize = baslik ? 11 : 10.5,
                FontWeight = kalin ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = oneri
                    ? YonetimTeklifKarsilastirmaRenkleri.OneriMetin
                    : Brushes.Black,
                TextAlignment = kolon == 0 ? TextAlignment.Left : TextAlignment.Center,
                HorizontalAlignment = kolon == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Center
            }
        };

        if (_secimAktif && kalem is not null && teklif is not null)
        {
            border.Cursor = Cursors.Hand;
            border.ToolTip = $"{kalem.Malzeme} için {teklif.FirmaAdi} seç";
            border.MouseLeftButtonUp += (_, _) =>
            {
                kalem.OnaylananTeklifId = teklif.Id;
                KarsilastirmaTablosunuYukle();
                SecimOzetiniGuncelle();
            };
        }

        Grid.SetColumn(border, kolon);
        Grid.SetRow(border, satir);
        KarsilastirmaGrid.Children.Add(border);
    }

    private void SecimOzetiniGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
        {
            TxtSecimOzeti.Text = "";
            return;
        }

        var secili = talep.Kalemler.Count(k => k.OnaylananTeklifId is not null);
        var toplam = talep.Kalemler.Count;
        TxtSecimOzeti.Text = secili == 0
            ? "Henüz kalem seçimi yapılmadı."
            : $"{secili}/{toplam} kalem için firma seçildi.";
    }

    private SatinalmaTalep? GuncelTalep() =>
        _talep is null ? null : SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == _talep.Id) ?? _talep;

    private void OneriyiUygula_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        var oneri = talep?.OnerilenTeklif();
        if (talep is null || oneri is null)
            return;

        foreach (var kalem in talep.Kalemler)
            kalem.OnaylananTeklifId = oneri.Id;

        KarsilastirmaTablosunuYukle();
        SecimOzetiniGuncelle();
    }

    private async void Onayla_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        if (talep.Kalemler.All(k => k.OnaylananTeklifId is null))
        {
            MessageBox.Show("En az bir kalem için firma seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var onay = MessageBox.Show(
            $"{talep.TalepNo} için seçilen teklifler onaylansın mı?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaSiparisIslemleri.KalemBazliOnaylaAsync(talep);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _degisti = true;
        MessageBox.Show($"{talep.TalepNo} onaylandı.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private async void Reddet_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        var gerekce = MetinGirisDialog.Goster(this, "Talep Red", "Red gerekçesi:");
        if (gerekce is null)
            return;

        var onay = MessageBox.Show("Talep reddedilsin mi?", UygulamaBilgisi.Ad,
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            var rol = OturumYoneticisi.AktifKullanici?.Rol;
            await PurchaseRequestDetailServisi.UygulaAsync(
                talep,
                PurchaseRequestDetailAction.RejectEntireRequest,
                rol,
                not: gerekce);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _degisti = true;
        MessageBox.Show("Talep reddedildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private async void GeriGonder_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        var not = MetinGirisDialog.Goster(this, "Teklifleri Revizeye Gönder", "Düzeltme notu (quoteCorrectionNote):");
        if (not is null)
            return;

        try
        {
            var rol = OturumYoneticisi.AktifKullanici?.Rol;
            await PurchaseRequestDetailServisi.UygulaAsync(
                talep,
                PurchaseRequestDetailAction.SendQuotesForRevision,
                rol,
                not: not);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _degisti = true;
        MessageBox.Show("Teklifler revizeye gönderildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void KarsilastirmaPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        if ((talep.Teklifler?.Count ?? 0) == 0)
        {
            MessageBox.Show("Gösterilecek teklif yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
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

    private void Kapat_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = _degisti;
        Close();
    }
}
