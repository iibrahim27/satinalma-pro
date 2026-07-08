package com.satinalmapro.android.services

import android.content.Context
import com.google.auth.oauth2.GoogleCredentials
import com.satinalmapro.android.core.helpers.BildirimLog
import com.satinalmapro.android.core.helpers.BildirimMantikAnahtari
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
    private val messagingScope = listOf("https://www.googleapis.com/auth/firebase.messaging")
    private val firestoreScope = listOf("https://www.googleapis.com/auth/datastore")

    data class PushResult(
        val basarili: Boolean,
        val hedefSayisi: Int,
        val inboxYazilan: Int,
        val fcmGonderilen: Int,
        val hatalar: List<String>,
        val uyarilar: List<String>
    )

    private data class PushTarget(val uid: String, val token: String, val rol: String)

    suspend fun push(record: BildirimRecord, olusturanUid: String): PushResult {
        BildirimLog.pushBasladi(record.tip, record.talepId, record.hedefRol, record.hedefUid)

        val hatalar = mutableListOf<String>()
        val uyarilar = mutableListOf<String>()

        val accountStream = openServiceAccount()
        if (accountStream == null) {
            val mesaj = "fcm-service-account.json bulunamadı — push gönderilemedi"
            BildirimLog.e("FCM_PUSH", mesaj)
            return PushResult(false, 0, 0, 0, listOf(mesaj), emptyList())
        }

        val accessToken = try {
            val credentials = GoogleCredentials.fromStream(accountStream)
                .createScoped(messagingScope + firestoreScope)
            credentials.refreshIfExpired()
            credentials.accessToken.tokenValue
        } catch (ex: Exception) {
            val mesaj = "Service account kimlik doğrulama hatası"
            BildirimLog.e("FCM_PUSH", mesaj, ex)
            return PushResult(false, 0, 0, 0, listOf("$mesaj: ${ex.message}"), emptyList())
        }

        val targets = resolveTargets(record, olusturanUid, accessToken, uyarilar, hatalar)
        if (targets.isEmpty()) {
            val mesaj = "Push hedefi bulunamadı (rol=${record.hedefRol}, uid=${record.hedefUid})"
            uyarilar.add(mesaj)
            BildirimLog.w("FCM_PUSH", mesaj)
            return PushResult(false, 0, 0, 0, hatalar, uyarilar)
        }

        val docId = BildirimMantikAnahtari.olustur(record)
        var inboxYazilan = 0
        var fcmGonderilen = 0

        for (target in targets) {
            try {
                firestore.writeInboxEntryAdmin(accessToken, target.uid, docId, record)
                inboxYazilan++
                BildirimLog.d("FCM_PUSH", "Inbox yazıldı uid=${target.uid} doc=$docId")
            } catch (ex: Exception) {
                val mesaj = "Inbox yazılamadı uid=${target.uid}: ${ex.message}"
                hatalar.add(mesaj)
                BildirimLog.e("FCM_PUSH", mesaj, ex)
            }

            try {
                val route = BildirimRota.hedefRoute(mapTip(record.tip), record.talepId, target.rol)
                send(accessToken, target.token, record, route, target.rol, olusturanUid, docId)
                fcmGonderilen++
                BildirimLog.d("FCM_PUSH", "FCM gönderildi uid=${target.uid} rol=${target.rol}")
            } catch (ex: Exception) {
                val mesaj = "FCM gönderilemedi uid=${target.uid}: ${ex.message}"
                hatalar.add(mesaj)
                BildirimLog.e("FCM_PUSH", mesaj, ex)
            }
        }

        val basarili = fcmGonderilen > 0
        val sonuc = PushResult(basarili, targets.size, inboxYazilan, fcmGonderilen, hatalar, uyarilar)
        BildirimLog.pushSonuc(sonuc)
        BildirimLog.kaydetContext(context, if (basarili) "INFO" else "ERROR", "FCM_PUSH",
            "hedef=${targets.size} fcm=$fcmGonderilen inbox=$inboxYazilan")
        return sonuc
    }

    private suspend fun resolveTargets(
        record: BildirimRecord,
        olusturanUid: String,
        adminToken: String,
        uyarilar: MutableList<String>,
        hatalar: MutableList<String>
    ): List<PushTarget> {
        if (!record.hedefUid.isNullOrBlank()) {
            if (record.hedefUid.equals(olusturanUid, ignoreCase = true)) {
                uyarilar.add("İşlemi yapan kişiye bildirim gönderilmez: ${record.hedefUid}")
                return emptyList()
            }
            val user = try {
                firestore.readUserAdmin(adminToken, record.hedefUid)
            } catch (ex: Exception) {
                hatalar.add("Kullanıcı okunamadı uid=${record.hedefUid}: ${ex.message}")
                null
            } ?: return emptyList()

            val token = user.optJSONObject("fcmToken")?.optString("stringValue").orEmpty()
            val rol = user.optJSONObject("rol")?.optString("stringValue")
                ?: user.optJSONObject("role")?.optString("stringValue").orEmpty()
            if (token.isBlank()) {
                uyarilar.add("Hedef kullanıcının FCM token'ı yok: ${record.hedefUid}")
                return emptyList()
            }
            return listOf(PushTarget(record.hedefUid, token, rol))
        }

        if (!record.hedefRol.isNullOrBlank()) {
            val hedefRol = KullaniciRolleri.normalize(record.hedefRol)
            val users = try {
                firestore.listUsersAdmin(adminToken)
            } catch (ex: Exception) {
                hatalar.add("Kullanıcı listesi okunamadı: ${ex.message}")
                BildirimLog.e("FCM_PUSH", "listUsersAdmin başarısız", ex)
                return emptyList()
            }

            val targets = users.mapNotNull { doc -> parsePushTarget(doc, olusturanUid, hedefRol) }
                .distinctBy { it.token }

            if (targets.isEmpty()) {
                uyarilar.add("Rol '$hedefRol' için aktif FCM token'lı kullanıcı bulunamadı (toplam kullanıcı: ${users.size})")
            }
            return targets
        }

        hatalar.add("Bildirimde hedefRol veya hedefUid tanımlı değil")
        return emptyList()
    }

    private fun parsePushTarget(
        doc: JSONObject,
        olusturanUid: String,
        hedefRol: String
    ): PushTarget? {
        val fields = doc.optJSONObject("fields") ?: return null
        fun s(k: String) = fields.optJSONObject(k)?.optString("stringValue").orEmpty()
        val uid = doc.optString("name").substringAfterLast('/')
        if (uid.isBlank() || uid == olusturanUid) return null
        val rol = KullaniciRolleri.normalize(s("rol").ifBlank { s("role") })
        if (rol != hedefRol) return null
        val aktif = fields.optJSONObject("aktif")?.optBoolean("booleanValue")
            ?: fields.optJSONObject("active")?.optBoolean("booleanValue") ?: true
        if (!aktif) return null
        val token = s("fcmToken")
        return if (token.isBlank()) null else PushTarget(uid, token, rol)
    }

    private suspend fun send(
        accessToken: String,
        deviceToken: String,
        record: BildirimRecord,
        route: String,
        rol: String,
        olusturanUid: String,
        bildirimId: String
    ) {
        val data = JSONObject()
            .put("title", record.baslik)
            .put("body", record.mesaj)
            .put("tip", record.tip)
            .put("route", BildirimRota.hedefRoute(mapTip(record.tip), record.talepId, rol))
            .put("talepId", record.talepId.orEmpty())
            .put("bildirimId", bildirimId)
            .put("olusturanUid", record.olusturanUid.ifBlank { olusturanUid })
            .put("module", "satinalma")
            .put("screen", route.substringBefore('?'))

        val body = JSONObject()
            .put("message", JSONObject()
                .put("token", deviceToken)
                .put("data", data)
                .put("android", JSONObject()
                    .put("priority", "HIGH")
                    .put("ttl", "3600s")
                    .put("direct_boot_ok", true)))

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
