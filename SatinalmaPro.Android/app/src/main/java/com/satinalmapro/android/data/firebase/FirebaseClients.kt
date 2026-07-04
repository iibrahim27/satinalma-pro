package com.satinalmapro.android.data.firebase

import com.satinalmapro.android.core.NetworkError
import java.net.UnknownHostException
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.RequestBody.Companion.toRequestBody
import org.json.JSONObject

data class FirebaseConfig(
    val apiKey: String,
    val projectId: String,
    val updateManifestUrl: String
) {
    val isConfigured: Boolean get() = apiKey.isNotBlank() && projectId.isNotBlank()
}

class FirebaseAuthClient(private val config: FirebaseConfig) {
    private var idToken: String? = null
    private var refreshToken: String? = null
    private var tokenExpiryMs: Long = 0
    var uid: String? = null
        private set
    var email: String? = null
        private set

    val isLoggedIn: Boolean get() = !idToken.isNullOrBlank() && System.currentTimeMillis() < tokenExpiryMs

    suspend fun signIn(email: String, password: String) {
        val body = JSONObject()
            .put("email", email.trim())
            .put("password", password)
            .put("returnSecureToken", true)
        val response = HttpClients.postJson(
            "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=${config.apiKey}",
            body.toString()
        )
        applyAuthResponse(response)
    }

    suspend fun restoreSession(savedRefreshToken: String, savedUid: String?, savedEmail: String?) {
        refreshToken = savedRefreshToken
        uid = savedUid
        email = savedEmail
        refreshIdToken()
    }

    suspend fun validToken(): String {
        if (isLoggedIn) return idToken!!
        if (refreshToken.isNullOrBlank()) throw IllegalStateException("Oturum süresi doldu")
        refreshIdToken()
        return idToken!!
    }

    fun sessionJson(rememberMe: Boolean): String = JSONObject()
        .put("refreshToken", refreshToken)
        .put("uid", uid)
        .put("email", email)
        .put("rememberMe", rememberMe)
        .toString()

    fun clear() {
        idToken = null
        refreshToken = null
        uid = null
        email = null
        tokenExpiryMs = 0
    }

    private suspend fun refreshIdToken() {
        val form = mapOf(
            "grant_type" to "refresh_token",
            "refresh_token" to refreshToken!!
        )
        val response = HttpClients.postForm(
            "https://securetoken.googleapis.com/v1/token?key=${config.apiKey}",
            form
        )
        val json = JSONObject(response)
        idToken = json.getString("id_token")
        refreshToken = json.getString("refresh_token")
        val expiresIn = json.optString("expires_in", "3600").toLongOrNull() ?: 3600
        tokenExpiryMs = System.currentTimeMillis() + (expiresIn - 60) * 1000
    }

    private fun applyAuthResponse(response: String) {
        val json = JSONObject(response)
        idToken = json.getString("idToken")
        refreshToken = json.getString("refreshToken")
        uid = json.getString("localId")
        email = json.optString("email")
        val expiresIn = json.optString("expiresIn", "3600").toLongOrNull() ?: 3600
        tokenExpiryMs = System.currentTimeMillis() + (expiresIn - 60) * 1000
    }
}

class FirestoreClient(
    private val config: FirebaseConfig,
    private val auth: FirebaseAuthClient
) {
    private val root get() = "https://firestore.googleapis.com/v1/projects/${config.projectId}/databases/(default)/documents"

    suspend fun readUser(uid: String): JSONObject? {
        val response = HttpClients.get("$root/users/$uid", auth.validToken())
        return if (response.isBlank()) null else JSONObject(response).optJSONObject("fields")
    }

    suspend fun listUsers(): List<JSONObject> {
        val response = HttpClients.get("$root/users", auth.validToken())
        if (response.isBlank()) return emptyList()
        val docs = JSONObject(response).optJSONArray("documents") ?: return emptyList()
        return buildList {
            for (i in 0 until docs.length()) add(docs.getJSONObject(i))
        }
    }

    suspend fun updateFcmToken(uid: String, token: String?) {
        val body = JSONObject()
            .put("fields", JSONObject().put("fcmToken", JSONObject().put("stringValue", token ?: "")))
        HttpClients.patch(
            "$root/users/$uid?updateMask.fieldPaths=fcmToken",
            body.toString(),
            auth.validToken()
        )
    }

    suspend fun readDocumentJson(path: String): String? {
        val response = HttpClients.get("$root/$path", auth.validToken())
        if (response.isBlank()) return null
        val fields = JSONObject(response).optJSONObject("fields") ?: return null
        return fields.optJSONObject("json")?.optString("stringValue")
    }

    suspend fun readInbox(uid: String, limit: Int = 50): List<JSONObject> {
        val response = HttpClients.get("$root/users/$uid/notification_inbox?pageSize=$limit", auth.validToken())
        if (response.isBlank()) return emptyList()
        val docs = JSONObject(response).optJSONArray("documents") ?: return emptyList()
        return buildList {
            for (i in 0 until docs.length()) add(docs.getJSONObject(i))
        }
    }

    suspend fun writeDocumentJson(path: String, json: String, updatedBy: String) {
        val body = JSONObject()
            .put("fields", JSONObject()
                .put("json", JSONObject().put("stringValue", json))
                .put("updatedAt", JSONObject().put("stringValue", java.time.Instant.now().toString()))
                .put("updatedBy", JSONObject().put("stringValue", updatedBy)))
        HttpClients.patch(
            "$root/$path?updateMask.fieldPaths=json&updateMask.fieldPaths=updatedAt&updateMask.fieldPaths=updatedBy",
            body.toString(),
            auth.validToken()
        )
    }

    suspend fun markInboxRead(uid: String, docId: String) {
        val body = JSONObject()
            .put("fields", JSONObject()
                .put("isRead", JSONObject().put("booleanValue", true))
                .put("okundu", JSONObject().put("booleanValue", true))
                .put("readAt", JSONObject().put("stringValue", java.time.Instant.now().toString())))
        HttpClients.patch(
            "$root/users/$uid/notification_inbox/$docId?updateMask.fieldPaths=isRead&updateMask.fieldPaths=okundu&updateMask.fieldPaths=readAt",
            body.toString(),
            auth.validToken()
        )
    }
}

