namespace SatinalmaPro.Models;

public sealed class VeriKaydiTanimi
{
    public required string ModulAdi { get; init; }
    public required string DosyaAdi { get; init; }
    public required string Aciklama { get; init; }
    public required string Kategori { get; init; }
}

public sealed class VeriKaydiDurumu
{
    public string ModulAdi { get; set; } = "";
    public string DosyaAdi { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public string Boyut { get; set; } = "—";
    public string SonGuncelleme { get; set; } = "—";
    public string Durum { get; set; } = "Bekliyor";
}
