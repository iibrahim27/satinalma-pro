using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Views.Controls;

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
        var rol = OturumYoneticisi.AktifKullanici?.Rol;

        var liste = SatinalmaDepo.Talepler
            .Where(t => SatinalmaTalepKuyrugu.TaleplerimListesindeGoster(t, uid, ad, rol))
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .Select(t => new TalepListeSatiri(t, uid, ad))
            .ToList();

        TalepListesi.ItemsSource = liste;

        if (_seciliTalep is not null)
        {
            var depoda = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == _seciliTalep.Id);
            var guncel = liste.FirstOrDefault(s => s.Talep.Id == _seciliTalep.Id);

            if (depoda is null)
            {
                if (_talepFormModu && SatinalmaDepo.KorunanBosTaslakId == _seciliTalep.Id)
                    return;

                _seciliTalep = null;
                _talepFormModu = false;
                _talepSerbestDuzenleme = false;
                TalepFormuGizle();
                TalepOnizlemePenceresiniKapat();
            }
            else if (_talepFormModu && KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(depoda))
            {
                _seciliTalep = depoda;
                if (_acikOnizleme is null)
                    TalepFormuGoster(depoda);
            }
            else if (_acikOnizleme is not null)
            {
                _seciliTalep = depoda;
                _acikOnizleme.Goster(depoda);
            }
            else if (_talepFormModu && guncel is not null
                     && KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(guncel.Talep)
                     && (_talepSerbestDuzenleme || guncel.Duzenlenebilir))
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

        if (_talepFormModu && _seciliTalep is not null
            && _seciliTalep.Durum == SatinalmaTalepDurumlari.Taslak
            && !SatinalmaTalepYardimcisi.IcerikVar(_seciliTalep))
        {
            SatinalmaDepo.KorunanBosTaslakId = _seciliTalep.Id;
            TalepFormuGoster(_seciliTalep);
            return;
        }

        BosTaslakTaslagiTemizle();

        var talep = SatinalmaDepo.YeniTalepOlustur(talepNoVer: true);
        talep.TalepEden = KullaniciYetkileri.AktifKullaniciAdi() ?? "";
        talep.OlusturanUid = OturumYoneticisi.AktifKullanici?.Uid ?? "";
        talep.OlusturanRol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
        SatinalmaDepo.Talepler.Insert(0, talep);
        SatinalmaDepo.KorunanBosTaslakId = talep.Id;
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

    private static void BosTaslakTaslagiTemizle()
    {
        if (SatinalmaDepo.BosTaslaklariTemizle())
            SatinalmaDepo.Kaydet();
    }

    private void BosTaslakTaslagiBirak()
    {
        SatinalmaDepo.KorunanBosTaslakId = null;
        BosTaslakTaslagiTemizle();
        _talepFormModu = false;
    }

    private void TalepListesi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TalepListesi.SelectedItem is not TalepListeSatiri satir)
        {
            if (e.RemovedItems.Count > 0 && _talepFormModu)
                BosTaslakTaslagiBirak();
            return;
        }

        if (_seciliTalep is not null && _seciliTalep.Id != satir.Talep.Id && _talepFormModu)
            BosTaslakTaslagiBirak();

        _seciliTalep = satir.Talep;

        if (_talepFormModu && KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(satir.Talep)
            && (_talepSerbestDuzenleme || satir.Duzenlenebilir))
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

    /// <summary>Bulut senkronu sonrası depodan yeniden bağlar — stale/null referans çökmesini önler.</summary>
    private void KayittanSonraFormuGoster(Guid talepId)
    {
        if (!_talepFormModu)
            return;

        var depoda = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talepId);
        if (depoda is null)
        {
            _seciliTalep = null;
            _talepFormModu = false;
            TalepFormuGizle();
            return;
        }

        _seciliTalep = depoda;
        TalepFormuGoster(depoda);
    }

    private void TalepFormuGoster(SatinalmaTalep? talep)
    {
        if (talep is null || TxtTalepTarih is null)
            return;

        if (_talepFormModu && talep.Durum == SatinalmaTalepDurumlari.Taslak)
            SatinalmaDepo.KorunanBosTaslakId = talep.Id;

        TxtTalepFormBos.Visibility = Visibility.Collapsed;
        TalepFormScroll.Visibility = Visibility.Visible;

        TxtTalepTarih.Text = talep.Tarih;
        TxtTalepNo.Text = string.IsNullOrWhiteSpace(talep.TalepNo) ? "Yeni" : talep.TalepNo;
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

        var metaDuzenle = KullaniciYetkileri.SatinalmaTalepMetaDuzenleyebilir();
        var metaAktif = metaDuzenle && _talepSerbestDuzenleme;
        TxtTalepEden.IsReadOnly = !metaAktif;
        TxtTalepTarih.IsReadOnly = !metaAktif;
        if (metaAktif)
        {
            TxtTalepEden.Background = System.Windows.Media.Brushes.White;
            TxtTalepTarih.Background = System.Windows.Media.Brushes.White;
        }
        else
        {
            TxtTalepEden.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8FAFC")!);
            TxtTalepTarih.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F8FAFC")!);
        }

        var kalemDuzenle = _talepSerbestDuzenleme
            ? KullaniciYetkileri.SatinalmaTalepKalemDuzenleyebilir(talep)
            : TalepGirisDurumlari.Contains(talep.Durum)
              && KullaniciYetkileri.SatinalmaTalepKalemDuzenleyebilir(talep);
        TxtTalepAciklama.IsReadOnly = !kalemDuzenle;
        CmbTalepTuru.IsEnabled = kalemDuzenle;
        KalemTablosu.IsReadOnly = !kalemDuzenle;
        BtnTalepKaydet.IsEnabled = kalemDuzenle || metaAktif;
        BtnTalepKaydet.Content = _talepSerbestDuzenleme && !TalepGirisDurumlari.Contains(talep.Durum)
            ? "Değişiklikleri Kaydet"
            : "Kaydet";
        BtnTalepOnayaGonder.IsEnabled = TalepGirisDurumlari.Contains(talep.Durum)
                                        && KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(talep);
        BtnTalepOnayaGonder.Visibility = TalepGirisDurumlari.Contains(talep.Durum)
            ? Visibility.Visible
            : Visibility.Collapsed;

        var yenidenGonder = KullaniciYetkileri.YonetimeYenidenGonderebilir(talep);
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
        if (!KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(talep))
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

        var duzenlenebilir = KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(satir.Talep);
        foreach (MenuItem item in TalepListesi.ContextMenu!.Items)
        {
            if (item.Header?.ToString() == "Düzenle")
                item.IsEnabled = duzenlenebilir;
        }

        MenuTalepSil.IsEnabled = KullaniciYetkileri.SatinalmaTalepSilebilir(satir.Talep);
    }

    private async void TalepListeSil_Click(object sender, RoutedEventArgs e)
    {
        if (TalepListesi.SelectedItem is not TalepListeSatiri satir)
            return;

        if (!KullaniciYetkileri.SatinalmaTalepSilebilir(satir.Talep))
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

    private static T? BulGorselUst<T>(DependencyObject? kok) where T : DependencyObject =>
        VisualTreeYardimcisi.FindAncestor<T>(kok);

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

    private void KalemTablosu_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.Column.Header?.ToString() != "Malzeme")
            return;

        if (e.EditingElement is MalzemeOneriGiris oneri)
        {
            oneri.OneriKaynaginiAyarla(MalzemeAdiOneriServisi.Ara);
            oneri.MetneOdaklan();
            return;
        }

        if (e.EditingElement is FrameworkElement kok)
        {
            var bulunan = BulAltEleman<MalzemeOneriGiris>(kok);
            if (bulunan is not null)
            {
                bulunan.OneriKaynaginiAyarla(MalzemeAdiOneriServisi.Ara);
                bulunan.MetneOdaklan();
            }
        }
    }

    private void KalemTablosu_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Column.Header?.ToString() == "Miktar"
            && e.Row.Item is SatinalmaTalepKalemi kalem
            && e.EditingElement is TextBox tb
            && double.TryParse(tb.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.CurrentCulture, out var miktar))
        {
            kalem.Miktar = miktar;
        }
    }

    private static T? BulAltEleman<T>(DependencyObject kok) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(kok); i++)
        {
            var cocuk = VisualTreeHelper.GetChild(kok, i);
            if (cocuk is T bulunan)
                return bulunan;

            var alt = BulAltEleman<T>(cocuk);
            if (alt is not null)
                return alt;
        }

        return null;
    }

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

        if (KullaniciYetkileri.SatinalmaTalepMetaDuzenleyebilir())
        {
            var eden = TxtTalepEden.Text.Trim();
            if (!string.IsNullOrWhiteSpace(eden))
                _seciliTalep.TalepEden = eden;
            var tarih = TxtTalepTarih.Text.Trim();
            if (!string.IsNullOrWhiteSpace(tarih))
                _seciliTalep.Tarih = tarih;
        }
        else if (string.IsNullOrWhiteSpace(_seciliTalep.TalepEden))
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

    private void TalepOlusturanBilgisiniTamamla(SatinalmaTalep talep)
    {
        if (string.IsNullOrWhiteSpace(talep.OlusturanUid))
            talep.OlusturanUid = OturumYoneticisi.AktifKullanici?.Uid ?? "";
        if (string.IsNullOrWhiteSpace(talep.OlusturanRol))
            talep.OlusturanRol = OturumYoneticisi.AktifKullanici?.Rol ?? "";
        if (string.IsNullOrWhiteSpace(talep.TalepEden))
            talep.TalepEden = KullaniciYetkileri.AktifKullaniciAdi() ?? "";
    }

    private async void TalepKaydet_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliTalep is null)
        {
            MessageBox.Show("Kaydedilecek talep seçilmedi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TalepFormunuModeleAktar(zorunluKalem: true))
            return;

        if (!KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(_seciliTalep)
            && !KullaniciYetkileri.SatinalmaTalepKalemDuzenleyebilir(_seciliTalep))
        {
            MessageBox.Show("Bu talebi kaydetme yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SatinalmaTalepYardimcisi.Dokun(_seciliTalep!);

        if (_talepSerbestDuzenleme && _seciliTalep is not null)
        {
            var kalemDuzenle = KullaniciYetkileri.SatinalmaTalepKalemDuzenleyebilir(_seciliTalep);
            var metaAktif = KullaniciYetkileri.SatinalmaTalepMetaDuzenleyebilir();

            if (kalemDuzenle || metaAktif)
            {
                if (kalemDuzenle && (_seciliTalep.Teklifler?.Count ?? 0) > 0)
                    SatinalmaDepo.TeklifDegisikligiIsle(_seciliTalep);

                var tamYetki = SatinalmaTalepYetkileri.SatinalmaTamYetki(
                    OturumYoneticisi.AktifKullanici?.Rol);

                if (tamYetki)
                    await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(_seciliTalep);
                else
                    SatinalmaDepo.Kaydet();

                var guncellemeId = _seciliTalep.Id;
                var guncellemeNo = _seciliTalep.TalepNo;
                var teklifVardi = kalemDuzenle && (_seciliTalep.Teklifler?.Count ?? 0) > 0;
                TalepListesiniYenile();
                KayittanSonraFormuGoster(guncellemeId);
                var mesaj = teklifVardi
                    ? $"{guncellemeNo} güncellendi. Teklifler senkronize edildi — düzeltmeleri yönetime «Yeniden Gönder» ile iletin."
                    : $"{guncellemeNo} güncellendi.";
                MessageBox.Show(mesaj, UygulamaBilgisi.Ad,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }

        SatinalmaTalepYardimcisi.KayitOncesiHazirla(_seciliTalep!);
        TalepOlusturanBilgisiniTamamla(_seciliTalep!);
        SatinalmaDepo.TalepNoAtaIfNeeded(_seciliTalep!);
        SatinalmaDepo.KorunanBosTaslakId = null;
        _seciliTalep!.Durum = SatinalmaTalepDurumlari.Hazirlaniyor;
        SatinalmaTalepYardimcisi.Dokun(_seciliTalep);
        SatinalmaDepo.Kaydet();

        var kaydedilenId = _seciliTalep.Id;
        var kaydedilenNo = _seciliTalep.TalepNo;
        TalepListesiniYenile();
        KayittanSonraFormuGoster(kaydedilenId);

        MessageBox.Show(
            $"{kaydedilenNo} kaydedildi. Yönetime göndermek için «Onaya Gönder» kullanın.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void TalepOnayaGonder_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliTalep is null)
        {
            MessageBox.Show("Önce bir talep seçin veya «Yeni Talep» ile formu açın.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TalepFormunuModeleAktar(zorunluKalem: true))
            return;

        if (!KullaniciYetkileri.SatinalmaTalepDuzenleyebilir(_seciliTalep))
        {
            MessageBox.Show("Bu talebi yönetime gönderme yetkiniz yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if ((_seciliTalep.Teklifler?.Count ?? 0) > 0)
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
        TalepOlusturanBilgisiniTamamla(_seciliTalep!);
        SatinalmaDepo.TalepNoAtaIfNeeded(_seciliTalep!);
        SatinalmaDepo.KorunanBosTaslakId = null;
        _seciliTalep.Durum = SatinalmaTalepDurumlari.ImzaSurecinde;
        SatinalmaTalepYardimcisi.Dokun(_seciliTalep);

        try
        {
            await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(_seciliTalep);
        }
        catch (Exception ex)
        {
            SatinalmaDepo.Kaydet();
            HataGunlugu.Kaydet(ex, "TalepOnayaGonder");
            MessageBox.Show(
                "Talep yerelde kaydedildi ancak buluta gönderilemedi. İnternet bağlantısı veya hesap yetkisini kontrol edin.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        try
        {
            await BildirimYoneticisi.GecersizleriOkunduYapAsync();
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
        SatinalmaDepo.KorunanBosTaslakId = null;
        BosTaslakTaslagiTemizle();
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

        if (!KullaniciYetkileri.YonetimeYenidenGonderebilir(_seciliTalep))
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
