using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaIsAkisi = SatinalmaPro.Shared.Helpers.SatinalmaIsAkisi;
using UygulamaBilgisi = SatinalmaPro.Helpers.UygulamaBilgisi;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public enum TeklifGirisModu
{
    TeklifIstenen,
    Karsilastirma,
    YonetimeGonderildi
}

public partial class TeklifGirisView : UserControl
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private SatinalmaTalep? _talep;
    private SatinalmaTeklif? _seciliTeklif;
    private TeklifGirisModu _mod;
    private List<KalemOnaySatiri> _kalemOnaySatirlari = [];
    private List<KalemOneriSatiri> _kalemOneriSatirlari = [];

    public event Action? Geri;
    public event Action? Degisti;
    /// <summary>Kayıt veya gönderim sonrası doğru listeye yönlendirme.</summary>
    public event Action<string>? Yonlendir;

    public TeklifGirisView() => InitializeComponent();

    public void Yukle(SatinalmaTalep talep, TeklifGirisModu mod)
    {
        if (!KullaniciRolleri.SatinalmaTeklifGirebilir(OturumYoneticisi.AktifKullanici?.Rol))
        {
            MessageBox.Show(
                "Teklif girişi yalnızca satınalma yetkisine açıktır.",
                UygulamaBilgisi.Ad,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Geri?.Invoke();
            return;
        }

        _talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;
        _mod = mod;
        _seciliTeklif = null;
        ArayuzuGuncelle();
    }

    private SatinalmaTalep? GuncelTalep() =>
        _talep is null ? null : SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == _talep.Id) ?? _talep;

    private void ArayuzuGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        _talep = talep;
        TxtBaslik.Text = $"Teklif Girişi — {talep.TalepNo}";
        TxtOzet.Text = $"{talep.Tarih} · {talep.TalepEden} · {SatinalmaPart1DurumEtiketi.TeklifDurumu(talep)}";

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        KalemTablosu.ItemsSource = talep.Kalemler.OrderBy(k => k.SiraNo).ToList();
        TeklifListesiniYenile();
        SatinalmaOnerisiniGuncelle();

        if (!string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu))
        {
            TxtDuzeltmeNotu.Text = $"Yönetim düzeltme notu: {talep.TeklifDuzeltmeNotu}";
            DuzeltmeNotuPanel.Visibility = Visibility.Visible;
        }
        else
        {
            DuzeltmeNotuPanel.Visibility = Visibility.Collapsed;
        }

        BtnKarsilastirmaPdf.Visibility = _mod is TeklifGirisModu.Karsilastirma or TeklifGirisModu.YonetimeGonderildi
            ? Visibility.Visible
            : Visibility.Collapsed;

        BtnOnayaGonder.Content = _mod == TeklifGirisModu.YonetimeGonderildi
            ? "Yeniden Yönetime Gönder"
            : "Onaya Gönder";

        BtnOnayaGonder.IsEnabled = SatinalmaTeklifDegerlendirmeYardimcisi.YonetimeTeklifGonderilebilir(talep)
            || (_mod == TeklifGirisModu.YonetimeGonderildi && KullaniciYetkileri.YonetimeYenidenGonderebilir(talep));
        BtnOnayaGonder.ToolTip = SatinalmaTeklifDegerlendirmeYardimcisi.YonetimeGonderEngelMesaji(talep)
            ?? "Teklifleri yönetim onayına gönderir";

        KalemOneriPaneliniGuncelle();
        KalemOnayPaneliniGuncelle();
    }

    private void KalemOneriPaneliniGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        var goster = SatinalmaTalepYardimcisi.GercekTeklifVar(talep)
            && !SatinalmaPart1OnayYardimcisi.TeklifOnaylanabilir(talep)
            && !talep.YonetimOnayKilitli
            && !talep.HerhangiKalemOnayli;

        KalemOneriPanel.Visibility = goster ? Visibility.Visible : Visibility.Collapsed;

        if (!goster)
        {
            _kalemOneriSatirlari = [];
            KalemOneriListesi.ItemsSource = null;
            return;
        }

        foreach (var teklif in talep.Teklifler ?? [])
            teklif.FiyatlariHesapla(talep.Kalemler);

        _kalemOneriSatirlari = talep.Kalemler
            .OrderBy(k => k.SiraNo)
            .Select(k => new KalemOneriSatiri(k, talep)
            {
                Degisti = () =>
                {
                    SatinalmaTalepYardimcisi.Dokun(talep);
                    SatinalmaOnerisiniGuncelle();
                    TeklifListesiniYenile();
                }
            })
            .ToList();
        KalemOneriListesi.ItemsSource = _kalemOneriSatirlari;
    }

    private void KalemOnayPaneliniGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        var onaylanabilir = SatinalmaPart1OnayYardimcisi.TeklifOnaylanabilir(talep);
        KalemOnayPanel.Visibility = onaylanabilir ? Visibility.Visible : Visibility.Collapsed;
        BtnTeklifOnayla.Visibility = onaylanabilir ? Visibility.Visible : Visibility.Collapsed;

        if (!onaylanabilir)
        {
            _kalemOnaySatirlari = [];
            KalemOnayListesi.ItemsSource = null;
            return;
        }

        _kalemOnaySatirlari = talep.Kalemler
            .OrderBy(k => k.SiraNo)
            .Select(k => new KalemOnaySatiri(k, talep.Teklifler ?? []))
            .ToList();
        KalemOnayListesi.ItemsSource = _kalemOnaySatirlari;
        KalemOnayListesi.IsEnabled = true;
    }

    private void TeklifListesiniYenile()
    {
        if (_talep is null)
            return;

        foreach (var teklif in _talep.Teklifler ?? [])
            teklif.FiyatlariHesapla(_talep.Kalemler);

        TeklifTablosu.ItemsSource = (_talep.Teklifler ?? [])
            .Select(t => new TeklifGirisSatiri(_talep, t))
            .ToList();
    }

    private void SatinalmaOnerisiniGuncelle()
    {
        if (_talep is null)
        {
            TxtSatinalmaOnerisi.Text = "";
            return;
        }

        TxtSatinalmaOnerisi.Text = SatinalmaOneriYardimcisi.OneriMetni(_talep);
    }

    private bool TeklifEklenebilirMi()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return false;

        var k = OturumYoneticisi.AktifKullanici;
        if (SatinalmaIsAkisi.TeklifEklenebilir(
                talep.Durum,
                talep.TalepTuru,
                talep.YonetimOnayKilitli,
                talep.OlusturanRol,
                k?.Rol,
                k?.Aktif ?? false))
            return true;

        MessageBox.Show(
            SatinalmaIsAkisi.TeklifEklemeEngelMesaji(
                talep.Durum,
                talep.TalepTuru,
                talep.YonetimOnayKilitli,
                talep.OlusturanRol,
                k?.Rol),
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private bool TeklifPenceresiAc(SatinalmaTeklif teklif)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return false;

        return SatinalmaPart1Servisi.TeklifDuzenlePenceresiAc(
            Window.GetWindow(this) ?? Application.Current.MainWindow!,
            teklif,
            talep.Kalemler);
    }

    private void YeniTeklif_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null || !TeklifEklenebilirMi())
            return;

        var teklif = new SatinalmaTeklif();
        SatinalmaDepo.TeklifFiyatlariniHazirla(talep, teklif);
        if (!TeklifPenceresiAc(teklif))
            return;

        talep.Teklifler ??= [];
        var ayniFirma = talep.Teklifler.FirstOrDefault(t =>
            !string.IsNullOrWhiteSpace(t.FirmaAdi)
            && string.Equals(t.FirmaAdi.Trim(), teklif.FirmaAdi.Trim(), StringComparison.OrdinalIgnoreCase));

        if (ayniFirma is not null)
        {
            var secim = MessageBox.Show(
                $"«{teklif.FirmaAdi}» için zaten bir teklif var.\n\nMevcut teklifi güncellemek ister misiniz?\n\nEvet = güncelle · Hayır = yeni teklif olarak ekle · İptal = vazgeç",
                UygulamaBilgisi.Ad,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (secim == MessageBoxResult.Cancel)
                return;

            if (secim == MessageBoxResult.Yes)
            {
                ayniFirma.FirmaAdi = teklif.FirmaAdi;
                ayniFirma.UsdKuru = teklif.UsdKuru;
                ayniFirma.EurKuru = teklif.EurKuru;
                ayniFirma.VadeGunu = teklif.VadeGunu;
                ayniFirma.TeslimSuresi = teklif.TeslimSuresi;
                ayniFirma.OdemeSekli = teklif.OdemeSekli;
                ayniFirma.Aciklama = teklif.Aciklama;
                ayniFirma.KdvOrani = teklif.KdvOrani;
                ayniFirma.Fiyatlar = teklif.Fiyatlar;
                ayniFirma.FiyatlariHesapla(talep.Kalemler);
                _seciliTeklif = ayniFirma;
            }
            else
            {
                talep.Teklifler.Add(teklif);
                _seciliTeklif = teklif;
            }
        }
        else
        {
            talep.Teklifler.Add(teklif);
            _seciliTeklif = teklif;
        }

        _talep = talep;
        SatinalmaDepo.TeklifDegisikligiIsle(talep);
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);
        TeklifListesiniYenile();
        SatinalmaOnerisiniGuncelle();
        KalemOneriPaneliniGuncelle();
        ArayuzuGuncelle();
        Degisti?.Invoke();
    }

    private void Duzenle_Click(object sender, RoutedEventArgs e) => TeklifDuzenle();

    private void TeklifTablosu_MouseDoubleClick(object sender, MouseButtonEventArgs e) => TeklifDuzenle();

    private void TeklifDuzenle()
    {
        var talep = GuncelTalep();
        if (talep is null || _seciliTeklif is null)
        {
            MessageBox.Show("Düzenlemek için bir teklif seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TeklifPenceresiAc(_seciliTeklif))
            return;

        SatinalmaDepo.TeklifDegisikligiIsle(talep);
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);
        TeklifListesiniYenile();
        SatinalmaOnerisiniGuncelle();
        Degisti?.Invoke();
    }

    private void Sil_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null || _seciliTeklif is null)
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

        talep.Teklifler.Remove(_seciliTeklif);
        SatinalmaOneriYardimcisi.TeklifSilindi(talep, _seciliTeklif.Id);

        _seciliTeklif = null;
        SatinalmaDepo.TeklifDegisikligiIsle(talep);
        _ = SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);
        TeklifListesiniYenile();
        SatinalmaOnerisiniGuncelle();
        Degisti?.Invoke();
    }

    private void KalemOneriTemizle_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        foreach (var kalem in talep.Kalemler)
            kalem.OnerilenTeklifId = null;

        talep.SatinalmaKalemOnerisiElleSecildi = false;
        if (talep.YonetimOnerilenTeklifId is null)
            talep.SatinalmaOnerisiElleSecildi = false;

        SatinalmaTalepYardimcisi.Dokun(talep);
        KalemOneriPaneliniGuncelle();
        SatinalmaOnerisiniGuncelle();
        TeklifListesiniYenile();
        ArayuzuGuncelle();
    }

    private void Oneri_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null || _seciliTeklif is null)
        {
            MessageBox.Show("Öneri için listeden bir firma seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaOneriYardimcisi.FirmaOnerisiAyarla(talep, _seciliTeklif.Id);
        SatinalmaPro.Helpers.SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaOnerisiniGuncelle();
        KalemOneriPaneliniGuncelle();
        TeklifListesiniYenile();
    }

    private void TedarikciPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        SatinalmaPdfOlusturucu.TedarikciTeklifTalebiYazdir(talep, SatinalmaDepo.Ayarlar);
    }

    private void KarsilastirmaPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        if ((talep.Teklifler?.Count ?? 0) == 0)
        {
            MessageBox.Show("Karşılaştırma için en az bir teklif girin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SatinalmaPdfOlusturucu.KarsilastirmaYazdir(talep, SatinalmaDepo.Ayarlar);
    }

    private async void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        foreach (var teklif in talep.Teklifler ?? [])
            teklif.FiyatlariHesapla(talep.Kalemler);

        if ((talep.Teklifler?.Count ?? 0) > 0
            && talep.Durum is SatinalmaTalepDurumlari.TeklifGirisi
                or SatinalmaTalepDurumlari.Hazirlaniyor
                or SatinalmaTalepDurumlari.ImzaSurecinde)
        {
            talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
            talep.Status = SatinalmaPro.Shared.Procurement.ProcurementStatus.Comparison;
        }

        SatinalmaDepo.TeklifDegisikligiIsle(talep);
        await SatinalmaKayitYardimcisi.KaydetVeBulutaGonderAsync(talep);

        MessageBox.Show(
            $"{talep.TalepNo} kaydedildi.\n\nTeklifler «Karşılaştırma» sekmesine taşındı. Buradan «Onaya Gönder» ile yönetime iletebilirsiniz.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);

        Degisti?.Invoke();
        Yonlendir?.Invoke(SatinalmaPart1Menusu.SatinalmaKarsilastirma);
    }

    private async void OnayaGonder_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        if (!BtnOnayaGonder.IsEnabled)
        {
            var engel = SatinalmaTeklifDegerlendirmeYardimcisi.YonetimeGonderEngelMesaji(talep);
            if (!string.IsNullOrWhiteSpace(engel))
            {
                MessageBox.Show(engel, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return;
        }

        if (!await SatinalmaTeklifDegerlendirmeYardimcisi.YonetimeGonderAsync(talep))
            return;

        MessageBox.Show(
            $"{talep.TalepNo} yönetime gönderildi.\n\nSatınalma: «Teklif Girilen» · Yönetim: «Teklif Girilen Talepler» sekmesinden görüntüleyebilirsiniz.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);

        Degisti?.Invoke();
        Yonlendir?.Invoke(SatinalmaPart1Menusu.SatinalmaTeklifGirilen);
    }

    private async void TeklifOnayla_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        if (!SatinalmaPart1OnayYardimcisi.TeklifOnaylanabilir(talep))
        {
            MessageBox.Show("Bu talep için teklif onayı verilemiyor.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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

        MessageBox.Show(
            $"{talep.TalepNo} onaylandı. «Onaylanan Teklifler ve Talepler» sekmesinden devam edebilirsiniz.",
            UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Information);

        Degisti?.Invoke();
        Geri?.Invoke();
    }

    private void TeklifTablosu_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        _seciliTeklif = (TeklifTablosu.SelectedItem as TeklifGirisSatiri)?.Teklif;

    private void Geri_Click(object sender, RoutedEventArgs e) => Geri?.Invoke();
}
