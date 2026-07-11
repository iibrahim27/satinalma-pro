package com.satinalmapro.shared.filter

import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.shared.model.PurchaseRequest

/**
 * Rol ve sekmeye göre Firestore sorgu şartlarını tek merkezden üretir.
 * Masaüstü (C# TabFilterManager) ile birebir aynı kurallar.
 */
object TabFilterManager {

    private val fullProcurementTabs = listOf(
        ProcurementTab.PENDING_DRAFT,
        ProcurementTab.MANAGEMENT_APPROVAL,
        ProcurementTab.QUOTE_REQUESTED,
        ProcurementTab.QUOTE_REVIEW,
        ProcurementTab.APPROVED_ORDERS,
        ProcurementTab.REJECTED,
        ProcurementTab.HISTORY_REPORTS
    )

    fun normalizeRole(role: String?): String {
        return when (KullaniciRolleri.normalize(role)) {
            KullaniciRolleri.ADMIN -> "admin"
            KullaniciRolleri.YONETIM -> "yonetim"
            KullaniciRolleri.SATINALMA -> "satinalma"
            KullaniciRolleri.SEF -> "sef"
            KullaniciRolleri.SAHA -> "saha"
            KullaniciRolleri.DEPO -> "depo"
            KullaniciRolleri.ATOLYE -> "atolye"
            else -> KullaniciRolleri.normalize(role).lowercase()
        }
    }

    /** Liste sorgularında oluşturan filtresi yok — firma içi tüm talepler görünür. */
    fun requiresRequesterScope(role: String?): Boolean = false

    fun getVisibleTabs(role: String?): List<ProcurementTab> {
        return when (normalizeRole(role)) {
            "depo" -> listOf(
                ProcurementTab.APPROVED_ORDERS,
                ProcurementTab.HISTORY_REPORTS,
                ProcurementTab.STOCK_MOVEMENTS
            )
            "atolye" -> listOf(ProcurementTab.STOCK_STATUS)
            "admin", "yonetim", "satinalma" -> fullProcurementTabs
            "sef", "saha" -> fullProcurementTabs
            else -> fullProcurementTabs
        }
    }

    fun isTabVisible(role: String?, tab: ProcurementTab): Boolean =
        getVisibleTabs(role).contains(tab)

    fun getQuerySpec(
        tab: ProcurementTab,
        role: String?,
        currentUid: String?
    ): FirestoreFilterSpec? {
        if (!isTabVisible(role, tab)) return null

        val requesterUid = if (requiresRequesterScope(role)) currentUid else null
        if (requiresRequesterScope(role) && requesterUid.isNullOrBlank()) return null

        return when (tab) {
            ProcurementTab.PENDING_DRAFT -> FirestoreFilterSpec(
                collection = "procurement_requests",
                statusIn = listOf(ProcurementStatus.DRAFT, ProcurementStatus.SUBMITTED),
                requesterUidEquals = requesterUid,
                orderBy = listOf(FirestoreOrderBy(field = "updatedAtUtc", descending = true))
            )
            ProcurementTab.MANAGEMENT_APPROVAL -> FirestoreFilterSpec(
                collection = "procurement_requests",
                statusIn = listOf(ProcurementStatus.SUBMITTED),
                requesterUidEquals = requesterUid,
                urgentFirst = true,
                orderBy = listOf(
                    FirestoreOrderBy(field = "priority", descending = true),
                    FirestoreOrderBy(field = "updatedAtUtc", descending = true)
                )
            )
            ProcurementTab.QUOTE_REQUESTED -> FirestoreFilterSpec(
                collection = "procurement_requests",
                statusIn = listOf(
                    ProcurementStatus.QUOTE_REQUESTED,
                    ProcurementStatus.QUOTE_ENTRY,
                    ProcurementStatus.COMPARISON
                ),
                requesterUidEquals = requesterUid,
                orderBy = listOf(FirestoreOrderBy(field = "updatedAtUtc", descending = true))
            )
            ProcurementTab.QUOTE_REVIEW -> FirestoreFilterSpec(
                collection = "procurement_requests",
                statusIn = listOf(ProcurementStatus.MANAGEMENT_QUOTE_REVIEW),
                requesterUidEquals = requesterUid,
                orderBy = listOf(FirestoreOrderBy(field = "updatedAtUtc", descending = true))
            )
            ProcurementTab.APPROVED_ORDERS -> FirestoreFilterSpec(
                collection = "procurement_requests",
                statusIn = listOf(ProcurementStatus.APPROVED, ProcurementStatus.ORDERED),
                requesterUidEquals = requesterUid,
                orderBy = listOf(FirestoreOrderBy(field = "updatedAtUtc", descending = true))
            )
            ProcurementTab.REJECTED -> FirestoreFilterSpec(
                collection = "procurement_requests",
                statusIn = listOf(ProcurementStatus.REJECTED),
                requesterUidEquals = requesterUid,
                orderBy = listOf(FirestoreOrderBy(field = "updatedAtUtc", descending = true))
            )
            ProcurementTab.HISTORY_REPORTS -> FirestoreFilterSpec(
                collection = "procurement_requests",
                statusIn = listOf(ProcurementStatus.COMPLETED),
                requesterUidEquals = requesterUid,
                orderBy = listOf(FirestoreOrderBy(field = "updatedAtUtc", descending = true))
            )
            ProcurementTab.STOCK_MOVEMENTS -> FirestoreFilterSpec.forStockMovements(readOnly = false)
            ProcurementTab.STOCK_STATUS -> FirestoreFilterSpec.forStockItems(readOnly = true)
        }
    }

