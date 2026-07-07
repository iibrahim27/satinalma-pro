namespace SatinalmaPro.Shared.Procurement.Detail;

/// <summary>
/// Talep detay aksiyonu sonrası uygulanacak alan güncellemeleri.
/// Legacy <c>Durum</c> ve enterprise <c>status</c> birlikte set edilir.
/// </summary>
public sealed class PurchaseRequestDetailMutation
{
    public required string NewStatus { get; init; }
    public string? NewLegacyDurum { get; init; }
    public string? ApprovedQuoteId { get; init; }
    public string? QuoteCorrectionNote { get; init; }
    public string? RejectionReason { get; init; }
    public bool TeklifsizYonetimOnayi { get; init; }
    public bool YonetimOnayKilitli { get; init; }
    public bool ClearApprovedQuote { get; init; }
    public bool ClearLineItemApprovals { get; init; }
    public bool ApplyQuoteToAllLineItems { get; init; }
    public long UpdatedAtUtcMs { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static PurchaseRequestDetailMutation Reject(string reason) => new()
    {
        NewStatus = ProcurementStatus.Rejected,
        NewLegacyDurum = "Reddedildi",
        RejectionReason = reason,
        YonetimOnayKilitli = false
    };

    public static PurchaseRequestDetailMutation DirectApprove() => new()
    {
        NewStatus = ProcurementStatus.Approved,
        NewLegacyDurum = "Onaylandı",
        TeklifsizYonetimOnayi = true,
        YonetimOnayKilitli = true
    };

    public static PurchaseRequestDetailMutation StartQuoteProcess() => new()
    {
        NewStatus = ProcurementStatus.QuoteRequested,
        NewLegacyDurum = "Teklif Girişi",
        TeklifsizYonetimOnayi = false,
        YonetimOnayKilitli = false
    };

    public static PurchaseRequestDetailMutation ApproveQuote(string quoteId) => new()
    {
        NewStatus = ProcurementStatus.Approved,
        NewLegacyDurum = "Onaylandı",
        ApprovedQuoteId = quoteId,
        ApplyQuoteToAllLineItems = true,
        YonetimOnayKilitli = true
    };

    public static PurchaseRequestDetailMutation SendForRevision(string note) => new()
    {
        NewStatus = ProcurementStatus.Comparison,
        NewLegacyDurum = "Karşılaştırma",
        QuoteCorrectionNote = note,
        ClearApprovedQuote = true,
        ClearLineItemApprovals = true,
        YonetimOnayKilitli = false
    };
}

public sealed class PurchaseRequestFirestorePatch
{
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? ApprovedQuoteId { get; init; }
    public string? QuoteRevisionNote { get; init; }
    public string? RejectionReason { get; init; }
    public long UpdatedAtUtcMs { get; init; }

    public static PurchaseRequestFirestorePatch FromMutation(
        PurchaseRequestDetailMutation mutation,
        string? priority) => new()
    {
        Status = mutation.NewStatus,
        Priority = priority,
        ApprovedQuoteId = mutation.ClearApprovedQuote ? "" : mutation.ApprovedQuoteId,
        QuoteRevisionNote = mutation.QuoteCorrectionNote,
        RejectionReason = mutation.RejectionReason,
        UpdatedAtUtcMs = mutation.UpdatedAtUtcMs
    };
}
