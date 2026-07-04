package com.satinalmapro.android.data.repository

import com.satinalmapro.android.core.JsonConfig
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.model.AppNotification
import com.satinalmapro.android.core.model.BildirimRecord
import com.satinalmapro.android.core.model.BildirimTipleri
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.roles.BildirimRota
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirestoreClient
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
            val index = list.indexOfFirst { it.id.equals(stamped.id, true) }
            val saved = if (index >= 0) birlestir(list[index], stamped).also { list[index] = it } else stamped.also { list.add(0, it) }
            pushed.add(saved)
        }
        firestore.writeDocumentJson("veri/bildirimler", gson.toJson(list), uid)
        val olusturanUid = auth.uid.orEmpty()
        pushed.forEach { saved -> runCatching { fcmPush?.push(saved, olusturanUid) } }
    }

    suspend fun okunduIsaretle(id: String) {
        val uid = auth.uid ?: return
        val list = loadAll().toMutableList()
        val index = list.indexOfFirst { it.id.equals(id, true) }
        if (index < 0) return
        list[index] = list[index].copy(okundu = true, guncellemeUtc = System.currentTimeMillis())
        firestore.writeDocumentJson("veri/bildirimler", gson.toJson(list), uid)
    }

    suspend fun tumunuOkunduIsaretle(user: UserProfile) {
        val uid = auth.uid ?: return
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
            firestore.writeDocumentJson("veri/bildirimler", gson.toJson(list), uid)
        }
    }

    suspend fun temizle(user: UserProfile, talepler: List<TalepItem>) {
        val uid = auth.uid ?: return
        val list = loadAll()
        val filtered = list.filterNot { record ->
            kullaniciyaMi(record, user) && !korunmali(record, talepler)
        }
        if (filtered.size != list.size) {
            firestore.writeDocumentJson("veri/bildirimler", gson.toJson(filtered), uid)
        }
    }

    private fun korunmali(record: BildirimRecord, talepler: List<TalepItem>): Boolean {
        if (record.tip != BildirimTipleri.TEKLIF_ONAYDA) return false
        val talepId = record.talepId ?: return false
        val talep = talepler.firstOrNull { it.id.equals(talepId, true) } ?: return false
        return talep.durum == com.satinalmapro.android.core.roles.TalepDurumlari.YONETIM_ONAY
    }

    suspend fun talepBildirimleri(
        tip: String,
        talep: TalepItem,
        user: UserProfile,
        hedefRol: String? = null,
        hedefUid: String? = null,
        firmaAdi: String? = null,
        ek: String? = null
    ) {
        val (baslik, mesaj) = BildirimMetni.olustur(tip, talep, firmaAdi, ek)
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

    fun kullaniciyaMi(record: BildirimRecord, user: UserProfile?): Boolean {
        if (user == null) return false
        if (!record.hedefUid.isNullOrBlank()) return record.hedefUid.equals(user.uid, true)
        if (!record.hedefRol.isNullOrBlank()) {
            return KullaniciRolleri.normalize(record.hedefRol) == KullaniciRolleri.normalize(user.role)
        }
        return KullaniciRolleri.isAdmin(user.role)
    }

    fun gecerliMi(record: BildirimRecord, talepler: List<TalepItem>): Boolean {
        val talepId = record.talepId ?: return true
        val talep = talepler.firstOrNull { it.id.equals(talepId, true) } ?: return true
        return when (record.tip) {
            BildirimTipleri.YONETIME_GONDERILDI -> com.satinalmapro.android.core.roles.TalepKuyrugu.yonetimTalepler(talep)
            BildirimTipleri.TEKLIF_ISTENDI -> talep.durum == com.satinalmapro.android.core.roles.TalepDurumlari.TEKLIF_GIRISI &&
                talep.teklifler.isEmpty() && !talep.yonetimOnayKilitli
            BildirimTipleri.TEKLIF_ONAYDA -> talep.durum == com.satinalmapro.android.core.roles.TalepDurumlari.YONETIM_ONAY
            BildirimTipleri.REDDEDILDI -> talep.durum == com.satinalmapro.android.core.roles.TalepDurumlari.REDDEDILDI
            BildirimTipleri.ONAYLANDI, BildirimTipleri.SIPARIS_OLUSTURULDU ->
                talep.durum in setOf(
                    com.satinalmapro.android.core.roles.TalepDurumlari.ONAYLANDI,
                    com.satinalmapro.android.core.roles.TalepDurumlari.SIPARIS
                )
            else -> true
        }
    }

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
                    id = record.id,
                    title = record.baslik,
                    message = record.mesaj,
                    type = record.tip,
                    time = record.olusturmaTarihi,
                    requestId = record.talepId,
                    route = route,
                    read = record.okundu
                )
            }

    private fun birlestir(a: BildirimRecord, b: BildirimRecord): BildirimRecord {
        val newer = if (b.guncellemeUtc >= a.guncellemeUtc) b else a
        val older = if (newer === b) a else b
        return newer.copy(okundu = newer.okundu || older.okundu)
    }

    private fun mapTip(tip: String): String = BildirimRota.normalizeTip(tip)
}
