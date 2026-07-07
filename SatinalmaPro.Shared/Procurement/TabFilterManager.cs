using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Procurement;

/// <summary>
/// Rol ve sekmeye göre Firestore sorgu şartlarını tek merkezden üretir.
/// Android (Kotlin) ve WPF aynı kuralları kullanır.
/// </summary>
public static class TabFilterManager
{
    private static readonly ProcurementTab[] FullProcurementTabs =
    [
        ProcurementTab.PendingDraft,
        ProcurementTab.ManagementApproval,
        ProcurementTab.QuoteRequested,
        ProcurementTab.QuoteReview,
        ProcurementTab.ApprovedOrders,
        ProcurementTab.Rejected,
        ProcurementTab.HistoryReports
    ];

    public static string NormalizeRole(string? role)
    {
        var normalized = KullaniciRolleri.Normalize(role);
        return normalized switch
        {
            KullaniciRolleri.Admin => "admin",
            KullaniciRolleri.Yonetim => "yonetim",
            KullaniciRolleri.Satinalma => "satinalma",
            KullaniciRolleri.Sef => "sef",
            KullaniciRolleri.Saha => "saha",
            KullaniciRolleri.Depo => "depo",
            KullaniciRolleri.Atolye => "atolye",
            _ => normalized.ToLowerInvariant()
        };
    }

    public static bool RequiresRequesterScope(string? role)
    {
        var key = NormalizeRole(role);
        return key is "sef" or "saha";
    }

    public static IReadOnlyList<ProcurementTab> GetVisibleTabs(string? role)
    {
        return NormalizeRole(role) switch
        {
            "depo" =>
            [
                ProcurementTab.ApprovedOrders,
                ProcurementTab.HistoryReports,
                ProcurementTab.StockMovements
            ],
            "atolye" => [ProcurementTab.StockStatus],
            "admin" or "yonetim" or "satinalma" => FullProcurementTabs,
            "sef" or "saha" => FullProcurementTabs,
            _ => FullProcurementTabs
        };
    }

    public static bool IsTabVisible(string? role, ProcurementTab tab) =>
        GetVisibleTabs(role).Contains(tab);

