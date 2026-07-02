using System.Windows;
using SatinalmaPro.Helpers;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Views.Modules;

public partial class SatinalmaView
{
    internal static readonly Dictionary<string, string> SekmeBasliklari = new(StringComparer.Ordinal)
    {
        ["Taleplerim"] = "Taleplerim",
        ["Gelen Talepler"] = "Gelen Talepler",
        ["Onay Bekleyen"] = "Onay Bekleyen",
        ["Teklif Bekleyen"] = "Teklif Bekleyen",
        ["Teklif Girişi"] = "Teklif Girişi",
        ["Karşılaştırma"] = "Karşılaştırma",
        ["Teklif Onay"] = "Teklif Onay",
        ["Onaylanan Talepler"] = "Onaylanan Talepler",
        ["Geçmiş Talepler"] = "Geçmiş Talepler",
        ["Geçmiş Teklifli Onaylar"] = "Geçmiş Teklifli",
        ["Red Talepler"] = "Red Talepler",
        ["Alınan Malzemeler"] = "Alınan Malzemeler",
        ["Gelen Siparişler"] = "Gelen Siparişler"
    };

    private void SekmeSayaclariniHazirla()
    {
        SatinalmaDepo.TaleplerGuncellendi += SekmeSayaclariniYenile;
        Unloaded += (_, _) => SatinalmaDepo.TaleplerGuncellendi -= SekmeSayaclariniYenile;
        SekmeSayaclariniYenile();
    }

    private void SekmeSayaclariniYenile()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(SekmeSayaclariniYenile);
            return;
        }

        if (_menuModunda)
            SekmeMenusuYenile();
    }

    private string VarsayilanSekme()
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        foreach (var ad in MasaustuRolHaritasi.SatinalmaSekmeleri(rol))
        {
            if (_sekmePanelleri.ContainsKey(ad) && KullaniciYetkileri.SekmeGorebilir("Satınalma", ad))
                return ad;
        }

        return _sekmePanelleri.Keys.FirstOrDefault(k => KullaniciYetkileri.SekmeGorebilir("Satınalma", k))
               ?? MasaustuRolHaritasi.Taleplerim;
    }

    private void SatinalmaAnaSayfa_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mw)
            mw.AnaSayfayaDon();
    }

    private void TalepDetayGeri_Click(object sender, RoutedEventArgs e)
    {
        _seciliTalep = null;
        _talepFormModu = false;
        _talepSerbestDuzenleme = false;
        TalepListesi.SelectedItem = null;
        TalepFormuGizle();
        TalepOnizlemePenceresiniKapat();
    }

    private void TeklifGirisDetayGeri_Click(object sender, RoutedEventArgs e)
    {
        _teklifGirisTalep = null;
        _seciliTeklif = null;
        TeklifGirisTalepListesi.SelectedItem = null;
        TeklifGirisFormuGizle();
    }

    private void TeklifDegerDetayGeri_Click(object sender, RoutedEventArgs e)
    {
        _teklifDegerTalep = null;
        _kalemOnaySatirlari = [];
        TeklifDegerTalepListesi.SelectedItem = null;
        TeklifDegerFormuGizle();
    }

    private void OnaylananDetayGeri_Click(object sender, RoutedEventArgs e)
    {
        _onaylananTalep = null;
        OnaylananTalepListesi.SelectedItem = null;
        OnaylananFormuGizle();
    }
}
