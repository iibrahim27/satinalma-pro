namespace SatinalmaPro.Shared.Procurement;

/// <summary>
/// Firestore REST structured query veya log için insan okunur sorgu özeti.
/// Masaüstü HTTP istemcisi bu özeti referans alarak sorgu oluşturur.
/// </summary>
public static class FirestoreQueryBuilder
{
    public static string Describe(FirestoreFilterSpec spec)
    {
        var parts = new List<string> { $"collection={spec.Collection}" };

        if (spec.StatusIn.Count > 0)
            parts.Add($"status in [{string.Join(", ", spec.StatusIn)}]");

        if (!string.IsNullOrWhiteSpace(spec.RequesterUidEquals))
            parts.Add($"requesterUid == {spec.RequesterUidEquals}");

        if (spec.UrgentFirst)
            parts.Add("order: priority DESC (urgent first)");

        if (spec.OrderBy.Count > 0)
        {
            var order = string.Join(", ", spec.OrderBy.Select(o =>
                $"{o.Field} {(o.Descending ? "DESC" : "ASC")}"));
            parts.Add($"orderBy: {order}");
        }

        if (spec.ReadOnly)
            parts.Add("readOnly=true");

        if (spec.RequiresReturnFlag)
            parts.Add("hasReturnFlag == true");

        return string.Join(" · ", parts);
    }

    public static FirestoreFilterSpec? ForTab(ProcurementTab tab, string? role, string? currentUid) =>
        TabFilterManager.GetQuerySpec(tab, role, currentUid);
}