    public static FirestoreFilterSpec? GetQuerySpec(
        ProcurementTab tab,
        string? role,
        string? currentUid)
    {
        if (!IsTabVisible(role, tab))
            return null;

        var requesterUid = RequiresRequesterScope(role) ? currentUid : null;
        if (RequiresRequesterScope(role) && string.IsNullOrWhiteSpace(requesterUid))
            return null;

        return tab switch
        {
            ProcurementTab.PendingDraft => new FirestoreFilterSpec
            {
                Collection = "procurement_requests",
                StatusIn = [ProcurementStatus.Draft, ProcurementStatus.Submitted],
                RequesterUidEquals = requesterUid,
                OrderBy =
                [
                    new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }
                ]
            },
            ProcurementTab.ManagementApproval => new FirestoreFilterSpec
            {
                Collection = "procurement_requests",
                StatusIn = [ProcurementStatus.Submitted],
                RequesterUidEquals = requesterUid,
                UrgentFirst = true,
                OrderBy =
                [
                    new FirestoreOrderBy { Field = "priority", Descending = true },
                    new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }
                ]
            },
            ProcurementTab.QuoteRequested => new FirestoreFilterSpec
            {
                Collection = "procurement_requests",
                StatusIn =
                [
                    ProcurementStatus.QuoteRequested,
                    ProcurementStatus.QuoteEntry,
                    ProcurementStatus.Comparison
                ],
                RequesterUidEquals = requesterUid,
                OrderBy = [new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }]
            },
            ProcurementTab.QuoteReview => new FirestoreFilterSpec
            {
                Collection = "procurement_requests",
                StatusIn = [ProcurementStatus.ManagementQuoteReview],
                RequesterUidEquals = requesterUid,
                OrderBy = [new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }]
            },
            ProcurementTab.ApprovedOrders => new FirestoreFilterSpec
            {
                Collection = "procurement_requests",
                StatusIn = [ProcurementStatus.Approved, ProcurementStatus.Ordered],
                RequesterUidEquals = requesterUid,
                OrderBy = [new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }]
            },
            ProcurementTab.Rejected => new FirestoreFilterSpec
            {
                Collection = "procurement_requests",
                StatusIn = [ProcurementStatus.Rejected],
                RequesterUidEquals = requesterUid,
                OrderBy = [new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }]
            },
            ProcurementTab.HistoryReports => new FirestoreFilterSpec
            {
                Collection = "procurement_requests",
                StatusIn = [ProcurementStatus.Completed],
                RequesterUidEquals = requesterUid,
                OrderBy = [new FirestoreOrderBy { Field = "updatedAtUtc", Descending = true }]
            },
            ProcurementTab.StockMovements => FirestoreFilterSpec.ForStockMovements(readOnly: false),
            ProcurementTab.StockStatus => FirestoreFilterSpec.ForStockItems(readOnly: true),
            _ => null
        };
    }

    public static IReadOnlyList<string> StatusesForTab(ProcurementTab tab) => tab switch
    {
        ProcurementTab.PendingDraft => [ProcurementStatus.Draft, ProcurementStatus.Submitted],
        ProcurementTab.ManagementApproval => [ProcurementStatus.Submitted],
        ProcurementTab.QuoteRequested =>
        [
            ProcurementStatus.QuoteRequested,
            ProcurementStatus.QuoteEntry,
            ProcurementStatus.Comparison
        ],
        ProcurementTab.QuoteReview => [ProcurementStatus.ManagementQuoteReview],
        ProcurementTab.ApprovedOrders => [ProcurementStatus.Approved, ProcurementStatus.Ordered],
        ProcurementTab.Rejected => [ProcurementStatus.Rejected],
        ProcurementTab.HistoryReports => [ProcurementStatus.Completed],
        _ => []
    };

    public static bool MatchesTab(
        ProcurementTab tab,
        ProcurementRequestSnapshot request,
        string? role,
        string? currentUid)
    {
        if (!IsTabVisible(role, tab))
            return false;

        if (tab is ProcurementTab.StockMovements or ProcurementTab.StockStatus)
            return false;

        if (RequiresRequesterScope(role))
        {
            if (string.IsNullOrWhiteSpace(currentUid))
                return false;
            if (!request.RequesterUid.Equals(currentUid, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var normalized = request.NormalizedStatus;
        var allowed = StatusesForTab(tab);
        if (!allowed.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            return false;

        return true;
    }

    public static IEnumerable<T> FilterForTab<T>(
        ProcurementTab tab,
        IEnumerable<T> source,
        Func<T, ProcurementRequestSnapshot> selector,
        string? role,
        string? currentUid)
    {
        if (!IsTabVisible(role, tab))
            return [];

        return source.Where(item => MatchesTab(tab, selector(item), role, currentUid));
    }

    public static IReadOnlyList<T> SortForTab<T>(
        ProcurementTab tab,
        IEnumerable<T> source,
        Func<T, ProcurementRequestSnapshot> selector)
    {
        var list = source.ToList();
        if (tab == ProcurementTab.ManagementApproval)
        {
            return list
                .OrderByDescending(item =>
                    selector(item).EffectivePriority.Equals(ProcurementPriority.Urgent, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(item => selector(item).Id)
                .ToList();
        }

        return list;
    }

    public static TabFilterContext CreateContext(string? role, string? currentUid) => new(role, currentUid);
}

public sealed class TabFilterContext
{
    public string RoleKey { get; }
    public string? CurrentUid { get; }
    public IReadOnlyList<ProcurementTab> VisibleTabs { get; }

    public TabFilterContext(string? role, string? currentUid)
    {
        RoleKey = TabFilterManager.NormalizeRole(role);
        CurrentUid = currentUid;
        VisibleTabs = TabFilterManager.GetVisibleTabs(role);
    }

    public bool IsVisible(ProcurementTab tab) => VisibleTabs.Contains(tab);

    public FirestoreFilterSpec? QueryFor(ProcurementTab tab) =>
        TabFilterManager.GetQuerySpec(tab, RoleKey, CurrentUid);

    public bool Matches(ProcurementTab tab, ProcurementRequestSnapshot request) =>
        TabFilterManager.MatchesTab(tab, request, RoleKey, CurrentUid);
}
