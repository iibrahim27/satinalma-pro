package com.satinalmapro.android.data.local

import android.content.Context
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.saas.TenantSession

data class RequestDraftLine(
    val malzeme: String = "",
    val miktar: String = "",
    val birim: String = "Adet"
)

data class RequestDraft(
    val site: String = "",
    val aciklama: String = "",
    val oncelikIndex: Int = 1,
    val lines: List<RequestDraftLine> = listOf(RequestDraftLine())
)

class RequestDraftStore(context: Context) {
    private val prefs = context.getSharedPreferences("satinalma_draft", Context.MODE_PRIVATE)
    private val gson = Gson()
    private val type = object : TypeToken<RequestDraft>() {}.type

    private fun key(): String {
        val tid = TenantSession.tenantId().orEmpty().ifBlank { "_none" }
        return "${KEY_PREFIX}_$tid"
    }

    fun load(): RequestDraft? {
        val json = prefs.getString(key(), null) ?: return null
        return runCatching { gson.fromJson<RequestDraft>(json, type) }.getOrNull()
    }

    fun save(draft: RequestDraft) {
        prefs.edit().putString(key(), gson.toJson(draft)).apply()
    }

    fun clear() {
        prefs.edit().remove(key()).apply()
    }

    /** Tüm kiracı taslaklarını siler (çıkış / güvenlik). */
    fun clearAll() {
        prefs.edit().clear().apply()
    }

    companion object {
        private const val KEY_PREFIX = "yeni_talep_draft"
    }
}
