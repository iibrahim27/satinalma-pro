package com.satinalmapro.android.data.detail

import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.data.repository.TalepRepository
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailPresenter

/**
 * Talep detay aksiyonlarını repository üzerinden uygular.
 */
class PurchaseRequestDetailController(
    private val repository: TalepRepository
) {
    fun uiState(talep: TalepItem, role: String?) =
        PurchaseRequestDetailPresenter.buildUiState(talep, role)

    fun quoteRows(talep: TalepItem, role: String?) =
        PurchaseRequestDetailPresenter.buildQuoteRows(talep, role)

    suspend fun apply(
        talepId: String,
        user: UserProfile,
        action: PurchaseRequestDetailAction,
        quoteId: String? = null,
        note: String? = null
    ): TalepItem {
        val mevcut = repository.loadTalepler().firstOrNull { it.id.equals(talepId, true) }
            ?: throw IllegalArgumentException("Talep bulunamadı")

        val mutation = PurchaseRequestDetailPresenter.createMutation(
            action, mevcut, user.role, quoteId, note
        ) ?: throw IllegalStateException("Bu aksiyon şu an uygulanamaz.")

        return repository.applyDetailMutation(talepId, user, mutation, action, note)
    }
}
