namespace SatinalmaPro.Shared.SaaS;

public static class LisansTipleri
{
  public const string Deneme = "deneme";
  public const string Yillik = "yillik";

  public static string GorunenAd(string? tip) => tip switch
  {
    Deneme => "30 günlük deneme",
    Yillik => "Yıllık lisans",
    _ => "Lisans yok"
  };
}

public sealed class KiracıLisansi
{
  public string Tip { get; set; } = LisansTipleri.Deneme;
  public DateTime? BaslangicUtc { get; set; }
  public DateTime? BitisUtc { get; set; }
  public bool Aktif { get; set; } = true;
  public int? KalanGun { get; set; }
  public bool SuresiDoldu { get; set; }

  public string DurumMetni
  {
    get
    {
      if (SuresiDoldu || !Aktif)
        return "Lisans süresi doldu";

      if (KalanGun is null)
        return LisansTipleri.GorunenAd(Tip);

      if (KalanGun <= 0)
        return "Lisans süresi doldu";

      if (KalanGun == 1)
        return $"{LisansTipleri.GorunenAd(Tip)} · son 1 gün";

      return $"{LisansTipleri.GorunenAd(Tip)} · {KalanGun} gün kaldı";
    }
  }

  public string KisaDurumMetni
  {
    get
    {
      if (SuresiDoldu || !Aktif || KalanGun is null || KalanGun <= 0)
        return "Lisans süresi doldu";

      return KalanGun <= 7
        ? $"Lisans: {KalanGun} gün kaldı"
        : $"Lisans: {KalanGun} gün";
    }
  }

  public void KalanGunHesapla()
  {
    if (BitisUtc is null)
    {
      KalanGun = null;
      SuresiDoldu = false;
      return;
    }

    var kalan = (int)Math.Ceiling((BitisUtc.Value.ToUniversalTime() - DateTime.UtcNow).TotalDays);
    KalanGun = kalan;
    SuresiDoldu = kalan <= 0;
    if (SuresiDoldu)
      Aktif = false;
  }
}
