package com.satinalmapro.shared.filter

enum class ProcurementTab {
    PENDING_DRAFT,
    MANAGEMENT_APPROVAL,
    QUOTE_REQUESTED,
    QUOTE_REVIEW,
    APPROVED_ORDERS,
    REJECTED,
    HISTORY_REPORTS,
    STOCK_MOVEMENTS,
    STOCK_STATUS;

    val title: String
        get() = when (this) {
            PENDING_DRAFT -> "Beklemede / Taslak"
            MANAGEMENT_APPROVAL -> "Yönetim Onayda"
            QUOTE_REQUESTED -> "Teklif İstendi"
            QUOTE_REVIEW -> "Teklif İnceleme"
            APPROVED_ORDERS -> "Onaylananlar / Sipariş"
            REJECTED -> "Reddedilenler"
            HISTORY_REPORTS -> "Geçmiş Raporlar"
            STOCK_MOVEMENTS -> "Stok / Hareketler"
            STOCK_STATUS -> "Stok Durumu"
        }

    val routeKey: String
        get() = when (this) {
            PENDING_DRAFT -> "pending-draft"
            MANAGEMENT_APPROVAL -> "management-approval"
            QUOTE_REQUESTED -> "quote-requested"
            QUOTE_REVIEW -> "quote-review"
            APPROVED_ORDERS -> "approved-orders"
            REJECTED -> "rejected"
            HISTORY_REPORTS -> "history-reports"
            STOCK_MOVEMENTS -> "stock-movements"
            STOCK_STATUS -> "stock-status"
        }

    val isProcurementCollection: Boolean
        get() = this != STOCK_MOVEMENTS && this != STOCK_STATUS
}
