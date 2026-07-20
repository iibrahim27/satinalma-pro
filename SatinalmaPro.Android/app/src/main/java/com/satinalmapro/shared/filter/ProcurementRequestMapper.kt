package com.satinalmapro.shared.filter

import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepKalem
import com.satinalmapro.android.core.model.TeklifFiyat
import com.satinalmapro.android.core.model.TeklifItem

/** Legacy [TalepItem] → enterprise sekme filtresi köprüsü. */
fun TalepItem.toProcurementSnapshot(): ProcurementRequestSnapshot =
    ProcurementRequestSnapshot(
        id = id,
        status = resolvedEnterpriseStatus(),
        requesterUid = olusturanUid,
        priority = resolvedEnterprisePriority(),
        requestType = talepTuru
    )

@Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
private fun TalepItem.safeKalemler(): List<TalepKalem> =
    (kalemler as List<TalepKalem>?) ?: emptyList()

@Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
private fun TalepItem.safeTeklifler(): List<TeklifItem> =
    (teklifler as List<TeklifItem>?) ?: emptyList()

@Suppress("UNCHECKED_CAST", "USELESS_ELVIS")
private fun TeklifItem.safeFiyatlar(): List<TeklifFiyat> =
    (fiyatlar as List<TeklifFiyat>?) ?: emptyList()

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
            val kalemler = safeKalemler().filter { it.malzeme.isNotBlank() }
            val tamam = kalemler.isNotEmpty() && kalemler.all {
                it.siparisTamamlandi || it.kabulEdilenMiktar >= it.miktar - 0.0001
            }
            if (tamam) ProcurementStatus.COMPLETED else ProcurementStatus.ORDERED
        }
        "Onaylandı", "Onaylandi" -> ProcurementStatus.APPROVED
        "Karşılaştırma", "Karsilastirma" -> {
            if (teklifDuzeltmeNotu.isNotBlank()) ProcurementStatus.QUOTE_REQUESTED
            else ProcurementStatus.COMPARISON
        }
        "Teklif Girişi", "Teklif Girisi" -> {
            val gercekTeklif = safeTeklifler().any {
                it.firmaAdi.isNotBlank() || it.safeFiyatlar().any { f -> f.birimFiyat > 0 }
            }
            if (!gercekTeklif || teklifDuzeltmeNotu.isNotBlank()) ProcurementStatus.QUOTE_REQUESTED
            else ProcurementStatus.QUOTE_ENTRY
        }
        "Yönetim Onayında", "Yonetim Onayinda" -> {
            val gercekTeklif = safeTeklifler().any {
                it.firmaAdi.isNotBlank() || it.safeFiyatlar().any { f -> f.birimFiyat > 0 }
            }
            val kalemOnayli = safeKalemler().any { !it.onaylananTeklifId.isNullOrBlank() }
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
