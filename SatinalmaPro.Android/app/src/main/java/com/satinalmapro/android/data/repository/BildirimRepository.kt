package com.satinalmapro.android.data.repository

import com.satinalmapro.android.core.JsonConfig
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.helpers.BildirimMantikAnahtari
import com.satinalmapro.android.core.helpers.BildirimTekillestirme
import com.satinalmapro.android.core.model.AppNotification
import com.satinalmapro.android.core.model.BildirimRecord
import com.satinalmapro.android.core.model.BildirimTipleri
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.TalepDurumlari
import com.satinalmapro.android.core.roles.TalepKuyrugu
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirestoreClient
import com.satinalmapro.android.core.helpers.BildirimLog
import com.satinalmapro.android.services.FcmPushService
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.UUID

class BildirimRepository(
    private val firestore: FirestoreClient,
    private val auth: FirebaseAuthClient,
    private val fcmPush: FcmPushService? = null
) {
    private val gson = JsonConfig.gson
    private val listType = object : TypeToken<List<BildirimRecord>>() {}.type

    suspend fun loadAll(): List<BildirimRecord> {
        val json = firestore.readDocumentJson("veri/bildirimler") ?: return emptyList()
        return runCatching { gson.fromJson<List<BildirimRecord>>(json, listType) ?: emptyList() }.getOrDefault(emptyList())
    }

    suspend fun ekle(records: List<BildirimRecord>) {
        if (records.isEmpty()) return
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        val list = loadAll().toMutableList()
        val pushed = mutableListOf<BildirimRecord>()
        records.forEach { incoming ->
            val stamped = incoming.copy(
                id = incoming.id.ifBlank { UUID.randomUUID().toString() },
                guncellemeUtc = if (incoming.guncellemeUtc > 0) incoming.guncellemeUtc else System.currentTimeMillis()
            )
            val mevcut = mevcutMantikKayit(list, stamped)
            if (mevcut != null) {
                val index = list.indexOfFirst { it.id.equals(mevcut.id, true) }
                if (index >= 0) {
                    val asamaDegisti = mevcut.tip != stamped.tip
                    val icerikDegisti = mevcut.baslik != stamped.baslik || mevcut.mesaj != stamped.mesaj
                    val guncel = mevcut.copy(
                        baslik = stamped.baslik,
                        mesaj = stamped.mesaj,
                        okundu = if (asamaDegisti || icerikDegisti) false else mevcut.okundu,
                        guncellemeUtc = stamped.guncellemeUtc,
                        inboxDocId = mevcut.inboxDocId?.takeIf { it.isNotBlank() } ?: stamped.inboxDocId
                    )
                    list[index] = guncel
                    if (asamaDegisti || icerikDegisti) {
                        pushed.add(guncel)
                    }
                }
            } else {
                val index = list.indexOfFirst { it.id.equals(stamped.id, true) }
                val saved = if (index >= 0) birlestir(list[index], stamped).also { list[index] = it } else stamped.also { list.add(0, it) }
                pushed.add(saved)
            }
        }
        val tekille = BildirimTekillestirme.tekille(list)
        firestore.writeDocumentJson("veri/bildirimler", gson.toJson(tekille), uid)
        val olusturanUid = auth.uid.orEmpty()
        BildirimLog.i("BILDIRIM", "Kaydedildi: ${records.size} kayıt, push kuyruğu: ${pushed.size}")
        for (saved in pushed) {
            if (fcmPush == null) {
                BildirimLog.w("BILDIRIM", "FCM servisi yok — yalnızca uygulama içi kayıt tip=${saved.tip}")
                continue
            }
            try {
                val sonuc = fcmPush.push(saved, olusturanUid)
                BildirimLog.pushSonuc(sonuc)
            } catch (ex: Exception) {
                BildirimLog.e("BILDIRIM", "Push hatası tip=${saved.tip} talep=${saved.talepId}", ex)
            }
        }
    }

    suspend fun okunduIsaretle(id: String) {
        val uid = auth.uid ?: return
        val list = loadAll().toMutableList()
        val target = list.firstOrNull { it.id.equals(id, true) }
            ?: list.firstOrNull { it.inboxDocId.equals(id, true) }
        val anahtar = target?.let { BildirimMantikAnahtari.olustur(it) }
        var changed = false
        for (i in list.indices) {
            val record = list[i]
            val eslesir = record.id.equals(id, true) ||
                record.inboxDocId.equals(id, true) ||
                (anahtar != null && BildirimMantikAnahtari.olustur(record) == anahtar)
            if (eslesir && !record.okundu) {
                list[i] = record.copy(okundu = true, guncellemeUtc = System.currentTimeMillis())
                changed = true
            }
        }
        if (changed) {
            firestore.writeDocumentJson("veri/bildirimler", gson.toJson(BildirimTekillestirme.tekille(list)), uid)
        }
        val inboxId = target?.inboxDocId?.takeIf { it.isNotBlank() } ?: target?.id ?: id
        try {
            firestore.markInboxRead(uid, inboxId)
        } catch (ex: Exception) {
            BildirimLog.e("BILDIRIM", "Inbox okundu işaretleme başarısız doc=$inboxId", ex)
        }
    }

    /** Talep aşaması değişince aynı hedefe ait önceki bildirimleri okundu işaretle. */
    suspend fun talepOncekiAsamaOkundu(
        talepId: String,
        yeniTip: String,
        hedefRol: String? = null,
        hedefUid: String? = null
    ) {
        val uid = auth.uid ?: return
        val list = loadAll().toMutableList()
        var changed = false
        for (i in list.indices) {
            val record = list[i]
            if (!record.talepId.equals(talepId, true)) continue
            if (record.tip == yeniTip) continue
            if (!hedefUid.isNullOrBlank()) {
                if (!record.hedefUid.equals(hedefUid, ignoreCase = true)) continue
            } else if (!hedefRol.isNullOrBlank()) {
                val kayitRol = record.hedefRol?.let { KullaniciRolleri.normalize(it) }
                if (kayitRol != KullaniciRolleri.normalize(hedefRol)) continue
            }
            if (!record.okundu) {
                list[i] = record.copy(okundu = true, guncellemeUtc = System.currentTimeMillis())
                changed = true
            }
        }
        if (changed) {
            firestore.writeDocumentJson("veri/bildirimler", gson.toJson(BildirimTekillestirme.tekille(list)), uid)
        }
        runCatching {
            val inbox = tumInboxSayfalari(uid)
            inbox.forEach { doc ->
                val docId = doc.optString("name").substringAfterLast('/')
                if (docId.isBlank()) return@forEach
                val fields = doc.optJSONObject("fields") ?: return@forEach
                if (inboxArsivlenmisMi(fields)) return@forEach
                fun s(key: String) = fields.optJSONObject(key)?.optString("stringValue").orEmpty()
                val docTalepId = s("talepId").ifBlank { s("entityId") }
                val docTip = s("tip").ifBlank { s("type") }.ifBlank { mapEventCodeToTip(s("eventCode")) }
                val docHedefRol = s("hedefRol").ifBlank { s("targetRole") }
                val docHedefUid = s("hedefUid").ifBlank { s("targetUid") }
                val okundu = fields.optJSONObject("isRead")?.optBoolean("booleanValue")
                    ?: fields.optJSONObject("okundu")?.optBoolean("booleanValue") ?: false
                if (!docTalepId.equals(talepId, true) || docTip == yeniTip || okundu) return@forEach
                if (!hedefUid.isNullOrBlank()) {
                    if (!docHedefUid.equals(hedefUid, ignoreCase = true)) return@forEach
                } else if (!hedefRol.isNullOrBlank()) {
                    if (KullaniciRolleri.normalize(docHedefRol) != KullaniciRolleri.normalize(hedefRol)) return@forEach
                }
                runCatching { firestore.markInboxRead(uid, docId) }
            }
        }
    }

    suspend fun tumunuOkunduIsaretle(user: UserProfile) {
        val uid = auth.uid ?: return
        tumInboxOkundu(uid)
        val list = loadAll().toMutableList()
        var changed = false
        for (i in list.indices) {
            val record = list[i]
            if (kullaniciyaMi(record, user) && !record.okundu) {
                list[i] = record.copy(okundu = true, guncellemeUtc = System.currentTimeMillis())
                changed = true
            }
        }
        if (changed) {
            firestore.writeDocumentJson(
                "veri/bildirimler",
                gson.toJson(BildirimTekillestirme.tekille(list)),
                uid
            )
        }
    }

    suspend fun temizle(user: UserProfile, talepler: List<TalepItem>) {
        val uid = auth.uid ?: return
        gecersizleriSil(talepler, uid)
        val list = loadAll().filterNot { record ->
            kullaniciyaMi(record, user) && !korunmali(record, talepler)
        }
        firestore.writeDocumentJson(
            "veri/bildirimler",
            gson.toJson(BildirimTekillestirme.tekille(list)),
            uid
        )
        tumInboxArsivle(uid, user, talepler)
    }

    private suspend fun tumInboxOkundu(uid: String) {
        var guard = 0
        while (guard++ < 20) {
            val inbox = runCatching { firestore.readInbox(uid, limit = 100) }.getOrDefault(emptyList())
            val okunacak = inbox.mapNotNull { doc ->
                val fields = doc.optJSONObject("fields") ?: return@mapNotNull null
                if (inboxArsivlenmisMi(fields)) return@mapNotNull null
                val okundu = fields.optJSONObject("isRead")?.optBoolean("booleanValue")
                    ?: fields.optJSONObject("okundu")?.optBoolean("booleanValue") ?: false
                if (okundu) return@mapNotNull null
                doc.optString("name").substringAfterLast('/').takeIf { it.isNotBlank() }
            }
            if (okunacak.isEmpty()) break
            okunacak.forEach { id ->
                try {
                    firestore.markInboxRead(uid, id)
                } catch (ex: Exception) {
                    BildirimLog.e("BILDIRIM", "Inbox okundu işaretleme başarısız doc=$id", ex)
                }
            }
        }
    }

    private suspend fun tumInboxArsivle(uid: String, user: UserProfile, talepler: List<TalepItem>) {
        var guard = 0
        while (guard++ < 20) {
            val inbox = runCatching { firestore.readInbox(uid, limit = 100) }.getOrDefault(emptyList())
            val arsivlenecek = inbox.mapNotNull { doc ->
                val fields = doc.optJSONObject("fields") ?: return@mapNotNull null
                if (inboxArsivlenmisMi(fields)) return@mapNotNull null
                val record = inboxDocToRecord(doc) ?: return@mapNotNull null
                if (!kullaniciyaMi(record, user) || korunmali(record, talepler)) return@mapNotNull null
                doc.optString("name").substringAfterLast('/').takeIf { it.isNotBlank() }
            }
            if (arsivlenecek.isEmpty()) break
            arsivlenecek.forEach { id ->
                try {
                    firestore.markInboxDismissed(uid, id)
                } catch (ex: Exception) {
                    BildirimLog.e("BILDIRIM", "Inbox arşivleme başarısız doc=$id", ex)
                }
            }
        }
    }

    fun korunmaliMi(record: BildirimRecord, talepler: List<TalepItem>): Boolean =
        korunmali(record, talepler)

    private fun korunmali(record: BildirimRecord, talepler: List<TalepItem>): Boolean {
        if (record.tip != BildirimTipleri.TEKLIF_ONAYDA) return false
        val talepId = record.talepId ?: return false
        val talep = talepler.firstOrNull { it.id.equals(talepId, true) } ?: return false
        return TalepKuyrugu.yonetimTeklifKarariBekliyor(talep)
    }

    suspend fun talepBildirimleriniSil(talepId: String) {
        val uid = auth.uid ?: return
        val list = loadAll()
        val kept = list.filterNot { it.talepId.equals(talepId, true) }
        if (kept.size != list.size) {
            firestore.writeDocumentJson(
                "veri/bildirimler",
                gson.toJson(BildirimTekillestirme.tekille(kept)),
                uid
            )
        }
        runCatching {
            tumInboxSayfalari(uid).forEach { doc ->
                val docId = doc.optString("name").substringAfterLast('/')
                if (docId.isBlank()) return@forEach
                val fields = doc.optJSONObject("fields") ?: return@forEach
                fun s(key: String) = fields.optJSONObject(key)?.optString("stringValue").orEmpty()
                val docTalepId = s("talepId").ifBlank { s("entityId") }
                if (docTalepId.equals(talepId, true)) {
                    runCatching { firestore.markInboxDismissed(uid, docId) }
                }
            }
        }
    }

    suspend fun gecersizleriSil(talepler: List<TalepItem>, uid: String): List<String> {
        val authUid = auth.uid ?: return emptyList()
        val removedIds = mutableListOf<String>()

        val list = loadAll()
        val kept = list.filter { record ->
            val valid = gecerliMi(record, talepler)
            if (!valid) removedIds.add(record.id)
            valid
        }
        if (kept.size != list.size) {
            firestore.writeDocumentJson(
                "veri/bildirimler",
                gson.toJson(BildirimTekillestirme.tekille(kept)),
                authUid
            )
        }

        var guard = 0
        while (guard++ < 20) {
            val inbox = runCatching { firestore.readInbox(uid, limit = 100) }.getOrDefault(emptyList())
            if (inbox.isEmpty()) break
            var silindi = 0
            inbox.forEach { doc ->
                val docId = doc.optString("name").substringAfterLast('/')
                if (docId.isBlank()) return@forEach
                val fields = doc.optJSONObject("fields") ?: return@forEach
                fun s(key: String) = fields.optJSONObject(key)?.optString("stringValue").orEmpty()
                val tip = s("tip").ifBlank { s("type") }.ifBlank { mapEventCodeToTip(s("eventCode")) }
                val talepId = s("talepId").ifBlank { s("entityId") }.ifBlank { null }
                val pseudo = BildirimRecord(id = docId, tip = tip, talepId = talepId)
                if (!gecerliMi(pseudo, talepler)) {
                    runCatching { firestore.markInboxDismissed(uid, docId) }
                    removedIds.add(docId)
                    silindi++
                }
            }
            if (silindi == 0) break
        }

        return removedIds.distinct()
    }

    suspend fun talepBildirimleri(
        tip: String,
        talep: TalepItem,
        user: UserProfile,
        hedefRol: String? = null,
        hedefUid: String? = null,
        firmaAdi: String? = null,
        ek: String? = null,
        onaylayanRol: String? = null
    ) {
        talepOncekiAsamaOkundu(talep.id, tip, hedefRol, hedefUid)
        val (baslik, mesaj) = BildirimMetni.olustur(tip, talep, firmaAdi, ek, onaylayanRol ?: user.role)
        val now = SimpleDateFormat("dd.MM.yyyy HH:mm", Locale("tr", "TR")).format(Date())
        ekle(
            listOf(
                BildirimRecord(
                    baslik = baslik,
                    mesaj = mesaj,
                    tip = tip,
                    talepId = talep.id,
                    hedefRol = hedefRol,
                    hedefUid = hedefUid,
                    olusturanUid = user.uid,
                    olusturanAd = user.fullName,
                    olusturmaTarihi = now
                )
            )
        )
    }

    suspend fun talepBildirimleriToplu(
        tip: String,
        talep: TalepItem,
        user: UserProfile,
        hedefler: List<Pair<String?, String?>>,
        firmaAdi: String? = null,
        ek: String? = null,
        onaylayanRol: String? = null
    ) {
        if (hedefler.isEmpty()) return
        hedefler.forEach { (rol, uid) -> talepOncekiAsamaOkundu(talep.id, tip, rol, uid) }
        val (baslik, mesaj) = BildirimMetni.olustur(tip, talep, firmaAdi, ek, onaylayanRol ?: user.role)
        val now = SimpleDateFormat("dd.MM.yyyy HH:mm", Locale("tr", "TR")).format(Date())
        val records = hedefler.map { (rol, uid) ->
            BildirimRecord(
                baslik = baslik,
                mesaj = mesaj,
                tip = tip,
                talepId = talep.id,
                hedefRol = rol,
                hedefUid = uid,
                olusturanUid = user.uid,
                olusturanAd = user.fullName,
                olusturmaTarihi = now
            )
        }
        ekle(records)
    }

    fun kullaniciyaMi(record: BildirimRecord, user: UserProfile?): Boolean {
        if (user == null) return false

        if (!record.hedefUid.isNullOrBlank()) {
            return record.hedefUid.equals(user.uid, ignoreCase = true)
        }

        val creatorSelf = record.olusturanUid.isNotBlank() &&
            record.olusturanUid.equals(user.uid, ignoreCase = true)
        if (creatorSelf) return false

        if (!record.inboxDocId.isNullOrBlank()) return true

        if (KullaniciRolleri.isAdmin(user.role)) {
            return !record.hedefRol.isNullOrBlank()
        }
        if (!record.hedefRol.isNullOrBlank()) {
            return KullaniciRolleri.normalize(record.hedefRol) == KullaniciRolleri.normalize(user.role)
        }
        return false
    }

    fun appNotificationKullaniciyaMi(item: AppNotification, user: UserProfile?): Boolean =
        kullaniciyaMi(
            BildirimRecord(
                tip = item.type,
                talepId = item.requestId,
                hedefRol = item.targetRole,
                hedefUid = item.targetUid,
                olusturanUid = item.createdByUid.orEmpty(),
                inboxDocId = item.inboxDocId
            ),
            user
        )

    fun gecerliMi(record: BildirimRecord, talepler: List<TalepItem>): Boolean {
        val tip = normalizeTip(record.tip)
        if (!talepBaglantili(tip)) {
            if (tip.isBlank() && record.talepId.isNullOrBlank()) return false
            return record.talepId.isNullOrBlank()
        }

        val talepId = record.talepId?.trim().orEmpty()
        if (talepId.isBlank()) return false

        val talep = talepler.firstOrNull { it.id.equals(talepId, true) } ?: return false
        return when (tip) {
            BildirimTipleri.YONETIME_GONDERILDI ->
                TalepKuyrugu.yonetimTalepler(talep) ||
                    talep.durum == TalepDurumlari.HAZIRLANIYOR
            BildirimTipleri.TEKLIF_ISTENDI ->
                !TalepKuyrugu.teklifYonetimOnayiBekliyor(talep) &&
                    !talep.yonetimOnayKilitli &&
                    (TalepKuyrugu.satinalmaTeklifGirisiAktif(talep) ||
                        (talep.durum == TalepDurumlari.TEKLIF_GIRISI && talep.teklifler.isEmpty()))
            BildirimTipleri.TEKLIF_DUZELTME_ISTENDI ->
                TalepKuyrugu.teklifDuzenlemeDevamEdiyor(talep) &&
                    talep.durum != TalepDurumlari.YONETIM_ONAY
            BildirimTipleri.TEKLIF_ONAYDA -> TalepKuyrugu.yonetimTeklifKarariBekliyor(talep)
            BildirimTipleri.REDDEDILDI -> talep.durum == TalepDurumlari.REDDEDILDI
            BildirimTipleri.ONAYLANDI ->
                talep.durum in setOf(TalepDurumlari.ONAYLANDI, TalepDurumlari.SIPARIS)
            BildirimTipleri.SIPARIS_OLUSTURULDU -> talep.durum == TalepDurumlari.SIPARIS
            BildirimTipleri.MAL_KABUL_EDILDI -> true
            else -> false
        }
    }

    private fun talepBaglantili(tip: String): Boolean =
        tip in TALEP_BAGLANTILI_TIPLER

    private fun normalizeTip(tip: String): String = when (tip.lowercase(Locale.ROOT)) {
        "yonetimegonderildi" -> BildirimTipleri.YONETIME_GONDERILDI
        "teklifistendi" -> BildirimTipleri.TEKLIF_ISTENDI
        "teklifonayda" -> BildirimTipleri.TEKLIF_ONAYDA
        "teklifduzeltmeistendi" -> BildirimTipleri.TEKLIF_DUZELTME_ISTENDI
        "onaylandi" -> BildirimTipleri.ONAYLANDI
        "reddedildi" -> BildirimTipleri.REDDEDILDI
        "siparisolusturuldu" -> BildirimTipleri.SIPARIS_OLUSTURULDU
        "malkabuledildi" -> BildirimTipleri.MAL_KABUL_EDILDI
        else -> tip.lowercase(Locale.ROOT)
    }

    private suspend fun tumInboxSayfalari(uid: String, limit: Int = 200): List<org.json.JSONObject> =
        runCatching { firestore.readInbox(uid, limit = limit) }.getOrDefault(emptyList())

    private fun inboxArsivlenmisMi(fields: org.json.JSONObject): Boolean {
        if (!fields.optJSONObject("dismissedAt")?.optString("timestampValue").isNullOrBlank()) return true
        if (!fields.optJSONObject("archivedAt")?.optString("timestampValue").isNullOrBlank()) return true
        return fields.optJSONObject("isArchived")?.optBoolean("booleanValue") == true
    }

    private fun inboxDocToRecord(doc: org.json.JSONObject): BildirimRecord? {
        val fields = doc.optJSONObject("fields") ?: return null
        if (inboxArsivlenmisMi(fields)) return null
        fun s(key: String) = fields.optJSONObject(key)?.optString("stringValue").orEmpty()
        val eventCode = s("eventCode")
        val tip = s("tip").ifBlank { s("type") }.ifBlank { mapEventCodeToTip(eventCode) }
        val talepId = s("talepId").ifBlank { s("entityId") }.ifBlank { null }
        val docId = doc.optString("name").substringAfterLast('/')
        if (docId.isBlank()) return null
        return BildirimRecord(
            id = docId,
            tip = tip,
            talepId = talepId,
            hedefRol = s("hedefRol").ifBlank { s("targetRole") }.ifBlank { null },
            hedefUid = s("hedefUid").ifBlank { s("targetUid") }.ifBlank { null },
            olusturanUid = s("olusturanUid").ifBlank { s("createdBy") },
            inboxDocId = docId
        )
    }

    private fun mapEventCodeToTip(eventCode: String): String = when (eventCode) {
        "talep.yonetime_gonderildi" -> BildirimTipleri.YONETIME_GONDERILDI
        "teklif.istendi" -> BildirimTipleri.TEKLIF_ISTENDI
        "teklif.yonetime_gonderildi" -> BildirimTipleri.TEKLIF_ONAYDA
        "teklif.duzeltme_istendi" -> BildirimTipleri.TEKLIF_DUZELTME_ISTENDI
        "talep.onaylandi" -> BildirimTipleri.ONAYLANDI
        "talep.reddedildi" -> BildirimTipleri.REDDEDILDI
        "siparis.olusturuldu" -> BildirimTipleri.SIPARIS_OLUSTURULDU
        "depo.mal_kabul_yapildi" -> BildirimTipleri.MAL_KABUL_EDILDI
        else -> eventCode
    }

    companion object {
        private val TALEP_BAGLANTILI_TIPLER = setOf(
            BildirimTipleri.YONETIME_GONDERILDI,
            BildirimTipleri.TEKLIF_ISTENDI,
            BildirimTipleri.TEKLIF_ONAYDA,
            BildirimTipleri.TEKLIF_DUZELTME_ISTENDI,
            BildirimTipleri.ONAYLANDI,
            BildirimTipleri.REDDEDILDI,
            BildirimTipleri.SIPARIS_OLUSTURULDU,
            BildirimTipleri.MAL_KABUL_EDILDI
        )
    }

    fun appNotificationGecerliMi(item: AppNotification, talepler: List<TalepItem>): Boolean =
        gecerliMi(BildirimRecord(tip = item.type, talepId = item.requestId), talepler)

    fun toAppNotifications(
        records: List<BildirimRecord>,
        user: UserProfile?,
        talepler: List<TalepItem>
    ): List<AppNotification> =
        records
            .filter { kullaniciyaMi(it, user) && gecerliMi(it, talepler) }
            .sortedByDescending { it.guncellemeUtc }
            .map { record ->
                val route = BildirimRota.hedefRoute(
                    mapTip(record.tip),
                    record.talepId,
                    user?.role
                )
                AppNotification(
                    id = record.inboxDocId?.takeIf { it.isNotBlank() }
                        ?: BildirimMantikAnahtari.olustur(record),
                    title = record.baslik,
                    message = record.mesaj,
                    type = record.tip,
                    time = record.olusturmaTarihi,
                    requestId = record.talepId,
                    route = route,
                    read = record.okundu,
                    targetRole = record.hedefRol,
                    targetUid = record.hedefUid,
                    createdByUid = record.olusturanUid,
                    inboxDocId = record.inboxDocId
                )
            }

    fun inboxIleBirlestir(
        legacy: List<BildirimRecord>,
        inbox: List<BildirimRecord>,
        user: UserProfile?
    ): List<BildirimRecord> =
        BildirimTekillestirme.inboxIleBirlestir(legacy, inbox, user, ::kullaniciyaMi)

    fun appNotificationToRecord(item: AppNotification): BildirimRecord =
        BildirimRecord(
            id = item.id,
            baslik = item.title,
            mesaj = item.message,
            tip = item.type,
            talepId = item.requestId,
            hedefRol = item.targetRole,
            hedefUid = item.targetUid,
            olusturanUid = item.createdByUid.orEmpty(),
            olusturmaTarihi = item.time,
            okundu = item.read,
            inboxDocId = item.inboxDocId
        )

    private fun mevcutMantikKayit(list: List<BildirimRecord>, bildirim: BildirimRecord): BildirimRecord? {
        val anahtar = BildirimMantikAnahtari.olustur(bildirim)
        return list.firstOrNull { BildirimMantikAnahtari.olustur(it) == anahtar }
    }

    private fun birlestir(a: BildirimRecord, b: BildirimRecord): BildirimRecord {
        val newer = if (b.guncellemeUtc >= a.guncellemeUtc) b else a
        val older = if (newer === b) a else b
        return newer.copy(okundu = newer.okundu || older.okundu)
    }

    private fun mapTip(tip: String): String = BildirimRota.normalizeTip(tip)
}
