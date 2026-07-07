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

fun TalepItem.resolvedEnterpriseStatus(): String =
    if (status.isNotBlank()) ProcurementStatus.normalize(status)
    else ProcurementStatus.normalize(durum)

fun TalepItem.resolvedEnterprisePriority(): String =
    if (priority.isNotBlank()) ProcurementPriority.normalize(priority)
    else ProcurementPriority.fromRequestType(talepTuru)
