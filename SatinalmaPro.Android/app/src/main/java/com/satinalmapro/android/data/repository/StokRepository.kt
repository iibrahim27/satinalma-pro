package com.satinalmapro.android.data.repository

import com.satinalmapro.android.core.JsonConfig
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.model.StokHareket
import com.satinalmapro.android.core.model.StokHareketTipi
import com.satinalmapro.android.core.model.StokKaydi
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirestoreClient
import com.satinalmapro.android.services.StokTeslimFisiHelper
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.UUID

class StokRepository(
    private val firestore: FirestoreClient,
    private val auth: FirebaseAuthClient
) {
    private val gson = JsonConfig.gson
    private val stokType = object : TypeToken<List<StokKaydi>>() {}.type
    private val hareketType = object : TypeToken<List<StokHareket>>() {}.type

    suspend fun loadStok(): List<StokKaydi> {
        val json = firestore.readDocumentJson("veri/stok") ?: return emptyList()
        return runCatching { gson.fromJson<List<StokKaydi>>(json, stokType) ?: emptyList() }.getOrDefault(emptyList())
    }

    suspend fun loadHareketler(): List<StokHareket> {
        val json = firestore.readDocumentJson("veri/stok_hareketleri") ?: return emptyList()
        return runCatching { gson.fromJson<List<StokHareket>>(json, hareketType) ?: emptyList() }.getOrDefault(emptyList())
    }

    private suspend fun saveStok(list: List<StokKaydi>) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        firestore.writeDocumentJson("veri/stok", gson.toJson(list), uid)
    }

    private suspend fun saveHareketler(list: List<StokHareket>) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        firestore.writeDocumentJson("veri/stok_hareketleri", gson.toJson(list), uid)
    }

    private fun bugun() = SimpleDateFormat("dd.MM.yyyy", Locale("tr", "TR")).format(Date())

    private fun stokBul(list: MutableList<StokKaydi>, malzeme: String, depo: String): StokKaydi? =
        list.firstOrNull {
            it.malzemeAdi.equals(malzeme.trim(), true) && it.depoSaha.equals(depo.trim(), true)
        }

    private fun stokBulMalzeme(
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

    data class GirisSatir(
        val malzeme: String,
        val miktar: Double,
        val birim: String,
        val kategori: String,
        val birimMaliyet: Double
    )

    data class CikisSatir(val malzeme: String, val miktar: Double)

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
        val stok = stokBul(stokList, malzeme, depo) ?: StokKaydi(
            malzemeAdi = malzeme.trim(),
            kategori = kategori.trim(),
            birim = birim.trim(),
            depoSaha = depo.trim(),
            sonGuncelleme = tarih
        ).also { stokList.add(it) }
        val index = stokList.indexOf(stok)
        val guncel = stok.copy(
            mevcutMiktar = stok.mevcutMiktar + miktar,
            birimMaliyet = if (birimMaliyet > 0) birimMaliyet else stok.birimMaliyet,
            sonGuncelleme = tarih,
            toplamDeger = (stok.mevcutMiktar + miktar) * (if (birimMaliyet > 0) birimMaliyet else stok.birimMaliyet)
        )
        stokList[index] = guncel
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
        val index = stokList.indexOf(stok)
        val guncel = stok.copy(
            mevcutMiktar = stok.mevcutMiktar - miktar,
            sonGuncelleme = tarih,
            toplamDeger = (stok.mevcutMiktar - miktar) * stok.birimMaliyet
        )
        stokList[index] = guncel
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
        val index = stokList.indexOf(stok)
        val guncel = stok.copy(
            mevcutMiktar = sayimMiktari,
            sonGuncelleme = tarih,
            toplamDeger = sayimMiktari * stok.birimMaliyet
        )
        stokList[index] = guncel
        hareketList.add(
            StokHareket(
                id = UUID.randomUUID().toString(),
                tarih = tarih,
                hareketTipi = if (fark > 0) StokHareketTipi.GIRIS else StokHareketTipi.CIKIS,
                malzemeAdi = guncel.malzemeAdi,
                kategori = guncel.kategori,
                birim = guncel.birim,
                miktar = kotlin.math.abs(fark),
                depoSaha = guncel.depoSaha,
                birimMaliyet = guncel.birimMaliyet,
                belgeNo = "SAY-${System.currentTimeMillis()}",
                islemYapan = user.fullName,
                teslimEdilen = "Sayım düzeltmesi"
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
            val stok = stokBul(stokList, satir.malzeme, depoAdi) ?: StokKaydi(
                malzemeAdi = satir.malzeme.trim(),
                kategori = satir.kategori.trim().ifBlank { "Genel" },
                birim = satir.birim.trim().ifBlank { "Adet" },
                depoSaha = depoAdi,
                sonGuncelleme = tarih
            ).also { stokList.add(it) }
            val index = stokList.indexOf(stok)
            val birimMaliyet = if (satir.birimMaliyet > 0) satir.birimMaliyet else stok.birimMaliyet
            val guncel = stok.copy(
                mevcutMiktar = stok.mevcutMiktar + satir.miktar,
                birim = satir.birim.trim().ifBlank { stok.birim },
                kategori = satir.kategori.trim().ifBlank { stok.kategori },
                birimMaliyet = birimMaliyet,
                sonGuncelleme = tarih,
                toplamDeger = (stok.mevcutMiktar + satir.miktar) * birimMaliyet
            )
            stokList[index] = guncel
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
            val stok = stokBulMalzeme(stokList, satir.malzeme, user.site)
                ?: throw IllegalArgumentException("Stok bulunamadı: ${satir.malzeme}")
            if (satir.miktar > stok.mevcutMiktar) {
                throw IllegalArgumentException("Yetersiz stok: ${stok.malzemeAdi} (${stok.depoSaha})")
            }
            val index = stokList.indexOf(stok)
            val guncel = stok.copy(
                mevcutMiktar = stok.mevcutMiktar - satir.miktar,
                sonGuncelleme = tarih,
                toplamDeger = (stok.mevcutMiktar - satir.miktar) * stok.birimMaliyet
            )
            stokList[index] = guncel
            hareketList.add(
                StokHareket(
                    id = UUID.randomUUID().toString(),
                    tarih = tarih,
                    hareketTipi = StokHareketTipi.CIKIS,
                    malzemeAdi = guncel.malzemeAdi,
                    kategori = guncel.kategori,
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
        val guncel = when {
            hareket.hareketTipi.equals(StokHareketTipi.GIRIS, true) -> stok.copy(
                mevcutMiktar = (stok.mevcutMiktar - hareket.miktar).coerceAtLeast(0.0),
                sonGuncelleme = bugun(),
                toplamDeger = (stok.mevcutMiktar - hareket.miktar).coerceAtLeast(0.0) * stok.birimMaliyet
            )
            hareket.hareketTipi.equals(StokHareketTipi.CIKIS, true) -> stok.copy(
                mevcutMiktar = stok.mevcutMiktar + hareket.miktar,
                sonGuncelleme = bugun(),
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
        val guncelStok = when {
            eski.hareketTipi.equals(StokHareketTipi.GIRIS, true) -> stok.copy(
                mevcutMiktar = stok.mevcutMiktar + miktar,
                sonGuncelleme = tarih.ifBlank { bugun() },
                toplamDeger = (stok.mevcutMiktar + miktar) * stok.birimMaliyet
            )
            else -> stok.copy(
                mevcutMiktar = stok.mevcutMiktar - miktar,
                sonGuncelleme = tarih.ifBlank { bugun() },
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
