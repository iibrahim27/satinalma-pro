using SatinalmaPro.Models;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Helpers;

public sealed record RoleTabMenuItem(string Baslik, string Route, string? GrupBasligi = null);

public sealed record RoleTabMenuGrubu(string? Baslik, IReadOnlyList<RoleTabMenuItem> Ogeler);

/// <summary>
/// Rol bazlı sekme görünürlüğü, Firestore sorgu şartları ve bellek içi talep filtreleri.
/// Görsel XAML değiştirilmez — yalnızca nav öğeleri ve grid veri kaynağı yönetilir.
/// </summary>
public static class DesktopRoleTabManager
{
    public static string NormalizeRole(string? role) => TabFilterManager.NormalizeRole(role);

    public static bool RequiresRequesterScope(string? role) =>
        ProcurementRouteMatcher.RequiresRequesterScope(role);

    public static RoleTabSession CreateSession(string? role, string? currentUid) =>
        new(role, currentUid);

    public static IReadOnlyList<RoleTabMenuGrubu> GetSatinalmaMenuGroups(string? role, string? currentUid)
    {
        _ = currentUid;
        return ProcurementRouteMatcher.GetMenuGroups(role)
            .Select(g => new RoleTabMenuGrubu(
                g.Baslik,
                g.Items.Select(i => new RoleTabMenuItem(i.Baslik, i.Route)).ToList()))
            .ToList();
    }

    public static IReadOnlyList<RoleTabMenuItem> GetFlatMenu(string? role, string? currentUid)
    {
        _ = currentUid;
        return GetSatinalmaMenuGroups(role, currentUid)
            .SelectMany(g => g.Ogeler)
            .ToList();
    }

    public static bool RouteVisible(string? role, string route) =>
        ProcurementRouteMatcher.IsRouteVisibleForRole(route, role);

    public static string IlkRoute(string? role) =>
        ProcurementRouteMatcher.FirstRoute(role);

    public static bool TalepFormuAcabilir(string? role)
    {
        var key = NormalizeRole(role);
        if (key is "yonetim" or "atolye" or "depo")
            return false;
        return key is "admin" or "satinalma" or "sef" or "saha";
    }

