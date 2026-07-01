namespace SatinalmaPro.Models;

public class StokKaydi
{
    public string MalzemeAdi { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string Birim { get; set; } = "";
    public double MevcutMiktar { get; set; }
    public double MinimumStok { get; set; }
    public string DepoSaha { get; set; } = "";
    public decimal BirimMaliyet { get; set; }
    public decimal ToplamDeger { get; set; }
    public string SonGuncelleme { get; set; } = "";
    public string Aciklama { get; set; } = "";

    public string DurumMetin => MevcutMiktar <= 0 ? "Tükendi"
        : MinimumStok > 0 && MevcutMiktar <= MinimumStok ? "Kritik"
        : "Normal";

    public void ToplamDegerHesapla() =>
        ToplamDeger = Math.Round((decimal)MevcutMiktar * BirimMaliyet, 2);

    public StokKaydi Kopyala() => new()
    {
        MalzemeAdi = MalzemeAdi,
        Kategori = Kategori,
        Birim = Birim,
        MevcutMiktar = MevcutMiktar,
        MinimumStok = MinimumStok,
        DepoSaha = DepoSaha,
        BirimMaliyet = BirimMaliyet,
        ToplamDeger = ToplamDeger,
        SonGuncelleme = SonGuncelleme,
        Aciklama = Aciklama
    };
}
