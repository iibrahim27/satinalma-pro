package com.satinalmapro.shared.filter

import com.satinalmapro.android.core.model.TalepItem

/** Legacy [TalepItem] → enterprise sekme filtresi köprüsü. */
fun TalepItem.toProcurementSnapshot(): ProcurementRequestSnapshot =
    ProcurementRequestSnapshot(
        id = id,
        status = resolvedEnterpriseStatus(),
        requesterUid = olusturanUid,
        priority = resolvedEnterprisePriority(),
        requestType = talepTuru
    )

/**
 * Sekme filtreleri için Durum kaynağıdır.
 * Eski/stale `status` (ör. quote_requested kalmışken Durum=Karşılaştırma) yok sayılır.
 */
fun TalepItem.resolvedEnterpriseStatus(): String {
    val d = durum.trim()
    if (d.isBlank()) {
        return if (status.isNotBlank()) ProcurementStatus.normalize(status) else ProcurementStatus.DRAFT
    }

    return when (d) {
        "Taslak" -> ProcurementStatus.DRAFT
        "Reddedildi" -> ProcurementStatus.REJECTED
        "Sipariş Oluşturuldu", "Siparis Olusturuldu" -> {
            val kalemler = kalemler.filter { it.malzeme.isNotBlank() }
            val tamam = kalemler.isNotEmpty() && kalemler.all {
                it.siparisTamamlandi || it.kabulEdilenMiktar >= it.miktar - 0.0001
            }
            if (tamam) ProcurementStatus.COMPLETED else ProcurementStatus.ORDERED
        }
        "Onaylandı", "Onaylandi" -> ProcurementStatus.APPROVED
        "Karşılaştırma", "Karsilastirma" -> ProcurementStatus.COMPARISON
        "Teklif Girişi", "Teklif Girisi" -> {
            val gercekTeklif = teklifler.any { it.firmaAdi.isNotBlank() || it.fiyatlar.any { f -> f.birimFiyat > 0 } }
            if (!gercekTeklif) ProcurementStatus.QUOTE_REQUESTED
            else ProcurementStatus.QUOTE_ENTRY
        }
        "Yönetim Onayında", "Yonetim Onayinda" -> {
            val gercekTeklif = teklifler.any { it.firmaAdi.isNotBlank() || it.fiyatlar.any { f -> f.birimFiyat > 0 } }
            val kalemOnayli = kalemler.any { !it.onaylananTeklifId.isNullOrBlank() }
            when {
                gercekTeklif && !kalemOnayli -> ProcurementStatus.MANAGEMENT_QUOTE_REVIEW
                // Kalem/teklif onayı var ama Durum güncellenmemiş
                kalemOnayli || teklifsizYonetimOnayi || yonetimOnayKilitli -> ProcurementStatus.APPROVED
                else -> ProcurementStatus.SUBMITTED
            }
        }
        "Hazırlanıyor", "Hazirlaniyor", "İmza Sürecinde", "Imza Surecinde" -> ProcurementStatus.SUBMITTED
        else -> ProcurementStatus.normalize(d)
    }
}

fun TalepItem.resolvedEnterprisePriority(): String =
    if (priority.isNotBlank()) ProcurementPriority.normalize(priority)
    else ProcurementPriority.fromRequestType(talepTuru)
