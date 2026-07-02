using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class SatinalmaTabFiltreleri
{
    public static bool TeklifDegerlendirme(SatinalmaTalep talep) =>
        SatinalmaTalepYardimcisi.SatinalmaTeklifDegerlendirmede(talep)
        || SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep)
        || SatinalmaTalepKuyrugu.SatinalmaKarsilastirma(talep);

    public static bool OnayBekleyen(SatinalmaTalep talep, bool talepSahibiModu) =>
        SatinalmaTalepKuyrugu.OnayBekleyenListede(talep, talepSahibiModu);

    /// <summary>Onaylanmış talepler: sipariş bekleyen (Onaylandı) ve sipariş verilmiş (Sipariş Oluşturuldu).</summary>
    public static bool Onaylananlar(SatinalmaTalep talep) =>
        talep.Durum is SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu
        && (talep.HerhangiKalemOnayli || talep.TeklifsizYonetimOnayi || talep.YonetimOnayKilitli);

    public static bool Siparisler(SatinalmaTalep talep) =>
        talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu;

    public static bool Reddedilenler(SatinalmaTalep talep) =>
        SatinalmaTalepKuyrugu.Reddedildi(talep);

    public static bool SiparisBekleyenMalzeme(OnaylananMalzemeSatiri satir) =>
        !satir.SiparisTamamlandi && satir.KalanMiktar > 0.0001
        && satir.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu;

    public static bool GelenSiparisMalzeme(OnaylananMalzemeSatiri satir) =>
        satir.KabulEdilenMiktar > 0.0001;

    public static bool GelenTalepler(SatinalmaTalep talep) =>
        SatinalmaTalepKuyrugu.YonetimTalepler(talep);

    public static bool TeklifOnay(SatinalmaTalep talep) =>
        SatinalmaTalepKuyrugu.YonetimTeklifler(talep);

    public static bool OnayGecmisi(SatinalmaTalep talep) =>
        GecmisTalepler(talep) || GecmisTeklifliOnaylar(talep);

    public static bool GecmisTalepler(SatinalmaTalep talep) =>
        SatinalmaTalepKuyrugu.YonetimGecmisTalep(talep);

    public static bool GecmisTeklifliOnaylar(SatinalmaTalep talep) =>
        SatinalmaTalepKuyrugu.YonetimGecmisTeklifli(talep);
}
