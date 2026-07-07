package com.satinalmapro.shared.model

/**
 * Firestore: `procurement_requests/{requestId}/quotes/{quoteId}`
 *
 * Teklif belgesi. Üst talep ID'si alt koleksiyon yolundan gelir;
 * aggregate okumada [requestId] alanı doldurulur.
 */
data class Quote(
    val id: String = "",
    /**
     * Üst talep ID — alt koleksiyon yolundan türetilir,
     * Firestore belgesinde zorunlu alan değildir.
     */
    val requestId: String? = null,
    /** FK → suppliers/{supplierId} */
    val supplierId: String? = null,
    /** Tedarikçi / firma adı (legacy: firmaAdi) */
    val supplierName: String = "",
    val brand: String? = null,
    val paymentTermDays: Int = 0,
    val deliveryTime: String? = null,
    val paymentMethod: String? = null,
    val vatRate: Double = 20.0,
    val description: String? = null,
    val usdRate: Double = 0.0,
    val eurRate: Double = 0.0,
    /** Onaylandı mı (legacy: onaylandi) */
    val approved: Boolean = false,
    /** Genel toplam */
    val totalAmount: Double = 0.0,
    /** Teklif tarihi (ISO8601 veya uygulama formatı) */
    val quoteDate: String? = null,
    /** Firebase Storage URL */
    val attachmentUrl: String? = null,
    /**
     * Alt koleksiyon: `quote_prices/{priceId}`
     * Firestore'da gömülü değil; okuma sonrası aggregate'e eklenir.
     */
    val prices: List<QuotePrice> = emptyList()
) {
    val subtotal: Double
        get() = prices.sumOf { it.subtotal }

    val vatAmount: Double
        get() = prices.sumOf { it.vatAmount }

    val totalWithVat: Double
        get() = if (prices.isNotEmpty()) {
            prices.sumOf { it.totalWithVat }
        } else {
            totalAmount
        }

    companion object {
        const val SUBCOLLECTION = "quotes"
        const val SUBCOLLECTION_PRICES = "quote_prices"
    }
}

/**
 * Firestore: `procurement_requests/{requestId}/quotes/{quoteId}/quote_prices/{priceId}`
 */
data class QuotePrice(
    /** priceId — belge kimliği (opsiyonel, okuma sırasında set edilir) */
    val id: String? = null,
    /** FK → line_items/{lineItemId} */
    val lineItemId: String = "",
    val brand: String? = null,
    /** TRY, USD, EUR */
    val currency: String = "TRY",
    val unitPrice: Double = 0.0,
    val vatRate: Double = 20.0,
    val subtotal: Double = 0.0,
    val vatAmount: Double = 0.0,
    val totalWithVat: Double = 0.0
)
