using System.Windows;
using System.Windows.Controls;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView : UserControl, IModulKlavyeKisayollari
{
    private readonly Dictionary<string, Border> _sekmePanelleri;
    private string _aktifSekme = MasaustuRolHaritasi.Taleplerim;
    private string? _sekmePanelOverride;

    public SatinalmaView()
    {
        InitializeComponent();

        _sekmePanelleri = new Dictionary<string, Border>(StringComparer.Ordinal)
        {
            ["Taleplerim"] = PanelTalepler,
            ["Gelen Talepler"] = PanelGelenTalepler,
            ["Onay Bekleyen"] = PanelOnayBekleyen,
            ["Teklif Bekleyen"] = PanelTeklifBekleyen,
            ["Teklif Girişi"] = PanelTeklifGirisi,
            ["Karşılaştırma"] = PanelTeklifDeger,
            ["Teklif Onay"] = PanelTeklifOnay,
            ["Onaylanan Talepler"] = PanelOnaylananlar,
            ["Geçmiş Talepler"] = PanelOnayGecmisi,
            ["Geçmiş Teklifli Onaylar"] = PanelGecmisTeklifli,
            ["Red Talepler"] = PanelReddedilenler,
            ["Alınan Malzemeler"] = PanelSiparisler,
            ["Gelen Siparişler"] = PanelGelenSiparis
        };

        Loaded += (_, _) =>
        {
            SekmeleriYetkiyeGoreAyarla();
            SekmeSayaclariniHazirla();
            TalepSekmesiniHazirla();
            TeklifGirisSekmesiniHazirla();
            AkisSekmeleriniHazirla();
            MenuGoster();
        };
    }

    public void KisayolYenile()
    {
        TalepListesiniYenile();
        TeklifGirisTalepListesiniYenile();
        AkisSekmeleriniYenile();
        SekmeSayaclariniYenile();
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
        BildirimTalebiniAc(talepId, hedef);
    }

    private void BildirimTalebiniAc(Guid? talepId, string sekmeAdi)
    {
        if (talepId is not { } id)
            return;

        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == id);
        if (talep is null)
            return;

        switch (sekmeAdi)
        {
            case "Teklif Girişi":
                TeklifGirisTalebiAc(talep);
                break;
            case "Karşılaştırma":
            case "Teklif Onay":
                _teklifOnaySecili = talep;
                TeklifOnayListesi.SelectedItem = talep;
                TeklifDegerTalebiAc(talep);
                break;
            case "Gelen Talepler":
                _gelenTalepSecili = talep;
                GelenTalepTablosu.SelectedItem = talep;
                GelenTalepButonlariniGuncelle();
                break;
            case "Onaylanan Talepler":
                var onaySatir = OnaylananTalepListesi.Items.Cast<OnaylananTalepListeSatiri>()
                    .FirstOrDefault(s => s.Talep.Id == id);
                if (onaySatir is not null)
                {
                    OnaylananTalepListesi.SelectedItem = onaySatir;
                    _onaylananTalep = talep;
                    OnaylananFormuGoster(talep);
                }
                break;
            case "Red Talepler":
                ReddedilenTablosu.SelectedItem = talep;
                break;
            case "Taleplerim":
                var satir = TalepListesi.Items.Cast<TalepListeSatiri>()
                    .FirstOrDefault(s => s.Talep.Id == id);
                if (satir is not null)
                {
                    TalepListesi.SelectedItem = satir;
                    _seciliTalep = talep;
                    if (satir.Duzenlenebilir)
                    {
                        _talepFormModu = true;
                        TalepFormuGoster(talep);
                    }
                    else
                        TalepOnizlemePenceresiniAc(talep);
                }
                break;
        }
    }

    private void SekmeyeGec(string sekmeAdi)
    {
        sekmeAdi = MasaustuRolHaritasi.SatinalmaSekmeNormalize(sekmeAdi);

        if (!_sekmePanelleri.ContainsKey(sekmeAdi))
            sekmeAdi = VarsayilanSekme();

        if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", sekmeAdi))
        {
            var ilk = _sekmePanelleri.Keys.FirstOrDefault(k =>
                KullaniciYetkileri.SekmeGorebilir("Satınalma", k));
            if (ilk is null)
                return;
            sekmeAdi = ilk;
        }

        _aktifSekme = sekmeAdi;
        var gosterilecekPanel = _sekmePanelOverride ?? sekmeAdi;
        _sekmePanelOverride = null;

        IcerikGoster(sekmeAdi);

        foreach (var (ad, panel) in _sekmePanelleri)
            panel.Visibility = ad == gosterilecekPanel ? Visibility.Visible : Visibility.Collapsed;

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
        else if (sekmeAdi == "Gelen Siparişler")
            GelenSiparisListesiniYenile();
        else if (sekmeAdi == "Taleplerim")
            TalepListesiniYenile();

        SekmeSayaclariniYenile();
    }

    private void SekmeleriYetkiyeGoreAyarla()
    {
        if (!KullaniciYetkileri.SekmeGorebilir("Satınalma", _aktifSekme))
        {
            var ilk = _sekmePanelleri.Keys.FirstOrDefault(k =>
                KullaniciYetkileri.SekmeGorebilir("Satınalma", k));
            if (ilk is not null)
                _aktifSekme = ilk;
        }
    }
}
