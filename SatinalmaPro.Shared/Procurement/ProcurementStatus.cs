namespace SatinalmaPro.Shared.Procurement;

/// <summary>Firestore <c>procurement_requests.status</c> — enterprise değerleri.</summary>
public static class ProcurementStatus
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string QuoteRequested = "quote_requested";
    public const string QuoteEntry = "quote_entry";
    public const string Comparison = "comparison";
    public const string ManagementQuoteReview = "management_quote_review";
    public const string Approved = "approved";
    public const string Ordered = "ordered";
    public const string Rejected = "rejected";
    public const string Completed = "completed";

    public static readonly IReadOnlyList<string> All =
    [
        Draft, Submitted, QuoteRequested, QuoteEntry, Comparison,
        ManagementQuoteReview, Approved, Ordered, Rejected, Completed
    ];

    /// <summary>Legacy Türkçe durum → enterprise status.</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Draft;

        var value = raw.Trim();
        if (All.Contains(value, StringComparer.OrdinalIgnoreCase))
            return All.First(s => s.Equals(value, StringComparison.OrdinalIgnoreCase));

        return value switch
        {
            "Taslak" => Draft,
            "Hazırlanıyor" or "Hazirlaniyor" => Submitted,
            "İmza Sürecinde" or "Imza Surecinde" => Submitted,
            "Yönetim Onayında" or "Yonetim Onayinda" => ManagementQuoteReview,
            "Teklif Girişi" or "Teklif Girisi" => QuoteEntry,
            "Karşılaştırma" or "Karsilastirma" => Comparison,
            "Onaylandı" or "Onaylandi" => Approved,
            "Sipariş Oluşturuldu" or "Siparis Olusturuldu" => Ordered,
            "Reddedildi" => Rejected,
            _ => value.ToLowerInvariant().Replace(' ', '_')
        };
    }
}

public static class ProcurementPriority
{
    public const string Normal = "normal";
    public const string Urgent = "urgent";

    public static string FromRequestType(string? requestType)
    {
        if (string.IsNullOrWhiteSpace(requestType))
            return Normal;
        return requestType.Trim().Equals("Acil", StringComparison.OrdinalIgnoreCase)
            ? Urgent
            : Normal;
    }

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Normal;
        return raw.Trim().Equals(Urgent, StringComparison.OrdinalIgnoreCase)
            || raw.Trim().Equals("Acil", StringComparison.OrdinalIgnoreCase)
            ? Urgent
            : Normal;
    }
}
