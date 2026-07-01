namespace SatinalmaPro.Models;

public class StokIslemSatirKaydi
{
    public string Kategori { get; set; } = "";
    public string Malzeme { get; set; } = "";
    public double Miktar { get; set; }
    public string Birim { get; set; } = "";
    public decimal BirimFiyat { get; set; }
    public string DepoSaha { get; set; } = "";
    public string MevcutStokMetin { get; set; } = "";

    public string MiktarGosterim => Miktar.ToString("N2");
    public string BirimFiyatGosterim => BirimFiyat > 0 ? $"₺{BirimFiyat:N2}" : "—";
}
