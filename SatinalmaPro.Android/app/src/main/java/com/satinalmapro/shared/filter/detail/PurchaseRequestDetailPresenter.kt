package com.satinalmapro.shared.filter.detail

import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.shared.filter.ProcurementPriority
import com.satinalmapro.shared.filter.ProcurementStatus
import com.satinalmapro.shared.filter.resolvedEnterprisePriority
import com.satinalmapro.shared.filter.resolvedEnterpriseStatus

/**
 * Talep detay — durum, öncelik ve role göre buton görünürlüğü (Android + ortak kurallar).
 */
object PurchaseRequestDetailPresenter {

    private val defaultLabels = mapOf(
        PurchaseRequestDetailAction.DIRECT_APPROVE to "Direkt Onay Ver",
        PurchaseRequestDetailAction.REJECT_REQUEST to "Talebi Reddet",
        PurchaseRequestDetailAction.START_QUOTE_PROCESS to "Teklif Sürecini Başlat",
        PurchaseRequestDetailAction.APPROVE_QUOTE to "Bu Firmayı Onayla",
        PurchaseRequestDetailAction.REJECT_ENTIRE_REQUEST to "Talebi Komple Reddet",
        PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION to "Teklifleri Revizeye Gönder"
    )

    fun resolveScreen(talep: TalepItem): PurchaseRequestDetailScreen =
        if (talep.resolvedEnterpriseStatus().equals(ProcurementStatus.MANAGEMENT_QUOTE_REVIEW, ignoreCase = true))
            PurchaseRequestDetailScreen.MANAGEMENT_QUOTE_REVIEW
        else
            PurchaseRequestDetailScreen.MANAGEMENT_SUBMITTED_REVIEW

    fun buildUiState(
        talep: TalepItem,
        role: String?,
        screen: PurchaseRequestDetailScreen? = null
    ): PurchaseRequestDetailUiState {
        val resolvedScreen = screen ?: resolveScreen(talep)
        val status = talep.resolvedEnterpriseStatus()
        val priority = talep.resolvedEnterprisePriority()
        val canManage = canManagementDecide(role)

        val actions = mutableListOf<PurchaseRequestDetailAction>()
        val labels = defaultLabels.toMutableMap()

        if (canManage &&
            resolvedScreen == PurchaseRequestDetailScreen.MANAGEMENT_SUBMITTED_REVIEW &&
            status.equals(ProcurementStatus.SUBMITTED, ignoreCase = true)
        ) {
            val urgent = priority.equals(ProcurementPriority.URGENT, ignoreCase = true)
            if (urgent) {
                actions += PurchaseRequestDetailAction.DIRECT_APPROVE
                actions += PurchaseRequestDetailAction.REJECT_REQUEST
            } else {
                actions += PurchaseRequestDetailAction.START_QUOTE_PROCESS
                actions += PurchaseRequestDetailAction.REJECT_REQUEST
            }
        }

        var showQuotes = false
        var showPerQuoteApprove = false

        if (canManage &&
            resolvedScreen == PurchaseRequestDetailScreen.MANAGEMENT_QUOTE_REVIEW &&
            status.equals(ProcurementStatus.MANAGEMENT_QUOTE_REVIEW, ignoreCase = true)
        ) {
            showQuotes = talep.teklifler.isNotEmpty()
            showPerQuoteApprove = showQuotes && !talep.yonetimOnayKilitli && !talep.herhangiKalemOnayli
            actions += PurchaseRequestDetailAction.REJECT_ENTIRE_REQUEST
            actions += PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION
        }

        return PurchaseRequestDetailUiState(
            requestId = talep.id,
            status = status,
            priority = priority,
            screen = resolvedScreen,
            visibleActions = actions,
            actionLabels = labels,
            showQuotesList = showQuotes,
            showPerQuoteApproveButtons = showPerQuoteApprove
        )
    }

    fun buildQuoteRows(talep: TalepItem, role: String?): List<PurchaseRequestQuoteRow> {
        val ui = buildUiState(talep, role, PurchaseRequestDetailScreen.MANAGEMENT_QUOTE_REVIEW)
        if (!ui.showPerQuoteApproveButtons) return emptyList()

        return talep.teklifler
            .filter { it.firmaAdi.isNotBlank() || it.fiyatlar.any { f -> f.birimFiyat > 0 } }
            .map {
                PurchaseRequestQuoteRow(
                    quoteId = it.id,
                    firmName = it.firmaAdi.ifBlank { "—" },
                    canApprove = ui.showPerQuoteApproveButtons
                )
            }
    }

    fun createMutation(
        action: PurchaseRequestDetailAction,
        talep: TalepItem,
        role: String?,
        quoteId: String? = null,
        note: String? = null
    ): PurchaseRequestDetailMutation? {
        val ui = buildUiState(talep, role)

        if (action == PurchaseRequestDetailAction.APPROVE_QUOTE) {
            if (quoteId.isNullOrBlank() || !ui.showPerQuoteApproveButtons) return null
            return PurchaseRequestDetailMutation.approveQuote(quoteId)
        }

        if (action !in ui.visibleActions) return null

        return when (action) {
            PurchaseRequestDetailAction.DIRECT_APPROVE -> PurchaseRequestDetailMutation.directApprove()
            PurchaseRequestDetailAction.REJECT_REQUEST,
            PurchaseRequestDetailAction.REJECT_ENTIRE_REQUEST -> PurchaseRequestDetailMutation.reject(note.orEmpty())
            PurchaseRequestDetailAction.START_QUOTE_PROCESS -> PurchaseRequestDetailMutation.startQuoteProcess()
            PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION -> PurchaseRequestDetailMutation.sendForRevision(note.orEmpty())
            else -> null
        }
    }

    fun canManagementDecide(role: String?): Boolean =
        KullaniciRolleri.canManagementDecide(role)
}

private val TalepItem.herhangiKalemOnayli: Boolean
    get() = kalemler.any { !it.onaylananTeklifId.isNullOrBlank() }