    fun statusesForTab(tab: ProcurementTab): List<String> = when (tab) {
        ProcurementTab.PENDING_DRAFT -> listOf(ProcurementStatus.DRAFT, ProcurementStatus.SUBMITTED)
        ProcurementTab.MANAGEMENT_APPROVAL -> listOf(ProcurementStatus.SUBMITTED)
        ProcurementTab.QUOTE_REQUESTED -> listOf(
            ProcurementStatus.QUOTE_REQUESTED,
            ProcurementStatus.QUOTE_ENTRY,
            ProcurementStatus.COMPARISON
        )
        ProcurementTab.QUOTE_REVIEW -> listOf(ProcurementStatus.MANAGEMENT_QUOTE_REVIEW)
        ProcurementTab.APPROVED_ORDERS -> listOf(ProcurementStatus.APPROVED, ProcurementStatus.ORDERED)
        ProcurementTab.REJECTED -> listOf(ProcurementStatus.REJECTED)
        ProcurementTab.HISTORY_REPORTS -> listOf(ProcurementStatus.COMPLETED)
        ProcurementTab.STOCK_MOVEMENTS, ProcurementTab.STOCK_STATUS -> emptyList()
    }

    fun matchesTab(
        tab: ProcurementTab,
        request: ProcurementRequestSnapshot,
        role: String?,
        currentUid: String?
    ): Boolean {
        if (!isTabVisible(role, tab)) return false
        if (tab == ProcurementTab.STOCK_MOVEMENTS || tab == ProcurementTab.STOCK_STATUS) return false

        if (requiresRequesterScope(role)) {
            if (currentUid.isNullOrBlank()) return false
            if (!request.requesterUid.equals(currentUid, ignoreCase = true)) return false
        }

        val allowed = statusesForTab(tab)
        return allowed.any { it.equals(request.normalizedStatus, ignoreCase = true) }
    }

    fun <T> filterForTab(
        tab: ProcurementTab,
        source: List<T>,
        selector: (T) -> ProcurementRequestSnapshot,
        role: String?,
        currentUid: String?
    ): List<T> {
        if (!isTabVisible(role, tab)) return emptyList()
        return source.filter { item -> matchesTab(tab, selector(item), role, currentUid) }
    }

    fun <T> sortForTab(
        tab: ProcurementTab,
        source: List<T>,
        selector: (T) -> ProcurementRequestSnapshot
    ): List<T> {
        if (tab != ProcurementTab.MANAGEMENT_APPROVAL) return source
        return source.sortedWith(
            compareByDescending<T> {
                selector(it).effectivePriority.equals(ProcurementPriority.URGENT, ignoreCase = true)
            }.thenByDescending { selector(it).id }
        )
    }

    fun createContext(role: String?, currentUid: String?): TabFilterContext =
        TabFilterContext(role, currentUid)

    fun snapshotFrom(request: PurchaseRequest): ProcurementRequestSnapshot =
        ProcurementRequestSnapshot(
            id = request.id,
            status = request.status,
            requesterUid = request.requesterUid,
            priority = ProcurementPriority.fromRequestType(request.requestType),
            requestType = request.requestType
        )
}

class TabFilterContext(
    role: String?,
    val currentUid: String?
) {
    val roleKey: String = TabFilterManager.normalizeRole(role)
    val visibleTabs: List<ProcurementTab> = TabFilterManager.getVisibleTabs(role)

    fun isVisible(tab: ProcurementTab): Boolean = visibleTabs.contains(tab)

    fun queryFor(tab: ProcurementTab): FirestoreFilterSpec? =
        TabFilterManager.getQuerySpec(tab, roleKey, currentUid)

    fun matches(tab: ProcurementTab, request: ProcurementRequestSnapshot): Boolean =
        TabFilterManager.matchesTab(tab, request, roleKey, currentUid)
}
