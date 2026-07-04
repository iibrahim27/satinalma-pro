package com.satinalmapro.android.data.local

import android.content.Context
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.model.TalepItem

class OfflineCache(context: Context) {
    private val prefs = context.getSharedPreferences("satinalma_cache", Context.MODE_PRIVATE)
    private val gson = Gson()
    private val talepType = object : TypeToken<List<TalepItem>>() {}.type

    fun saveTalepler(list: List<TalepItem>) {
        prefs.edit().putString(KEY_TALEPLER, gson.toJson(list)).apply()
    }

    fun loadTalepler(): List<TalepItem> {
        val json = prefs.getString(KEY_TALEPLER, null) ?: return emptyList()
        return runCatching { gson.fromJson<List<TalepItem>>(json, talepType) ?: emptyList() }.getOrDefault(emptyList())
    }

    companion object {
        private const val KEY_TALEPLER = "talepler_json"
    }
}
