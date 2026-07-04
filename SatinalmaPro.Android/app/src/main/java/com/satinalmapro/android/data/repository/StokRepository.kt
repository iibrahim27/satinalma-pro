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
}
