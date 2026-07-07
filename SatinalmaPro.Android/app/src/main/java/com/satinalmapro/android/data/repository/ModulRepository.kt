package com.satinalmapro.android.data.repository

import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.JsonConfig
import com.satinalmapro.android.core.model.AgregaKaydi
import com.satinalmapro.android.core.model.AlinanMalzemeKaydi
import com.satinalmapro.android.core.model.CimentoKaydi
import com.satinalmapro.android.core.model.ModulKayitTipi
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirestoreClient
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class ModulRepository(
    private val firestore: FirestoreClient,
    private val auth: FirebaseAuthClient
) {
    private val gson = JsonConfig.gson
    private val agregaType = object : TypeToken<List<AgregaKaydi>>() {}.type
    private val cimentoType = object : TypeToken<List<CimentoKaydi>>() {}.type
    private val malzemeType = object : TypeToken<List<AlinanMalzemeKaydi>>() {}.type

    suspend fun loadAgrega(): List<AgregaKaydi> {
        val loaded: List<AgregaKaydi> = loadList(ModulKayitTipi.AGREGA.firestorePath, agregaType)
        return loaded.map { if (it.id.isBlank()) it.copy(id = java.util.UUID.randomUUID().toString()) else it }
    }

    suspend fun loadCimento(): List<CimentoKaydi> {
        val loaded: List<CimentoKaydi> = loadList(ModulKayitTipi.CIMENTO.firestorePath, cimentoType)
        return loaded.map { if (it.id.isBlank()) it.copy(id = java.util.UUID.randomUUID().toString()) else it }
    }

    suspend fun loadAlinanMalzemeler(): List<AlinanMalzemeKaydi> {
        val loaded: List<AlinanMalzemeKaydi> = loadList(ModulKayitTipi.ALINAN_MALZEME.firestorePath, malzemeType)
        return loaded.map { if (it.id.isBlank()) it.copy(id = java.util.UUID.randomUUID().toString()) else it }
    }

    suspend fun saveAgrega(list: List<AgregaKaydi>, role: String?) {
        requireWrite(role)
        saveList(ModulKayitTipi.AGREGA.firestorePath, list.map { it.hesaplaToplam() })
    }

    suspend fun saveCimento(list: List<CimentoKaydi>, role: String?) {
        requireWrite(role)
        saveList(ModulKayitTipi.CIMENTO.firestorePath, list.map { it.hesaplaToplam() })
    }

    suspend fun saveAlinanMalzemeler(list: List<AlinanMalzemeKaydi>, role: String?) {
        requireWrite(role)
        saveList(ModulKayitTipi.ALINAN_MALZEME.firestorePath, list.map { it.hesaplaToplam() })
    }

    fun bugun(): String =
        SimpleDateFormat("dd.MM.yyyy", Locale("tr", "TR")).format(Date())

    private fun requireWrite(role: String?) {
        if (!KullaniciRolleri.canModulKayitWrite(role)) {
            throw IllegalStateException("Bu modülde kayıt düzenleme yetkiniz yok")
        }
    }

    private suspend fun <T> loadList(path: String, type: java.lang.reflect.Type): List<T> {
        val json = firestore.readDocumentJson(path) ?: return emptyList()
        return runCatching { gson.fromJson<List<T>>(json, type) ?: emptyList() }.getOrDefault(emptyList())
    }

    private suspend fun saveList(path: String, list: Any) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        firestore.writeDocumentJson(path, gson.toJson(list), uid)
    }

    companion object {
        fun agregaArtisYuzdesi(list: List<AgregaKaydi>, kayit: AgregaKaydi): Double? {
            val cins = kayit.agregaCinsi.trim().lowercase()
            val onceki = list
                .filter { it.agregaCinsi.trim().equals(cins, true) && it.id != kayit.id }
                .sortedByDescending { it.tarih }
                .firstOrNull { it.tarih <= kayit.tarih && it.birimFiyati > 0 }
                ?: return null
            if (onceki.birimFiyati <= 0) return null
            return ((kayit.birimFiyati - onceki.birimFiyati) / onceki.birimFiyati) * 100.0
        }

        fun cimentoArtisYuzdesi(list: List<CimentoKaydi>, kayit: CimentoKaydi): Double? {
            val cins = kayit.cimentoCinsi.trim().lowercase()
            val onceki = list
                .filter { it.cimentoCinsi.trim().equals(cins, true) && it.id != kayit.id }
                .sortedByDescending { it.tarih }
                .firstOrNull { it.tarih <= kayit.tarih && it.birimFiyati > 0 }
                ?: return null
            if (onceki.birimFiyati <= 0) return null
            return ((kayit.birimFiyati - onceki.birimFiyati) / onceki.birimFiyati) * 100.0
        }

        fun malzemeArtisYuzdesi(list: List<AlinanMalzemeKaydi>, kayit: AlinanMalzemeKaydi): Double? {
            val ad = kayit.malzemeHizmet.trim().lowercase()
            val onceki = list
                .filter { it.malzemeHizmet.trim().equals(ad, true) && it.id != kayit.id }
                .sortedByDescending { it.tarih }
                .firstOrNull { it.tarih <= kayit.tarih && it.birimFiyati > 0 }
                ?: return null
            if (onceki.birimFiyati <= 0) return null
            return ((kayit.birimFiyati - onceki.birimFiyati) / onceki.birimFiyati) * 100.0
        }
    }
}
