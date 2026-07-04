using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView
{
    private SatinalmaTalep? _teklifDegerTalep;
    private SatinalmaTalep? _onaylananTalep;
    private OnaylananMalzemeSatiri? _seciliSiparisKalem;
    private Guid? _siparisTalepSecimi;
    private List<KalemOnaySatiri> _kalemOnaySatirlari = [];

    private void AkisSekmeleriniHazirla()
    {
        SatinalmaDepo.TaleplerGuncellendi += AkisSekmeleriniYenile;
        Unloaded += (_, _) => SatinalmaDepo.TaleplerGuncellendi -= AkisSekmeleriniYenile;
    }

    private void AkisSekmeleriniYenile()
    {
        var malKabulYapabilir = KullaniciYetkileri.MalKabulVeStokAktarYapabilir();
        BtnMalKabul.Visibility = malKabulYapabilir ? Visibility.Visible : Visibility.Collapsed;

        OnayBekleyenListesiniYenile();
        GelenTalepListesiniYenile();
        TeklifDegerTalepListesiniYenile();
        TeklifOnayListesiniYenile();
        OnaylananTalepListesiniYenile();
        OnayGecmisiListesiniYenile();
        OnaylananTeklifListesiniYenile();
        GecmisTalepListesiniYenile();
        GecmisTeklifliListesiniYenile();
        ReddedilenListesiniYenile();
        SiparisMalzemeListesiniYenile();
        GelenSiparisListesiniYenile();
        SekmeSayaclariniYenile();
    }

    public void TeklifDegerTalebiAc(SatinalmaTalep? talep)
    {
        if (KullaniciYetkileri.YonetimOnayModu())
        {
            _sekmePanelOverride = "Karşılaştırma";
            SekmeyeGec("Teklif Onay");
        }
        else
        {
            SekmeyeGec("Karşılaştırma");
            TeklifDegerTalepListesiniYenile();
        }

        if (talep is null)
            return;

        if (!KullaniciYetkileri.YonetimOnayModu())
            TeklifDegerTalepListesi.SelectedItem = talep;
        _teklifDegerTalep = talep;
        TeklifDegerFormuGoster(talep);
    }

    private void TeklifDegerTalepListesiniYenile()
    {
        var liste = SatinalmaDepo.Talepler
            .Where(SatinalmaTabFiltreleri.TeklifDegerlendirme)
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .ToList();

        TeklifDegerTalepListesi.ItemsSource = liste;

        if (_teklifDegerTalep is not null && liste.All(t => t.Id != _teklifDegerTalep.Id))
        {
            _teklifDegerTalep = null;
            _kalemOnaySatirlari = [];
            TeklifDegerFormuGizle();
        }
    }

    private void TeklifDegerTalepListesi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TeklifDegerTalepListesi.SelectedItem is SatinalmaTalep talep)
        {
            _teklifDegerTalep = talep;
            TeklifDegerFormuGoster(talep);
        }
    }

    private void TeklifDegerFormuGizle()
    {
        TxtTeklifDegerBos.Visibility = Visibility.Visible;
        TeklifDegerIcerik.Visibility = Visibility.Collapsed;
    }

    private void TeklifDegerFormuGoster(SatinalmaTalep talep)
    {
        TxtTeklifDegerBos.Visibility = Visibility.Collapsed;
        TeklifDegerIcerik.Visibility = Visibility.Visible;

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        foreach (var teklif in talep.Teklifler)
            teklif.FiyatlariHesapla(talep.Kalemler);

        var oneri = talep.OnerilenTeklif();
        TxtTeklifDegerOzet.Text =
            $"{talep.TalepNo} · {talep.Tarih} · {talep.TalepEden}\n" +
            (oneri is not null
                ? $"Satınalma önerisi: {oneri.FirmaAdi}"
                : "Her kalem için onaylanacak firmayı seçin.");

        TeklifDegerTeklifTablosu.ItemsSource = (talep.Teklifler ?? [])
            .Select(t => new TeklifGirisSatiri(talep, t))
            .ToList();

        _kalemOnaySatirlari = talep.Kalemler
            .OrderBy(k => k.SiraNo)
            .Select(k => new KalemOnaySatiri(k, talep.Teklifler ?? []))
            .ToList();
        KalemOnayListesi.ItemsSource = _kalemOnaySatirlari;

        var yonetimModu = KullaniciYetkileri.YonetimOnayModu();
        TeklifDegerSolListePanel.Visibility = yonetimModu ? Visibility.Collapsed : Visibility.Visible;

        var bekleyenTeklif = SatinalmaTabFiltreleri.TeklifOnay(talep);
        var saltOkunur = !bekleyenTeklif;
        KalemOnayListesi.IsEnabled = !saltOkunur;

        var geriAlGoster = !saltOkunur && FirmaOnayiGeriAlGoster(talep);
        BtnTeklifDegerOnayGeriAl.Visibility = geriAlGoster ? Visibility.Visible : Visibility.Collapsed;
        BtnTeklifDegerOnayla.Visibility = bekleyenTeklif && KullaniciYetkileri.TeklifOnayVerebilir()
            ? Visibility.Visible
            : Visibility.Collapsed;

        var yonetimKarar = bekleyenTeklif && KullaniciYetkileri.YonetimKararVerebilir();
        BtnTeklifDegerGeriGonder.Visibility = yonetimKarar ? Visibility.Visible : Visibility.Collapsed;
        BtnTeklifDegerReddet.Visibility = yonetimKarar ? Visibility.Visible : Visibility.Collapsed;

        var yonetimGonder = SatinalmaTalepYardimcisi.SatinalmaTeklifDegerlendirmede(talep)
            && !yonetimModu;
        BtnTeklifDegerYonetimeGonder.Visibility = yonetimGonder
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void TeklifDegerYonetimeGonder_Click(object sender, RoutedEventArgs e)
    {
        if (!await YonetimeTeklifDegerlendirmeyeGonderAsync(_teklifDegerTalep))
            return;

        var no = _teklifDegerTalep?.TalepNo ?? "";
        _teklifDegerTalep = null;
        _kalemOnaySatirlari = [];
        TeklifDegerTalepListesiniYenile();
        TeklifDegerFormuGizle();
        TeklifDegerTalepListesi.SelectedItem = null;
        TeklifGirisTalepListesiniYenile();
        SekmeSayaclariniYenile();

        MessageBox.Show(
            $"{no} yönetim teklif onayına gönderildi.\n\nYönetim kullanıcıları «Teklif Onay» ekranından inceleyebilir.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void TeklifDegerGeriGonder_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifDegerTalep is null)
            return;

        var gerekce = RedGerekcesiIste("Teklifleri Satınalmaya Geri Gönder", "Düzeltme notu (isteğe bağlı):", "Geri Gönder", zorunlu: false);
        if (gerekce is null)
            return;

        try
        {
            await SatinalmaYonetimIslemleri.TeklifGeriGonderAsync(_teklifDegerTalep, gerekce);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var no = _teklifDegerTalep.TalepNo;
        _teklifDegerTalep = null;
        _kalemOnaySatirlari = [];
        AkisSekmeleriniYenile();
        TeklifDegerFormuGizle();
        TeklifDegerTalepListesi.SelectedItem = null;
        MessageBox.Show($"{no} satınalmaya düzeltme için geri gönderildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void TeklifDegerReddet_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifDegerTalep is null)
            return;

        var gerekce = RedGerekcesiIste("Teklif Red", "Red gerekçesini girin:");
        if (gerekce is null)
            return;

        try
        {
            await SatinalmaYonetimIslemleri.TeklifReddetAsync(_teklifDegerTalep, gerekce);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var no = _teklifDegerTalep.TalepNo;
        _teklifDegerTalep = null;
        _kalemOnaySatirlari = [];
        AkisSekmeleriniYenile();
        TeklifDegerFormuGizle();
        TeklifDegerTalepListesi.SelectedItem = null;
        MessageBox.Show($"{no} reddedildi.", UygulamaBilgisi.Ad,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static bool FirmaOnayiGeriAlGoster(SatinalmaTalep talep) =>
        KullaniciYetkileri.SatinalmaFirmaOnayiDuzenlenebilir()
        && (talep.HerhangiKalemOnayli
            || talep.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu);

    private void TeklifDegerKarsilastirmaPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifDegerTalep is null)
            return;

        SatinalmaPdfOlusturucu.KarsilastirmaYazdir(_teklifDegerTalep, SatinalmaDepo.Ayarlar, _teklifDegerTalep.OnerilenTeklif());
    }

    private void TeklifDegerYonetimOnayPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifDegerTalep is null)
            return;

        SatinalmaPdfOlusturucu.KarsilastirmaYazdir(
            _teklifDegerTalep, SatinalmaDepo.Ayarlar, _teklifDegerTalep.OnerilenTeklif(), yonetimFormu: true);
    }

    private void OnaylananOnayPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_onaylananTalep is null)
        {
            MessageBox.Show("PDF için önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaPdfOlusturucu.YonetimOnayBelgesiYazdir(_onaylananTalep, SatinalmaDepo.Ayarlar);
    }

    private void OnaylananSiparisPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_onaylananTalep is null)
        {
            MessageBox.Show("PDF için önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaPdfOlusturucu.SiparisFormlariYazdir(_onaylananTalep, SatinalmaDepo.Ayarlar);
    }

    private void OnaylananSiparisOnayPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_onaylananTalep is null)
        {
            MessageBox.Show("PDF için önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaPdfOlusturucu.SiparisOnayFormlariYazdir(_onaylananTalep, SatinalmaDepo.Ayarlar);
    }

    private void AlinanMalzemePdf_Click(object sender, RoutedEventArgs e)
    {
        var satirlar = (SiparisMalzemeTablosu.ItemsSource as IEnumerable<SiparisKalemSatiri>)?
            .Select(s => s.Kaynak)
            .ToList() ?? [];

        if (satirlar.Count == 0)
        {
            MessageBox.Show("Yazdırılacak malzeme kalemi bulunamadı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaPdfOlusturucu.OnaylananMalzemelerYazdir(satirlar, SatinalmaDepo.Ayarlar);
    }

    private void TeklifDegerOnayla_Click(object sender, RoutedEventArgs e)
    {
        if (_teklifDegerTalep is null)
            return;

        if (!KullaniciYetkileri.TeklifOnayVerebilir())
        {
            MessageBox.Show("Teklif onay yetkiniz yok.", UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _ = TeklifDegerOnaylaAsync();
    }

    private async Task TeklifDegerOnaylaAsync()
    {
        if (_teklifDegerTalep is null)
            return;

        try
        {
            await SatinalmaSiparisIslemleri.KalemBazliOnaylaAsync(_teklifDegerTalep);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var no = _teklifDegerTalep.TalepNo;
        _teklifDegerTalep = null;
        _kalemOnaySatirlari = [];
        AkisSekmeleriniYenile();
        TeklifDegerFormuGizle();
        TeklifDegerTalepListesi.SelectedItem = null;

        if (KullaniciYetkileri.YonetimOnayModu())
        {
            SekmeyeGec("Geçmiş Teklifli Onaylar");
            MessageBox.Show($"{no} onaylandı.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SekmeyeGec("Onaylanan Talepler");
        MessageBox.Show($"{no} onaylandı ve Onaylanan Talepler sekmesine aktarıldı.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnaylananTalepListesiniYenile()
    {
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var ad = KullaniciYetkileri.AktifKullaniciAdi();

        var liste = SatinalmaDepo.Talepler
            .Where(SatinalmaTabFiltreleri.Onaylananlar)
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .Select(t => new OnaylananTalepListeSatiri(t, uid, ad))
            .ToList();

        OnaylananTalepListesi.ItemsSource = liste;
        TxtOnaylananListeBos.Visibility = liste.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_onaylananTalep is not null && liste.All(s => s.Talep.Id != _onaylananTalep.Id))
        {
            _onaylananTalep = null;
            OnaylananFormuGizle();
        }
    }

    private void OnaylananTalepListesi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OnaylananTalepListesi.SelectedItem is OnaylananTalepListeSatiri satir)
        {
            _onaylananTalep = satir.Talep;
            OnaylananFormuGoster(satir.Talep);
        }
    }

    private void OnaylananFormuGizle()
    {
        TxtOnaylananBos.Visibility = Visibility.Visible;
        OnaylananIcerik.Visibility = Visibility.Collapsed;
    }

    private void OnaylananFormuGoster(SatinalmaTalep talep)
    {
        TxtOnaylananBos.Visibility = Visibility.Collapsed;
        OnaylananIcerik.Visibility = Visibility.Visible;

        var satirlar = SatinalmaDepo.OnaylananMalzemeleriOlustur()
            .Where(s => s.TalepId == talep.Id)
            .ToList();

        TxtOnaylananOzet.Text =
            $"{talep.TalepNo} · {talep.Tarih} · {talep.TalepEden}\n" +
            $"Onaylayan: {(string.IsNullOrWhiteSpace(talep.YonetimOnaylayanAd) ? "—" : talep.YonetimOnaylayanAd)}";

        var durumEtiketi = TalepListeSatiri.DurumEtiketiOlustur(talep);
        var siparisVerildi = talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu;
        TxtOnaylananDurum.Text = durumEtiketi switch
        {
            SatinalmaPro.Shared.Helpers.SatinalmaTalepDurumEtiketi.DepoTeslimOldu =>
                "Durum: Onaylanan malzeme depoya teslim edildi.",
            SatinalmaPro.Shared.Helpers.SatinalmaTalepDurumEtiketi.Sipariste =>
                "Durum: Firmaya sipariş verildi — depo teslimi bekleniyor.",
            _ => siparisVerildi
                ? "Durum: Firmaya sipariş verildi — depo teslimi bekleniyor."
                : "Durum: Onaylandı — firmaya sipariş henüz verilmedi."
        };
        TxtOnaylananDurum.Foreground = durumEtiketi == SatinalmaPro.Shared.Helpers.SatinalmaTalepDurumEtiketi.DepoTeslimOldu
            ? (System.Windows.Media.Brush)FindResource("InkBrush")
            : (System.Windows.Media.Brush)FindResource("InkMutedBrush");

        var islemYapabilir = !KullaniciYetkileri.SatinalmaSurecTakipModu();
        BtnSiparisVer.Visibility = islemYapabilir && !siparisVerildi
            ? Visibility.Visible
            : Visibility.Collapsed;

        var bekleyenKalem = satirlar.Any(s => !s.SiparisTamamlandi && s.KalanMiktar > 0.0001);
        var malKabulYapabilir = KullaniciYetkileri.MalKabulVeStokAktarYapabilir();
        BtnOnaylananMalKabul.Visibility = islemYapabilir && malKabulYapabilir && siparisVerildi && bekleyenKalem
            ? Visibility.Visible
            : Visibility.Collapsed;

        BtnOnaylananOnayGeriAl.Visibility = FirmaOnayiGeriAlGoster(talep)
            ? Visibility.Visible
            : Visibility.Collapsed;

        OnaylananKalemTablosu.ItemsSource = satirlar.Select(s => new OnaylananKalemSatiri
        {
            Malzeme = s.Malzeme,
            MiktarMetni = $"{s.SiparisMiktari:G} {s.Birim}",
            Firma = s.Firma,
            SiparisNo = string.IsNullOrWhiteSpace(s.SiparisNo) ? "—" : s.SiparisNo,
            TeslimDurumu = TeslimDurumuMetni(s)
        }).ToList();
    }

    private static string TeslimDurumuMetni(OnaylananMalzemeSatiri satir) =>
        satir.KabulDurumu switch
        {
            "Tamamlandı" => "Depo teslim",
            "Kısmi" => $"Kısmi ({satir.KabulEdilenMiktar:G}/{satir.SiparisMiktari:G})",
            _ => satir.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu
                ? "Siparişte"
                : "Onaylı — sipariş bekliyor"
        };

    private async void SiparisVer_Click(object sender, RoutedEventArgs e)
    {
        if (_onaylananTalep is null)
            return;

        var onay = MessageBox.Show(
            "Seçili talep için firmaya sipariş verilsin mi?",
            UygulamaBilgisi.Ad, MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (onay != MessageBoxResult.Yes)
            return;

        try
        {
            await SatinalmaSiparisIslemleri.SiparisVerAsync(_onaylananTalep);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var no = _onaylananTalep.TalepNo;
        var talepId = _onaylananTalep.Id;
        AkisSekmeleriniYenile();
        OnaylananTalepListesi.SelectedItem = OnaylananTalepListesi.Items
            .Cast<OnaylananTalepListeSatiri>()
            .FirstOrDefault(s => s.Talep.Id == talepId);

        _siparisTalepSecimi = talepId;
        SekmeyeGec("Alınan Malzemeler");
        MessageBox.Show($"{no} için sipariş verildi.\nMalzeme geldiğinde Alınan Malzemeler sekmesinden mal kabul yapın.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnaylananMalKabul_Click(object sender, RoutedEventArgs e)
    {
        if (_onaylananTalep is null)
            return;

        _siparisTalepSecimi = _onaylananTalep.Id;
        SekmeyeGec("Alınan Malzemeler");
    }

    private void ReddedilenListesiniYenile()
    {
        ReddedilenTablosu.ItemsSource = SatinalmaDepo.Talepler
            .Where(SatinalmaTabFiltreleri.Reddedilenler)
            .OrderByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .ToList();
    }

    private void ReddedilenTablosu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReddedilenTablosu.SelectedItem is not SatinalmaTalep talep)
            return;

        TalepOnizlemePenceresiniAc(talep);
    }

    private void AlinanMalzemeListesiniYenile() => SiparisMalzemeListesiniYenile();

    private void SiparisMalzemeListesiniYenile()
    {
        var takipModu = KullaniciYetkileri.SatinalmaSurecTakipModu()
                        && !KullaniciYetkileri.MalKabulVeStokAktarYapabilir();
        var kaynak = SatinalmaDepo.OnaylananMalzemeleriOlustur();
        if (!takipModu)
            kaynak = kaynak.Where(SatinalmaTabFiltreleri.SiparisBekleyenMalzeme).ToList();

        var liste = kaynak
            .Select(s => new SiparisKalemSatiri(s))
            .OrderByDescending(s => s.SiparisNo)
            .ThenBy(s => s.Malzeme)
            .ToList();

        SiparisMalzemeTablosu.ItemsSource = liste;
        BtnMalKabul.Visibility = takipModu ? Visibility.Collapsed : Visibility.Visible;

        if (_siparisTalepSecimi is { } talepId)
        {
            var hedef = liste.FirstOrDefault(s => s.Kaynak.TalepId == talepId);
            if (hedef is not null)
                SiparisMalzemeTablosu.SelectedItem = hedef;
            _siparisTalepSecimi = null;
        }

        if (_seciliSiparisKalem is not null
            && liste.All(s => s.Kaynak.KalemId != _seciliSiparisKalem.KalemId))
        {
            _seciliSiparisKalem = null;
            BtnMalKabul.IsEnabled = false;
        }
    }

    private void SiparisMalzemeTablosu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var malKabulYapabilir = KullaniciYetkileri.MalKabulVeStokAktarYapabilir();
        if (SiparisMalzemeTablosu.SelectedItem is SiparisKalemSatiri satir)
        {
            _seciliSiparisKalem = satir.Kaynak;
            BtnMalKabul.IsEnabled = malKabulYapabilir && satir.Kaynak.KalanMiktar > 0.0001;
        }
        else
        {
            _seciliSiparisKalem = null;
            BtnMalKabul.IsEnabled = false;
        }
    }

    private void SiparisMalzemeTablosu_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SiparisMalzemeTablosu.SelectedItem is SiparisKalemSatiri satir
            && satir.Kaynak.KalanMiktar > 0.0001)
            MalKabulGoster();
    }

    private void MalKabul_Click(object sender, RoutedEventArgs e) => MalKabulGoster();

    private void MalKabulGoster()
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
        {
            MessageBox.Show("Mal kabul işlemi yalnızca Satınalma rolü tarafından yapılabilir.",
                UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_seciliSiparisKalem is null)
            return;

        var pencere = new AlinanMalzemeAktarWindow(_seciliSiparisKalem, _seciliSiparisKalem.KalanMiktar, miktarDuzenlenebilir: true)
        {
            Owner = Window.GetWindow(this)
        };

        if (pencere.ShowDialog() != true)
            return;

        try
        {
            SatinalmaSiparisIslemleri.MalKabulVeDepoyaKaydet(
                _seciliSiparisKalem,
                pencere.GirilenMiktar,
                pencere.SecilenKategori,
                pencere.GirilenTarih,
                pencere.GirilenFisNo,
                pencere.GirilenTeslimAlan,
                pencere.GirilenIndirildigiSaha,
                pencere.GirilenAciklama);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _seciliSiparisKalem = null;
        AkisSekmeleriniYenile();
        BtnMalKabul.IsEnabled = false;

        MessageBox.Show(
            "Malzeme Alınan Malzemeler modülüne ve depo stoğuna kaydedildi.\nGelen Siparişler sekmesinde görüntüleyebilirsiniz.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void GelenSiparisListesiniYenile()
    {
        GelenSiparisTablosu.ItemsSource = SatinalmaDepo.OnaylananMalzemeleriOlustur()
            .Where(SatinalmaTabFiltreleri.GelenSiparisMalzeme)
            .Select(s => new SiparisKalemSatiri(s))
            .OrderByDescending(s => s.Tarih)
            .ThenByDescending(s => s.SiparisNo)
            .ToList();
    }
}
