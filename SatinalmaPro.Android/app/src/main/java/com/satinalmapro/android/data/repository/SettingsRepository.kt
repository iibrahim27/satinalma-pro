package com.satinalmapro.android.data.repository

import com.satinalmapro.android.core.model.ManagedUser
import com.satinalmapro.android.core.model.UygulamaAyarlar
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirestoreClient
import org.json.JSONArray
import org.json.JSONObject

class SettingsRepository(
    private val firestore: FirestoreClient,
    private val auth: FirebaseAuthClient
) {
    suspend fun loadSettings(): UygulamaAyarlar {
        val json = readSettingsJson()
        return parseSettings(json)
    }

    suspend fun saveSettings(ayarlar: UygulamaAyarlar) {
        val updatedBy = auth.email ?: auth.uid ?: "android"
        // Masaüstü alanlarını (logo, filo zimmet) koru — yalnızca Android alanlarını güncelle.
        val mevcut = runCatching { readSettingsJson() }.getOrNull()
        val birlesik = mergePreserveDesktopFields(mevcut, ayarlar)
        firestore.writeDocumentJson(SETTINGS_PATH, birlesik, updatedBy)
    }

    /** json wrapper veya eski düz alan seed formatını okur. */
    private suspend fun readSettingsJson(): String? {
        val raw = firestore.readDocumentRaw(SETTINGS_PATH) ?: return null
        val fields = JSONObject(raw).optJSONObject("fields") ?: return null
        fields.optJSONObject("json")?.optString("stringValue")?.takeIf { it.isNotBlank() }?.let {
            return it
        }
        // Flat seed (Yönetici eski format): firmaAdi / diziler üst düzeyde
        val firma = fields.optJSONObject("firmaAdi")?.optString("stringValue").orEmpty()
        val birimler = firestoreArrayToList(fields.optJSONObject("malzemeBirimleri"))
        val kategoriler = firestoreArrayToList(fields.optJSONObject("malzemeKategorileri"))
        if (firma.isBlank() && birimler.isEmpty() && kategoriler.isEmpty()) return null
        return JSONObject()
            .put("firmaAdi", firma)
            .put("malzemeBirimleri", JSONArray(birimler))
            .put("malzemeKategorileri", JSONArray(kategoriler))
            .toString()
    }

    private fun firestoreArrayToList(field: JSONObject?): List<String> {
        if (field == null) return emptyList()
        val arr = field.optJSONObject("arrayValue")?.optJSONArray("values") ?: return emptyList()
        return buildList {
            for (i in 0 until arr.length()) {
                arr.optJSONObject(i)?.optString("stringValue")?.trim()
                    ?.takeIf { it.isNotBlank() }?.let(::add)
            }
        }
    }

    private fun mergePreserveDesktopFields(existingJson: String?, ayarlar: UygulamaAyarlar): String {
        val obj = try {
            if (existingJson.isNullOrBlank()) JSONObject()
            else JSONObject(existingJson)
        } catch (_: Exception) {
            JSONObject()
        }
        obj.put("firmaAdi", ayarlar.firmaAdi)
        obj.put(
            "malzemeKategorileri",
            JSONArray(ayarlar.malzemeKategorileri.distinctBy { it.lowercase() })
        )
        obj.put(
            "malzemeBirimleri",
            JSONArray(ayarlar.malzemeBirimleri.distinctBy { it.lowercase() })
        )
        return obj.toString()
    }

    suspend fun loadUsers(parseUser: (String, JSONObject) -> UserProfile): List<ManagedUser> =
        firestore.listUsers()
            .mapNotNull { doc ->
                val uid = doc.optString("name").substringAfterLast('/').ifBlank { return@mapNotNull null }
                val fields = doc.optJSONObject("fields") ?: return@mapNotNull null
                val profile = parseUser(uid, fields)
                ManagedUser(
                    uid = uid,
                    email = profile.email,
                    fullName = profile.fullName,
                    role = profile.role,
                    active = profile.active,
                    site = profile.site.orEmpty()
                )
            }
            .sortedBy { it.fullName.lowercase() }

    suspend fun saveUser(user: ManagedUser) {
        firestore.saveUserProfile(
            user.copy(role = KullaniciRolleri.normalize(user.role))
        )
    }

    suspend fun createUser(
        email: String,
        password: String,
        fullName: String,
        role: String,
        site: String,
        active: Boolean
    ): ManagedUser {
        val uid = auth.createUserAccount(email, password)
        val user = ManagedUser(
            uid = uid,
            email = email.trim(),
            fullName = fullName.trim(),
            role = KullaniciRolleri.normalize(role),
            active = active,
            site = site.trim()
        )
        firestore.saveUserProfile(user)
        return user
    }

    companion object {
        private const val SETTINGS_PATH = "veri/uygulama_ayarlar"

        fun parseSettings(json: String?): UygulamaAyarlar {
            if (json.isNullOrBlank()) return withDefaults(UygulamaAyarlar())
            return try {
                val obj = JSONObject(json)
                withDefaults(
                    UygulamaAyarlar(
                        firmaAdi = obj.optString("firmaAdi"),
                        malzemeKategorileri = obj.optJSONArray("malzemeKategorileri").toStringList(),
                        malzemeBirimleri = obj.optJSONArray("malzemeBirimleri").toStringList()
                    )
                )
            } catch (_: Exception) {
                withDefaults(UygulamaAyarlar())
            }
        }

        fun toJson(ayarlar: UygulamaAyarlar): String = JSONObject()
            .put("firmaAdi", ayarlar.firmaAdi)
            .put(
                "malzemeKategorileri",
                JSONArray(ayarlar.malzemeKategorileri.distinctBy { it.lowercase() })
            )
            .put(
                "malzemeBirimleri",
                JSONArray(ayarlar.malzemeBirimleri.distinctBy { it.lowercase() })
            )
            .toString()

        private fun withDefaults(ayarlar: UygulamaAyarlar): UygulamaAyarlar {
            val birimler = ayarlar.malzemeBirimleri.ifEmpty { UygulamaAyarlar.varsayilanBirimler }
            val kategoriler = ayarlar.malzemeKategorileri.ifEmpty { UygulamaAyarlar.varsayilanKategoriler }
            return ayarlar.copy(
                malzemeBirimleri = birimler,
                malzemeKategorileri = kategoriler
            )
        }

        private fun JSONArray?.toStringList(): List<String> {
            if (this == null) return emptyList()
            return buildList {
                for (i in 0 until length()) {
                    optString(i).trim().takeIf { it.isNotBlank() }?.let(::add)
                }
            }
        }
    }
}
