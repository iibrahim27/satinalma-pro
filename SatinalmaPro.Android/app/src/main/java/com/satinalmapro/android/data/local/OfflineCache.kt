package com.satinalmapro.android.data.local

import android.content.Context
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.model.AppNotification
import com.satinalmapro.android.core.model.StokKaydi
import com.satinalmapro.android.core.model.TalepItem

/**
 * Kiracıya özel offline önbellek (talep + bildirim + stok).
 * Aynı cihazda firma değiştirince başka firmanın verisi asla gösterilmez.
 */
class OfflineCache(context: Context) {
    private val prefs = context.getSharedPreferences("satinalma_cache", Context.MODE_PRIVATE)
    private val gson = Gson()
    private val talepType = object : TypeToken<List<TalepItem>>() {}.type
    private val notifType = object : TypeToken<List<AppNotification>>() {}.type
    private val stokType = object : TypeToken<List<StokKaydi>>() {}.type

    fun saveTalepler(tenantId: String, list: List<TalepItem>) {
        val tid = tenantId.trim()
        if (tid.isBlank()) return
        prefs.edit()
            .putString(taleplerKey(tid), gson.toJson(list))
            .putString(KEY_LAST_TENANT, tid)
            .remove(KEY_LEGACY_TALEPLER)
            .apply()
    }

    fun loadTalepler(tenantId: String): List<TalepItem> {
        val tid = tenantId.trim()
        if (tid.isBlank()) return emptyList()
        val json = prefs.getString(taleplerKey(tid), null) ?: return emptyList()
        return runCatching {
            (gson.fromJson<List<TalepItem?>>(json, talepType) ?: emptyList())
                .mapNotNull { item -> runCatching { item?.normalized() }.getOrNull() }
        }.getOrDefault(emptyList())
    }

    fun saveNotifications(tenantId: String, list: List<AppNotification>) {
        val tid = tenantId.trim()
        if (tid.isBlank()) return
        prefs.edit()
            .putString(notificationsKey(tid), gson.toJson(list.take(100)))
            .apply()
    }

    fun loadNotifications(tenantId: String): List<AppNotification> {
        val tid = tenantId.trim()
        if (tid.isBlank()) return emptyList()
        val json = prefs.getString(notificationsKey(tid), null) ?: return emptyList()
        return runCatching {
            gson.fromJson<List<AppNotification>>(json, notifType) ?: emptyList()
        }.getOrDefault(emptyList())
    }

    fun saveStok(tenantId: String, list: List<StokKaydi>) {
        val tid = tenantId.trim()
        if (tid.isBlank()) return
        prefs.edit()
            .putString(stokKey(tid), gson.toJson(list))
            .apply()
    }

    fun loadStok(tenantId: String): List<StokKaydi> {
        val tid = tenantId.trim()
        if (tid.isBlank()) return emptyList()
        val json = prefs.getString(stokKey(tid), null) ?: return emptyList()
        return runCatching {
            gson.fromJson<List<StokKaydi>>(json, stokType) ?: emptyList()
        }.getOrDefault(emptyList())
    }

    /** Firma değişiminde veya çıkışta tüm önbelleği sil. */
    fun clearAll() {
        prefs.edit().clear().apply()
    }

    fun clearTenant(tenantId: String) {
        val tid = tenantId.trim()
        if (tid.isBlank()) return
        prefs.edit()
            .remove(taleplerKey(tid))
            .remove(notificationsKey(tid))
            .remove(stokKey(tid))
            .apply()
    }

    private fun taleplerKey(tenantId: String) = "talepler_json_$tenantId"
    private fun notificationsKey(tenantId: String) = "notifications_json_$tenantId"
    private fun stokKey(tenantId: String) = "stok_json_$tenantId"

    companion object {
        private const val KEY_LEGACY_TALEPLER = "talepler_json"
        private const val KEY_LAST_TENANT = "last_tenant_id"
    }
}
