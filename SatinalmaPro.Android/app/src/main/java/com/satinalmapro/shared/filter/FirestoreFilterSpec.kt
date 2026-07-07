package com.satinalmapro.shared.filter

data class FirestoreOrderBy(
    val field: String = "updatedAtUtc",
    val descending: Boolean = true
)

/**
 * Firestore sorgu şartları — platform bağımsız tanım.
 * Android: [com.satinalmapro.shared.filter.FirestoreQueryBuilder]
 */
data class FirestoreFilterSpec(
    val collection: String = "procurement_requests",
    val statusIn: List<String> = emptyList(),
    val requesterUidEquals: String? = null,
    val orderBy: List<FirestoreOrderBy> = listOf(FirestoreOrderBy()),
    val urgentFirst: Boolean = false,
    val readOnly: Boolean = false
) {
    companion object {
        fun forStockMovements(readOnly: Boolean = false) = FirestoreFilterSpec(
            collection = "stock_movements",
            orderBy = listOf(FirestoreOrderBy(field = "date", descending = true)),
            readOnly = readOnly
        )

        fun forStockItems(readOnly: Boolean = true) = FirestoreFilterSpec(
            collection = "stock_items",
            orderBy = listOf(FirestoreOrderBy(field = "materialName", descending = false)),
            readOnly = readOnly
        )
    }
}

data class ProcurementRequestSnapshot(
    val id: String = "",
    val status: String = "",
    val requesterUid: String = "",
    val priority: String = ProcurementPriority.NORMAL,
    val requestType: String = "Normal"
) {
    val normalizedStatus: String
        get() = ProcurementStatus.normalize(status)

    val effectivePriority: String
        get() = if (priority.isNotBlank() && !priority.equals(ProcurementPriority.NORMAL, ignoreCase = true)) {
            priority
        } else {
            ProcurementPriority.fromRequestType(requestType)
        }
}
