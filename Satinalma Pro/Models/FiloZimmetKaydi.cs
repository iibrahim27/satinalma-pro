namespace SatinalmaPro.Models;

public class FiloZimmetKaydi
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Plaka { get; set; } = "";
    public string SoforAdi { get; set; } = "";
    public string Tarih { get; set; } = "";
    public bool Aktif { get; set; } = true;
    public string IptalTarihi { get; set; } = "";
    /// <summary>filo/zimmetler/ altında saklanan PDF (göreli yol).</summary>
    public string PdfDosyaYolu { get; set; } = "";

    public string DurumMetin => Aktif
        ? "Aktif"
        : string.IsNullOrWhiteSpace(IptalTarihi) ? "İptal" : $"İptal ({IptalTarihi})";

    public string PdfDurumMetin => string.IsNullOrWhiteSpace(PdfDosyaYolu)
        ? "—"
        : System.IO.Path.GetFileName(PdfDosyaYolu.Replace('\\', '/'));
}
