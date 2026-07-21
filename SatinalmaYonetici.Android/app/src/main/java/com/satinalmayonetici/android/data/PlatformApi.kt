package com.satinalmayonetici.android.data

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import org.json.JSONArray
import org.json.JSONObject
import java.util.concurrent.TimeUnit

data class FirebaseConfig(val apiKey: String, val projectId: String)

data class AuthSession(
    val idToken: String,
    val refreshToken: String,
    val uid: String,
    val email: String,
    val expiresAtMs: Long
)

data class TenantRow(
    val id: String,
    val kod: String,
    val ad: String,
    val aktif: Boolean,
    val lisansTipi: String,
    val lisansBaslangic: String?,
    val lisansBitis: String?,
    val lisansKalanGun: Int?,
    val lisansSuresiDoldu: Boolean
)

class PlatformApi(private val config: FirebaseConfig) {
    private val http = OkHttpClient.Builder()
        .connectTimeout(45, TimeUnit.SECONDS)
        .readTimeout(120, TimeUnit.SECONDS)
        .build()
    private val region = "europe-west1"
    private val jsonMedia = "application/json".toMediaType()

    suspend fun signIn(email: String, password: String): AuthSession = withContext(Dispatchers.IO) {
        val body = JSONObject()
            .put("email", email.trim())
            .put("password", password)
            .put("returnSecureToken", true)
            .toString()
        val req = Request.Builder()
            .url("https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=${config.apiKey}")
            .post(body.toRequestBody(jsonMedia))
            .build()
        val text = execute(req)
        val root = JSONObject(text)
        val expiresIn = root.optString("expiresIn", "3600").toIntOrNull() ?: 3600
        AuthSession(
            idToken = root.getString("idToken"),
            refreshToken = root.getString("refreshToken"),
            uid = root.getString("localId"),
            email = email.trim(),
            expiresAtMs = System.currentTimeMillis() + (expiresIn - 60) * 1000L
        )
    }

    suspend fun refresh(refreshToken: String): AuthSession = withContext(Dispatchers.IO) {
        val body = "grant_type=refresh_token&refresh_token=${java.net.URLEncoder.encode(refreshToken, "UTF-8")}"
        val req = Request.Builder()
            .url("https://securetoken.googleapis.com/v1/token?key=${config.apiKey}")
            .post(body.toRequestBody("application/x-www-form-urlencoded".toMediaType()))
            .build()
        val text = execute(req)
        val root = JSONObject(text)
        val expiresIn = root.optString("expires_in", "3600").toIntOrNull() ?: 3600
        AuthSession(
            idToken = root.getString("id_token"),
            refreshToken = root.getString("refresh_token"),
            uid = root.optString("user_id"),
            email = "",
            expiresAtMs = System.currentTimeMillis() + (expiresIn - 60) * 1000L
        )
    }

    suspend fun listTenants(idToken: String): List<TenantRow> = withContext(Dispatchers.IO) {
        val arr = callArray("platformListTenants", JSONObject(), idToken)
        buildList {
            for (i in 0 until arr.length()) {
                add(parseTenant(arr.getJSONObject(i)))
            }
        }
    }

    suspend fun saveTenant(
        idToken: String,
        tenant: TenantRow,
        lisansYenile: Boolean = false,
        lisansGunEkle: Int? = null,
        lisansGunEkleModu: Boolean = false
    ): TenantRow = withContext(Dispatchers.IO) {
        val data = JSONObject()
            .put("id", tenant.id)
            .put("kod", tenant.kod)
            .put("ad", tenant.ad)
            .put("aktif", tenant.aktif)
            .put("lisansTipi", tenant.lisansTipi)
            .put("lisansYenile", lisansYenile)
            .put("lisansGunEkleModu", lisansGunEkleModu)
        if (lisansGunEkle != null) {
            data.put("lisansGunEkle", lisansGunEkle)
            data.put("lisansManuelGun", lisansGunEkle)
        }
        if (!tenant.lisansBaslangic.isNullOrBlank() && !lisansGunEkleModu) {
            data.put("lisansBaslangic", tenant.lisansBaslangic)
        }
        if (!tenant.lisansBitis.isNullOrBlank() && !lisansGunEkleModu) {
            data.put("lisansBitis", tenant.lisansBitis)
        }
        parseTenant(callObject("platformSaveTenant", data, idToken))
    }

    suspend fun resetTenant(idToken: String, tenantId: String): Long = withContext(Dispatchers.IO) {
        val o = callObject(
            "platformResetTenantData",
            JSONObject().put("tenantId", tenantId).put("scope", "all"),
            idToken
        )
        o.optLong("veriSifirlamaUtc", 0L)
    }

    private fun parseTenant(o: JSONObject) = TenantRow(
        id = o.optString("id"),
        kod = o.optString("kod"),
        ad = o.optString("ad"),
        aktif = o.optBoolean("aktif", true),
        lisansTipi = o.optString("lisansTipi", "deneme"),
        lisansBaslangic = o.optString("lisansBaslangic").ifBlank { null },
        lisansBitis = o.optString("lisansBitis").ifBlank { null },
        lisansKalanGun = if (o.has("lisansKalanGun") && !o.isNull("lisansKalanGun")) o.optInt("lisansKalanGun") else null,
        lisansSuresiDoldu = o.optBoolean("lisansSuresiDoldu", false)
    )

    private fun callObject(name: String, data: JSONObject, idToken: String): JSONObject {
        val text = postCallable(name, data, idToken)
        return JSONObject(text).getJSONObject("result")
    }

    private fun callArray(name: String, data: JSONObject, idToken: String): JSONArray {
        val text = postCallable(name, data, idToken)
        val result = JSONObject(text).get("result")
        return result as? JSONArray ?: JSONArray()
    }

    private fun postCallable(name: String, data: JSONObject, idToken: String): String {
        val body = JSONObject().put("data", data).toString()
        val req = Request.Builder()
            .url("https://$region-${config.projectId}.cloudfunctions.net/$name")
            .addHeader("Authorization", "Bearer $idToken")
            .post(body.toRequestBody(jsonMedia))
            .build()
        return execute(req)
    }

    private fun execute(req: Request): String {
        http.newCall(req).execute().use { resp ->
            val text = resp.body?.string().orEmpty()
            if (!resp.isSuccessful) throw IllegalStateException(parseError(text))
            return text
        }
    }

    private fun parseError(text: String): String {
        return runCatching {
            val err = JSONObject(text).optJSONObject("error")
            val msg = err?.optString("message").orEmpty()
            when {
                msg.contains("Platform yöneticisi", ignoreCase = true) ->
                    "Platform yöneticisi yetkisi gerekli."
                msg.isNotBlank() && msg.length < 200 -> msg
                else -> "İşlem başarısız."
            }
        }.getOrDefault("İşlem başarısız.")
    }
}
