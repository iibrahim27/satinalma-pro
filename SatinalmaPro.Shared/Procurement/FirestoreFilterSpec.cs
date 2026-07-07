namespace SatinalmaPro.Shared.Procurement;

public sealed class FirestoreOrderBy
{
    public string Field { get; init; } = "updatedAtUtc";
    public bool Descending { get; init; } = true;
}

/// <summary>
/// Firestore sorgu şartları — platform bağımsız tanım.
/// Android: <c>FirebaseFirestore</c> · Masaüstü: REST structured query veya bellek içi filtre.
/// </summary>
public sealed class FirestoreFilterSpec
{
    public string Collection { get; init; } = "procurement_requests";
    public IReadOnlyList<string> StatusIn { get; init; } = [];
    public string? RequesterUidEquals { get; init; }
    public IReadOnlyList<FirestoreOrderBy> OrderBy { get; init; } = [new() { Field = "updatedAtUtc", Descending = true }];
    public bool UrgentFirst { get; init; }
    public bool ReadOnly { get; init; }
    public bool RequiresReturnFlag { get; init; }

    public static FirestoreFilterSpec ForStockMovements(bool readOnly = false) => new()
    {
        Collection = "stock_movements",
        OrderBy = [new() { Field = "date", Descending = true }],
        ReadOnly = readOnly
    };

    public static FirestoreFilterSpec ForStockItems(bool readOnly = true) => new()
    {
        Collection = "stock_items",
        OrderBy = [new() { Field = "materialName", Descending = false }],
        ReadOnly = readOnly
    };
}

/// <summary>Sekme eşleştirmesi için minimal talep görünümü.</summary>
public sealed class ProcurementRequestSnapshot
{
    public string Id { get; init; } = "";
    public string Status { get; init; } = "";
    public string RequesterUid { get; init; } = "";
    public string Priority { get; init; } = ProcurementPriority.Normal;
    public string RequestType { get; init; } = "Normal";

    public string NormalizedStatus => ProcurementStatus.Normalize(Status);

    public string EffectivePriority =>
        !string.IsNullOrWhiteSpace(Priority) && !Priority.Equals(ProcurementPriority.Normal, StringComparison.OrdinalIgnoreCase)
            ? Priority
            : ProcurementPriority.FromRequestType(RequestType);
}
