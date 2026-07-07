namespace SatinalmaPro.Shared.Procurement.Detail;

/// <summary>Talep detay ekranında tetiklenebilir aksiyonlar.</summary>
public enum PurchaseRequestDetailAction
{
    /// <summary>Teklifsiz / acil doğrudan onay → <c>approved</c></summary>
    DirectApprove,

    /// <summary>Talebi reddet → <c>rejected</c></summary>
    RejectRequest,

    /// <summary>Teklif sürecini başlat → <c>quote_requested</c></summary>
    StartQuoteProcess,

    /// <summary>Tek bir firma teklifini onayla → <c>approved</c> + <c>approvedQuoteId</c></summary>
    ApproveQuote,

    /// <summary>Tüm talebi reddet (teklif inceleme aşamasında)</summary>
    RejectEntireRequest,

    /// <summary>Teklifleri revizeye gönder → <c>comparison</c> + <c>quoteCorrectionNote</c></summary>
    SendQuotesForRevision
}

public enum PurchaseRequestDetailScreen
{
    /// <summary>Yönetim — gelen talep (submitted)</summary>
    ManagementSubmittedReview,

    /// <summary>Yönetim — teklif inceleme (management_quote_review)</summary>
    ManagementQuoteReview
}
