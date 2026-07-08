namespace SatinalmaPro.Shared.SaaS;

public sealed class SaaSLoginSonucu
{
  public required string IdToken { get; init; }
  public required string RefreshToken { get; init; }
  public required string Uid { get; init; }
  public required string TenantId { get; init; }
  public string? TenantAd { get; init; }
  public string? Eposta { get; init; }
  public string? KullaniciAdi { get; init; }
  public int ExpiresIn { get; init; }
  public KiracıLisansi? Lisans { get; init; }
}
