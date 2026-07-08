package com.satinalmapro.android.data.firebase

import com.satinalmapro.android.BuildConfig
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.android.core.NetworkError
import java.io.File
import java.net.UnknownHostException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
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
        .put("lisansTip", TenantSession.license()?.tip)
        .put("lisansBitisUtc", TenantSession.license()?.bitisUtc)
        .apply {
            val kalan = TenantSession.license()?.kalanGun
            if (kalan != null) put("lisansKalanGun", kalan)
        }
        .toString()

    fun clear() {
        idToken = null
        refreshToken = null
        uid = null
        email = null
        tokenExpiryMs = 0
    }

    suspend fun changePassword(email: String, currentPassword: String, newPassword: String) {
        signIn(email, currentPassword)
        val body = JSONObject()
            .put("idToken", idToken!!)
            .put("password", newPassword)
            .put("returnSecureToken", true)
        val response = HttpClients.postJson(
            "https://identitytoolkit.googleapis.com/v1/accounts:update?key=${config.apiKey}",
            body.toString()
        )
        applyAuthResponse(response)
    }

    suspend fun sendPasswordResetEmail(email: String) {
        val body = JSONObject()
            .put("requestType", "PASSWORD_RESET")
            .put("email", email.trim())
        HttpClients.postJson(
            "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=${config.apiKey}",
            body.toString()
        )
    }

    /** Yeni Firebase hesabı oluşturur; mevcut admin oturumunu korur. */
    suspend fun createUserAccount(email: String, password: String): String {
        val savedRefresh = refreshToken
        val savedUid = uid
        val savedEmail = email
        val savedToken = idToken
        val savedExpiry = tokenExpiryMs
        return try {
            val body = JSONObject()
                .put("email", email.trim())
                .put("password", password)
                .put("returnSecureToken", false)
            val response = HttpClients.postJson(
                "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=${config.apiKey}",
                body.toString()
            )
            JSONObject(response).getString("localId")
        } finally {
            refreshToken = savedRefresh
            uid = savedUid
            this.email = savedEmail
            idToken = savedToken
            tokenExpiryMs = savedExpiry
        }
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

    fun applySaaSLogin(result: SaaSLoginResult) {
        idToken = result.idToken
        refreshToken = result.refreshToken
        uid = result.uid
        email = result.eposta
        tokenExpiryMs = System.currentTimeMillis() + (result.expiresIn - 60) * 1000L
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
    private fun tenantRoot() = "$root/tenants/${TenantSession.requireTenantId()}"
    private fun userDoc(uid: String) = "${tenantRoot()}/users/$uid"

    suspend fun readUser(uid: String): JSONObject? {
        val response = HttpClients.get(userDoc(uid), auth.validToken())
        return if (response.isBlank()) null else JSONObject(response).optJSONObject("fields")
    }

    suspend fun listUsers(): List<JSONObject> {
        val response = HttpClients.get("${tenantRoot()}/users", auth.validToken())
        if (response.isBlank()) return emptyList()
        val docs = JSONObject(response).optJSONArray("documents") ?: return emptyList()
        return buildList {
            for (i in 0 until docs.length()) add(docs.getJSONObject(i))
        }
    }

    suspend fun listUsersAdmin(adminToken: String): List<JSONObject> {
        val response = HttpClients.get("$root/users", adminToken)
        if (response.isBlank()) return emptyList()
        val docs = JSONObject(response).optJSONArray("documents") ?: return emptyList()
        return buildList {
            for (i in 0 until docs.length()) add(docs.getJSONObject(i))
        }
    }

    suspend fun readUserAdmin(adminToken: String, uid: String): JSONObject? {
        val response = HttpClients.get("$root/users/$uid", adminToken)
        return if (response.isBlank()) null else JSONObject(response).optJSONObject("fields")
    }

    private fun inboxRoot(uid: String) = "${userDoc(uid)}/notification_inbox"

    suspend fun updateFcmToken(uid: String, token: String?) {
        val body = JSONObject()
            .put("fields", JSONObject().put("fcmToken", JSONObject().put("stringValue", token ?: "")))
        HttpClients.patch(
            "${userDoc(uid)}?updateMask.fieldPaths=fcmToken",
            body.toString(),
            auth.validToken()
        )
    }

    suspend fun readDocumentJson(path: String): String? {
        val tenantPath = if (path.startsWith("tenants/")) path else "${tenantRoot()}/$path"
        val response = HttpClients.get("$root/$tenantPath", auth.validToken())
        if (response.isBlank()) return null
        val fields = JSONObject(response).optJSONObject("fields") ?: return null
        return fields.optJSONObject("json")?.optString("stringValue")
    }

    suspend fun readInbox(uid: String, limit: Int = 100): List<JSONObject> {
        val response = HttpClients.get("${inboxRoot(uid)}?pageSize=$limit", auth.validToken())
        if (response.isBlank()) return emptyList()
        val docs = JSONObject(response).optJSONArray("documents") ?: return emptyList()
        return buildList {
            for (i in 0 until docs.length()) add(docs.getJSONObject(i))
        }
    }

    suspend fun writeInboxEntry(
        uid: String,
        docId: String,
        record: com.satinalmapro.android.core.model.BildirimRecord
    ) {
        if (uid.isBlank() || docId.isBlank()) return
        writeInboxEntryAdmin(auth.validToken(), uid, docId, record)
    }

    suspend fun writeInboxEntryAdmin(
        adminToken: String,
        uid: String,
        docId: String,
        record: com.satinalmapro.android.core.model.BildirimRecord
    ) {
        if (uid.isBlank() || docId.isBlank()) return
        val now = java.time.Instant.now().toString()
        val fields = JSONObject()
            .put("title", JSONObject().put("stringValue", record.baslik))
            .put("baslik", JSONObject().put("stringValue", record.baslik))
            .put("message", JSONObject().put("stringValue", record.mesaj))
            .put("mesaj", JSONObject().put("stringValue", record.mesaj))
            .put("tip", JSONObject().put("stringValue", record.tip))
            .put("type", JSONObject().put("stringValue", record.tip))
            .put("talepId", JSONObject().put("stringValue", record.talepId.orEmpty()))
            .put("entityId", JSONObject().put("stringValue", record.talepId.orEmpty()))
            .put("entityType", JSONObject().put("stringValue", "talep"))
            .put("eventCode", JSONObject().put("stringValue", record.tip))
            .put("hedefRol", JSONObject().put("stringValue", record.hedefRol.orEmpty()))
            .put("targetRole", JSONObject().put("stringValue", record.hedefRol.orEmpty()))
            .put("hedefUid", JSONObject().put("stringValue", record.hedefUid.orEmpty()))
            .put("targetUid", JSONObject().put("stringValue", record.hedefUid.orEmpty()))
            .put("olusturanUid", JSONObject().put("stringValue", record.olusturanUid))
            .put("createdBy", JSONObject().put("stringValue", record.olusturanUid))
            .put("isRead", JSONObject().put("booleanValue", false))
            .put("okundu", JSONObject().put("booleanValue", false))
            .put("createdAt", JSONObject().put("timestampValue", now))
            .put("guncellemeUtc", JSONObject().put(
                "integerValue",
                (if (record.guncellemeUtc > 0) record.guncellemeUtc else System.currentTimeMillis()).toString()
            ))
        val body = JSONObject().put("fields", fields).toString()
        HttpClients.postJsonAuthorized(
            "${inboxRoot(uid)}?documentId=${java.net.URLEncoder.encode(docId, Charsets.UTF_8.name())}",
            body,
            adminToken
        )
    }

    suspend fun writeDocumentJson(path: String, json: String, updatedBy: String) {
        val tenantPath = if (path.startsWith("tenants/")) path else "${tenantRoot()}/$path"
        val body = JSONObject()
            .put("fields", JSONObject()
                .put("json", JSONObject().put("stringValue", json))
                .put("updatedAt", JSONObject().put("stringValue", java.time.Instant.now().toString()))
                .put("updatedBy", JSONObject().put("stringValue", updatedBy)))
        HttpClients.patch(
            "$root/$tenantPath?updateMask.fieldPaths=json&updateMask.fieldPaths=updatedAt&updateMask.fieldPaths=updatedBy",
            body.toString(),
            auth.validToken()
        )
    }

    suspend fun markInboxRead(uid: String, docId: String) {
        val now = java.time.Instant.now().toString()
        val body = JSONObject()
            .put("fields", JSONObject()
                .put("isRead", JSONObject().put("booleanValue", true))
                .put("readAt", JSONObject().put("timestampValue", now)))
        HttpClients.patch(
            "${inboxRoot(uid)}/$docId?updateMask.fieldPaths=isRead&updateMask.fieldPaths=readAt",
            body.toString(),
            auth.validToken()
        )
    }

    suspend fun markInboxDismissed(uid: String, docId: String) {
        val now = java.time.Instant.now().toString()
        val body = JSONObject()
            .put("fields", JSONObject()
                .put("isRead", JSONObject().put("booleanValue", true))
                .put("isArchived", JSONObject().put("booleanValue", true))
                .put("dismissedAt", JSONObject().put("timestampValue", now))
                .put("archivedAt", JSONObject().put("timestampValue", now)))
        HttpClients.patch(
            "${inboxRoot(uid)}/$docId" +
                "?updateMask.fieldPaths=isRead&updateMask.fieldPaths=isArchived" +
                "&updateMask.fieldPaths=dismissedAt&updateMask.fieldPaths=archivedAt",
            body.toString(),
            auth.validToken()
        )
    }

    suspend fun deleteInboxDocument(uid: String, docId: String) {
        HttpClients.delete(
            "${inboxRoot(uid)}/$docId",
            auth.validToken()
        )
    }

    suspend fun clearInbox(uid: String) {
        readInbox(uid).forEach { doc ->
            val id = doc.optString("name").substringAfterLast('/')
            if (id.isNotBlank()) {
                runCatching { markInboxDismissed(uid, id) }
            }
        }
    }

    suspend fun markAllInboxRead(uid: String) {
        readInbox(uid).forEach { doc ->
            val id = doc.optString("name").substringAfterLast('/')
            if (id.isNotBlank()) {
                runCatching { markInboxRead(uid, id) }
            }
        }
    }

    suspend fun saveUserProfile(user: com.satinalmapro.android.core.model.ManagedUser) {
        if (user.uid.isBlank()) throw IllegalArgumentException("Kullanıcı kimliği boş")
        val fields = JSONObject()
            .put("eposta", JSONObject().put("stringValue", user.email))
            .put("adSoyad", JSONObject().put("stringValue", user.fullName))
            .put("rol", JSONObject().put("stringValue", user.role))
            .put("aktif", JSONObject().put("booleanValue", user.active))
            .put("saha", JSONObject().put("stringValue", user.site))
        val body = JSONObject().put("fields", fields).toString()
        val token = auth.validToken()
        val patchUrl = "$root/users/${user.uid}?updateMask.fieldPaths=eposta&updateMask.fieldPaths=adSoyad&updateMask.fieldPaths=rol&updateMask.fieldPaths=aktif&updateMask.fieldPaths=saha"
        val patchResult = runCatching { HttpClients.patch(patchUrl, body, token) }.getOrDefault("")
        if (patchResult.isNotBlank()) return
        HttpClients.postJsonAuthorized(
            "$root/users?documentId=${user.uid}",
            body,
            token
        )
    }
}

object HttpClients {
    private val userAgent = "SatinalmaPro-Android/${BuildConfig.VERSION_NAME} (build ${BuildConfig.VERSION_CODE})"

    private val client: OkHttpClient = OkHttpClient.Builder()
        .connectTimeout(java.time.Duration.ofSeconds(30))
        .readTimeout(java.time.Duration.ofSeconds(180))
        .writeTimeout(java.time.Duration.ofSeconds(60))
        .followRedirects(true)
        .followSslRedirects(true)
        .build()

    private fun requestBuilder(url: String): okhttp3.Request.Builder =
        okhttp3.Request.Builder()
            .url(url)
            .header("User-Agent", userAgent)
            .header("Accept", "*/*")

    suspend fun get(url: String, token: String? = null): String = withContext(Dispatchers.IO) {
        executeRequest {
            requestBuilder(url).apply {
                token?.let { addHeader("Authorization", "Bearer $it") }
            }.build()
        }
    }

    suspend fun download(url: String, target: File, onProgress: (Int) -> Unit) = withContext(Dispatchers.IO) {
        val request = requestBuilder(url).build()
        client.newCall(request).execute().use { response ->
            if (!response.isSuccessful) {
                throw IllegalStateException("İndirme başarısız: HTTP ${response.code}")
            }
            val body = response.body ?: throw IllegalStateException("İndirme başarısız: Boş yanıt")
            val total = body.contentLength()
            target.outputStream().use { out ->
                body.byteStream().use { input ->
                    val buffer = ByteArray(8192)
                    var read: Int
                    var downloaded = 0L
                    while (input.read(buffer).also { read = it } != -1) {
                        out.write(buffer, 0, read)
                        downloaded += read
                        if (total > 0) onProgress(((downloaded * 100) / total).toInt())
                    }
                }
            }
        }
    }

    suspend fun postJson(url: String, json: String): String = withContext(Dispatchers.IO) {
        val body = json.toRequestBodyJson()
        executeRequest { requestBuilder(url).post(body).build() }
    }

    suspend fun postForm(url: String, fields: Map<String, String>): String = withContext(Dispatchers.IO) {
        val form = okhttp3.FormBody.Builder().apply { fields.forEach { (k, v) -> add(k, v) } }.build()
        executeRequest { requestBuilder(url).post(form).build() }
    }

    suspend fun patch(url: String, json: String, token: String): String = withContext(Dispatchers.IO) {
        val body = json.toRequestBodyJson()
        executeRequest {
            requestBuilder(url)
                .patch(body)
                .addHeader("Authorization", "Bearer $token")
                .build()
        }
    }

    suspend fun postJsonAuthorized(url: String, json: String, token: String): String =
        withContext(Dispatchers.IO) {
            val body = json.toRequestBodyJson()
            executeRequest {
                requestBuilder(url)
                    .post(body)
                    .addHeader("Authorization", "Bearer $token")
                    .build()
            }
        }

    suspend fun delete(url: String, token: String): String = withContext(Dispatchers.IO) {
        executeRequest {
            requestBuilder(url)
                .delete()
                .addHeader("Authorization", "Bearer $token")
                .build()
        }
    }

    private inline fun executeRequest(build: () -> okhttp3.Request): String {
        try {
            client.newCall(build()).execute().use { response ->
                if (response.code == 404) return ""
                val text = response.body?.string() ?: ""
                if (!response.isSuccessful) {
                    throw IllegalStateException(parseFirebaseError(text.ifBlank { "HTTP ${response.code}" }))
                }
                return text
            }
        } catch (e: IllegalStateException) {
            throw e
        } catch (e: UnknownHostException) {
            throw IllegalStateException(NetworkError.translate(e))
        } catch (e: Exception) {
            throw IllegalStateException(NetworkError.translate(e))
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
