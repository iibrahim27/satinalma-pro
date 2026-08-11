package com.satinalmapro.android.data.repository

import com.satinalmapro.android.core.JsonConfig
import com.satinalmapro.android.core.NetworkError
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.model.StokHareket
import com.satinalmapro.android.core.model.StokHareketTipi
import com.satinalmapro.android.core.model.StokKaydi
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirestoreClient
import com.satinalmapro.android.data.local.OfflineCache
import com.satinalmapro.android.services.StokTeslimFisiHelper
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.UUID

class StokRepository(
    private val firestore: FirestoreClient,
    private val auth: FirebaseAuthClient,
    private val offlineCache: OfflineCache? = null
) {
    private val gson = JsonConfig.gson
    private val stokType = object : TypeToken<List<StokKaydi>>() {}.type
    private val hareketType = object : TypeToken<List<StokHareket>>() {}.type
    /** Son saveStok buluta yazıldı mı — pending yalnızca ikisi de gidince kalkar. */
    @Volatile private var lastStokCloudOk = false

    private fun tenantId(): String = TenantSession.tenantId().orEmpty()

    suspend fun loadStok(): List<StokKaydi> {
        val tid = tenantId()
        if (tid.isNotBlank() && offlineCache?.hasStokPending(tid) == true) {
            return offlineCache.loadStok(tid)
        }
        return try {
            val json = firestore.readDocumentJson("veri/stok")
            val list = if (json.isNullOrBlank()) emptyList()
            else runCatching { gson.fromJson<List<StokKaydi>>(json, stokType) ?: emptyList() }
                .getOrDefault(emptyList())
            val tekil = tekillestirStok(list, tercihYerel = false)
            if (tid.isNotBlank()) offlineCache?.saveStok(tid, tekil)
            tekil
        } catch (e: Exception) {
            if (NetworkError.isNetworkRelated(e) && tid.isNotBlank()) {
                tekillestirStok(offlineCache?.loadStok(tid) ?: emptyList(), tercihYerel = true)
            } else {
                throw e
            }
        }
    }

    suspend fun loadHareketler(): List<StokHareket> {
        val tid = tenantId()
        if (tid.isNotBlank() && offlineCache?.hasStokPending(tid) == true) {
            return offlineCache.loadStokHareketleri(tid)
        }
        return try {
            val json = firestore.readDocumentJson("veri/stok_hareketleri")
            val list = if (json.isNullOrBlank()) emptyList()
            else runCatching { gson.fromJson<List<StokHareket>>(json, hareketType) ?: emptyList() }
                .getOrDefault(emptyList())
            if (tid.isNotBlank()) offlineCache?.saveStokHareketleri(tid, list)
            list
        } catch (e: Exception) {
            if (NetworkError.isNetworkRelated(e) && tid.isNotBlank()) {
                offlineCache?.loadStokHareketleri(tid) ?: emptyList()
            } else {
                throw e
            }
        }
    }

    /** Malzeme+depo — kategori anahtarda olmamalı (çıkışta kategori değişince satır çiftlenmesin). */
    private fun stokAnahtar(s: StokKaydi) =
        "${s.malzemeAdi.trim().lowercase()}|${s.depoSaha.trim().lowercase()}"

    private fun stokTarihMs(metin: String?): Long {
        if (metin.isNullOrBlank()) return 0L
        val temiz = metin.trim()
        val formatlar = arrayOf(
            "dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy",
            "d.M.yyyy HH:mm", "d.M.yyyy", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd"
        )
        for (f in formatlar) {
            val t = runCatching {
                SimpleDateFormat(f, Locale("tr", "TR")).apply { isLenient = false }.parse(temiz)?.time
            }.getOrNull()
            if (t != null) return t
        }
        return 0L
    }

    /** Aynı malzeme+depo için tek kayıt (en yeni damga; eşitlikte tercih edilen taraf). */
    private fun tekillestirStok(liste: List<StokKaydi>, tercihYerel: Boolean = true): List<StokKaydi> {
        val map = linkedMapOf<String, StokKaydi>()
        for (s in liste) {
            val key = stokAnahtar(s)
            if (key == "|") continue
            val mevcut = map[key]
            if (mevcut == null) {
                map[key] = s
                continue
            }
            val tYeni = stokTarihMs(s.sonGuncelleme)
            val tEski = stokTarihMs(mevcut.sonGuncelleme)
            if (tYeni > tEski || (tYeni == tEski && tercihYerel)) map[key] = s
        }
        return map.values.toList()
    }

    /**
     * Yerel (yazılacak) + bulut birleştir.
     * Daha yeni SonGuncelleme kazanır; aynı anda yerel kazanır.
     * Miktar karşılaştırması YOK — çıkışı (düşük miktar) geri almasın.
     */
    private fun birlestirStok(yerel: List<StokKaydi>, bulut: List<StokKaydi>): List<StokKaydi> {
        val map = linkedMapOf<String, StokKaydi>()
        fun dahaGuncel(aday: StokKaydi, mevcut: StokKaydi, yerelAday: Boolean): Boolean {
            val tA = stokTarihMs(aday.sonGuncelleme)
            val tM = stokTarihMs(mevcut.sonGuncelleme)
            return tA > tM || (tA == tM && yerelAday)
        }
        for (s in tekillestirStok(bulut, tercihYerel = false)) {
            map[stokAnahtar(s)] = s
        }
        for (s in tekillestirStok(yerel, tercihYerel = true)) {
            val key = stokAnahtar(s)
            val mevcut = map[key]
            if (mevcut == null || dahaGuncel(s, mevcut, yerelAday = true)) map[key] = s
        }
        return map.values.toList()
    }

    private fun ayniMalzemeDepoTekBirak(list: MutableList<StokKaydi>, keeper: StokKaydi) {
        list.removeAll {
            it !== keeper &&
                it.malzemeAdi.equals(keeper.malzemeAdi, true) &&
                it.depoSaha.equals(keeper.depoSaha, true)
        }
    }

    private fun birlestirHareket(yerel: List<StokHareket>, bulut: List<StokHareket>): List<StokHareket> {
        val map = linkedMapOf<String, StokHareket>()
        for (h in bulut) if (h.id.isNotBlank()) map[h.id] = h
        for (h in yerel) if (h.id.isNotBlank()) map[h.id] = h
        return map.values.toList()
    }

    /** Okuma başarısızsa boş liste sanma — aksi halde buluttaki stok ezilir. */
    private suspend fun buluttanStokOku(): List<StokKaydi> {
        val json = firestore.readDocumentJson("veri/stok")
        if (json.isNullOrBlank()) return emptyList()
        return gson.fromJson<List<StokKaydi>>(json, stokType) ?: emptyList()
    }

    private suspend fun buluttanHareketOku(): List<StokHareket> {
        val json = firestore.readDocumentJson("veri/stok_hareketleri")
        if (json.isNullOrBlank()) return emptyList()
        return gson.fromJson<List<StokHareket>>(json, hareketType) ?: emptyList()
    }

    private suspend fun saveStok(list: List<StokKaydi>) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        val tid = tenantId()
        val temizYerel = tekillestirStok(list, tercihYerel = true)
        if (tid.isNotBlank()) offlineCache?.saveStok(tid, temizYerel)
        try {
            val bulut = buluttanStokOku()
            val birlesik = if (bulut.isEmpty()) temizYerel else birlestirStok(temizYerel, bulut)
            firestore.writeDocumentJson("veri/stok", gson.toJson(birlesik), uid)
            if (tid.isNotBlank()) offlineCache?.saveStok(tid, birlesik)
            lastStokCloudOk = true
        } catch (e: Exception) {
            lastStokCloudOk = false
            if (tid.isNotBlank()) offlineCache?.markStokPending(tid, true)
            if (NetworkError.isNetworkRelated(e)) return
            throw e
        }
    }

    private suspend fun saveHareketler(list: List<StokHareket>) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        val tid = tenantId()
        if (tid.isNotBlank()) offlineCache?.saveStokHareketleri(tid, list)
        try {
            val bulut = buluttanHareketOku()
            val birlesik = if (bulut.isEmpty()) list else birlestirHareket(list, bulut)
            firestore.writeDocumentJson("veri/stok_hareketleri", gson.toJson(birlesik), uid)
            if (tid.isNotBlank()) {
                offlineCache?.saveStokHareketleri(tid, birlesik)
                if (lastStokCloudOk) offlineCache?.markStokPending(tid, false)
                else offlineCache?.markStokPending(tid, true)
            }
        } catch (e: Exception) {
            if (tid.isNotBlank()) offlineCache?.markStokPending(tid, true)
            if (NetworkError.isNetworkRelated(e)) return
            throw e
        }
    }

    /**
     * Çevrimdışı yapılan stok giriş/çıkış/sayımı internet gelince Firebase'e yollar.
     * @return true gönderildiyse veya bekleyen yoksa
     */
    suspend fun flushPendingWrites(): Boolean {
        val tid = tenantId()
        val cache = offlineCache ?: return true
        if (tid.isBlank() || !cache.hasStokPending(tid)) return true
        val uid = auth.uid ?: return false
        val stok = cache.loadStok(tid)
        val hareket = cache.loadStokHareketleri(tid)
        return try {
            val bulutStok = buluttanStokOku()
            val bulutHareket = buluttanHareketOku()
            val birlesikStok = if (bulutStok.isEmpty()) stok else birlestirStok(stok, bulutStok)
            val birlesikHareket = if (bulutHareket.isEmpty()) hareket else birlestirHareket(hareket, bulutHareket)
            firestore.writeDocumentJson("veri/stok", gson.toJson(birlesikStok), uid)
            firestore.writeDocumentJson("veri/stok_hareketleri", gson.toJson(birlesikHareket), uid)
            cache.saveStok(tid, birlesikStok)
            cache.saveStokHareketleri(tid, birlesikHareket)
            cache.markStokPending(tid, false)
            true
        } catch (e: Exception) {
            cache.markStokPending(tid, true)
            if (NetworkError.isNetworkRelated(e)) false else throw e
        }
    }

    fun hasPendingWrites(): Boolean {
        val tid = tenantId()
        return tid.isNotBlank() && offlineCache?.hasStokPending(tid) == true
    }

    private fun bugun() = SimpleDateFormat("dd.MM.yyyy", Locale("tr", "TR")).format(Date())

    /** Stok satırı damgası — aynı gün giriş/çıkışta bulut birleştirmesinde yerel kazanır. */
    private fun simdi() = SimpleDateFormat("dd.MM.yyyy HH:mm:ss", Locale("tr", "TR")).format(Date())

    private fun stokBul(list: MutableList<StokKaydi>, malzeme: String, depo: String): StokKaydi? =
        list.firstOrNull {
            it.malzemeAdi.equals(malzeme.trim(), true) && it.depoSaha.equals(depo.trim(), true)
        }

    fun stokBulMalzeme(
        list: List<StokKaydi>,
        malzeme: String,
        preferredDepo: String? = null
    ): StokKaydi? {
        val matches = list.filter {
            it.malzemeAdi.equals(malzeme.trim(), true) && it.mevcutMiktar > 0
        }
        if (matches.isEmpty()) return null
        preferredDepo?.trim()?.takeIf { it.isNotBlank() }?.let { depo ->
            matches.firstOrNull { it.depoSaha.equals(depo, true) }?.let { return it }
        }
        return matches.maxByOrNull { it.mevcutMiktar }
    }

    /** Aynı malzeme adındaki başka kayıttan veya varsayılandan kategori. */
    fun kategoriCozumle(list: List<StokKaydi>, malzeme: String, mevcut: String?): String {
        if (!mevcut.isNullOrBlank()) return mevcut.trim()
        val ad = malzeme.trim()
        list.firstOrNull {
            it.malzemeAdi.equals(ad, true) && it.kategori.isNotBlank()
        }?.kategori?.trim()?.takeIf { it.isNotBlank() }?.let { return it }
        return "Malzeme"
    }

    data class GirisSatir(
        val malzeme: String,
        val miktar: Double,
        val birim: String,
        val kategori: String,
        val birimMaliyet: Double
    )

    data class CikisSatir(val malzeme: String, val miktar: Double, val depo: String = "")

    suspend fun girisYap(
        user: UserProfile,
        malzeme: String,
        miktar: Double,
        birim: String,
        kategori: String,
        depo: String,
        birimMaliyet: Double,
        belgeNo: String,
        teslimEden: String,
        teslimAlan: String
    ) {
        if (!KullaniciRolleri.canStockWrite(user.role)) throw IllegalStateException("Stok giriş yetkiniz yok")
        if (malzeme.isBlank() || miktar <= 0) throw IllegalArgumentException("Malzeme ve miktar gerekli")
        val stokList = loadStok().toMutableList()
        val hareketList = loadHareketler().toMutableList()
        val tarih = bugun()
        val damga = simdi()
        val stok = stokBul(stokList, malzeme, depo) ?: StokKaydi(
            malzemeAdi = malzeme.trim(),
            kategori = kategori.trim(),
            birim = birim.trim(),
            depoSaha = depo.trim(),
            sonGuncelleme = damga
        ).also { stokList.add(it) }
        val index = stokList.indexOf(stok)
        val guncel = stok.copy(
            mevcutMiktar = stok.mevcutMiktar + miktar,
            birimMaliyet = if (birimMaliyet > 0) birimMaliyet else stok.birimMaliyet,
            sonGuncelleme = damga,
            toplamDeger = (stok.mevcutMiktar + miktar) * (if (birimMaliyet > 0) birimMaliyet else stok.birimMaliyet)
        )
        stokList[index] = guncel
        ayniMalzemeDepoTekBirak(stokList, guncel)
        hareketList.add(
            StokHareket(
                id = UUID.randomUUID().toString(),
                tarih = tarih,
                hareketTipi = StokHareketTipi.GIRIS,
                malzemeAdi = guncel.malzemeAdi,
                kategori = guncel.kategori,
                birim = guncel.birim,
                miktar = miktar,
                depoSaha = guncel.depoSaha,
                birimMaliyet = guncel.birimMaliyet,
                belgeNo = belgeNo.ifBlank { "STG-${System.currentTimeMillis()}" },
                islemYapan = teslimEden.ifBlank { user.fullName },
                teslimEdilen = teslimAlan
            )
        )
        saveStok(stokList)
        saveHareketler(hareketList)
    }

    suspend fun cikisYap(
        user: UserProfile,
        malzeme: String,
        miktar: Double,
        depo: String,
        belgeNo: String,
        teslimEden: String,
        teslimAlan: String
    ) {
        if (!KullaniciRolleri.canStockWrite(user.role)) throw IllegalStateException("Stok çıkış yetkiniz yok")
        if (miktar <= 0) throw IllegalArgumentException("Miktar gerekli")
        val stokList = loadStok().toMutableList()
        val hareketList = loadHareketler().toMutableList()
        val stok = stokBul(stokList, malzeme, depo) ?: throw IllegalArgumentException("Stok bulunamadı")
        if (miktar > stok.mevcutMiktar) throw IllegalArgumentException("Yetersiz stok")
        val tarih = bugun()
        val damga = simdi()
        val index = stokList.indexOf(stok)
        val guncel = stok.copy(
            mevcutMiktar = stok.mevcutMiktar - miktar,
            sonGuncelleme = damga,
            toplamDeger = (stok.mevcutMiktar - miktar) * stok.birimMaliyet
        )
        stokList[index] = guncel
        ayniMalzemeDepoTekBirak(stokList, guncel)
        hareketList.add(
            StokHareket(
                id = UUID.randomUUID().toString(),
                tarih = tarih,
                hareketTipi = StokHareketTipi.CIKIS,
                malzemeAdi = guncel.malzemeAdi,
                kategori = guncel.kategori,
                birim = guncel.birim,
                miktar = miktar,
                depoSaha = guncel.depoSaha,
                birimMaliyet = guncel.birimMaliyet,
                belgeNo = belgeNo.ifBlank { "STC-${System.currentTimeMillis()}" },
                islemYapan = teslimEden.ifBlank { user.fullName },
                teslimEdilen = teslimAlan
            )
        )
        saveStok(stokList)
        saveHareketler(hareketList)
    }

    suspend fun sayimYap(user: UserProfile, malzeme: String, depo: String, sayimMiktari: Double) {
        if (!KullaniciRolleri.canStockWrite(user.role)) throw IllegalStateException("Stok sayım yetkiniz yok")
        if (malzeme.isBlank() || sayimMiktari < 0) throw IllegalArgumentException("Malzeme ve geçerli sayım miktarı gerekli")
        val stokList = loadStok().toMutableList()
        val hareketList = loadHareketler().toMutableList()
        val stok = stokBul(stokList, malzeme, depo) ?: throw IllegalArgumentException("Stok bulunamadı")
        val fark = sayimMiktari - stok.mevcutMiktar
        if (kotlin.math.abs(fark) < 0.0001) return
        val tarih = bugun()
        val damga = simdi()
        val index = stokList.indexOf(stok)
        val guncel = stok.copy(
            mevcutMiktar = sayimMiktari,
            sonGuncelleme = damga,
            toplamDeger = sayimMiktari * stok.birimMaliyet
        )
        stokList[index] = guncel
        ayniMalzemeDepoTekBirak(stokList, guncel)
        hareketList.add(
            StokHareket(
                id = UUID.randomUUID().toString(),
                tarih = tarih,
                hareketTipi = StokHareketTipi.SAYIM,
                malzemeAdi = guncel.malzemeAdi,
                kategori = guncel.kategori,
                birim = guncel.birim,
                miktar = kotlin.math.abs(fark),
                depoSaha = guncel.depoSaha,
                birimMaliyet = guncel.birimMaliyet,
                belgeNo = "SAY-${System.currentTimeMillis()}",
                islemYapan = user.fullName,
                teslimEdilen = "",
                aciklama = if (fark > 0) "Sayım fazlası (önceki: ${stok.mevcutMiktar})"
                else "Sayım eksiği (önceki: ${stok.mevcutMiktar})"
            )
        )
        saveStok(stokList)
        saveHareketler(hareketList)
    }

    suspend fun girisYapCoklu(
        user: UserProfile,
        belgeNo: String,
        depo: String,
        teslimAlan: String,
        satirlar: List<GirisSatir>
    ) {
        if (!KullaniciRolleri.canStockWrite(user.role)) throw IllegalStateException("Stok giriş yetkiniz yok")
        if (satirlar.isEmpty()) throw IllegalArgumentException("En az bir satır girin")
        val stokList = loadStok().toMutableList()
        val hareketList = loadHareketler().toMutableList()
        val tarih = bugun()
        val belge = belgeNo.ifBlank { "STG-${System.currentTimeMillis()}" }
        val depoAdi = depo.ifBlank { user.site.orEmpty() }.ifBlank { "Merkez Depo" }
        satirlar.forEach { satir ->
            if (satir.malzeme.isBlank() || satir.miktar <= 0) {
                throw IllegalArgumentException("Geçerli malzeme ve miktar girin")
            }
            val damga = simdi()
            val stok = stokBul(stokList, satir.malzeme, depoAdi) ?: StokKaydi(
                malzemeAdi = satir.malzeme.trim(),
                kategori = satir.kategori.trim().ifBlank { "Genel" },
                birim = satir.birim.trim().ifBlank { "Adet" },
                depoSaha = depoAdi,
                sonGuncelleme = damga
            ).also { stokList.add(it) }
            val index = stokList.indexOf(stok)
            val birimMaliyet = if (satir.birimMaliyet > 0) satir.birimMaliyet else stok.birimMaliyet
            val guncel = stok.copy(
                mevcutMiktar = stok.mevcutMiktar + satir.miktar,
                birim = satir.birim.trim().ifBlank { stok.birim },
                kategori = satir.kategori.trim().ifBlank { stok.kategori },
                birimMaliyet = birimMaliyet,
                sonGuncelleme = damga,
                toplamDeger = (stok.mevcutMiktar + satir.miktar) * birimMaliyet
            )
            stokList[index] = guncel
            ayniMalzemeDepoTekBirak(stokList, guncel)
            hareketList.add(
                StokHareket(
                    id = UUID.randomUUID().toString(),
                    tarih = tarih,
                    hareketTipi = StokHareketTipi.GIRIS,
                    malzemeAdi = guncel.malzemeAdi,
                    kategori = guncel.kategori,
                    birim = guncel.birim,
                    miktar = satir.miktar,
                    depoSaha = guncel.depoSaha,
                    birimMaliyet = guncel.birimMaliyet,
                    belgeNo = belge,
                    islemYapan = user.fullName,
                    teslimEdilen = teslimAlan.ifBlank { user.fullName }
                )
            )
        }
        saveStok(stokList)
        saveHareketler(hareketList)
    }

    suspend fun cikisYapCoklu(
        user: UserProfile,
        belgeNo: String,
        teslimAlan: String,
        satirlar: List<CikisSatir>
    ) {
        if (!KullaniciRolleri.canStockWrite(user.role)) throw IllegalStateException("Stok çıkış yetkiniz yok")
        if (satirlar.isEmpty()) throw IllegalArgumentException("En az bir satır girin")
        val stokList = loadStok().toMutableList()
        val hareketList = loadHareketler().toMutableList()
        val tarih = bugun()
        val belge = belgeNo.ifBlank { "STC-${System.currentTimeMillis()}" }
        satirlar.forEach { satir ->
            if (satir.malzeme.isBlank() || satir.miktar <= 0) {
                throw IllegalArgumentException("Geçerli malzeme ve miktar girin")
            }
            val stok = if (satir.depo.isNotBlank()) {
                stokBul(stokList, satir.malzeme, satir.depo)
            } else {
                stokBulMalzeme(stokList, satir.malzeme, user.site)
            } ?: throw IllegalArgumentException("Stok bulunamadı: ${satir.malzeme}")
            if (satir.miktar > stok.mevcutMiktar) {
                throw IllegalArgumentException("Yetersiz stok: ${stok.malzemeAdi} (${stok.depoSaha})")
            }
            val kategori = kategoriCozumle(stokList, stok.malzemeAdi, stok.kategori)
            val index = stokList.indexOf(stok)
            val guncel = stok.copy(
                mevcutMiktar = stok.mevcutMiktar - satir.miktar,
                kategori = kategori,
                sonGuncelleme = simdi(),
                toplamDeger = (stok.mevcutMiktar - satir.miktar) * stok.birimMaliyet
            )
            stokList[index] = guncel
            ayniMalzemeDepoTekBirak(stokList, guncel)
            hareketList.add(
                StokHareket(
                    id = UUID.randomUUID().toString(),
                    tarih = tarih,
                    hareketTipi = StokHareketTipi.CIKIS,
                    malzemeAdi = guncel.malzemeAdi,
                    kategori = kategori,
                    birim = guncel.birim,
                    miktar = satir.miktar,
                    depoSaha = guncel.depoSaha,
                    birimMaliyet = guncel.birimMaliyet,
                    belgeNo = belge,
                    islemYapan = StokTeslimFisiHelper.teslimEdenMetni(user.role, user.fullName),
                    teslimEdilen = teslimAlan
                )
            )
        }
        saveStok(stokList)
        saveHareketler(hareketList)
    }

    private fun hareketDuzenlenebilir(hareket: StokHareket): Boolean =
        hareket.hareketTipi.equals(StokHareketTipi.GIRIS, true) ||
            hareket.hareketTipi.equals(StokHareketTipi.CIKIS, true)

    private fun stokEtkisiniGeriAl(stokList: MutableList<StokKaydi>, hareket: StokHareket) {
        val stok = stokBul(stokList, hareket.malzemeAdi, hareket.depoSaha) ?: return
        val index = stokList.indexOf(stok)
        val damga = simdi()
        val guncel = when {
            hareket.hareketTipi.equals(StokHareketTipi.GIRIS, true) -> stok.copy(
                mevcutMiktar = (stok.mevcutMiktar - hareket.miktar).coerceAtLeast(0.0),
                sonGuncelleme = damga,
                toplamDeger = (stok.mevcutMiktar - hareket.miktar).coerceAtLeast(0.0) * stok.birimMaliyet
            )
            hareket.hareketTipi.equals(StokHareketTipi.CIKIS, true) -> stok.copy(
                mevcutMiktar = stok.mevcutMiktar + hareket.miktar,
                sonGuncelleme = damga,
                toplamDeger = (stok.mevcutMiktar + hareket.miktar) * stok.birimMaliyet
            )
            else -> return
        }
        stokList[index] = guncel
    }

    suspend fun hareketSil(user: UserProfile, hareketId: String) {
        if (!KullaniciRolleri.canStockWrite(user.role)) throw IllegalStateException("Stok düzenleme yetkiniz yok")
        val hareketList = loadHareketler().toMutableList()
        val hareket = hareketList.firstOrNull { it.id == hareketId }
            ?: throw IllegalArgumentException("Hareket bulunamadı")
        if (!hareketDuzenlenebilir(hareket)) throw IllegalArgumentException("Bu hareket düzenlenemez")
        val stokList = loadStok().toMutableList()
        stokEtkisiniGeriAl(stokList, hareket)
        hareketList.removeAll { it.id == hareketId }
        saveStok(stokList)
        saveHareketler(hareketList)
    }

    suspend fun hareketGuncelle(
        user: UserProfile,
        hareketId: String,
        tarih: String,
        miktar: Double,
        belgeNo: String,
        islemYapan: String,
        teslimEdilen: String,
        aciklama: String
    ) {
        if (!KullaniciRolleri.canStockWrite(user.role)) throw IllegalStateException("Stok düzenleme yetkiniz yok")
        if (miktar <= 0) throw IllegalArgumentException("Geçerli miktar girin")
        val hareketList = loadHareketler().toMutableList()
        val eski = hareketList.firstOrNull { it.id == hareketId }
            ?: throw IllegalArgumentException("Hareket bulunamadı")
        if (!hareketDuzenlenebilir(eski)) throw IllegalArgumentException("Bu hareket düzenlenemez")
        val stokList = loadStok().toMutableList()
        stokEtkisiniGeriAl(stokList, eski)
        hareketList.removeAll { it.id == hareketId }

        val stok = stokBul(stokList, eski.malzemeAdi, eski.depoSaha)
            ?: throw IllegalArgumentException("Stok kaydı bulunamadı")
        if (eski.hareketTipi.equals(StokHareketTipi.CIKIS, true) && miktar > stok.mevcutMiktar) {
            throw IllegalArgumentException("Yetersiz stok")
        }

        val index = stokList.indexOf(stok)
        val damga = simdi()
        val guncelStok = when {
            eski.hareketTipi.equals(StokHareketTipi.GIRIS, true) -> stok.copy(
                mevcutMiktar = stok.mevcutMiktar + miktar,
                sonGuncelleme = damga,
                toplamDeger = (stok.mevcutMiktar + miktar) * stok.birimMaliyet
            )
            else -> stok.copy(
                mevcutMiktar = stok.mevcutMiktar - miktar,
                sonGuncelleme = damga,
                toplamDeger = (stok.mevcutMiktar - miktar) * stok.birimMaliyet
            )
        }
        stokList[index] = guncelStok
        hareketList.add(
            eski.copy(
                tarih = tarih.ifBlank { eski.tarih },
                miktar = miktar,
                belgeNo = belgeNo.ifBlank { eski.belgeNo },
                islemYapan = islemYapan.ifBlank { eski.islemYapan },
                teslimEdilen = teslimEdilen.ifBlank { eski.teslimEdilen },
                aciklama = aciklama
            )
        )
        saveStok(stokList)
        saveHareketler(hareketList)
    }
}
