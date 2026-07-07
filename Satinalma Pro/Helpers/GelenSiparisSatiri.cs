using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

/// <summary>Mal kabul sonrası Alınan Malzemeler defterindeki kayıt satırı.</summary>
public class GelenSiparisSatiri
{
    public GelenSiparisSatiri(AlinanMalzemeKaydi kaynak, string? talepNo = null)
    {
        Kaynak = kaynak;
        TalepNo = talepNo ?? "";
    }

    public AlinanMalzemeKaydi Kaynak { get; }
    public string TalepNo { get; }

    public string Tarih => Kaynak.Tarih;
    public string SiparisNo => string.IsNullOrWhiteSpace(Kaynak.FaturaNo) ? TalepNo : Kaynak.FaturaNo;
    public string Firma => string.IsNullOrWhiteSpace(Kaynak.Tedarikci) ? "—" : Kaynak.Tedarikci;
    public string Malzeme => Kaynak.MalzemeHizmet;
    public string Birim => Kaynak.Birim;
    public string KabulMiktarMetni => $"{Kaynak.Miktar:G} {Kaynak.Birim}";
    public string KabulDurumu => "Depoya girdi";
}
