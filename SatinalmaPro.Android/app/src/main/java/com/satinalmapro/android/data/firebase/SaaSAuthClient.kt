package com.satinalmapro.android.data.firebase

import com.satinalmapro.android.data.firebase.FirebaseConfig
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import org.json.JSONObject

data class SaaSLoginResult(
    val idToken: String,
    val refreshToken: String,
    val uid: String,
    val tenantId: String,
    val tenantAd: String?,
    val eposta: String?,
    val kullaniciAdi: String?,
    val expiresIn: Int
)

class SaaSAuthClient(private val config: FirebaseConfig) {
    private val http = OkHttpClient.Builder().build()
    private val region = "europe-west1"

    suspend fun loginWithUsername(username: String, password: String): SaaSLoginResult = withContext(Dispatchers.IO) {
        val body = JSONObject()
            .put("data", JSONObject()
                .put("username", username.trim().lowercase())
                .put("password", password))
            .toString()

        val url = "https://$region-${config.projectId}.cloudfunctions.net/loginWithUsername"
        val request = Request.Builder()
            .url(url)
            .post(body.toRequestBody("application/json".toMediaType()))
            .build()

        val response = http.newCall(request).execute()
        val text = response.body?.string().orEmpty()
        if (!response.isSuccessful) {
            throw IllegalStateException(parseError(text))
        }

        val result = JSONObject(text).getJSONObject("result")
        SaaSLoginResult(
            idToken = result.getString("idToken"),
            refreshToken = result.getString("refreshToken"),
            uid = result.getString("uid"),
            tenantId = result.getString("tenantId"),
            tenantAd = result.optString("tenantAd").ifBlank { null },
            eposta = result.optString("eposta").ifBlank { null },
            kullaniciAdi = result.optString("kullaniciAdi").ifBlank { null },
            expiresIn = result.optInt("expiresIn", 3600)
        )
    }

    suspend fun passwordResetByUsername(username: String) = withContext(Dispatchers.IO) {
        val body = JSONObject()
            .put("data", JSONObject().put("username", username.trim().lowercase()))
            .toString()
        val url = "https://$region-${config.projectId}.cloudfunctions.net/passwordResetByUsername"
        val request = Request.Builder()
            .url(url)
            .post(body.toRequestBody("application/json".toMediaType()))
            .build()
        val response = http.newCall(request).execute()
        if (!response.isSuccessful) {
            throw IllegalStateException(parseError(response.body?.string().orEmpty()))
        }
    }

    private fun parseError(text: String): String {
        return runCatching {
            val err = JSONObject(text).optJSONObject("error")
            when (err?.optString("message")) {
                "INVALID_LOGIN", "USER_NOT_FOUND" -> "Kullanıcı adı veya şifre hatalı."
                "USER_INACTIVE" -> "Hesabınız pasif durumda."
                "TENANT_INACTIVE" -> "Firma hesabı pasif durumda."
                else -> err?.optString("message") ?: "Giriş başarısız."
            }
        }.getOrDefault("Giriş başarısız.")
    }
}
