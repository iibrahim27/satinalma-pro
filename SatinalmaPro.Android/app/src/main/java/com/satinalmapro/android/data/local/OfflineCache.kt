package com.satinalmapro.android.data.local

import android.content.Context
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.model.TalepItem

/**
 * Kiracıya özel offline talep önbelleği.
 * Aynı cihazda firma değiştirince başka firmanın verisi asla gösterilmez.
 */
class OfflineCache(context: Context) {
    private val prefs = context.getSharedPreferences("satinalma_cache", Context.MODE_PRIVATE)
    private val gson = Gson()
    private val talepType = object : TypeToken<List<TalepItem>>() {}.type

    fun saveTalepler(tenantId: String, list: List<TalepItem>) {
        val tid = tenantId.trim()
        if (tid.isBlank()) return
        prefs.edit()
            .putString(taleplerKey(tid), gson.toJson(list))
            .putString(KEY_LAST_TENANT, tid)
            // Eski global anahtarı temizle (çapraz kiracı sızıntısı).
            .remove(KEY_LEGACY_TALEPLER)
            .apply()
    }

    fun loadTalepler(tenantId: String): List<TalepItem> {
        val tid = tenantId.trim()
        if (tid.isBlank()) return emptyList()
        val json = prefs.getString(taleplerKey(tid), null) ?: return emptyList()
        return runCatching {
            gson.fromJson<List<TalepItem>>(json, talepType) ?: emptyList()
        }.getOrDefault(emptyList())
    }

    /** Firma değişiminde veya çıkışta tüm önbelleği sil. */
    fun clearAll() {
        prefs.edit().clear().apply()
    }

    fun clearTenant(tenantId: String) {
        val tid = tenantId.trim()
        if (tid.isBlank()) return
        prefs.edit().remove(taleplerKey(tid)).apply()
    }

    private fun taleplerKey(tenantId: String) = "talepler_json_$tenantId"

    companion object {
        private const val KEY_LEGACY_TALEPLER = "talepler_json"
        private const val KEY_LAST_TENANT = "last_tenant_id"
    }
}
