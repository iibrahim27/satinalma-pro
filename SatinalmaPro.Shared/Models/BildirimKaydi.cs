namespace SatinalmaPro.Shared.Models;

public class BildirimKaydi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Baslik { get; set; } = "";
    public string Mesaj { get; set; } = "";
    public string Tip { get; set; } = "";
    public Guid? TalepId { get; set; }
    public string? HedefRol { get; set; }
    public string? HedefUid { get; set; }
    public string OlusturanUid { get; set; } = "";
    public string OlusturanAd { get; set; } = "";
    public string OlusturmaTarihi { get; set; } = "";
    public bool Okundu { get; set; }
    /// <summary>UTC ms — birleştirme ve çakışma çözümü.</summary>
    public long GuncellemeUtc { get; set; }
}

public static class BildirimTipleri
{
    public const string YonetimeGonderildi = "yonetime_gonderildi";
    public const string TeklifIstendi = "teklif_istendi";
    public const string TeklifOnayda = "teklif_onayda";
    public const string TeklifDuzeltmeIstendi = "teklif_duzeltme_istendi";
    public const string Onaylandi = "onaylandi";
    public const string Reddedildi = "reddedildi";
    public const string SiparisOlusturuldu = "siparis_olusturuldu";
    public const string MalKabulEdildi = "mal_kabul_edildi";
}
