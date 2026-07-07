using System.Text.Json.Serialization;

namespace SatinalmaPro.Models;

public class AgregaKaydi
{
    public string Tarih { get; set; } = "";

    [JsonPropertyName("faturaNo")]
    public string IrsaliyeNo { get; set; } = "";

    public string SiparisNo { get; set; } = "";

    public string AgregaTuru { get; set; } = "";
    public string AgregaCinsi { get; set; } = "";
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

    public string FaturaDurumuMetin => FaturasiKesildi ? "Kesildi" : "Bekliyor";

    public void ToplamTutariHesapla() =>
        ToplamTutar = Math.Round((decimal)Miktar * BirimFiyati, 2);
}
