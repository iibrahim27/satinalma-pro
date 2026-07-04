using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

/// <summary>Satınalma panel özet kartları — mobil ve masaüstü ortak sorgular.</summary>
public interface ISatinalmaDashboardSorgu
{
    IEnumerable<SatinalmaTalep> YonetimTalepleri();
    IEnumerable<SatinalmaTalep> YonetimTeklifOnayiBekleyenleri();
    IEnumerable<SatinalmaTalep> TeklifGirisiBekleyenleri();
    IEnumerable<SatinalmaTalep> KarsilastirmaBekleyenleri();
    IEnumerable<SatinalmaTalep> TeklifsizFirmaFiyatBekleyenleri();
    IEnumerable<SatinalmaTalep> OnayBekleyenTalepler();
    IEnumerable<SatinalmaTalep> YonetimOnaylananTeklifleri();
    IEnumerable<SatinalmaTalep> YonetimOnaylananTalepleri();
    IEnumerable<SatinalmaTalep> YonetimGecmisTalepleri();
    IEnumerable<SatinalmaTalep> YonetimGecmisTeklifliOnaylari();
    IEnumerable<SatinalmaTalep> YonetimReddedilenleri();
    IEnumerable<SatinalmaTalep> OnaylanmisTalepler();
    IEnumerable<SatinalmaTalep> KayitliTalepler();
    IEnumerable<SatinalmaTalep> KullaniciTalepleri(string uid);
    List<OnaylananMalzemeSatiri> OnaylananMalzemeleriOlustur();
    int MalKabulBekleyenSayisi();
}
