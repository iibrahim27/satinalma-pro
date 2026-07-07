namespace SatinalmaPro.Shared.Procurement;

/// <summary>Satınalma modülü ana sekmeleri — Android ve masaüstü ortak.</summary>
public enum ProcurementTab
{
    PendingDraft,
    ManagementApproval,
    QuoteRequested,
    QuoteReview,
    ApprovedOrders,
    Rejected,
    HistoryReports,
    StockMovements,
    StockStatus
}

public static class ProcurementTabExtensions
{
    public static string Title(this ProcurementTab tab) => tab switch
    {
        ProcurementTab.PendingDraft => "Beklemede / Taslak",
        ProcurementTab.ManagementApproval => "Yönetim Onayda",
        ProcurementTab.QuoteRequested => "Teklif İstendi",
        ProcurementTab.QuoteReview => "Teklif İnceleme",
        ProcurementTab.ApprovedOrders => "Onaylananlar / Sipariş",
        ProcurementTab.Rejected => "Reddedilenler",
        ProcurementTab.HistoryReports => "Geçmiş Raporlar",
        ProcurementTab.StockMovements => "Stok / Hareketler",
        ProcurementTab.StockStatus => "Stok Durumu",
        _ => tab.ToString()
    };

    public static string RouteKey(this ProcurementTab tab) => tab switch
    {
        ProcurementTab.PendingDraft => "pending-draft",
        ProcurementTab.ManagementApproval => "management-approval",
        ProcurementTab.QuoteRequested => "quote-requested",
        ProcurementTab.QuoteReview => "quote-review",
        ProcurementTab.ApprovedOrders => "approved-orders",
        ProcurementTab.Rejected => "rejected",
        ProcurementTab.HistoryReports => "history-reports",
        ProcurementTab.StockMovements => "stock-movements",
        ProcurementTab.StockStatus => "stock-status",
        _ => "dashboard"
    };

    public static bool IsProcurementCollection(this ProcurementTab tab) =>
        tab is not (ProcurementTab.StockMovements or ProcurementTab.StockStatus);
}
