namespace SatinalmaPro.Models;

public class FiloAracKaydi
{
    public string Plaka { get; set; } = "";
    public string AracTipi { get; set; } = "Binek";
    public string MarkaModel { get; set; } = "";
    public string SasiNo { get; set; } = "";
    public string ModelYili { get; set; } = "";
    public string SahiplikTipi { get; set; } = "Bizim";
    public string Sirket { get; set; } = "";
    public string Saha { get; set; } = "";
    public string MuayeneBitisTarihi { get; set; } = "";
    public string SigortaBitisTarihi { get; set; } = "";
    public string Durum { get; set; } = "Aktif";
    public string RuhsatDosyaYolu { get; set; } = "";
    public List<string> GorselDosyaYollari { get; set; } = [];
    public string Aciklama { get; set; } = "";
    public string KayitTarihi { get; set; } = "";

    public decimal ToplamGider { get; set; }
    public string MuayeneUyariMetin { get; set; } = "—";
    public string SigortaUyariMetin { get; set; } = "—";
    public string ZimmetMetin { get; set; } = "—";

    public string SahiplikMetin => string.IsNullOrWhiteSpace(SahiplikTipi) ? "—" : SahiplikTipi;

    public string MuayeneBitisMetin => string.IsNullOrWhiteSpace(MuayeneBitisTarihi) ? "—" : MuayeneBitisTarihi;

    public string SigortaBitisMetin => string.IsNullOrWhiteSpace(SigortaBitisTarihi) ? "—" : SigortaBitisTarihi;

    public string ToplamGiderMetin => ToplamGider > 0 ? $"₺{ToplamGider:N2}" : "₺0";
}
