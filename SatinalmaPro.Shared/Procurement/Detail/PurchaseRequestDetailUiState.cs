namespace SatinalmaPro.Shared.Procurement.Detail;

public sealed class PurchaseRequestDetailUiState
{
    public required string RequestId { get; init; }
    public required string Status { get; init; }
    public required string Priority { get; init; }
    public required PurchaseRequestDetailScreen Screen { get; init; }

    /// <summary>Görünür aksiyonlar — sıra UI'da korunur.</summary>
    public IReadOnlyList<PurchaseRequestDetailAction> VisibleActions { get; init; } = [];

    /// <summary>Aksiyon → buton metni.</summary>
    public IReadOnlyDictionary<PurchaseRequestDetailAction, string> ActionLabels { get; init; }
        = new Dictionary<PurchaseRequestDetailAction, string>();

    public bool ShowQuotesList { get; init; }
    public bool ShowPerQuoteApproveButtons { get; init; }
    public bool RequiresRejectionReason { get; init; }
    public bool RequiresRevisionNote { get; init; }

    public bool IsActionVisible(PurchaseRequestDetailAction action) =>
        VisibleActions.Contains(action);

    public string LabelFor(PurchaseRequestDetailAction action) =>
        ActionLabels.TryGetValue(action, out var label) ? label : action.ToString();
}

public sealed class PurchaseRequestQuoteRow
{
    public required string QuoteId { get; init; }
    public required string FirmName { get; init; }
    public bool CanApprove { get; init; }
}
