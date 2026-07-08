using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Services.Firebase;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Services.Procurement;

/// <summary>
/// Sekme tıklanınca rol/route'a göre Firestore sorgusu + bellek içi filtre ile grid verisi üretir.
/// </summary>
public static class ProcurementTalepSorguServisi
{
    public static FirestoreFilterSpec? SorguOzelligi(string route)
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        return DesktopRoleTabManager.GetQuerySpec(route, rol, uid);
    }

    public static string SorguAciklamasi(string route)
    {
        var spec = SorguOzelligi(route);
        return spec is null ? "" : FirestoreQueryBuilder.Describe(spec);
    }

    /// <summary>Bellek içi filtre — legacy JSON senkronu ile uyumlu.</summary>
    public static List<SatinalmaTalep> Listele(string route) =>
        DesktopRoleTabManager.FilterAndSort(
            route,
            SatinalmaDepo.Talepler,
            OturumYoneticisi.AktifKullanici?.Rol,
            OturumYoneticisi.AktifKullanici?.Uid);

    /// <summary>
    /// Firestore <c>procurement_requests</c> sorgusu çalıştırır;
    /// sonuç yoksa veya hata olursa bellek içi listeye düşer.
    /// </summary>
    public static async Task<List<SatinalmaTalep>> ListeleAsync(
        string route,
        CancellationToken iptal = default)
    {
        var rol = OturumYoneticisi.AktifKullanici?.Rol;
        var uid = OturumYoneticisi.AktifKullanici?.Uid;
        var bellek = DesktopRoleTabManager.FilterAndSort(route, SatinalmaDepo.Talepler, rol, uid);

        var spec = DesktopRoleTabManager.GetQuerySpec(route, rol, uid);
        if (spec is null || spec.Collection != "procurement_requests")
            return bellek;

        if (!OturumYoneticisi.BulutAktif || OturumYoneticisi.Firestore is null)
            return bellek;

        try
        {
            var idler = await OturumYoneticisi.Firestore
                .ProcurementRequestIdleriSorgulaAsync(spec, iptal);

            if (idler.Count == 0)
                return bellek;

            var idKumesi = idler.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var eslesen = SatinalmaDepo.Talepler
                .Where(t => idKumesi.Contains(t.Id.ToString()))
                .ToList();

            if (eslesen.Count == 0)
                return bellek;

            var birlesik = bellek
                .Concat(eslesen)
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .ToList();

            return DesktopRoleTabManager.FilterAndSort(route, birlesik, rol, uid);
        }
        catch (Exception ex)
        {
            HataGunlugu.Kaydet(ex, $"ProcurementTalepSorgu.{route}");
            return bellek;
        }
    }
}
