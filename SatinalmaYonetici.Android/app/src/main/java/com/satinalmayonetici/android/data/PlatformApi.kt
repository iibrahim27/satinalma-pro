package com.satinalmayonetici.android.data

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import org.json.JSONArray
import org.json.JSONObject
import java.io.File
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
    val id: String = "",
    val kod: String = "",
    val ad: String = "",
    val aktif: Boolean = true,
    val lisansTipi: String = "deneme",
    val lisansBaslangic: String? = null,
    val lisansBitis: String? = null,
    val lisansKalanGun: Int? = null,
    val lisansSuresiDoldu: Boolean = false
)

data class UserRow(
    val uid: String = "",
    val kullaniciAdi: String = "",
    val eposta: String = "",
    val adSoyad: String = "",
    val rol: String = "Saha",
    val saha: String = "",
    val aktif: Boolean = true,
    val moduller: List<String> = emptyList(),
    val modulYetkileri: List<ModulYetki> = emptyList()
)

data class BackupResult(val downloadUrl: String, val path: String, val sizeBytes: Long)

class PlatformApi(private val config: FirebaseConfig) {
    private val http = OkHttpClient.Builder()
        .connectTimeout(45, TimeUnit.SECONDS)
        .readTimeout(180, TimeUnit.SECONDS)
        .build()
    private val region = "europe-west1"
    private val jsonMedia = "application/json".toMediaType()