object HttpClients {
    private val client = okhttp3.OkHttpClient.Builder()
        .connectTimeout(java.time.Duration.ofSeconds(45))
        .readTimeout(java.time.Duration.ofSeconds(120))
        .build()

    suspend fun get(url: String, token: String? = null): String = kotlinx.coroutines.withContext(kotlinx.coroutines.Dispatchers.IO) {
        try {
            val request = okhttp3.Request.Builder().url(url).apply {
                token?.let { addHeader("Authorization", "Bearer $it") }
            }.build()
            client.newCall(request).execute().use { response ->
                if (response.code == 404) return@withContext ""
                if (!response.isSuccessful) throw IllegalStateException(parseFirebaseError(response.body?.string() ?: "HTTP ${response.code}"))
                response.body?.string() ?: ""
            }
        } catch (e: UnknownHostException) {
            throw IllegalStateException(NetworkError.translate(e.message))
        }
    }

    suspend fun postJson(url: String, json: String): String = kotlinx.coroutines.withContext(kotlinx.coroutines.Dispatchers.IO) {
        try {
            val body = json.toRequestBodyJson()
            val request = okhttp3.Request.Builder().url(url).post(body).build()
            client.newCall(request).execute().use { response ->
                val text = response.body?.string() ?: ""
                if (!response.isSuccessful) throw IllegalStateException(parseFirebaseError(text))
                text
            }
        } catch (e: UnknownHostException) {
            throw IllegalStateException(NetworkError.translate(e.message))
        }
    }

    suspend fun postForm(url: String, fields: Map<String, String>): String = kotlinx.coroutines.withContext(kotlinx.coroutines.Dispatchers.IO) {
        val form = okhttp3.FormBody.Builder().apply { fields.forEach { (k, v) -> add(k, v) } }.build()
        val request = okhttp3.Request.Builder().url(url).post(form).build()
        client.newCall(request).execute().use { response ->
            val text = response.body?.string() ?: ""
            if (!response.isSuccessful) throw IllegalStateException(parseFirebaseError(text))
            text
        }
    }

    suspend fun patch(url: String, json: String, token: String): String = kotlinx.coroutines.withContext(kotlinx.coroutines.Dispatchers.IO) {
        val body = json.toRequestBodyJson()
        val request = okhttp3.Request.Builder()
            .url(url)
            .patch(body)
            .addHeader("Authorization", "Bearer $token")
            .build()
        client.newCall(request).execute().use { response ->
            val text = response.body?.string() ?: ""
            if (!response.isSuccessful) throw IllegalStateException(parseFirebaseError(text))
            text
        }
    }

    suspend fun postJsonAuthorized(url: String, json: String, token: String): String =
        kotlinx.coroutines.withContext(kotlinx.coroutines.Dispatchers.IO) {
            val body = json.toRequestBodyJson()
            val request = okhttp3.Request.Builder()
                .url(url)
                .post(body)
                .addHeader("Authorization", "Bearer $token")
                .build()
            client.newCall(request).execute().use { response ->
                val text = response.body?.string() ?: ""
                if (!response.isSuccessful) throw IllegalStateException(parseFirebaseError(text))
                text
            }
        }

    private fun parseFirebaseError(body: String): String {
        val raw = try {
            JSONObject(body).optJSONObject("error")?.optString("message") ?: body
        } catch (_: Exception) {
            body
        }
        return NetworkError.translate(raw)
    }

    private fun String.toRequestBodyJson() = this.toRequestBody("application/json".toMediaType())
}
