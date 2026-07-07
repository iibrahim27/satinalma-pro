using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Services.Procurement;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class OnaylananTalepListesiView : UserControl
{
    public event Action<SatinalmaTalep>? TalepSecildi;
    public event Action? Degisti;

    private string _route = SatinalmaPart1Menusu.SatinalmaOnaylanan;
    private List<OnaylananTalepListeSatiri> _tumSatirlar = [];

    public OnaylananTalepListesiView()
    {
        InitializeComponent();
        GuncelleYardimMetni();
    }

    public void Goster(string route)
    {
        _route = route;
        GuncelleYardimMetni();
        Yenile();
    }

    public void Yenile() => Yenile(_route);

    public void Yenile(string route)
    {
        _route = route;
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var ad = KullaniciYetkileri.AktifKullaniciAdi();

        _tumSatirlar = ProcurementTalepSorguServisi.Listele(route)
            .OrderByDescending(t => t.YonetimOnayTarihi)
            .ThenByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .Select(t => new OnaylananTalepListeSatiri(t, uid, ad))
            .ToList();

        TabloyuFiltrele();

        var gecmisModu = route == SatinalmaPart1Menusu.YonetimOnayGecmisi;
        BtnSiparis.Visibility = route == SatinalmaPart1Menusu.SatinalmaOnaylanan && !gecmisModu
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void GuncelleYardimMetni()
    {
        TxtYardim.Text = _route switch
        {
            SatinalmaPart1Menusu.YonetimOnayGecmisi =>
                "Yönetimin verdiği tüm onaylar (teklifsiz + teklifli). Sipariş ve mal kabul sonrası kayıtlar dahil. Detay ve PDF için çift tıklayın veya üstteki yazdır düğmelerini kullanın.",
            SatinalmaPart1Menusu.YonetimOnaylananTeklifler =>
                "Onayladığınız teklifli talepler. Detay ve PDF için çift tıklayın veya sağ tık menüsünü kullanın.",
            _ =>
                "Onaylanmış talep ve teklifler. Detay için çift tıklayın; sipariş için sağ tık → Siparişlendir."
        };
    }

    private void TabloyuFiltrele()
    {
        var arama = TxtArama.Text.Trim();
        Tablo.ItemsSource = string.IsNullOrEmpty(arama)
            ? _tumSatirlar
            : _tumSatirlar.Where(s =>
                string.Join(" ", s.TalepNo, s.OnayTuruMetni, s.OnayTarihi, s.TalepEden,
                    s.OnaylayanOzet, s.OnayliFirmaOzet, s.DurumEtiketi)
                    .Contains(arama, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void AramaDegisti(object sender, TextChangedEventArgs e) => TabloyuFiltrele();

    private SatinalmaTalep? SeciliTalep() =>
        Tablo.SelectedItem is OnaylananTalepListeSatiri satir ? satir.Talep : null;

    private void Tablo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SeciliTalep() is { } talep)
            TalepSecildi?.Invoke(talep);
    }

    private async void Siparislendir_Click(object sender, RoutedEventArgs e)
    {
        var talep = SeciliTalep();
        if (talep is null)
        {
            MessageBox.Show("Önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await SatinalmaPart1Servisi.SiparisVerAsync(talep);
        Yenile();
        Degisti?.Invoke();
    }

    private void KarsilastirmaPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = SeciliTalep();
        if (talep is null)
        {
            MessageBox.Show("Önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if ((talep.Teklifler?.Count ?? 0) == 0)
        {
            MessageBox.Show("Bu talep teklifsiz onaylı — karşılaştırma PDF yok.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaPdfOlusturucu.KarsilastirmaYazdir(talep, SatinalmaDepo.Ayarlar, yonetimFormu: true);
    }

    private void OnayPdf_Click(object sender, RoutedEventArgs e)
    {
        var talep = SeciliTalep();
        if (talep is null)
        {
            MessageBox.Show("Önce bir talep seçin.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SatinalmaPdfOlusturucu.YonetimOnayBelgesiYazdir(talep, SatinalmaDepo.Ayarlar);
    }
}
