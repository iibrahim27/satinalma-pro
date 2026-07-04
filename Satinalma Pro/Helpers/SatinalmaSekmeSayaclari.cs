using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

/// <summary>Satınalma sekme rozetleri — her sekmenin listesiyle aynı filtre.</summary>
public static class SatinalmaSekmeSayaclari
{
    public static string Baslik(string sekmeAdi, int sayi) =>
        sayi > 0 ? $"{sekmeAdi} ({sayi})" : sekmeAdi;

    public static int Say(string sekmeAdi)
    {
        var talepler = SatinalmaDepo.Talepler;
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var ad = KullaniciYetkileri.AktifKullaniciAdi();
        var rol = OturumYoneticisi.AktifKullanici?.Rol;

        return sekmeAdi switch
        {
            "Taleplerim" => talepler.Count(t => SatinalmaTalepKuyrugu.TaleplerimListesindeGoster(t, uid, ad, rol)),
            "Gelen Talepler" => talepler.Count(SatinalmaTabFiltreleri.GelenTalepler),
            "Onay Bekleyen" => talepler.Count(t => SatinalmaTabFiltreleri.OnayBekleyen(t, KullaniciYetkileri.SatinalmaSadeceTalepModu())),
            "Teklif Bekleyen" => talepler.Count(TeklifBekleyenSatiri.KuyruktaGoster),
            "Teklif Girişi" => talepler.Count(SatinalmaTalepKuyrugu.SatinalmaTeklifGirisiAktif),
            "Karşılaştırma" => talepler.Count(SatinalmaTabFiltreleri.TeklifDegerlendirme),
            "Teklif Onay" => talepler.Count(SatinalmaTabFiltreleri.TeklifOnay),
            "Onaylanan Talepler" => talepler.Count(SatinalmaTabFiltreleri.Onaylananlar),
            "Geçmiş Talepler" => talepler.Count(SatinalmaTabFiltreleri.GecmisTalepler),
            "Geçmiş Teklifli Onaylar" => talepler.Count(SatinalmaTabFiltreleri.GecmisTeklifliOnaylar),
            "Red Talepler" => talepler.Count(SatinalmaTabFiltreleri.Reddedilenler),
            "Alınan Malzemeler" => AlinanMalzemeSayisi(),
            "Gelen Siparişler" => SatinalmaDepo.OnaylananMalzemeleriOlustur()
                .Count(SatinalmaTabFiltreleri.GelenSiparisMalzeme),
            _ => 0
        };
    }

    private static int AlinanMalzemeSayisi()
    {
        var takipModu = KullaniciYetkileri.SatinalmaSurecTakipModu()
                        && !KullaniciYetkileri.MalKabulVeStokAktarYapabilir();
        var kaynak = SatinalmaDepo.OnaylananMalzemeleriOlustur();
        if (!takipModu)
            kaynak = kaynak.Where(SatinalmaTabFiltreleri.SiparisBekleyenMalzeme).ToList();
        return kaynak.Count;
    }
}
