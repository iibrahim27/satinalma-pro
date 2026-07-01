using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView
{
    private SatinalmaTalep? _seciliTalep;
    private SatinalmaTalepKalemi? _seciliKalem;
    private bool _talepFormModu;
    private bool _talepSerbestDuzenleme;
    private SatinalmaTalepOnizlemeWindow? _acikOnizleme;

    private static readonly string[] TalepGirisDurumlari =
    [
        SatinalmaTalepDurumlari.Taslak,
        SatinalmaTalepDurumlari.Hazirlaniyor
    ];

    private void TalepSekmesiniHazirla()
    {
        if (KalemTablosu.Columns.Count > 2 && KalemTablosu.Columns[2] is DataGridComboBoxColumn birimCol)
            birimCol.ItemsSource = MalzemeBirimDeposu.Liste;

        CmbTalepTuru.ItemsSource = Models.TalepTurleri.Tum
            .Select(t => new { Kod = t, Ad = Models.TalepTurleri.TurkceAd(t) })
            .ToList();
        CmbTalepTuru.DisplayMemberPath = "Ad";
        CmbTalepTuru.SelectedValuePath = "Kod";

        BtnYeniTalep.Visibility = KullaniciYetkileri.TalepOlusturabilir()
            ? Visibility.Visible
            : Visibility.Collapsed;

        SatinalmaDepo.TaleplerGuncellendi += TalepListesiniYenile;
        Unloaded += (_, _) => SatinalmaDepo.TaleplerGuncellendi -= TalepListesiniYenile;
        TalepListesiniYenile();
    }

    private void TalepListesiniYenile()
    {
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var ad = KullaniciYetkileri.AktifKullaniciAdi();

        var liste = SatinalmaDepo.Talepler
            .Where(t => SatinalmaTalepKuyrugu.TaleplerimListesindeGoster(t, uid, ad))
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .Select(t => new TalepListeSatiri(t, uid, ad))
            .ToList();

        TalepListesi.ItemsSource = liste;

        if (_seciliTalep is not null)
        {
            var guncel = liste.FirstOrDefault(s => s.Talep.Id == _seciliTalep.Id);
            if (guncel is null)
            {
                _seciliTalep = null;
                _talepFormModu = false;
                _talepSerbestDuzenleme = false;
                TalepFormuGizle();
                TalepOnizlemePenceresiniKapat();
            }
            else if (_acikOnizleme is not null)
            {
                var guncelTalep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == _seciliTalep.Id);
                if (guncelTalep is not null)
                    _acikOnizleme.Goster(guncelTalep);
            }
            else if (_talepFormModu && (_talepSerbestDuzenleme || guncel.Duzenlenebilir))
                TalepFormuGoster(guncel.Talep);
        }

        TeklifBekleyenListesiniYenile();
        TeklifGirisTalepListesiniYenile();
        AkisSekmeleriniYenile();
    }

    private void TeklifBekleyenListesiniYenile()
    {
        TeklifBekleyenTablosu.ItemsSource = SatinalmaDepo.Talepler
            .Where(TeklifBekleyenSatiri.KuyruktaGoster)
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .Select(t => new TeklifBekleyenSatiri(t))
            .ToList();
    }

    private void YeniTalep_Click(object sender, RoutedEventArgs e)
    {
        if (!KullaniciYetkileri.TalepOlusturabilir())
        {
            MessageBox.Show("Talep oluşturma yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var talep = SatinalmaDepo.YeniTalepOlustur();
        talep.TalepEden = KullaniciYetkileri.AktifKullaniciAdi() ?? "";
        talep.OlusturanUid = OturumYoneticisi.AktifKullanici?.Uid ?? "";
        talep.OlusturanRol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
        SatinalmaDepo.Talepler.Insert(0, talep);
        SatinalmaDepo.Kaydet();

        _seciliTalep = talep;
        _talepFormModu = true;
        _talepSerbestDuzenleme = false;
        TalepListesiniYenile();

        var satir = TalepListesi.Items.Cast<TalepListeSatiri>().FirstOrDefault(s => s.Talep.Id == talep.Id);
        TalepListesi.SelectedItem = satir;
        TalepOnizlemePenceresiniKapat();
        TalepFormuGoster(talep);
    }

    private void TalepListesi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TalepListesi.SelectedItem is not TalepListeSatiri satir)
            return;

        _seciliTalep = satir.Talep;

        if (_talepFormModu && (_talepSerbestDuzenleme || satir.Duzenlenebilir))
        {
            TalepOnizlemePenceresiniKapat();
            TalepFormuGoster(satir.Talep);
            return;
        }

        _talepFormModu = false;
        _talepSerbestDuzenleme = false;
        TalepFormuGizle();
        TalepOnizlemePenceresiniAc(satir.Talep);
    }

    private void TalepFormuGizle()
    {
        TxtTalepFormBos.Visibility = Visibility.Visible;
        TalepFormScroll.Visibility = Visibility.Collapsed;
    }

    private void TalepFormuGoster(SatinalmaTalep talep)
    {
        TxtTalepFormBos.Visibility = Visibility.Collapsed;
        TalepFormScroll.Visibility = Visibility.Visible;

        TxtTalepTarih.Text = talep.Tarih;
        TxtTalepNo.Text = talep.TalepNo;
        TxtTalepEden.Text = string.IsNullOrWhiteSpace(talep.TalepEden)
            ? KullaniciYetkileri.AktifKullaniciAdi() ?? ""
            : talep.TalepEden;
        TxtTalepAciklama.Text = talep.TalepAciklamasi;

        CmbTalepTuru.SelectedValue = string.IsNullOrWhiteSpace(talep.TalepTuru)
            ? Models.TalepTurleri.Normal
            : talep.TalepTuru;

        if (talep.Kalemler.Count == 0)
            talep.Kalemler.Add(new SatinalmaTalepKalemi { SiraNo = 1, Birim = "Adet" });

        KalemTablosu.ItemsSource = talep.Kalemler;
        _seciliKalem = null;

        var duzenlenebilir = KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(talep.TalepEden)
                             && (_talepSerbestDuzenleme || TalepGirisDurumlari.Contains(talep.Durum));
        TxtTalepAciklama.IsReadOnly = !duzenlenebilir;
        CmbTalepTuru.IsEnabled = duzenlenebilir;
        KalemTablosu.IsReadOnly = !duzenlenebilir;
        BtnTalepKaydet.IsEnabled = duzenlenebilir;
        BtnTalepKaydet.Content = _talepSerbestDuzenleme && !TalepGirisDurumlari.Contains(talep.Durum)
            ? "Değişiklikleri Kaydet"
            : "Kaydet";
        BtnTalepOnayaGonder.IsEnabled = TalepGirisDurumlari.Contains(talep.Durum);
        BtnTalepOnayaGonder.Visibility = TalepGirisDurumlari.Contains(talep.Durum)
            ? Visibility.Visible
            : Visibility.Collapsed;

        var yenidenGonder = SatinalmaYonetimGonderimi.YenidenGonderebilir(talep);
        BtnYonetimeYenidenGonder.IsEnabled = yenidenGonder;
        BtnYonetimeYenidenGonder.Visibility = yenidenGonder
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void TalepOnizlemePenceresiniAc(SatinalmaTalep talep)
    {
        if (_acikOnizleme is not null)
        {
            _acikOnizleme.Goster(talep);
            if (!_acikOnizleme.IsVisible)
                _acikOnizleme.Show();
            _acikOnizleme.Activate();
            return;
        }

        _acikOnizleme = new SatinalmaTalepOnizlemeWindow(talep)
        {
            Owner = Window.GetWindow(this)
        };
        _acikOnizleme.DuzenleIstendi += OnizlemedenDuzenleIstendi;
        _acikOnizleme.Closed += (_, _) => _acikOnizleme = null;
        _acikOnizleme.Show();
    }

    private void OnizlemedenDuzenleIstendi(SatinalmaTalep talep)
    {
        var satir = TalepListesi.Items.Cast<TalepListeSatiri>().FirstOrDefault(s => s.Talep.Id == talep.Id);
        if (satir is not null)
            TalepListesi.SelectedItem = satir;
        TalepDuzenlemeyiAc(talep);
    }

    private void TalepOnizlemePenceresiniKapat()
    {
        if (_acikOnizleme is null)
            return;

        _acikOnizleme.DuzenleIstendi -= OnizlemedenDuzenleIstendi;
        _acikOnizleme.Close();
        _acikOnizleme = null;
    }

    private void TalepListeDuzenle_Click(object sender, RoutedEventArgs e)
    {
        if (TalepListesi.SelectedItem is not TalepListeSatiri satir)
            return;

        TalepDuzenlemeyiAc(satir.Talep);
    }

    private void TalepDuzenlemeyiAc(SatinalmaTalep talep)
    {
        if (!KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(talep.TalepEden))
        {
            MessageBox.Show("Bu talebi düzenleme yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _seciliTalep = talep;
        _talepFormModu = true;
        _talepSerbestDuzenleme = true;
        TalepOnizlemePenceresiniKapat();
        TalepFormuGoster(talep);
    }

    private void TalepListesi_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = BulGorselUst<ListViewItem>((DependencyObject)e.OriginalSource);
        if (item is null)
            return;

        item.IsSelected = true;
    }

    private void TalepListesi_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (TalepListesi.SelectedItem is not TalepListeSatiri satir)
        {
            e.Handled = true;
            return;
        }

        var duzenlenebilir = KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(satir.Talep.TalepEden)
                             && satir.Duzenlenebilir;
        foreach (MenuItem item in TalepListesi.ContextMenu!.Items)
        {
            if (item.Header?.ToString() == "Düzenle")
                item.IsEnabled = duzenlenebilir;
        }

        MenuTalepSil.IsEnabled = KullaniciYetkileri.SatinalmaTalepSilebilir();
    }

    private async void TalepListeSil_Click(object sender, RoutedEventArgs e)
    {
        if (TalepListesi.SelectedItem is not TalepListeSatiri satir)
            return;

        if (!KullaniciYetkileri.SatinalmaTalepSilebilir())
        {
            MessageBox.Show("Talep silme yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var talep = satir.Talep;
        var teklifVar = (talep.Teklifler?.Count ?? 0) > 0;
        var onayVar = talep.HerhangiKalemOnayli
                      || talep.Durum is SatinalmaTalepDurumlari.Onaylandi
                          or SatinalmaTalepDurumlari.SiparisOlusturuldu;

        var uyari = teklifVar || onayVar
            ? $"{talep.TalepNo} talebi ve ilişkili tüm teklif / onay kayıtları kalıcı olarak silinsin mi?\n\nBu işlem geri alınamaz."
            : $"{talep.TalepNo} talebi kalıcı olarak silinsin mi?";

        if (MessageBox.Show(uyari, UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Warning)
            != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaTalepSilmeYardimcisi.SilAsync(talep);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Talep silinemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (_seciliTalep?.Id == talep.Id)
        {
            _seciliTalep = null;
            _talepFormModu = false;
            _talepSerbestDuzenleme = false;
            TalepFormuGizle();
            TalepOnizlemePenceresiniKapat();
        }

        TalepListesiniYenile();
        TalepListesi.SelectedItem = null;

        MessageBox.Show($"{talep.TalepNo} silindi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static T? BulGorselUst<T>(DependencyObject? kok) where T : DependencyObject
    {
        while (kok is not null)
        {
            if (kok is T hedef)
                return hedef;
            kok = VisualTreeHelper.GetParent(kok);
        }

        return null;
    }

    private void KalemEkle_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliTalep is null)
            return;

        var sira = (_seciliTalep.Kalemler.MaxBy(k => k.SiraNo)?.SiraNo ?? 0) + 1;
        _seciliTalep.Kalemler.Add(new SatinalmaTalepKalemi { SiraNo = sira, Birim = "Adet" });
        KalemTablosu.Items.Refresh();
    }

    private void KalemSil_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliTalep is null)
            return;

        if (_seciliKalem is null)
        {
            MessageBox.Show("Silmek için bir satır seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_seciliTalep.Kalemler.Count <= 1)
        {
            MessageBox.Show("En az bir kalem satırı kalmalıdır.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _seciliTalep.Kalemler.Remove(_seciliKalem);
        _seciliKalem = null;
        KalemSiraNumaralariniGuncelle();
        KalemTablosu.Items.Refresh();
    }

    private void KalemTablosu_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _seciliKalem = KalemTablosu.SelectedItem as SatinalmaTalepKalemi;

    private void KalemTablosu_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) { }

    private static void KalemSiraNumaralariniGuncelle(IEnumerable<SatinalmaTalepKalemi> kalemler)
    {
        var sira = 1;
        foreach (var k in kalemler.OrderBy(k => k.SiraNo))
            k.SiraNo = sira++;
    }

    private void KalemSiraNumaralariniGuncelle() =>
        KalemSiraNumaralariniGuncelle(_seciliTalep?.Kalemler ?? []);

    private bool TalepFormunuModeleAktar(bool zorunluKalem)
    {
        if (_seciliTalep is null)
        {
            MessageBox.Show("Kaydedilecek talep seçilmedi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        _seciliTalep.TalepAciklamasi = TxtTalepAciklama.Text.Trim();
        _seciliTalep.TalepTuru = CmbTalepTuru.SelectedValue?.ToString() ?? Models.TalepTurleri.Normal;
        if (string.IsNullOrWhiteSpace(_seciliTalep.TalepEden))
            _seciliTalep.TalepEden = KullaniciYetkileri.AktifKullaniciAdi() ?? "";

        KalemSiraNumaralariniGuncelle();

        if (!zorunluKalem)
            return true;

        if (!_seciliTalep.Kalemler.Any(k => !string.IsNullOrWhiteSpace(k.Malzeme)))
        {
            MessageBox.Show("En az bir malzeme kalemi girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        foreach (var k in _seciliTalep.Kalemler.Where(k => !string.IsNullOrWhiteSpace(k.Malzeme)))
        {
            if (k.Miktar <= 0)
            {
                MessageBox.Show($"'{k.Malzeme}' için miktar girin.", UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        return true;
    }

    private void TalepKaydet_Click(object sender, RoutedEventArgs e)
    {
        if (!TalepFormunuModeleAktar(zorunluKalem: true))
            return;

        if (_talepSerbestDuzenleme && _seciliTalep is not null
            && !TalepGirisDurumlari.Contains(_seciliTalep.Durum))
        {
            SatinalmaDepo.Kaydet();
            TalepListesiniYenile();
            MessageBox.Show($"{_seciliTalep.TalepNo} güncellendi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaTalepYardimcisi.KayitOncesiHazirla(_seciliTalep!);
        if (string.IsNullOrWhiteSpace(_seciliTalep!.OlusturanRol))
            _seciliTalep.OlusturanRol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
        _seciliTalep!.Durum = SatinalmaTalepDurumlari.Hazirlaniyor;
        SatinalmaDepo.Kaydet();

        var kaydedilenNo = _seciliTalep.TalepNo;
        TalepListesiniYenile();
        TalepFormuGoster(_seciliTalep);

        MessageBox.Show(
            $"{kaydedilenNo} kaydedildi. Yönetime göndermek için «Onaya Gönder» kullanın.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void TalepOnayaGonder_Click(object sender, RoutedEventArgs e)
    {
        if (!TalepFormunuModeleAktar(zorunluKalem: true))
            return;

        if ((_seciliTalep!.Teklifler?.Count ?? 0) > 0)
        {
            MessageBox.Show(
                "Teklif girilmiş talepler «Teklif Girişi» ekranından «Değerlendirmeye Gönder» ile yönetime iletilmelidir.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var onay = MessageBox.Show(
            "Talep yönetim onayına gönderilsin mi?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        SatinalmaTalepYardimcisi.KayitOncesiHazirla(_seciliTalep!);
        _seciliTalep!.Durum = SatinalmaTalepDurumlari.ImzaSurecinde;
        SatinalmaDepo.Kaydet();

        try
        {
            await SatinalmaBildirimleri.YonetimeGonderildiAsync(_seciliTalep);
        }
        catch
        {
            // bildirim hatası kaydı engellemez
        }

        var no = _seciliTalep.TalepNo;
        _seciliTalep = null;
        _talepFormModu = false;
        _talepSerbestDuzenleme = false;
        TalepListesiniYenile();
        TalepFormuGizle();
        TalepOnizlemePenceresiniKapat();
        TalepListesi.SelectedItem = null;

        MessageBox.Show($"{no} yönetim onayına gönderildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void YonetimeYenidenGonder_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliTalep is null)
            return;

        if (!SatinalmaYonetimGonderimi.YenidenGonderebilir(_seciliTalep))
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
            await SatinalmaYonetimGonderimi.YenidenGonderAsync(_seciliTalep);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Gönderilemedi:\n{ex.Message}", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var no = _seciliTalep.TalepNo;
        TalepListesiniYenile();
        MessageBox.Show($"{no} için yönetime yeniden bildirim gönderildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void TalepPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliTalep is null)
        {
            MessageBox.Show("PDF için önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        TalepFormunuModeleAktar(zorunluKalem: false);
        SatinalmaPdfOlusturucu.TalepFormuYazdir(_seciliTalep, SatinalmaDepo.Ayarlar);
    }
}