    public static FirestoreFilterSpec? GetQuerySpec(string route, string? role, string? currentUid)
    {
        if (!RouteVisible(role, route))
            return null;

        if (route == "stok-durum")
            return FirestoreFilterSpec.ForStockItems(readOnly: NormalizeRole(role) is "atolye" or "sef" or "saha");

        var statuses = ProcurementRouteMatcher.StatusesForRoute(route);
        if (statuses.Count == 0 && route is not SatinalmaRoutes.SatinalmaIade)
            return null;

        var requesterUid = RequiresRequesterScope(role) ? currentUid : null;
        if (RequiresRequesterScope(role) && string.IsNullOrWhiteSpace(requesterUid))
            return null;

        return new FirestoreFilterSpec
        {
            Collection = "procurement_requests",
            StatusIn = statuses,
            RequesterUidEquals = requesterUid,
            RequiresReturnFlag = route == SatinalmaRoutes.SatinalmaIade,
            UrgentFirst = route == SatinalmaRoutes.YonetimGelenTalepler,
            OrderBy = route == SatinalmaRoutes.YonetimGelenTalepler
                ?
                [
                    new FirestoreOrderBy { Field = "priority", Descending = true },
                    new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }
                ]
                : [new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }]
        };
    }

    public static Func<SatinalmaTalep, bool>? GetDataFilter(string route, string? role, string? currentUid)
    {
        if (route == "stok-durum")
            return null;

        if (!RouteVisible(role, route))
            return _ => false;

        var roleKey = NormalizeRole(role);
        var scopeUid = currentUid;

        return talep => MatchesPro(route, talep, roleKey, scopeUid);
    }

    internal static bool MatchesPro(string route, SatinalmaTalep talep, string roleKey, string? currentUid)
    {
        var shared = TalepPaylasimaCevir(talep);
        return ProcurementRouteMatcher.Matches(route, shared, roleKey, currentUid);
    }

    public static List<SatinalmaTalep> FilterAndSort(
        string route,
        IEnumerable<SatinalmaTalep> source,
        string? role,
        string? currentUid)
    {
        var filter = GetDataFilter(route, role, currentUid);
        if (filter is null)
            return source.ToList();

        var list = source.Where(filter).ToList();

        if (route == SatinalmaRoutes.YonetimGelenTalepler)
        {
            return list
                .OrderByDescending(t =>
                    ProcurementTalepAdapter.EffectivePriority(t)
                        .Equals(ProcurementPriority.Urgent, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(t => t.TalepTuru == TalepTurleri.Acil)
                .ThenByDescending(t => t.GuncellemeUtc)
                .ThenByDescending(t => t.TalepNo)
                .ToList();
        }

        return list
            .OrderByDescending(t => t.TalepTuru == TalepTurleri.Acil)
            .ThenByDescending(t => t.TalepTuru == TalepTurleri.Oncelikli)
            .ThenByDescending(t => t.Tarih)
            .ThenByDescending(t => t.TalepNo)
            .ToList();
    }

    public static Dictionary<string, int> CountBadges(IReadOnlyList<string> routes, string? role, string? currentUid)
    {
        var sonuc = routes.ToDictionary(r => r, _ => 0, StringComparer.Ordinal);
        foreach (var talep in SatinalmaDepo.Talepler)
        {
            foreach (var route in routes)
            {
                var filter = GetDataFilter(route, role, currentUid);
                if (filter?.Invoke(talep) == true)
                    sonuc[route]++;
            }
        }

        return sonuc;
    }

    public static IReadOnlyList<string> GetVisibleStockTabs(string? role) => NormalizeRole(role) switch
    {
        "depo" =>
        [
            StokRoutes.StokDurumu,
            StokRoutes.StokGirisi,
            StokRoutes.StokCikisi,
            StokRoutes.StokHareketleri
        ],
        "atolye" => [StokRoutes.StokDurumu],
        "sef" or "saha" => [StokRoutes.StokDurumu],
        "admin" or "satinalma" =>
        [
            StokRoutes.StokDurumu,
            StokRoutes.StokGirisi,
            StokRoutes.StokCikisi,
            StokRoutes.StokHareketleri,
            StokRoutes.StokSayim
        ],
        _ => [StokRoutes.StokDurumu, StokRoutes.StokHareketleri]
    };

    public static bool StockTabVisible(string? role, string tabName) =>
        GetVisibleStockTabs(role).Contains(tabName, StringComparer.Ordinal);

    public static bool StockReadOnly(string? role) =>
        NormalizeRole(role) is "atolye" or "sef" or "saha";

    public static bool StockCanWrite(string? role) =>
        NormalizeRole(role) is "depo" or "admin" or "satinalma";

    private static SatinalmaPro.Shared.Models.SatinalmaTalep TalepPaylasimaCevir(SatinalmaTalep talep)
    {
        // CamelCase JSON round-trip Durum/Status alanlarını kaybediyordu (case-sensitive deserialize)
        // → Resolve() her şeyi Taslak sanıyordu → tüm sekmeler boş.
        var opts = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var json = System.Text.Json.JsonSerializer.Serialize(talep);
        return System.Text.Json.JsonSerializer.Deserialize<SatinalmaPro.Shared.Models.SatinalmaTalep>(json, opts)
               ?? new SatinalmaPro.Shared.Models.SatinalmaTalep();
    }
}

public sealed class RoleTabSession
{
    public string RoleKey { get; }
    public string? CurrentUid { get; }
    public IReadOnlyList<RoleTabMenuGrubu> MenuGroups { get; }

    public RoleTabSession(string? role, string? currentUid)
    {
        RoleKey = DesktopRoleTabManager.NormalizeRole(role);
        CurrentUid = currentUid;
        MenuGroups = DesktopRoleTabManager.GetSatinalmaMenuGroups(role, currentUid);
    }

    public bool RouteVisible(string route) =>
        MenuGroups.SelectMany(g => g.Ogeler).Any(i => i.Route == route);

    public FirestoreFilterSpec? QueryFor(string route) =>
        DesktopRoleTabManager.GetQuerySpec(route, RoleKey, CurrentUid);

    public Func<SatinalmaTalep, bool>? FilterFor(string route) =>
        DesktopRoleTabManager.GetDataFilter(route, RoleKey, CurrentUid);
}
