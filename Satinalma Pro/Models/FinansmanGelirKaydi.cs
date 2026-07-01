namespace SatinalmaPro.Models;

public class FinansmanGelirKaydi
{
    public string Tarih { get; set; } = "";
    public string BelgeNo { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public string Kaynak { get; set; } = "";
    public string Saha { get; set; } = "";
    public decimal Tutar { get; set; }
    public string OdemeSekli { get; set; } = "";
    public string Notlar { get; set; } = "";

    public void ToplamTutariHesapla() { }
}
