package com.satinalmapro.shared.filter

object ProcurementStatus {
    const val DRAFT = "draft"
    const val SUBMITTED = "submitted"
    const val QUOTE_REQUESTED = "quote_requested"
    const val QUOTE_ENTRY = "quote_entry"
    const val COMPARISON = "comparison"
    const val MANAGEMENT_QUOTE_REVIEW = "management_quote_review"
    const val APPROVED = "approved"
    const val ORDERED = "ordered"
    const val REJECTED = "rejected"
    const val COMPLETED = "completed"

    val ALL = listOf(
        DRAFT, SUBMITTED, QUOTE_REQUESTED, QUOTE_ENTRY, COMPARISON,
        MANAGEMENT_QUOTE_REVIEW, APPROVED, ORDERED, REJECTED, COMPLETED
    )

    fun normalize(raw: String?): String {
        if (raw.isNullOrBlank()) return DRAFT
        val value = raw.trim()
        ALL.firstOrNull { it.equals(value, ignoreCase = true) }?.let { return it }
        return when (value) {
            "Taslak" -> DRAFT
            "Hazırlanıyor", "Hazirlaniyor" -> SUBMITTED
            "İmza Sürecinde", "Imza Surecinde" -> SUBMITTED
            "Yönetim Onayında", "Yonetim Onayinda" -> MANAGEMENT_QUOTE_REVIEW
            "Teklif Girişi", "Teklif Girisi" -> QUOTE_ENTRY
            "Karşılaştırma", "Karsilastirma" -> COMPARISON
            "Onaylandı", "Onaylandi" -> APPROVED
            "Sipariş Oluşturuldu", "Siparis Olusturuldu" -> ORDERED
            "Reddedildi" -> REJECTED
            else -> value.lowercase().replace(' ', '_')
        }
    }
}

object ProcurementPriority {
    const val NORMAL = "normal"
    const val URGENT = "urgent"

    fun fromRequestType(requestType: String?): String {
        if (requestType.isNullOrBlank()) return NORMAL
        return if (requestType.trim().equals("Acil", ignoreCase = true)) URGENT else NORMAL
    }

    fun normalize(raw: String?): String {
        if (raw.isNullOrBlank()) return NORMAL
        return if (raw.trim().equals(URGENT, ignoreCase = true) ||
            raw.trim().equals("Acil", ignoreCase = true)
        ) URGENT else NORMAL
    }
}
