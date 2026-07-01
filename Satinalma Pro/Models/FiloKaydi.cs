namespace SatinalmaPro.Models;

public class FiloKaydi
{
    public string KayitTipi { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string PlakaKod { get; set; } = "";
    public string AracTipi { get; set; } = "";
    public string MarkaModel { get; set; } = "";
    public string ModelYili { get; set; } = "";
    public string Durum { get; set; } = "Aktif";
    public decimal Tutar { get; set; }
    public string BitisTarihi { get; set; } = "";
    public string ZimmetliKisi { get; set; } = "";
    public string BelgeNo { get; set; } = "";
    public string Saha { get; set; } = "";
    public string Aciklama { get; set; } = "";

    public string GuncelZimmetMetin { get; set; } = "—";
    public string UyariMetin { get; set; } = "—";

    public bool VarlikKaydi =>
        KayitTipi.Equals("Varlık", StringComparison.OrdinalIgnoreCase);

    public bool MasrafKaydi =>
        KayitTipi is "Tamir" or "Bakım" or "Sigorta" or "Kasko";

    public bool SurecKaydi =>
        KayitTipi is "Muayene" or "Sigorta" or "Kasko";

    public bool ZimmetKaydi =>
        KayitTipi is "Zimmet" or "Zimmet İade";

    public string TutarMetin => MasrafKaydi && Tutar > 0
        ? $"₺{Tutar:N2}"
        : "—";

    public string BitisTarihiMetin => !string.IsNullOrWhiteSpace(BitisTarihi)
        ? BitisTarihi
        : "—";

    public string ZimmetliKisiMetin => !string.IsNullOrWhiteSpace(ZimmetliKisi)
        ? ZimmetliKisi
        : "—";
}
