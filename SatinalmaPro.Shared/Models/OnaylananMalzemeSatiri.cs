namespace SatinalmaPro.Shared.Models;

public class OnaylananMalzemeSatiri
{
    public Guid TalepId { get; set; }
    public Guid KalemId { get; set; }
    public Guid TeklifId { get; set; }
    public string TalepNo { get; set; } = "";
    public string SiparisNo { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string Durum { get; set; } = "";
    public string Firma { get; set; } = "";
    public string Marka { get; set; } = "";
    public string Malzeme { get; set; } = "";
    public double SiparisMiktari { get; set; }
    public double KabulEdilenMiktar { get; set; }
    public bool SiparisTamamlandi { get; set; }
    public string Birim { get; set; } = "";
    public decimal BirimFiyati { get; set; }
    public decimal ToplamTutar { get; set; }
    public string KalemAciklamasi { get; set; } = "";
    public int VadeGunu { get; set; }

    public double KalanMiktar => Math.Max(0, SiparisMiktari - KabulEdilenMiktar);

    public string VadeOzeti => VadeGunu > 0 ? $"Vade: {VadeGunu} gün" : "Vade: —";

    public string KabulDurumu => SiparisTamamlandi || KabulEdilenMiktar >= SiparisMiktari
        ? "Tamamlandı"
        : KabulEdilenMiktar > 0
            ? "Kısmi"
            : "Bekliyor";

    public string MiktarOzeti =>
        $"Sipariş: {SiparisMiktari:N2} {Birim} · Kabul: {KabulEdilenMiktar:N2}";

    public string FiyatOzeti =>
        $"Birim: {BirimFiyati:N2} ₺ · Toplam: {ToplamTutar:N2} ₺";

    public string AktarimBelgeNo() =>
        string.IsNullOrWhiteSpace(SiparisNo) ? TalepNo.Trim() : SiparisNo.Trim();
}
