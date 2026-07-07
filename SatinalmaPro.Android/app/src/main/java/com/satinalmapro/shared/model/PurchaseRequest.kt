package com.satinalmapro.shared.model

/**
 * Firestore: `procurement_requests/{requestId}`
 *
 * Satınalma talebi (aggregate root).
 * Kalemler ve teklifler alt koleksiyonlarda tutulur; uygulama katmanında birleştirilmiş
 * görünüm için [lineItems] ve [quotes] listeleri isteğe bağlı doldurulur.
 */
data class PurchaseRequest(
    /** Belge PK — UUID */
    val id: String = "",
    /** Örn. TLP-2026-001 */
    val requestNo: String = "",
    val requestDate: String = "",
    /** Normal, Acil */
    val requestType: String = "",
    /** SatinalmaTalepDurumlari */
    val status: String = "",
    /** FK → sites/{siteId} */
    val siteId: String? = null,
    /** Denormalize şantiye adı */
    val siteName: String? = null,
    val requesterName: String = "",
    /** Talep eden kullanıcı UID (legacy: olusturanUid) */
    val requesterUid: String = "",
    /** Talep eden rol (legacy: olusturanRol) */
    val requesterRole: String? = null,
    val description: String? = null,
    /** Red gerekçesi (legacy: redGerekcesi) */
    val rejectionReason: String? = null,
    /** Teklif düzeltme notu (legacy: teklifDuzeltmeNotu) */
    val quoteRevisionNote: String? = null,
    /** Teklif İste, Doğrudan Onay, Reddet */
    val managementDecision: String? = null,
    val managementApproverUid: String? = null,
    val managementApproverName: String? = null,
    val managementApprovalDate: String? = null,
    /** Yönetim onayı kilitli (legacy: yonetimOnayKilitli) */
    val managementApprovalLocked: Boolean = false,
    /** Teklifsiz yönetim onayı (legacy: teklifsizYonetimOnayi) */
    val directApprovalWithoutQuote: Boolean = false,
    /** Satınalma önerisi teklif ID (legacy: yonetimOnerilenTeklifId) */
    val recommendedQuoteId: String? = null,
    /** Öneri elle seçildi (legacy: satinalmaOnerisiElleSecildi) */
    val manualRecommendationSelected: Boolean = false,
    val approvedQuoteId: String? = null,
    val orderNo: String? = null,
    /** quoteId → sipariş no eşlemesi (legacy: firmaSiparisNolari) */
    val firmOrderNumbers: Map<String, String> = emptyMap(),
    /** Optimistic concurrency — ms (legacy: guncellemeUtc) */
    val updatedAtUtc: Long = 0L,
    val createdAt: Long? = null,
    val updatedAt: Long? = null,
    /** Soft delete zamanı */
    val deletedAt: Long? = null,
    /** Optimistic lock sürümü */
    val version: Long = 0L,
    /**
     * Alt koleksiyon: `line_items/{lineItemId}`
     * Firestore'da gömülü değil; okuma sonrası aggregate'e eklenir.
     */
    val lineItems: List<PurchaseRequestLineItem> = emptyList(),
    /**
     * Alt koleksiyon: `quotes/{quoteId}`
     * Firestore'da gömülü değil; okuma sonrası aggregate'e eklenir.
     */
    val quotes: List<Quote> = emptyList()
) {
    companion object {
        const val COLLECTION = "procurement_requests"
        const val SUBCOLLECTION_LINE_ITEMS = "line_items"
        const val SUBCOLLECTION_QUOTES = "quotes"
        const val SUBCOLLECTION_COMMENTS = "comments"
        const val SUBCOLLECTION_ATTACHMENTS = "attachments"
        const val SUBCOLLECTION_AUDIT_TRAIL = "audit_trail"
    }
}

/**
 * Firestore: `procurement_requests/{requestId}/line_items/{lineItemId}`
 */
data class PurchaseRequestLineItem(
    val id: String = "",
    val sequenceNo: Int = 1,
    /** FK → materials/{materialId} (opsiyonel) */
    val materialId: String? = null,
    val materialName: String = "",
    val quantity: Double = 0.0,
    val unit: String = "",
    val description: String? = null,
    val approvedQuoteId: String? = null,
    val acceptedQuantity: Double = 0.0,
    val orderCompleted: Boolean = false
) {
    val remainingQuantity: Double
        get() = (quantity - acceptedQuantity).coerceAtLeast(0.0)
}
