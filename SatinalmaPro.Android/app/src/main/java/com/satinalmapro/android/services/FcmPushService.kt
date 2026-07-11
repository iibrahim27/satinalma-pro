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
            // Service account yoksa yine de kullanıcı token'ı ile inbox yazmayı dene.
            return pushInboxOnlyWithUserToken(record, olusturanUid, hatalar, uyarilar)
        }

        val accessToken = try {
            val credentials = GoogleCredentials.fromStream(accountStream)
                .createScoped(messagingScope + firestoreScope)
            credentials.refreshIfExpired()
            credentials.accessToken.tokenValue
        } catch (ex: Exception) {
            val mesaj = "Service account kimlik doğrulama hatası"
            BildirimLog.e("FCM_PUSH", mesaj, ex)
            return pushInboxOnlyWithUserToken(
                record,
                olusturanUid,
                hatalar,
                uyarilar,
                listOf("$mesaj: ${ex.message}")
            )
        }

        val targets = resolveTargets(record, olusturanUid, accessToken, uyarilar, hatalar, requireToken = false)
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

            if (target.token.isBlank()) {
                uyarilar.add("FCM token yok uid=${target.uid} — yalnızca inbox yazıldı")
                continue
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

        val basarili = inboxYazilan > 0 || fcmGonderilen > 0
        val sonuc = PushResult(basarili, targets.size, inboxYazilan, fcmGonderilen, hatalar, uyarilar)
        BildirimLog.pushSonuc(sonuc)
        BildirimLog.kaydetContext(context, if (basarili) "INFO" else "ERROR", "FCM_PUSH",
            "hedef=${targets.size} fcm=$fcmGonderilen inbox=$inboxYazilan")
        return sonuc
    }

    private suspend fun pushInboxOnlyWithUserToken(
        record: BildirimRecord,
        olusturanUid: String,
        hatalar: MutableList<String>,
        uyarilar: MutableList<String>,
        ekstraHatalar: List<String> = emptyList()
    ): PushResult {
        hatalar.addAll(ekstraHatalar)
        uyarilar.add("fcm-service-account yok/hatalı — yalnızca kullanıcı token ile inbox deneniyor")
        val targets = resolveTargetsUserToken(record, olusturanUid, uyarilar, hatalar)
        if (targets.isEmpty()) {
            BildirimLog.e("FCM_PUSH", "Inbox-only hedef yok")
            return PushResult(false, 0, 0, 0, hatalar, uyarilar)
        }
        val docId = BildirimMantikAnahtari.olustur(record)
        var inboxYazilan = 0
        for (target in targets) {
            try {
                firestore.writeInboxEntry(target.uid, docId, record)
                inboxYazilan++
            } catch (ex: Exception) {
                hatalar.add("Inbox (user) yazılamadı uid=${target.uid}: ${ex.message}")
            }
        }
        return PushResult(inboxYazilan > 0, targets.size, inboxYazilan, 0, hatalar, uyarilar)
    }

    private suspend fun resolveTargets(
        record: BildirimRecord,
        olusturanUid: String,
        adminToken: String,
        uyarilar: MutableList<String>,
        hatalar: MutableList<String>,
        requireToken: Boolean = true
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
            if (requireToken && token.isBlank()) {
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

            val targets = users.mapNotNull { doc ->
                parsePushTarget(doc, olusturanUid, hedefRol, requireToken)
            }.distinctBy { it.uid }

            if (targets.isEmpty()) {
                uyarilar.add("Rol '$hedefRol' için aktif kullanıcı bulunamadı (toplam: ${users.size})")
            }
            return targets
        }

        hatalar.add("Bildirimde hedefRol veya hedefUid tanımlı değil")
        return emptyList()
    }

    private suspend fun resolveTargetsUserToken(
        record: BildirimRecord,
        olusturanUid: String,
        uyarilar: MutableList<String>,
        hatalar: MutableList<String>
    ): List<PushTarget> {
        // Kullanıcı token ile yalnızca hedefUid biliniyorsa güvenli yazım mümkün.
        val uid = record.hedefUid?.takeIf { it.isNotBlank() } ?: return emptyList()
        if (uid.equals(olusturanUid, ignoreCase = true)) return emptyList()
        return listOf(PushTarget(uid, "", record.hedefRol.orEmpty()))
    }

    private fun parsePushTarget(
        doc: JSONObject,
        olusturanUid: String,
        hedefRol: String,
        requireToken: Boolean = true
    ): PushTarget? {
        val fields = doc.optJSONObject("fields") ?: return null
        fun s(k: String) = fields.optJSONObject(k)?.optString("stringValue").orEmpty()
        val uid = doc.optString("name").substringAfterLast('/')
        if (uid.isBlank() || uid.equals(olusturanUid, ignoreCase = true)) return null
        val rol = KullaniciRolleri.normalize(s("rol").ifBlank { s("role") })
        if (rol != hedefRol) return null
        val aktif = fields.optJSONObject("aktif")?.optBoolean("booleanValue")
            ?: fields.optJSONObject("active")?.optBoolean("booleanValue") ?: true
        if (!aktif) return null
        val token = s("fcmToken")
        if (requireToken && token.isBlank()) return null
        return PushTarget(uid, token, rol)
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
