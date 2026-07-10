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
        val response = firestore.readDocumentRaw(MEDYA_PATH) ?: return MedyaPaketi()
        return try {
            val fields = JSONObject(response).optJSONObject("fields") ?: return MedyaPaketi()
            // Masaüstü BelgeJsonYazAsync: { json: "{...}" }
            val blob = fields.optJSONObject("json")?.optString("stringValue").orEmpty()
            if (blob.isNotBlank()) {
                return parsePaket(JSONObject(blob))
            }
            // Yönetici seed / düz alan formatı
            MedyaPaketi(
                firmaLogoBase64 = fieldString(fields, "firmaLogoBase64"),
                anasayfaLogoBase64 = fieldString(fields, "anasayfaLogoBase64"),
                firmaLogoDosya = fieldString(fields, "firmaLogoDosya"),
                anasayfaLogoDosya = fieldString(fields, "anasayfaLogoDosya")
            )
        } catch (_: Exception) {
            MedyaPaketi()
        }
    }

    /** PDF için firma logosu; yoksa anasayfa logosu. */
    fun logoBytes(paket: MedyaPaketi): ByteArray? {
        decodeBase64(paket.firmaLogoBase64)?.let { return it }
        return decodeBase64(paket.anasayfaLogoBase64)
    }

    private fun parsePaket(obj: JSONObject): MedyaPaketi = MedyaPaketi(
        firmaLogoBase64 = obj.optString("firmaLogoBase64").ifBlank { null },
        anasayfaLogoBase64 = obj.optString("anasayfaLogoBase64").ifBlank { null },
        firmaLogoDosya = obj.optString("firmaLogoDosya").ifBlank { null },
        anasayfaLogoDosya = obj.optString("anasayfaLogoDosya").ifBlank { null }
    )

    private fun fieldString(fields: JSONObject, key: String): String? =
        fields.optJSONObject(key)?.optString("stringValue")?.ifBlank { null }

    private fun decodeBase64(raw: String?): ByteArray? {
        var b64 = raw?.trim().orEmpty()
        if (b64.isBlank()) return null
        // data:image/png;base64,....
        val comma = b64.indexOf(',')
        if (b64.startsWith("data:", ignoreCase = true) && comma > 0) {
            b64 = b64.substring(comma + 1)
        }
        b64 = b64.replace("\\s".toRegex(), "")
        return try {
            Base64.decode(b64, Base64.DEFAULT)
        } catch (_: Exception) {
            try {
                Base64.decode(b64, Base64.URL_SAFE or Base64.NO_WRAP)
            } catch (_: Exception) {
                null
            }
        }
    }

    companion object {
        private const val MEDYA_PATH = "veri/medya"
    }
}
