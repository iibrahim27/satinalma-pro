using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Services.Procurement;
using SatinalmaPro.Shared.Procurement;
using SatinalmaPro.Shared.Procurement.Detail;

namespace SatinalmaPro.Views.Modules.Satinalma.Part1;

public partial class TalepListesiView : UserControl
{
    public event Action<SatinalmaTalep>? TalepSecildi;

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    private string _route = SatinalmaPart1Menusu.SatinalmaTalepler;
    private List<TalepListeSatiriPart1> _tumSatirlar = [];
    private List<TalepListeSatiriPart1> _filtreli = [];
    private string _sekme = "tumu";
    private int _sayfa;
    private int _sayfaBoyutu = 10;

    public TalepListesiView()
    {
        InitializeComponent();
    }

    public void Goster(string route)
    {
        _route = route;
        var teklifInceleme = SatinalmaPart1Menusu.YonetimTeklifIncelemeRoute(route);
        OzetKartlar.Visibility = teklifInceleme ? Visibility.Visible : Visibility.Collapsed;

        TxtListeBaslik.Text = teklifInceleme ? "İncelenecek Talepler" : SatinalmaPart1Menusu.Baslik(route).baslik;
        TxtYardim.Text = route switch
        {
            SatinalmaRoutes.YonetimTeklifGirilen => "Onay, red veya revize için bir talep seçin.",
            SatinalmaRoutes.YonetimGelenTalepler => "Satır seçerek detay açın; işlem için çift tıklayın veya İncele’ye basın.",
            SatinalmaRoutes.SatinalmaTeklifIstenen => "Teklif girmek için satıra çift tıklayın.",
            SatinalmaRoutes.SatinalmaTeklifGirilen => "Teklif girmek için satıra çift tıklayın.",
            SatinalmaRoutes.SatinalmaKarsilastirma => "Teklifleri karşılaştırmak için satıra çift tıklayın.",
            _ => "Detay için satırı açın veya çift tıklayın."
        };

        _sayfa = 0;
        _sekme = "tumu";
        SekmeAktifGuncelle();
        Yenile();
    }

    public void Yenile() => _ = YenileAsync();

    private async Task YenileAsync()
    {
        if (DesktopRoleTabManager.GetDataFilter(
                _route,
                OturumYoneticisi.AktifKullanici?.Rol,
                OturumYoneticisi.AktifKullanici?.Uid) is null)
        {
            _tumSatirlar = [];
            UygulaFiltreVeSayfa();
            return;
        }

        var liste = await ProcurementTalepSorguServisi.ListeleAsync(_route);
        _tumSatirlar = liste.Select(t => new TalepListeSatiriPart1(t)).ToList();
        UygulaFiltreVeSayfa();
    }

    private void UygulaFiltreVeSayfa()
    {
        var q = (TxtArama.Text ?? "").Trim();
        IEnumerable<TalepListeSatiriPart1> kaynak = _tumSatirlar;

        if (!string.IsNullOrWhiteSpace(q))
        {
            kaynak = kaynak.Where(s =>
                s.TalepNo.Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.TalepEden.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (s.Talep.SantiyeAdi?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (s.Talep.Teklifler?.Any(t =>
                    t.FirmaAdi?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ?? false));
        }

        kaynak = _sekme switch
        {
            "acil" => kaynak.Where(s => s.AcilMi),
            "hafta" => kaynak.Where(s => s.BuHaftaMi),
            _ => kaynak
        };

        _filtreli = kaynak.ToList();
        if (_sayfa * _sayfaBoyutu >= _filtreli.Count && _sayfa > 0)
            _sayfa = Math.Max(0, (_filtreli.Count - 1) / _sayfaBoyutu);

        Tablo.ItemsSource = _filtreli
            .Skip(_sayfa * _sayfaBoyutu)
            .Take(_sayfaBoyutu)
            .ToList();

        OzetleriGuncelle();
        SayfalamaMetniniGuncelle();
        SekmeSayilariniGuncelle();
        TxtAramaPlaceholder.Visibility = string.IsNullOrEmpty(TxtArama.Text)
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OzetleriGuncelle()
    {
        if (OzetKartlar.Visibility != Visibility.Visible) return;
        TxtKpiOnay.Text = _tumSatirlar.Count.ToString(Tr);
        TxtKpiBugun.Text = _tumSatirlar.Count(s => s.BugunMu).ToString(Tr);
        TxtKpiRevize.Text = _tumSatirlar.Count(s => s.RevizeBekliyorMu).ToString(Tr);
        var toplam = _tumSatirlar.Sum(s => s.TahminiTutar);
        TxtKpiTutar.Text = toplam <= 0 ? "—"
            : toplam >= 1_000_000m ? $"₺{(toplam / 1_000_000m).ToString("0.##", Tr)} Mn"
            : toplam >= 1_000m ? $"₺{(toplam / 1_000m).ToString("0.#", Tr)} B"
            : toplam.ToString("C0", Tr);
    }

    private void SayfalamaMetniniGuncelle()
    {
        var toplam = _filtreli.Count;
        if (toplam == 0)
        {
            TxtSayfalamaBilgi.Text = "0 kayıt";
            TxtSayfaNo.Text = "1";
            return;
        }

        var bas = _sayfa * _sayfaBoyutu + 1;
        var bit = Math.Min(toplam, (_sayfa + 1) * _sayfaBoyutu);
        TxtSayfalamaBilgi.Text = $"{toplam} kayıttan {bas}–{bit} arası";
        TxtSayfaNo.Text = (_sayfa + 1).ToString(Tr);
    }

    private void SekmeSayilariniGuncelle()
    {
        BtnFiltreTumu.Content = $"Tümü ({_tumSatirlar.Count})";
        BtnFiltreAcil.Content = $"Acil ({_tumSatirlar.Count(s => s.AcilMi)})";
        BtnFiltreHafta.Content = $"Bu Hafta ({_tumSatirlar.Count(s => s.BuHaftaMi)})";
    }

    private void SekmeAktifGuncelle()
    {
        BtnFiltreTumu.Tag = _sekme == "tumu" ? "Active" : null;
        BtnFiltreAcil.Tag = _sekme == "acil" ? "Active" : null;
        BtnFiltreHafta.Tag = _sekme == "hafta" ? "Active" : null;
    }

    private void TxtArama_TextChanged(object sender, TextChangedEventArgs e)
    {
        _sayfa = 0;
        UygulaFiltreVeSayfa();
    }

    private void BtnFiltre_Click(object sender, RoutedEventArgs e)
    {
        TxtArama.Text = "";
        _sekme = "tumu";
        SekmeAktifGuncelle();
        _sayfa = 0;
        UygulaFiltreVeSayfa();
    }

    private void FiltreTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender == BtnFiltreAcil) _sekme = "acil";
        else if (sender == BtnFiltreHafta) _sekme = "hafta";
        else _sekme = "tumu";
        SekmeAktifGuncelle();
        _sayfa = 0;
        UygulaFiltreVeSayfa();
    }

    private void CmbSayfaBoyutu_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (CmbSayfaBoyutu.SelectedItem is ComboBoxItem { Tag: string t }
            && int.TryParse(t, out var n) && n > 0)
        {
            _sayfaBoyutu = n;
            _sayfa = 0;
            UygulaFiltreVeSayfa();
        }
    }

