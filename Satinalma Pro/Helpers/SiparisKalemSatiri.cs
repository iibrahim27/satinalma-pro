using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public class SiparisKalemSatiri
{
    public SiparisKalemSatiri(OnaylananMalzemeSatiri kaynak) => Kaynak = kaynak;

    public OnaylananMalzemeSatiri Kaynak { get; }

    public string SiparisNo => Kaynak.SiparisNo;
    public string TalepNo => Kaynak.TalepNo;
    public string Firma => string.IsNullOrWhiteSpace(Kaynak.Firma) ? "—" : Kaynak.Firma;
    public string Malzeme => Kaynak.Malzeme;
    public string Birim => Kaynak.Birim;
    public string KabulDurumu => Kaynak.KabulDurumu;
    public string SiparisMiktariMetni => $"{Kaynak.SiparisMiktari:G} {Kaynak.Birim}";
    public string KalanMiktarMetni => $"{Kaynak.KalanMiktar:G} {Kaynak.Birim}";
    public string KabulMiktarMetni => $"{Kaynak.KabulEdilenMiktar:G} {Kaynak.Birim}";
    public string Tarih => Kaynak.Tarih;
}
