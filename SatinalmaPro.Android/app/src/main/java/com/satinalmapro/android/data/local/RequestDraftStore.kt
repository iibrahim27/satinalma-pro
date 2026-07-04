package com.satinalmapro.android.data.local

import android.content.Context
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken

data class RequestDraftLine(
    val malzeme: String = "",
    val miktar: String = "",
    val birim: String = "adet"
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

    fun load(): RequestDraft? {
        val json = prefs.getString(KEY, null) ?: return null
        return runCatching { gson.fromJson<RequestDraft>(json, type) }.getOrNull()
    }

    fun save(draft: RequestDraft) {
        prefs.edit().putString(KEY, gson.toJson(draft)).apply()
    }

    fun clear() {
        prefs.edit().remove(KEY).apply()
    }

    companion object {
        private const val KEY = "yeni_talep_draft"
    }
}
