using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView : UserControl, IModulKlavyeKisayollari
{
    private readonly Dictionary<string, (Button Btn, Border Panel)> _sekmeler;
    private string _aktifSekme = MasaustuRolHaritasi.Panel;
    private string? _sekmePanelOverride;

    public SatinalmaView()
    {
        InitializeComponent();

        _sekmeler = new Dictionary<string, (Button, Border)>(StringComparer.Ordinal)
        {
            [MasaustuRolHaritasi.Panel] = (BtnSekmePanel, PanelDashboard),
            ["Taleplerim"] = (BtnSekmeTalepler, PanelTalepler),
            ["Gelen Talepler"] = (BtnSekmeGelenTalepler, PanelGelenTalepler),
            ["Onay Bekleyen"] = (BtnSekmeOnayBekleyen, PanelOnayBekleyen),
            ["Teklif Bekleyen"] = (BtnSekmeTeklifBekleyen, PanelTeklifBekleyen),
            ["Teklif Girişi"] = (BtnSekmeTeklifGirisi, PanelTeklifGirisi),
            ["Karşılaştırma"] = (BtnSekmeTeklifDeger, PanelTeklifDeger),
            ["Teklif Onay"] = (BtnSekmeTeklifOnay, PanelTeklifOnay),
            ["Onaylanan Talepler"] = (BtnSekmeOnaylananlar, PanelOnaylananlar),
            ["Geçmiş Talepler"] = (BtnSekmeOnayGecmisi, PanelOnayGecmisi),
            ["Geçmiş Teklifli Onaylar"] = (BtnSekmeGecmisTeklifli, PanelGecmisTeklifli),
            ["Red Talepler"] = (BtnSekmeReddedilenler, PanelReddedilenler),
            ["Alınan Malzemeler"] = (BtnSekmeAlinanMalzemeler, PanelSiparisler),
            ["Gelen Siparişler"] = (BtnSekmeGelenSiparis, PanelGelenSiparis)
        };

        Loaded += (_, _) =>
        {
            SekmeleriYetkiyeGoreAyarla();
            PanelSekmesiniHazirla();
            TalepSekmesiniHazirla();
            TeklifGirisSekmesiniHazirla();
            AkisSekmeleriniHazirla();
            SekmeyeGec(_aktifSekme);
        };
    }

    public void KisayolYenile()
    {
        if (_aktifSekme == MasaustuRolHaritasi.Panel)
            PaneliYenile();
        TalepListesiniYenile();
        TeklifGirisTalepListesiniYenile();
        AkisSekmeleriniYenile();
    }

    public void BildirimdenAc(Guid? talepId, int adim = 0, string sekme = "talepler")
    {
        var hedef = sekme switch
        {
            "teklifler" or "teklif-bekleyen" => "Teklif Bekleyen",
            "teklif-giris" => "Teklif Girişi",
            "gelen-talepler" => "Gelen Talepler",
            "teklif-onay" => "Teklif Onay",
            "onaylar" or "onaylanan" or "onaylanan-talepler" => "Onaylanan Talepler",
            "onay-bekleyen" or "bekleyen" => "Onay Bekleyen",
            "gecmis-talepler" or "onay-gecmisi" => "Geçmiş Talepler",
            "gecmis-teklifli-onaylar" => "Geçmiş Teklifli Onaylar",
            "siparisler" or "alinan-malzemeler" or "onaylanan-malzemeler" => "Alınan Malzemeler",
            "teklif-karsilastirma" or "karsilastirma" =>
                KullaniciYetkileri.YonetimOnayModu() ? "Teklif Onay" : "Karşılaştırma",
            "red" or "reddedilen" or "red-talepler" => "Red Talepler",
            _ => adim switch
            {
                1 => "Teklif Bekleyen",
                2 => "Teklif Girişi",
                3 => "Karşılaştırma",
                4 => "Onaylanan Talepler",
                _ => "Taleplerim"
            }
        };
        SekmeyeGec(hedef);
    }

    private void Sekme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string ad })
            SekmeyeGec(ad);
    }

    private void SekmeyeGec(string sekmeAdi)
    {
        sekmeAdi = SatinalmaPro.Shared.Services.MasaustuRolHaritasi.SatinalmaSekmeNormalize(sekmeAdi);

        if (!_sekmeler.ContainsKey(sekmeAdi))
            sekmeAdi = KullaniciYetkileri.SekmeGorebilir("Satınalma", MasaustuRolHaritasi.Panel)
                ? MasaustuRolHaritasi.Panel
                : "Taleplerim";

        if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", sekmeAdi))
        {
            var ilk = _sekmeler.Keys.FirstOrDefault(k =>
                KullaniciYetkileri.SekmeGorebilir("Satınalma", k));
            if (ilk is null)
                return;
            sekmeAdi = ilk;
        }

        _aktifSekme = sekmeAdi;
        var gosterilecekPanel = _sekmePanelOverride ?? sekmeAdi;
        _sekmePanelOverride = null;

        foreach (var (ad, (btn, panel)) in _sekmeler)
        {
            var aktif = ad == sekmeAdi;
            panel.Visibility = ad == gosterilecekPanel ? Visibility.Visible : Visibility.Collapsed;
            btn.Style = (Style)FindResource(aktif ? "SatSekmeBtnAktif" : "SatSekmeBtn");
        }

        if (sekmeAdi == "Teklif Bekleyen")
            TeklifBekleyenListesiniYenile();
        else if (sekmeAdi == "Onay Bekleyen")
            OnayBekleyenListesiniYenile();
        else if (sekmeAdi == "Teklif Girişi")
            TeklifGirisTalepListesiniYenile();
        else if (sekmeAdi == "Karşılaştırma")
            TeklifDegerTalepListesiniYenile();
        else if (sekmeAdi == "Gelen Talepler")
            GelenTalepListesiniYenile();
        else if (sekmeAdi == "Teklif Onay")
        {
            if (gosterilecekPanel == "Karşılaştırma")
                TeklifDegerTalepListesiniYenile();
            else
                TeklifOnayListesiniYenile();
        }
        else if (sekmeAdi == "Onaylanan Talepler")
            OnaylananTalepListesiniYenile();
        else if (sekmeAdi == "Red Talepler")
            ReddedilenListesiniYenile();
        else if (sekmeAdi == "Geçmiş Talepler")
            GecmisTalepListesiniYenile();
        else if (sekmeAdi == "Geçmiş Teklifli Onaylar")
            GecmisTeklifliListesiniYenile();
        else if (sekmeAdi == "Alınan Malzemeler")
            AlinanMalzemeListesiniYenile();
        else if (sekmeAdi == MasaustuRolHaritasi.Panel)
            PaneliYenile();
        else if (sekmeAdi == "Gelen Siparişler")
            GelenSiparisListesiniYenile();
    }

    private void SekmeleriYetkiyeGoreAyarla()
    {
        foreach (var (ad, (btn, _)) in _sekmeler)
        {
            btn.Visibility = KullaniciYetkileri.SekmeGorebilir("Satınalma", ad)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", _aktifSekme))
        {
            var ilk = _sekmeler.Keys.FirstOrDefault(k =>
                KullaniciYetkileri.SekmeGorebilir("Satınalma", k));
            if (ilk is not null)
                _aktifSekme = ilk;
        }
    }
}
