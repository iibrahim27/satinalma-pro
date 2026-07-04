namespace SatinalmaPro.Shared.Models;

public sealed class TeklifsizFirmaFiyatGirdisi
{
    public Guid KalemId { get; set; }
    public string FirmaAdi { get; set; } = "";
    public decimal BirimFiyat { get; set; }

    public bool Gecerli =>
        !string.IsNullOrWhiteSpace(FirmaAdi) && BirimFiyat > 0;
}
