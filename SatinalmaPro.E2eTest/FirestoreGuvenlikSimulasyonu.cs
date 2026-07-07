using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.E2eTest;

/// <summary>
/// firestore.rules dosyasındaki kritik satınalma/stok kurallarının bellek içi simülasyonu.
/// Gerçek Firebase'e yazmadan permission-denied senaryolarını doğrular.
/// </summary>
public static class FirestoreGuvenlikSimulasyonu
{
    public enum IslemSonucu
    {
        IzinVerildi,
        PermissionDenied
    }

    public static IslemSonucu StockMovementOlustur(string? rol) =>
        CanWriteStock(rol) ? IslemSonucu.IzinVerildi : IslemSonucu.PermissionDenied;

    public static IslemSonucu ProcurementQuotesOku(string? rol) =>
        CanReadProcurementQuotes(rol) ? IslemSonucu.IzinVerildi : IslemSonucu.PermissionDenied;

    public static IslemSonucu ProcurementRequestOku(string? rol, string? requesterUid, string? currentUid) =>
        CanReadProcurement(rol)
        && (IsAdmin(rol) || IsManagement(rol) || IsProcurement(rol)
            || string.Equals(requesterUid, currentUid, StringComparison.OrdinalIgnoreCase)
            || IsWarehouse(rol))
            ? IslemSonucu.IzinVerildi
            : IslemSonucu.PermissionDenied;

    private static bool IsAdmin(string? rol) =>
        KullaniciRolleri.Normalize(rol) == KullaniciRolleri.Admin;

    private static bool IsManagement(string? rol) =>
        KullaniciRolleri.Normalize(rol) == KullaniciRolleri.Yonetim;

    private static bool IsProcurement(string? rol) =>
        KullaniciRolleri.Normalize(rol) == KullaniciRolleri.Satinalma;

    private static bool IsWarehouse(string? rol) =>
        KullaniciRolleri.Normalize(rol) == KullaniciRolleri.Depo;

    private static bool IsFieldRole(string? rol)
    {
        var r = KullaniciRolleri.Normalize(rol);
        return r is KullaniciRolleri.Sef or KullaniciRolleri.Saha or KullaniciRolleri.Atolye;
    }

    private static bool CanReadProcurement(string? rol) =>
        IsAdmin(rol) || IsManagement(rol) || IsProcurement(rol) || IsFieldRole(rol);

    private static bool CanReadProcurementQuotes(string? rol) =>
        IsAdmin(rol) || IsManagement(rol) || IsProcurement(rol);

    private static bool CanWriteStock(string? rol) =>
        IsAdmin(rol) || IsProcurement(rol) || IsWarehouse(rol);
}
