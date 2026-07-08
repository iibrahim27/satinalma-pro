using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Shared.SaaS;

/// <summary>Aktif oturumun kiracı (firma) kimliği — tüm bulut sorguları buna göre kapsüllenir.</summary>
public static class KiracıOturumu
{
  private static readonly object Kilit = new();
  private static string? _tenantId;
  private static string? _tenantAd;
  private static KiracıLisansi? _lisans;

  public static string? TenantId
  {
    get { lock (Kilit) return _tenantId; }
  }

  public static string? TenantAd
  {
    get { lock (Kilit) return _tenantAd; }
  }

  public static KiracıLisansi? Lisans
  {
    get { lock (Kilit) return _lisans; }
  }

  public static bool Aktif => !string.IsNullOrWhiteSpace(_tenantId);

  public static void Ayarla(string tenantId, string? tenantAd = null, KiracıLisansi? lisans = null)
  {
    if (string.IsNullOrWhiteSpace(tenantId))
      throw new ArgumentException("tenantId zorunludur.", nameof(tenantId));

    lock (Kilit)
    {
      _tenantId = tenantId.Trim();
      _tenantAd = tenantAd?.Trim();
      _lisans = lisans;
    }
  }

  public static void LisansAyarla(KiracıLisansi? lisans)
  {
    lock (Kilit) _lisans = lisans;
  }

  public static void Temizle()
  {
    lock (Kilit)
    {
      _tenantId = null;
      _tenantAd = null;
      _lisans = null;
    }
  }

  public static string ZorunluTenantId()
  {
    var id = TenantId;
    if (string.IsNullOrWhiteSpace(id))
      throw new InvalidOperationException("Kiracı oturumu bulunamadı. Tekrar giriş yapın.");
    return id;
  }
}
