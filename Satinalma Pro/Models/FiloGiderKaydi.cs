namespace SatinalmaPro.Models;

public class FiloGiderKaydi
{
    public string Plaka { get; set; } = "";
    public string Tarih { get; set; } = "";
    public string GiderTipi { get; set; } = "";
    public decimal Tutar { get; set; }
    public string BelgeNo { get; set; } = "";
    public string Aciklama { get; set; } = "";

    public string TutarMetin => Tutar > 0 ? $"₺{Tutar:N2}" : "—";
}
