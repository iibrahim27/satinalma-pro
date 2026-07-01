using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView
{
    private SatinalmaTalep? _gelenTalepSecili;
    private SatinalmaTalep? _teklifOnaySecili;

    private void GelenTalepListesiniYenile()
    {
        var liste = SatinalmaDepo.Talepler
            .Where(SatinalmaTabFiltreleri.GelenTalepler)
            .OrderByDescending(t => t.TalepTuru == Models.TalepTurleri.Acil)
            .ThenByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .ToList();

        GelenTalepTablosu.ItemsSource = liste;
        GelenTalepButonlariniGuncelle();
    }

    private void TeklifOnayListesiniYenile()
    {
        var liste = SatinalmaDepo.Talepler
            .Where(SatinalmaTabFiltreleri.TeklifOnay)
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .ToList();

        TeklifOnayListesi.ItemsSource = liste;

        if (_teklifOnaySecili is not null && liste.All(t => t.Id != _teklifOnaySecili.Id))
            _teklifOnaySecili = null;
    }

    private void OnayGecmisiListesiniYenile() => GecmisTalepListesiniYenile();

    private void GecmisTalepListesiniYenile()
    {
        OnayGecmisiTablosu.ItemsSource = SatinalmaDepo.Talepler
            .Where(SatinalmaTabFiltreleri.GecmisTalepler)
            .OrderByDescending(t => t.YonetimOnayTarihi)
            .ThenByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .ToList();
    }

    private void GecmisTeklifliListesiniYenile()
    {
        GecmisTeklifliTablosu.ItemsSource = SatinalmaDepo.Talepler
            .Where(SatinalmaTabFiltreleri.GecmisTeklifliOnaylar)
            .OrderByDescending(t => t.YonetimOnayTarihi)
            .ThenByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .ToList();
    }

    private void GelenTalepTablosu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _gelenTalepSecili = GelenTalepTablosu.SelectedItem as SatinalmaTalep;
        GelenTalepButonlariniGuncelle();
    }

    private void GelenTalepButonlariniGuncelle()
    {
        var yetki = KullaniciYetkileri.YonetimKararVerebilir();
        var secili = _gelenTalepSecili is not null;
        BtnGelenTalepOnayla.IsEnabled = yetki && secili;
        BtnGelenTalepTeklifIste.IsEnabled = yetki && secili;
        BtnGelenTalepReddet.IsEnabled = yetki && secili;
    }

    private async void GelenTalepOnayla_Click(object sender, RoutedEventArgs e)
    {
        if (_gelenTalepSecili is null)
            return;

        if (!KullaniciYetkileri.YonetimKararVerebilir())
        {
            MessageBox.Show("Talep onay yetkiniz yok.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_gelenTalepSecili.TalepTuru == Models.TalepTurleri.Acil)
        {
            var onay = MessageBox.Show(
                $"{_gelenTalepSecili.TalepNo} acil talep olarak onaylansın mı?",
                UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes)
                return;
        }
        else
        {
            var onay = MessageBox.Show(
                $"{_gelenTalepSecili.TalepNo} teklifsiz onaylansın mı?",
                UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (onay != MessageBoxResult.Yes)
                return;
        }

        try
        {
            await SatinalmaYonetimIslemleri.OnaylaAsync(_gelenTalepSecili, teklifIste: false);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show($"{_gelenTalepSecili.TalepNo} onaylandı.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
        _gelenTalepSecili = null;
        AkisSekmeleriniYenile();
    }

    private async void GelenTalepTeklifIste_Click(object sender, RoutedEventArgs e)
    {
        if (_gelenTalepSecili is null)
            return;

        if (!KullaniciYetkileri.YonetimKararVerebilir())
        {
            MessageBox.Show("Teklif isteme yetkiniz yok.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var onay = MessageBox.Show(
            $"{_gelenTalepSecili.TalepNo} için teklif girilmesi istensin mi?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaYonetimIslemleri.OnaylaAsync(_gelenTalepSecili, teklifIste: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show($"{_gelenTalepSecili.TalepNo} teklif girişine yönlendirildi.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        _gelenTalepSecili = null;
        AkisSekmeleriniYenile();
    }

    private async void GelenTalepReddet_Click(object sender, RoutedEventArgs e)
    {
        if (_gelenTalepSecili is null)
            return;

        if (!KullaniciYetkileri.YonetimKararVerebilir())
        {
            MessageBox.Show("Red yetkiniz yok.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var gerekce = RedGerekcesiIste();
        if (gerekce is null)
            return;

        try
        {
            await SatinalmaYonetimIslemleri.ReddetAsync(_gelenTalepSecili, gerekce);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show($"{_gelenTalepSecili.TalepNo} reddedildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
        _gelenTalepSecili = null;
        AkisSekmeleriniYenile();
    }

    private void GelenTalepTablosu_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (GelenTalepTablosu.SelectedItem is SatinalmaTalep talep)
            TalepOnizlemePenceresiniAc(talep);
    }

    private void TeklifOnayListesi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _teklifOnaySecili = TeklifOnayListesi.SelectedItem as SatinalmaTalep;
    }

    private void TeklifOnayListesi_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TeklifOnayListesi.SelectedItem is SatinalmaTalep talep)
            TeklifDegerTalebiAc(talep);
    }

    private void TeklifOnayAc_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifOnaySecili is null)
        {
            MessageBox.Show("Önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        TeklifDegerTalebiAc(_teklifOnaySecili);
    }

    private void OnayGecmisiTablosu_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (OnayGecmisiTablosu.SelectedItem is SatinalmaTalep talep)
            TalepOnizlemePenceresiniAc(talep);
    }

    private void GecmisTeklifliTablosu_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (GecmisTeklifliTablosu.SelectedItem is not SatinalmaTalep talep)
            return;

        if (KullaniciYetkileri.YonetimOnayModu())
        {
            _sekmePanelOverride = "Karşılaştırma";
            SekmeyeGec("Geçmiş Teklifli Onaylar");
        }
        else
            SekmeyeGec("Karşılaştırma");

        _teklifDegerTalep = talep;
        TeklifDegerFormuGoster(talep);
    }

    private void TeklifDegerOnayGeriAl_Click(object sender, RoutedEventArgs e) =>
        FirmaOnayiniGeriAl(_teklifDegerTalep);

    private void OnaylananOnayGeriAl_Click(object sender, RoutedEventArgs e) =>
        FirmaOnayiniGeriAl(_onaylananTalep);

    private void FirmaOnayiniGeriAl(SatinalmaTalep? talep)
    {
        if (talep is null)
            return;

        if (!KullaniciYetkileri.SatinalmaFirmaOnayiDuzenlenebilir())
        {
            MessageBox.Show("Onay geri alma yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var onay = MessageBox.Show(
            "Tüm firma onayları geri alınacak. Devam edilsin mi?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            SatinalmaSiparisIslemleri.FirmaOnaylariniGeriAl(talep);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show("Onaylar geri alındı. Firmaları yeniden seçebilirsiniz.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
        AkisSekmeleriniYenile();

        if (_teklifDegerTalep?.Id == talep.Id)
            TeklifDegerFormuGoster(talep);
        if (_onaylananTalep?.Id == talep.Id)
            OnaylananFormuGoster(talep);
    }

    private static string? RedGerekcesiIste()
    {
        var dialog = new Window
        {
            Title = "Talep Red",
            Width = 440,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current?.MainWindow,
            ResizeMode = ResizeMode.NoResize
        };

        var kutu = new TextBox
        {
            Margin = new Thickness(16, 12, 16, 0),
            Height = 72,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(new TextBlock
        {
            Text = "Red gerekçesini girin:",
            Margin = new Thickness(16, 16, 16, 0),
            FontWeight = FontWeights.SemiBold
        });
        panel.Children.Add(kutu);

        var butonlar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16, 8, 16, 16)
        };
        var iptal = new Button { Content = "İptal", Width = 88, Margin = new Thickness(0, 0, 8, 0) };
        var tamam = new Button { Content = "Reddet", Width = 88, IsDefault = true };
        iptal.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };
        tamam.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
        butonlar.Children.Add(iptal);
        butonlar.Children.Add(tamam);
        panel.Children.Add(butonlar);

        dialog.Content = panel;
        kutu.Focus();

        if (dialog.ShowDialog() != true)
            return null;

        var metin = kutu.Text.Trim();
        if (string.IsNullOrWhiteSpace(metin))
        {
            MessageBox.Show("Red gerekçesi zorunludur.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return null;
        }

        return metin;
    }
}
