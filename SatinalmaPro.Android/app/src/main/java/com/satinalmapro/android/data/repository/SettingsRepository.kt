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
        val json = firestore.readDocumentJson(SETTINGS_PATH)
        return parseSettings(json)
    }

    suspend fun saveSettings(ayarlar: UygulamaAyarlar) {
        val updatedBy = auth.email ?: auth.uid ?: "android"
        // Önce buluttaki güncel listeyi oku; eklenen terimleri kaybetmemek için birleştir.
        // Silme: ayarlar listesinde olmayan terim bilinçli silinmiş kabul edilir —
        // bu yüzden birleştirmede "ayarlar" öncelikli değil; union kullanıp
        // silmeyi AppViewModel.removeBirim → tam liste yazımı ile yapıyoruz.
        // removeBirim zaten güncel listeden çıkarır; burada union, eşzamanlı
        // masaüstü eklemelerini korur. Silinen terim bulutta hâlâ varsa geri gelir —
        // bunu önlemek için: union yerine "yazılan listeyi esas al, yalnızca
        // buluttaki ekstra terimleri ekle" değil; silme sonrası anında yaz.
        // Pratik çözüm: union yapma, doğrudan yaz (sık yoklama ile senkron).
        firestore.writeDocumentJson(SETTINGS_PATH, toJson(ayarlar), updatedBy)
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
