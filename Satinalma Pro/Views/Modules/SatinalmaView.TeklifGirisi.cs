using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView
{
    private SatinalmaTalep? _teklifGirisTalep;
    private SatinalmaTeklif? _seciliTeklif;

    private void TeklifGirisSekmesiniHazirla()
    {
        TeklifGirisTalepListesiniYenile();
    }

    public void TeklifGirisTalebiAc(SatinalmaTalep? talep)
    {
        SekmeyeGec("Teklif Girişi");
        TeklifGirisTalepListesiniYenile();

        if (talep is null)
            return;

        TeklifGirisTalepListesi.SelectedItem = talep;
        _teklifGirisTalep = talep;
        TeklifGirisFormuGoster(talep);
    }

    private void TeklifGirisTalepListesiniYenile()
    {
        var seciliId = _teklifGirisTalep?.Id;
        var formAcik = TeklifGirisIcerik.Visibility == Visibility.Visible;

        var liste = SatinalmaDepo.Talepler
            .Where(SatinalmaTalepKuyrugu.SatinalmaTeklifGirisiAktif)
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .ToList();

        TeklifGirisTalepListesi.ItemsSource = liste;

        if (seciliId is { } id)
        {
            var hedef = liste.FirstOrDefault(t => t.Id == id)
                        ?? SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == id);
            if (hedef is not null)
            {
                _teklifGirisTalep = hedef;
                if (liste.Contains(hedef))
                    TeklifGirisTalepListesi.SelectedItem = hedef;
                if (formAcik)
                    TeklifGirisFormuGoster(hedef);
                TeklifBekleyenListesiniYenile();
                return;
            }
        }

        if (_teklifGirisTalep is not null && liste.All(t => t.Id != _teklifGirisTalep.Id))
        {
            _teklifGirisTalep = null;
            _seciliTeklif = null;
            TeklifGirisFormuGizle();
        }

        TeklifBekleyenListesiniYenile();
    }

    private static SatinalmaTalep? GuncelTalepGetir(SatinalmaTalep? talep) =>
        talep is null ? null : SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;

    private async Task<bool> YonetimeTeklifDegerlendirmeyeGonderAsync(SatinalmaTalep? talep)
    {
        talep = GuncelTalepGetir(talep);
        if (talep is null)
            return false;

        talep.Teklifler ??= [];
        talep.Kalemler ??= [];
        foreach (var teklif in talep.Teklifler)
            teklif.FiyatlariHesapla(talep.Kalemler);

        if (talep.Teklifler.Count == 0)
        {
            MessageBox.Show("Göndermek için en az bir teklif girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        foreach (var teklif in talep.Teklifler)
        {
            if (teklif.GenelToplam <= 0)
            {
                MessageBox.Show($"'{teklif.FirmaAdi}' teklifinde geçerli fiyat bulunamadı.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        var oneri = talep.OnerilenTeklif();
        if (oneri is null)
        {
            MessageBox.Show("Geçerli bir satınalma önerisi oluşturulamadı. Teklif fiyatlarını kontrol edin.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!talep.SatinalmaOnerisiElleSecildi)
            talep.YonetimOnerilenTeklifId = oneri.Id;

        var onay = MessageBox.Show(
            $"Teklifler yönetim onayına gönderilsin mi?\n\nSatınalma önerisi: {oneri.FirmaAdi}",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return false;

        talep.Durum = SatinalmaTalepDurumlari.YonetimOnayinda;
        SatinalmaTalepYardimcisi.Dokun(talep);
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

        try
        {
            await BildirimYoneticisi.GecersizleriOkunduYapAsync();
            await SatinalmaBildirimleri.TeklifOnaydaAsync(talep);
        }
        catch
        {
            // bildirim hatası kaydı engellemez
        }

        return true;
    }

    private void TeklifBekleyenTablosu_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TeklifBekleyenTablosu.SelectedItem is TeklifBekleyenSatiri satir)
            TeklifGirisTalebiAc(satir.Talep);
    }

    private void TeklifGirisTalepListesi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TeklifGirisTalepListesi.SelectedItem is SatinalmaTalep talep)
        {
            _teklifGirisTalep = talep;
            TeklifGirisFormuGoster(talep);
        }
    }

    private void TeklifGirisFormuGizle()
    {
        TxtTeklifGirisBos.Visibility = Visibility.Visible;
        TeklifGirisIcerik.Visibility = Visibility.Collapsed;
    }

    private void TeklifGirisFormuGoster(SatinalmaTalep talep)
    {
        TxtTeklifGirisBos.Visibility = Visibility.Collapsed;
        TeklifGirisIcerik.Visibility = Visibility.Visible;

        TxtTeklifTalepNo.Text = talep.TalepNo;
        TxtTeklifTalepTarih.Text = talep.Tarih;
        TxtTeklifTalepEden.Text = talep.TalepEden;

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        TeklifKalemTablosu.ItemsSource = talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
        TeklifListesiniYenile();
        SatinalmaOnerisiniGuncelle();

        _seciliTeklif = null;
        GuncelleTeklifYonetimeYenidenGonder();

        if (!string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu))
        {
            TxtTeklifDuzeltmeNotu.Text = $"Yönetim düzeltme notu: {talep.TeklifDuzeltmeNotu}";
            TeklifDuzeltmeNotuPanel.Visibility = Visibility.Visible;
        }
        else
        {
            TeklifDuzeltmeNotuPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void GuncelleTeklifYonetimeYenidenGonder()
    {
        var goster = _teklifGirisTalep is not null
            && SatinalmaYonetimGonderimi.YenidenGonderebilir(_teklifGirisTalep);
        BtnTeklifYonetimeYenidenGonder.IsEnabled = goster;
        BtnTeklifYonetimeYenidenGonder.Visibility = goster
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SatinalmaOnerisiniGuncelle()
    {
        if (_teklifGirisTalep is null)
        {
            TxtSatinalmaOnerisi.Text = "Satınalma önerisi: Henüz seçilmedi.";
            return;
        }

        if (_teklifGirisTalep.SatinalmaOnerisiElleSecildi
            && _teklifGirisTalep.YonetimOnerilenTeklifId is { } oneriId)
        {
            var onerilen = (_teklifGirisTalep.Teklifler ?? [])
                .FirstOrDefault(t => t.Id == oneriId);
            if (onerilen is not null)
            {
                var tr = CultureInfo.GetCultureInfo("tr-TR");
                TxtSatinalmaOnerisi.Text =
                    $"Satınalma önerisi: {onerilen.FirmaAdi} — KDV Hariç: {onerilen.AraToplam.ToString("N2", tr)} ₺ | KDV: {onerilen.KdvTutari.ToString("N2", tr)} ₺ | KDV Dahil: {onerilen.GenelToplam.ToString("N2", tr)} ₺";
                return;
            }
        }

        TxtSatinalmaOnerisi.Text = (_teklifGirisTalep.Teklifler?.Count ?? 0) == 0
            ? "Satınalma önerisi: Teklif girildikten sonra bir firmayı öneri olarak işaretleyin."
            : "Satınalma önerisi: Henüz seçilmedi — listeden firma seçip «Öneri Olarak İşaretle» deyin.";
    }

    private void TeklifListesiniYenile()
    {
        if (_teklifGirisTalep is null)
            return;

        _teklifGirisTalep.Teklifler ??= [];
        foreach (var teklif in _teklifGirisTalep.Teklifler)
            teklif.FiyatlariHesapla(_teklifGirisTalep.Kalemler);

        TeklifListesiTablosu.ItemsSource = _teklifGirisTalep.Teklifler
            .OrderBy(t => t.FirmaAdi)
            .Select(t => new TeklifGirisSatiri(_teklifGirisTalep, t))
            .ToList();

        SatinalmaOnerisiniGuncelle();
    }

    private void TeklifListesiTablosu_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _seciliTeklif = (TeklifListesiTablosu.SelectedItem as TeklifGirisSatiri)?.Teklif;

    private void TeklifListesiTablosu_DoubleClick(object sender, MouseButtonEventArgs e) =>
        TeklifDuzenle_Click(sender, e);

    private void YeniTeklif_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifGirisTalep is null)
        {
            MessageBox.Show("Önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var k = OturumYoneticisi.AktifKullanici;
        if (!SatinalmaPro.Shared.Helpers.SatinalmaIsAkisi.TeklifEklenebilir(
                _teklifGirisTalep.Durum,
                _teklifGirisTalep.TalepTuru,
                _teklifGirisTalep.YonetimOnayKilitli,
                _teklifGirisTalep.OlusturanRol,
                k?.Rol,
                k?.Aktif ?? false))
        {
            MessageBox.Show(
                SatinalmaPro.Shared.Helpers.SatinalmaIsAkisi.TeklifEklemeEngelMesaji(
                    _teklifGirisTalep.Durum,
                    _teklifGirisTalep.TalepTuru,
                    _teklifGirisTalep.YonetimOnayKilitli,
                    _teklifGirisTalep.OlusturanRol,
                    k?.Rol),
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var teklif = new SatinalmaTeklif();
        SatinalmaDepo.TeklifFiyatlariniHazirla(_teklifGirisTalep, teklif);

        if (!TeklifPenceresiAc(teklif))
            return;

        _teklifGirisTalep.Teklifler.Add(teklif);
        if (_teklifGirisTalep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
            or SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.ImzaSurecinde)
            _teklifGirisTalep.Durum = SatinalmaTalepDurumlari.Karsilastirma;

        SatinalmaDepo.TeklifDegisikligiIsle(_teklifGirisTalep);
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(_teklifGirisTalep);
        TeklifListesiniYenile();
        TeklifGirisTalepListesiniYenile();
        GuncelleTeklifYonetimeYenidenGonder();
    }

    private void TeklifDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifGirisTalep is null || _seciliTeklif is null)
        {
            MessageBox.Show("Düzenlemek için bir teklif seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TeklifPenceresiAc(_seciliTeklif))
            return;

        SatinalmaDepo.TeklifDegisikligiIsle(_teklifGirisTalep);
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(_teklifGirisTalep);
        TeklifListesiniYenile();
        GuncelleTeklifYonetimeYenidenGonder();
    }

    private void TeklifSil_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifGirisTalep is null || _seciliTeklif is null)
        {
            MessageBox.Show("Silmek için bir teklif seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var onay = MessageBox.Show(
            $"'{_seciliTeklif.FirmaAdi}' teklifi silinsin mi?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        _teklifGirisTalep.Teklifler.Remove(_seciliTeklif);
        if (_teklifGirisTalep.YonetimOnerilenTeklifId == _seciliTeklif.Id)
        {
            _teklifGirisTalep.YonetimOnerilenTeklifId = null;
            _teklifGirisTalep.SatinalmaOnerisiElleSecildi = false;
        }
        _seciliTeklif = null;
        SatinalmaDepo.TeklifDegisikligiIsle(_teklifGirisTalep);
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(_teklifGirisTalep);
        TeklifListesiniYenile();
        TeklifGirisTalepListesiniYenile();
        GuncelleTeklifYonetimeYenidenGonder();
    }

    private bool TeklifPenceresiAc(SatinalmaTeklif teklif)
    {
        var pencere = new SatinalmaTeklifDuzenleWindow(teklif, _teklifGirisTalep!.Kalemler, SatinalmaDepo.Ayarlar)
        {
            Owner = Window.GetWindow(this)
        };

        if (pencere.ShowDialog() != true)
            return false;

        teklif.FiyatlariHesapla(_teklifGirisTalep.Kalemler);
        return true;
    }

    private void TedarikciTeklifPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifGirisTalep is null)
            return;

        SatinalmaPdfOlusturucu.TedarikciTeklifTalebiYazdir(_teklifGirisTalep, SatinalmaDepo.Ayarlar);
    }

    private void KarsilastirmaPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifGirisTalep is null)
            return;

        if ((_teklifGirisTalep.Teklifler?.Count ?? 0) == 0)
        {
            MessageBox.Show("Karşılaştırma için en az bir teklif girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SatinalmaPdfOlusturucu.KarsilastirmaYazdir(
            _teklifGirisTalep,
            SatinalmaDepo.Ayarlar,
            _teklifGirisTalep.OnerilenTeklif());
    }

    private void TeklifOneriYap_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifGirisTalep is null || _seciliTeklif is null)
        {
            MessageBox.Show("Öneri için listeden bir firma teklifi seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var seciliTeklifId = _seciliTeklif.Id;
        var firmaAdi = _seciliTeklif.FirmaAdi;

        _teklifGirisTalep.SatinalmaOnerisiElleSecildi = true;
        _teklifGirisTalep.YonetimOnerilenTeklifId = seciliTeklifId;
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(_teklifGirisTalep);
        TeklifListesiniYenile();

        var satir = TeklifListesiTablosu.Items.Cast<TeklifGirisSatiri>()
            .FirstOrDefault(s => s.Teklif.Id == seciliTeklifId);
        if (satir is not null)
            TeklifListesiTablosu.SelectedItem = satir;

        MessageBox.Show($"'{firmaAdi}' satınalma önerisi olarak işaretlendi.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void TeklifGirisKaydet_Click(object sender, RoutedEventArgs e)
    {
        _teklifGirisTalep = GuncelTalepGetir(_teklifGirisTalep);
        if (_teklifGirisTalep is null)
            return;

        foreach (var teklif in _teklifGirisTalep.Teklifler ?? [])
            teklif.FiyatlariHesapla(_teklifGirisTalep.Kalemler);

        if ((_teklifGirisTalep.Teklifler?.Count ?? 0) > 0
            && _teklifGirisTalep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
                or SatinalmaTalepDurumlari.Hazirlaniyor
                or SatinalmaTalepDurumlari.ImzaSurecinde)
            _teklifGirisTalep.Durum = SatinalmaTalepDurumlari.Karsilastirma;

        SatinalmaDepo.TeklifDegisikligiIsle(_teklifGirisTalep);
        var talepNo = _teklifGirisTalep.TalepNo;
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(_teklifGirisTalep);
        TeklifGirisTalepListesiniYenile();
        SatinalmaOnerisiniGuncelle();
        SekmeSayaclariniYenile();
        GuncelleTeklifYonetimeYenidenGonder();

        MessageBox.Show(
            $"{talepNo} ve teklifler kaydedildi. Daha fazla teklif ekleyebilir veya «Değerlendirmeye Gönder» ile yönetime iletebilirsiniz.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void DegerlendirmeyeGonder_Click(object sender, RoutedEventArgs e)
    {
        if (!await YonetimeTeklifDegerlendirmeyeGonderAsync(_teklifGirisTalep))
            return;

        var no = _teklifGirisTalep?.TalepNo ?? "";
        _teklifGirisTalep = null;
        _seciliTeklif = null;
        TeklifGirisTalepListesiniYenile();
        TeklifGirisFormuGizle();
        TeklifGirisTalepListesi.SelectedItem = null;
        SekmeSayaclariniYenile();
        AkisSekmeleriniYenile();

        MessageBox.Show(
            $"{no} yönetim teklif onayına gönderildi.\n\nYönetim kullanıcıları «Teklif Onay» ekranından inceleyebilir.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void TeklifYonetimeYenidenGonder_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifGirisTalep is null)
            return;

        if (!SatinalmaYonetimGonderimi.YenidenGonderebilir(_teklifGirisTalep))
        {
            MessageBox.Show("Bu talep yönetime yeniden gönderilemez.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var onay = MessageBox.Show(
            "Yönetime yeniden bildirim gönderilsin mi?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaYonetimGonderimi.YenidenGonderAsync(_teklifGirisTalep);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gönderilemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        TeklifGirisTalepListesiniYenile();
        GuncelleTeklifYonetimeYenidenGonder();
        AkisSekmeleriniYenile();
        MessageBox.Show($"{_teklifGirisTalep.TalepNo} için yönetime yeniden bildirim gönderildi.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
