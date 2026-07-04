using System.Text.Json.Serialization;

namespace SatinalmaPro.Models;

public class CimentoKaydi
{
    public string Tarih { get; set; } = "";

    [JsonPropertyName("FaturaNo")]
    public string IrsaliyeNo { get; set; } = "";

    public string CimentoSinifi { get; set; } = "";
    public string CimentoCinsi { get; set; } = "";
    public double Miktar { get; set; }
    public string Birim { get; set; } = "";
    public decimal BirimFiyati { get; set; }
    public decimal ToplamTutar { get; set; }
    public double? ArtisYuzdesi { get; set; }
    public string Tedarikci { get; set; } = "";
    public string IndirildigiSaha { get; set; } = "";
    public string TeslimAlan { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public bool FaturasiKesildi { get; set; }

    public string ArtisYuzdesiMetin => ArtisYuzdesi switch
    {
        null => "—",
        > 0 => $"+{ArtisYuzdesi.Value:N1}%",
        _ => $"{ArtisYuzdesi.Value:N1}%"
    };

    public string FaturaDurumuMetin => FaturasiKesildi ? "Kesildi" : "Kesilmedi";

    public void ToplamTutariHesapla() =>
        ToplamTutar = Math.Round((decimal)Miktar * BirimFiyati, 2);
}
