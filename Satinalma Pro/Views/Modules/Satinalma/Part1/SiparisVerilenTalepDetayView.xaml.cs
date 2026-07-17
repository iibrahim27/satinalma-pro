using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class SiparisVerilenTalepDetayView : UserControl
{
    private SatinalmaTalep? _talep;
    private OnaylananMalzemeSatiri? _seciliKalem;

    public event Action? Geri;
    public event Action? Degisti;

    public SiparisVerilenTalepDetayView() => InitializeComponent();

    public void Yukle(SatinalmaTalep talep)
    {
        _talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talep.Id) ?? talep;
        ArayuzuGuncelle();
    }

    private void ArayuzuGuncelle()
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        _talep = talep;
        var siparisNo = string.IsNullOrWhiteSpace(talep.SiparisNo) ? talep.TalepNo : talep.SiparisNo;
        TxtBaslik.Text = $"Sipariş — {talep.TalepNo}";
        TxtOzet.Text = $"{talep.Tarih} · {talep.TalepEden} · Sipariş No: {siparisNo}";
        TxtDurum.Text = SatinalmaPart1Filtreleri.MalKabulTamam(talep)
            ? "Tüm kalemlerin mal kabulü tamamlandı."
            : "Kalem seçip mal kabul yapın. Fazla teslimat kabul edilir; eksik kalan için «Sevkiyatı Tamamla» ile miktarları güncelleyebilirsiniz.";

        var kalemler = SatinalmaDepo.OnaylananMalzemeleriOlustur()
            .Where(s => s.TalepId == talep.Id)
            .Select(s => new SiparisKalemSatiri(s))
            .OrderBy(s => s.Malzeme)
            .ToList();

        KalemTablosu.ItemsSource = kalemler;

        var malKabulYapabilir = KullaniciYetkileri.MalKabulVeStokAktarYapabilir();
        BtnMalKabul.Visibility = malKabulYapabilir ? Visibility.Visible : Visibility.Collapsed;
        BtnSevkiyatiTamamla.Visibility = malKabulYapabilir ? Visibility.Visible : Visibility.Collapsed;
        BtnTopluMalKabul.Visibility = malKabulYapabilir ? Visibility.Visible : Visibility.Collapsed;

        if (_seciliKalem is not null && kalemler.All(k => k.Kaynak.KalemId != _seciliKalem.KalemId))
            _seciliKalem = null;

        ButonlariGuncelle();
    }

    private void ButonlariGuncelle()
    {
        var malKabulYapabilir = KullaniciYetkileri.MalKabulVeStokAktarYapabilir();
        var topluKabulYapilabilir = _seciliKalem is { } seciliKalem
            && GuncelTalep() is { } talep
            && SatinalmaDepo.OnaylananMalzemeleriOlustur()
                .Count(s => s.TalepId == talep.Id
                    && s.TeklifId == seciliKalem.TeklifId
                    && !s.SiparisTamamlandi
                    && s.KalanMiktar > 0.0001) > 1;
        BtnTopluMalKabul.IsEnabled = malKabulYapabilir && topluKabulYapilabilir;

        if (_seciliKalem is null)
        {
            BtnMalKabul.IsEnabled = false;
            BtnSevkiyatiTamamla.IsEnabled = false;
            return;
        }

        BtnMalKabul.IsEnabled = malKabulYapabilir && !_seciliKalem.SiparisTamamlandi;
        BtnSevkiyatiTamamla.IsEnabled = malKabulYapabilir
            && !_seciliKalem.SiparisTamamlandi
            && _seciliKalem.KabulEdilenMiktar > 0.0001
            && _seciliKalem.KabulEdilenMiktar < _seciliKalem.SiparisMiktari - 0.0001;
    }

    private SatinalmaTalep? GuncelTalep() =>
        _talep is null ? null : SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == _talep.Id) ?? _talep;

    private void KalemTablosu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KalemTablosu.SelectedItem is SiparisKalemSatiri satir)
            _seciliKalem = satir.Kaynak;
        else
            _seciliKalem = null;

        ButonlariGuncelle();
    }

    private void KalemTablosu_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (KalemTablosu.SelectedItem is SiparisKalemSatiri satir
            && !satir.Kaynak.SiparisTamamlandi)
            MalKabulYap();
    }

    private void MalKabul_Click(object sender, RoutedEventArgs e) => MalKabulYap();

    private void TopluMalKabul_Click(object sender, RoutedEventArgs e)
    {
        var talep = GuncelTalep();
        if (talep is null)
            return;

        if (_seciliKalem is null
            || !SatinalmaPart1Servisi.TopluMalKabulGoster(Window.GetWindow(this), talep, _seciliKalem))
            return;

        _seciliKalem = null;
        ArayuzuGuncelle();
        Degisti?.Invoke();

        if (GuncelTalep() is { } guncel && SatinalmaPart1Filtreleri.MalKabulTamam(guncel))
            Geri?.Invoke();
    }

    private void SevkiyatiTamamla_Click(object sender, RoutedEventArgs e)
    {
        if (_seciliKalem is null)
            return;

        if (!SatinalmaPart1Servisi.SevkiyatiTamamlaGoster(Window.GetWindow(this), _seciliKalem))
            return;

        _seciliKalem = null;
        ArayuzuGuncelle();
        Degisti?.Invoke();

        if (GuncelTalep() is { } talep && SatinalmaPart1Filtreleri.MalKabulTamam(talep))
            Geri?.Invoke();
    }

    private void MalKabulYap()
    {
        if (_seciliKalem is null)
            return;

        if (!SatinalmaPart1Servisi.MalKabulGoster(Window.GetWindow(this), _seciliKalem))
            return;

        _seciliKalem = null;
        ArayuzuGuncelle();
        Degisti?.Invoke();

        if (GuncelTalep() is { } talep && SatinalmaPart1Filtreleri.MalKabulTamam(talep))
            Geri?.Invoke();
    }

    private void Geri_Click(object sender, RoutedEventArgs e) => Geri?.Invoke();
}
