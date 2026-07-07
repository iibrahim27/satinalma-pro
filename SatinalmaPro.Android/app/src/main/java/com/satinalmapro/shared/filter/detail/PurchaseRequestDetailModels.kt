package com.satinalmapro.shared.filter.detail

import com.satinalmapro.shared.filter.ProcurementStatus

enum class PurchaseRequestDetailAction {
    DIRECT_APPROVE,
    REJECT_REQUEST,
    START_QUOTE_PROCESS,
    APPROVE_QUOTE,
    REJECT_ENTIRE_REQUEST,
    SEND_QUOTES_FOR_REVISION
}

enum class PurchaseRequestDetailScreen {
    MANAGEMENT_SUBMITTED_REVIEW,
    MANAGEMENT_QUOTE_REVIEW
}

data class PurchaseRequestDetailUiState(
    val requestId: String,
    val status: String,
    val priority: String,
    val screen: PurchaseRequestDetailScreen,
    val visibleActions: List<PurchaseRequestDetailAction>,
    val actionLabels: Map<PurchaseRequestDetailAction, String>,
    val showQuotesList: Boolean,
    val showPerQuoteApproveButtons: Boolean
) {
    fun isVisible(action: PurchaseRequestDetailAction): Boolean =
        action in visibleActions

    fun labelFor(action: PurchaseRequestDetailAction): String =
        actionLabels[action] ?: action.name
}

data class PurchaseRequestQuoteRow(
    val quoteId: String,
    val firmName: String,
    val canApprove: Boolean
)

data class PurchaseRequestDetailMutation(
    val newStatus: String,
    val newLegacyDurum: String? = null,
    val approvedQuoteId: String? = null,
    val quoteCorrectionNote: String? = null,
    val rejectionReason: String? = null,
    val teklifsizYonetimOnayi: Boolean = false,
    val yonetimOnayKilitli: Boolean = false,
    val clearApprovedQuote: Boolean = false,
    val clearLineItemApprovals: Boolean = false,
    val applyQuoteToAllLineItems: Boolean = false,
    val updatedAtUtcMs: Long = System.currentTimeMillis()
) {
    companion object {
        fun reject(reason: String) = PurchaseRequestDetailMutation(
            newStatus = ProcurementStatus.REJECTED,
            newLegacyDurum = "Reddedildi",
            rejectionReason = reason,
            yonetimOnayKilitli = false
        )

        fun directApprove() = PurchaseRequestDetailMutation(
            newStatus = ProcurementStatus.APPROVED,
            newLegacyDurum = "Onaylandı",
            teklifsizYonetimOnayi = true,
            yonetimOnayKilitli = true
        )

        fun startQuoteProcess() = PurchaseRequestDetailMutation(
            newStatus = ProcurementStatus.QUOTE_REQUESTED,
            newLegacyDurum = "Teklif Girişi",
            teklifsizYonetimOnayi = false,
            yonetimOnayKilitli = false
        )

        fun approveQuote(quoteId: String) = PurchaseRequestDetailMutation(
            newStatus = ProcurementStatus.APPROVED,
            newLegacyDurum = "Onaylandı",
            approvedQuoteId = quoteId,
            applyQuoteToAllLineItems = true,
            yonetimOnayKilitli = true
        )

        fun sendForRevision(note: String) = PurchaseRequestDetailMutation(
            newStatus = ProcurementStatus.COMPARISON,
            newLegacyDurum = "Karşılaştırma",
            quoteCorrectionNote = note,
            clearApprovedQuote = true,
            clearLineItemApprovals = true,
            yonetimOnayKilitli = false
        )
    }
}
