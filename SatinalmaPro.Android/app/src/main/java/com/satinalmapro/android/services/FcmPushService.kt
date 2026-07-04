package com.satinalmapro.android.services

import android.content.Context
import com.google.auth.oauth2.GoogleCredentials
import com.satinalmapro.android.core.model.BildirimRecord
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.data.firebase.FirestoreClient
import com.satinalmapro.android.data.firebase.HttpClients
import org.json.JSONObject
import java.io.InputStream

class FcmPushService(
    private val context: Context,
    private val projectId: String,
    private val firestore: FirestoreClient
) {
    private val scope = listOf("https://www.googleapis.com/auth/firebase.messaging")

    suspend fun push(record: BildirimRecord, olusturanUid: String) {
        val accountStream = openServiceAccount() ?: return
        val credentials = GoogleCredentials.fromStream(accountStream).createScoped(scope)
        credentials.refreshIfExpired()
        val accessToken = credentials.accessToken.tokenValue

        val targets = resolveTargets(record, olusturanUid)
        val route = BildirimRota.hedefRoute(mapTip(record.tip), record.talepId, null)
        for ((token, rol) in targets) {
            runCatching {
                send(accessToken, token, record, route, rol)
            }
        }
    }

    private suspend fun resolveTargets(record: BildirimRecord, olusturanUid: String): List<Pair<String, String>> {
        if (!record.hedefUid.isNullOrBlank()) {
            val user = firestore.readUser(record.hedefUid) ?: return emptyList()
            val token = user.optJSONObject("fcmToken")?.optString("stringValue").orEmpty()
            val rol = user.optJSONObject("rol")?.optString("stringValue")
                ?: user.optJSONObject("role")?.optString("stringValue").orEmpty()
            return if (token.isNotBlank()) listOf(token to rol) else emptyList()
        }
        if (!record.hedefRol.isNullOrBlank()) {
            val hedefRol = KullaniciRolleri.normalize(record.hedefRol)
            return firestore.listUsers()
                .mapNotNull { doc ->
                    val fields = doc.optJSONObject("fields") ?: return@mapNotNull null
                    fun s(k: String) = fields.optJSONObject(k)?.optString("stringValue").orEmpty()
                    val uid = doc.optString("name").substringAfterLast('/')
                    if (uid == olusturanUid) return@mapNotNull null
                    val rol = KullaniciRolleri.normalize(s("rol").ifBlank { s("role") })
                    if (rol != hedefRol) return@mapNotNull null
                    val aktif = fields.optJSONObject("aktif")?.optBoolean("booleanValue")
                        ?: fields.optJSONObject("active")?.optBoolean("booleanValue") ?: true
                    if (!aktif) return@mapNotNull null
                    val token = s("fcmToken")
                    if (token.isBlank()) null else token to rol
                }
                .distinctBy { it.first }
        }
        return emptyList()
    }

    private suspend fun send(
        accessToken: String,
        deviceToken: String,
        record: BildirimRecord,
        route: String,
        rol: String
    ) {
        val data = JSONObject()
            .put("title", record.baslik)
            .put("body", record.mesaj)
            .put("tip", record.tip)
            .put("route", BildirimRota.hedefRoute(mapTip(record.tip), record.talepId, rol))
            .put("talepId", record.talepId.orEmpty())
            .put("bildirimId", record.id)
            .put("module", "satinalma")
            .put("screen", route.substringBefore('?'))

        val body = JSONObject()
            .put("message", JSONObject()
                .put("token", deviceToken)
                .put("data", data)
                .put("android", JSONObject()
                    .put("priority", "HIGH")
                    .put("ttl", "3600s")))

        val url = "https://fcm.googleapis.com/v1/projects/$projectId/messages:send"
        HttpClients.postJsonAuthorized(url, body.toString(), accessToken)
    }

    private fun openServiceAccount(): InputStream? =
        runCatching { context.assets.open("fcm-service-account.json") }.getOrNull()

    private fun mapTip(tip: String): String = when (tip) {
        "yonetime_gonderildi" -> "YonetimeGonderildi"
        "teklif_istendi" -> "TeklifIstendi"
        "teklif_onayda" -> "TeklifOnayda"
        "teklif_duzeltme_istendi" -> "TeklifDuzeltmeIstendi"
        "onaylandi" -> "Onaylandi"
        "reddedildi" -> "Reddedildi"
        "siparis_olusturuldu" -> "SiparisOlusturuldu"
        "mal_kabul_edildi" -> "MalKabulEdildi"
        else -> tip
    }
}
