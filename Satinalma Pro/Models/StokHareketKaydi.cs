namespace SatinalmaPro.Models;

public static class StokHareketTipleri
{
    public const string Giris = "Giriş";
    public const string Cikis = "Çıkış";
    public const string Sayim = "Sayım";

    public static IReadOnlyList<string> Tum { get; } = [Giris, Cikis, Sayim];
}

public class StokHareketKaydi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Tarih { get; set; } = "";
    public string HareketTipi { get; set; } = "";
    public string MalzemeAdi { get; set; } = "";
    public string Kategori { get; set; } = "";
    public string Birim { get; set; } = "";
    public double Miktar { get; set; }
    public double? OncekiMiktar { get; set; }
    public double? SayimMiktar { get; set; }
    public string DepoSaha { get; set; } = "";
    public decimal BirimMaliyet { get; set; }
    public string BelgeNo { get; set; } = "";
    public string IslemYapan { get; set; } = "";
    public string TeslimEdilen { get; set; } = "";
    public string Aciklama { get; set; } = "";

    public string MiktarGosterim => HareketTipi switch
    {
        StokHareketTipleri.Sayim when SayimMiktar.HasValue && OncekiMiktar.HasValue =>
            $"{SayimMiktar.Value:N2} (fark: {SayimMiktar.Value - OncekiMiktar.Value:+#.##;-#.##;0})",
        _ => Miktar.ToString("N2")
    };
}