    suspend fun signIn(email: String, password: String): AuthSession = withContext(Dispatchers.IO) {
        val body = JSONObject()
            .put("email", email.trim())
            .put("password", password)
            .put("returnSecureToken", true)
            .toString()
        val text = execute(
            Request.Builder()
                .url("https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=${config.apiKey}")
                .post(body.toRequestBody(jsonMedia))
                .build()
        )
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
        val text = execute(
            Request.Builder()
                .url("https://securetoken.googleapis.com/v1/token?key=${config.apiKey}")
                .post(body.toRequestBody("application/x-www-form-urlencoded".toMediaType()))
                .build()
        )
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
        buildList { for (i in 0 until arr.length()) add(parseTenant(arr.getJSONObject(i))) }
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

    suspend fun deleteTenant(idToken: String, tenantId: String, confirmKod: String): Int =
        withContext(Dispatchers.IO) {
            val o = callObject(
                "platformDeleteTenant",
                JSONObject().put("tenantId", tenantId).put("confirmKod", confirmKod),
                idToken
            )
            o.optInt("deletedUsers", 0)
        }

    suspend fun backupTenant(idToken: String, tenantId: String): BackupResult =
        withContext(Dispatchers.IO) {
            val o = callObject("platformBackupTenant", JSONObject().put("tenantId", tenantId), idToken)
            BackupResult(
                downloadUrl = o.optString("downloadUrl"),
                path = o.optString("path"),
                sizeBytes = o.optLong("sizeBytes", 0L)
            )
        }

    suspend fun restoreTenant(idToken: String, tenantId: String, storagePath: String): Pair<Int, Int> =
        withContext(Dispatchers.IO) {
            val o = callObject(
                "platformRestoreTenant",
                JSONObject()
                    .put("tenantId", tenantId)
                    .put("storagePath", storagePath)
                    .put("restoreUsers", true),
                idToken
            )
            o.optInt("restoredDocs") to o.optInt("restoredUsers")
        }

    suspend fun resetTenant(idToken: String, tenantId: String): Long = withContext(Dispatchers.IO) {
        val o = callObject(
            "platformResetTenantData",
            JSONObject().put("tenantId", tenantId).put("scope", "all"),
            idToken
        )
        o.optLong("veriSifirlamaUtc", 0L)
    }

    suspend fun listUsers(idToken: String, tenantId: String): List<UserRow> = withContext(Dispatchers.IO) {
        val arr = callArray("platformListTenantUsers", JSONObject().put("tenantId", tenantId), idToken)
        buildList { for (i in 0 until arr.length()) add(parseUser(arr.getJSONObject(i))) }
    }

    suspend fun saveUser(
        idToken: String,
        tenantId: String,
        user: UserRow,
        sifre: String?
    ): UserRow = withContext(Dispatchers.IO) {
        val yetkiler = JSONArray()
        user.modulYetkileri.forEach { y ->
            val sekmeler = JSONArray()
            y.sekmeler.forEach { sekmeler.put(it) }
            yetkiler.put(
                JSONObject()
                    .put("modul", y.modul)
                    .put("okuma", y.okuma)
                    .put("yazma", y.yazma)
                    .put("sekmeler", sekmeler)
            )
        }
        val moduller = JSONArray()
        user.moduller.forEach { moduller.put(it) }
        val data = JSONObject()
            .put("tenantId", tenantId)
            .put("uid", user.uid)
            .put("kullaniciAdi", user.kullaniciAdi)
            .put("eposta", user.eposta)
            .put("adSoyad", user.adSoyad)
            .put("rol", user.rol)
            .put("saha", user.saha)
            .put("aktif", user.aktif)
            .put("moduller", moduller)
            .put("modulYetkileri", yetkiler)
        if (!sifre.isNullOrBlank()) data.put("sifre", sifre)
        parseUser(callObject("platformSaveTenantUser", data, idToken))
    }

    suspend fun deleteUser(idToken: String, tenantId: String, uid: String) = withContext(Dispatchers.IO) {
        callObject(
            "platformDeleteTenantUser",
            JSONObject().put("tenantId", tenantId).put("uid", uid),
            idToken
        )
        Unit
    }

    suspend fun importLegacyUsers(idToken: String, tenantId: String): Triple<Int, Int, Int> =
        withContext(Dispatchers.IO) {
            val o = callObject(
                "platformImportLegacyUsers",
                JSONObject().put("tenantId", tenantId),
                idToken
            )
            Triple(o.optInt("imported"), o.optInt("skipped"), o.optInt("total"))
        }

    suspend fun detachSelf(idToken: String): Pair<Int, Int> = withContext(Dispatchers.IO) {
        val o = callObject("platformDetachSelf", JSONObject(), idToken)
        o.optInt("removedUsers") to o.optInt("removedUsernames")
    }

    suspend fun bootstrapAdmin(idToken: String) = withContext(Dispatchers.IO) {
        callObject("platformBootstrapAdmin", JSONObject(), idToken)
        Unit
    }

    suspend fun downloadToFile(url: String, dest: File) = withContext(Dispatchers.IO) {
        http.newCall(Request.Builder().url(url).get().build()).execute().use { resp ->
            if (!resp.isSuccessful) throw IllegalStateException("İndirme başarısız (${resp.code})")
            dest.outputStream().use { out ->
                resp.body?.byteStream()?.copyTo(out) ?: throw IllegalStateException("Boş yanıt")
            }
        }
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

    private fun parseUser(o: JSONObject): UserRow {
        val yetkiler = mutableListOf<ModulYetki>()
        val arr = o.optJSONArray("modulYetkileri")
        if (arr != null) {
            for (i in 0 until arr.length()) {
                val y = arr.optJSONObject(i) ?: continue
                val sekmeler = mutableListOf<String>()
                val sArr = y.optJSONArray("sekmeler")
                if (sArr != null) for (j in 0 until sArr.length()) sekmeler += sArr.optString(j)
                yetkiler += ModulYetki(
                    modul = y.optString("modul"),
                    okuma = y.optBoolean("okuma", true),
                    yazma = y.optBoolean("yazma", false),
                    sekmeler = sekmeler
                )
            }
        }
        val moduller = mutableListOf<String>()
        val mArr = o.optJSONArray("moduller")
        if (mArr != null) for (i in 0 until mArr.length()) moduller += mArr.optString(i)
        return UserRow(
            uid = o.optString("uid"),
            kullaniciAdi = o.optString("kullaniciAdi"),
            eposta = o.optString("eposta"),
            adSoyad = o.optString("adSoyad"),
            rol = o.optString("rol", "Saha"),
            saha = o.optString("saha"),
            aktif = o.optBoolean("aktif", true),
            moduller = moduller,
            modulYetkileri = yetkiler
        )
    }

    private fun callObject(name: String, data: JSONObject, idToken: String): JSONObject {
        val text = postCallable(name, data, idToken)
        return JSONObject(text).getJSONObject("result")
    }

    private fun callArray(name: String, data: JSONObject, idToken: String): JSONArray {
        val result = JSONObject(postCallable(name, data, idToken)).get("result")
        return result as? JSONArray ?: JSONArray()
    }

    private fun postCallable(name: String, data: JSONObject, idToken: String): String {
        val body = JSONObject().put("data", data).toString()
        return execute(
            Request.Builder()
                .url("https://$region-${config.projectId}.cloudfunctions.net/$name")
                .addHeader("Authorization", "Bearer $idToken")
                .post(body.toRequestBody(jsonMedia))
                .build()
        )
    }

    private fun execute(req: Request): String {
        http.newCall(req).execute().use { resp ->
            val text = resp.body?.string().orEmpty()
            if (!resp.isSuccessful) throw IllegalStateException(parseError(text))
            return text
        }
    }

    private fun parseError(text: String): String = runCatching {
        val msg = JSONObject(text).optJSONObject("error")?.optString("message").orEmpty()
        when {
            msg.contains("Platform yöneticisi", ignoreCase = true) -> "Platform yöneticisi yetkisi gerekli."
            msg.isNotBlank() && msg.length < 220 -> msg
            else -> "İşlem başarısız."
        }
    }.getOrDefault("İşlem başarısız.")
}

fun validateUsername(raw: String?): String? {
    if (raw.isNullOrBlank()) return "Kullanıcı adı zorunludur."
    val n = raw.trim().lowercase()
        .replace('ı', 'i').replace('ğ', 'g').replace('ü', 'u')
        .replace('ş', 's').replace('ö', 'o').replace('ç', 'c')
    if (n.length !in 3..32) return "Kullanıcı adı 3-32 karakter olmalıdır."
    if (!Regex("^[a-z0-9][a-z0-9._-]{2,31}$").matches(n)) {
        return "Kullanıcı adı yalnızca küçük harf, rakam, nokta, tire ve alt çizgi içerebilir."
    }
    return null
}
