namespace SatinalmaPro.Shared.Models;

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
}

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
}

public class StokIslemSatirKaydi
{
    public string Kategori { get; set; } = "";
    public string Malzeme { get; set; } = "";
    public double Miktar { get; set; }
    public string Birim { get; set; } = "";
    public decimal BirimFiyat { get; set; }
    public string DepoSaha { get; set; } = "";
}
