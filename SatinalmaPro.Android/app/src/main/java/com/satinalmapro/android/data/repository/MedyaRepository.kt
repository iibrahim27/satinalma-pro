package com.satinalmapro.android.data.repository

import android.util.Base64
import com.satinalmapro.android.data.firebase.FirestoreClient
import org.json.JSONObject

data class MedyaPaketi(
    val firmaLogoBase64: String? = null,
    val anasayfaLogoBase64: String? = null,
    val firmaLogoDosya: String? = null,
    val anasayfaLogoDosya: String? = null
)

class MedyaRepository(
    private val firestore: FirestoreClient
) {
    suspend fun loadMedya(): MedyaPaketi {
        val json = firestore.readDocumentJson(MEDYA_PATH) ?: return MedyaPaketi()
        return try {
            val obj = JSONObject(json)
            MedyaPaketi(
                firmaLogoBase64 = obj.optString("firmaLogoBase64").ifBlank { null },
                anasayfaLogoBase64 = obj.optString("anasayfaLogoBase64").ifBlank { null },
                firmaLogoDosya = obj.optString("firmaLogoDosya").ifBlank { null },
                anasayfaLogoDosya = obj.optString("anasayfaLogoDosya").ifBlank { null }
            )
        } catch (_: Exception) {
            MedyaPaketi()
        }
    }

    fun logoBytes(paket: MedyaPaketi): ByteArray? {
        val b64 = paket.firmaLogoBase64?.takeIf { it.isNotBlank() } ?: return null
        return try {
            Base64.decode(b64, Base64.DEFAULT)
        } catch (_: Exception) {
            null
        }
    }

    companion object {
        private const val MEDYA_PATH = "veri/medya"
    }
}
