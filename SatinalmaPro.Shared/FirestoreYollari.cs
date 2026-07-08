using SatinalmaPro.Shared.SaaS;

namespace SatinalmaPro.Shared;

public static class FirestoreYollari
{
  public static string TenantKok(string tenantId) => $"tenants/{tenantId}";

  public static string TenantVeri(string tenantId, string altYol) =>
    $"{TenantKok(tenantId)}/veri/{altYol}";

  public static string Stok(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "stok");

  public static string StokHareket(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "stok_hareketleri");

  public static string SatinalmaTalepler(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "satinalma_talepler");

  public static string SatinalmaAyarlar(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "satinalma_ayarlar");

  public static string AlinanMalzemeler(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "alinan_malzemeler");

  public static string Bildirimler(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "bildirimler");

  public static string IadeKayitlari(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "iade_kayitlari");

  public static string Users(string? tenantId = null) =>
    $"{TenantKok(TenantCoz(tenantId))}/users";

  public static string User(string uid, string? tenantId = null) =>
    $"{Users(tenantId)}/{uid}";

  public static string UserNotificationInbox(string uid, string? tenantId = null) =>
    $"{User(uid, tenantId)}/notification_inbox";

  public static string Medya(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "medya");

  public static string EpostaSablonlari(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "eposta_sablonlari");

  public static string UygulamaAyarlar(string? tenantId = null) =>
    TenantVeri(TenantCoz(tenantId), "uygulama_ayarlar");

  public static string ProcurementRequests(string? tenantId = null) =>
    $"{TenantKok(TenantCoz(tenantId))}/procurement_requests";

  private static string TenantCoz(string? tenantId) =>
    !string.IsNullOrWhiteSpace(tenantId) ? tenantId.Trim() : KiracıOturumu.ZorunluTenantId();
}