    private void BtnIlkSayfa_Click(object sender, RoutedEventArgs e) { _sayfa = 0; UygulaFiltreVeSayfa(); }
    private void BtnOnceki_Click(object sender, RoutedEventArgs e) { if (_sayfa > 0) _sayfa--; UygulaFiltreVeSayfa(); }
    private void BtnSonraki_Click(object sender, RoutedEventArgs e)
    {
        var max = Math.Max(0, (_filtreli.Count - 1) / _sayfaBoyutu);
        if (_sayfa < max) _sayfa++;
        UygulaFiltreVeSayfa();
    }
    private void BtnSonSayfa_Click(object sender, RoutedEventArgs e)
    {
        _sayfa = Math.Max(0, (_filtreli.Count - 1) / _sayfaBoyutu);
        UygulaFiltreVeSayfa();
    }

    private void Tablo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Tablo.SelectedItem is TalepListeSatiriPart1 satir)
            TalepSecildi?.Invoke(satir.Talep);
    }

    private void Tablo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Tablo.SelectedItem is TalepListeSatiriPart1 secili)
        {
            foreach (var s in _filtreli)
                s.Acik = ReferenceEquals(s, secili);
        }
    }

    private void BtnSatirAc_Click(object sender, RoutedEventArgs e)
    {
        if (SatirAl(sender) is not { } satir) return;
        var yeni = !satir.Acik;
        foreach (var s in _filtreli) s.Acik = false;
        satir.Acik = yeni;
        Tablo.SelectedItem = satir;
    }

    private void BtnIncele_Click(object sender, RoutedEventArgs e)
    {
        if (SatirAl(sender) is { } satir)
            TalepSecildi?.Invoke(satir.Talep);
    }

    private void BtnOnayla_Click(object sender, RoutedEventArgs e)
    {
        if (SatirAl(sender) is not { } satir) return;
        if (YonetimTeklifIncelemeWindow.Goster(Window.GetWindow(this), satir.Talep))
            Yenile();
    }

    private async void BtnRevize_Click(object sender, RoutedEventArgs e)
    {
        if (SatirAl(sender) is not { } satir) return;
        var not = MetinGirisDialog.Goster(
            Window.GetWindow(this),
            "Teklifleri Revizeye Gönder",
            "Düzeltme notu (quoteCorrectionNote):");
        if (not is null) return;

        try
        {
            await PurchaseRequestDetailServisi.UygulaAsync(
                satir.Talep,
                PurchaseRequestDetailAction.SendQuotesForRevision,
                OturumYoneticisi.AktifKullanici?.Rol,
                not: not);
            MessageBox.Show("Teklifler revizeye gönderildi.", UygulamaBilgisi.Ad,
                MessageBoxButton.OK, MessageBoxImage.Information);
            Yenile();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UygulamaBilgisi.Ad, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static TalepListeSatiriPart1? SatirAl(object sender) =>
        sender is FrameworkElement { Tag: TalepListeSatiriPart1 satir } ? satir : null;
}
