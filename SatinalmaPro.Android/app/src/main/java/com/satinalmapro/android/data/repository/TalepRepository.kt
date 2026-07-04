package com.satinalmapro.android.data.repository

import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.model.DashboardActivity
import com.satinalmapro.android.core.model.DashboardCard
import com.satinalmapro.android.core.model.SatinalmaAyarlar
import com.satinalmapro.android.core.model.TeklifFiyat
import com.satinalmapro.android.core.model.TeklifItem
import com.satinalmapro.android.core.model.BildirimTipleri
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepKalem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.TalepDurumlari
import com.satinalmapro.android.core.roles.TalepKuyrugu
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirestoreClient
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.UUID

class TalepRepository(
    private val firestore: FirestoreClient,
    private val auth: FirebaseAuthClient,
    private val bildirimler: BildirimRepository? = null
) {
    private val gson = Gson()
    private val listType = object : TypeToken<List<TalepItem>>() {}.type
    private val ayarType = object : TypeToken<SatinalmaAyarlar>() {}.type

    suspend fun loadTalepler(): List<TalepItem> {
        val json = firestore.readDocumentJson("veri/satinalma_talepler") ?: return emptyList()
        return runCatching { gson.fromJson<List<TalepItem>>(json, listType) ?: emptyList() }.getOrDefault(emptyList())
    }

    suspend fun loadAyarlar(): SatinalmaAyarlar {
        val json = firestore.readDocumentJson("veri/satinalma_ayarlar") ?: return SatinalmaAyarlar()
        return runCatching { gson.fromJson(json, ayarType) ?: SatinalmaAyarlar() }.getOrDefault(SatinalmaAyarlar())
    }

    suspend fun saveTalepler(talepler: List<TalepItem>) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        val json = gson.toJson(talepler)
        firestore.writeDocumentJson("veri/satinalma_talepler", json, uid)
    }

    suspend fun saveAyarlar(ayarlar: SatinalmaAyarlar) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        firestore.writeDocumentJson("veri/satinalma_ayarlar", gson.toJson(ayarlar), uid)
    }

    suspend fun createAndSend(
        user: UserProfile,
        malzeme: String,
        miktar: String,
        birim: String,
        site: String,
        aciklama: String,
        oncelik: String
    ): TalepItem = createWithKalemler(
        user, site, aciklama, oncelik,
        listOf(Triple(malzeme, miktar, birim))
    )

    suspend fun createWithKalemler(
        user: UserProfile,
        site: String,
        aciklama: String,
        oncelik: String,
        kalemler: List<Triple<String, String, String>>
    ): TalepItem {
        if (kalemler.isEmpty()) throw IllegalArgumentException("En az bir kalem gerekli")
        val parsed = kalemler.mapIndexed { index, (malzeme, miktar, birim) ->
            val miktarVal = miktar.replace(',', '.').toDoubleOrNull()
                ?: throw IllegalArgumentException("Geçerli bir miktar girin")
            if (malzeme.isBlank()) throw IllegalArgumentException("Malzeme adı gerekli")
            TalepKalem(
                id = UUID.randomUUID().toString(),
                siraNo = index + 1,
                malzeme = malzeme.trim(),
                miktar = miktarVal,
                birim = birim.ifBlank { "Adet" },
                aciklama = aciklama
            )
        }

        val cloud = loadTalepler().toMutableList()
        val ayarlar = loadAyarlar()
        val yil = SimpleDateFormat("yyyy", Locale("tr", "TR")).format(Date())
        ayarlar.sonTalepSira += 1
        val talepNo = "TLP-$yil-${ayarlar.sonTalepSira.toString().padStart(4, '0')}"

        val talep = TalepItem(
            id = UUID.randomUUID().toString(),
            talepNo = talepNo,
            tarih = SimpleDateFormat("dd.MM.yyyy", Locale("tr", "TR")).format(Date()),
            talepEden = user.fullName,
            santiyeAdi = site,
            talepAciklamasi = buildString {
                if (aciklama.isNotBlank()) append(aciklama)
                if (oncelik != "Orta") {
                    if (isNotEmpty()) append(" · ")
                    append("Aciliyet: $oncelik")
                }
            },
            olusturanUid = user.uid,
            olusturanRol = user.role,
            durum = TalepDurumlari.IMZA,
            guncellemeUtc = System.currentTimeMillis(),
            kalemler = parsed
        )

        cloud.removeAll { it.id.equals(talep.id, true) }
        cloud.add(talep)
        saveTalepler(cloud)
        saveAyarlar(ayarlar)
        bildirimler?.talepBildirimleri(BildirimTipleri.YONETIME_GONDERILDI, talep, user, hedefRol = KullaniciRolleri.YONETIM)
        bildirimler?.talepBildirimleri(BildirimTipleri.YONETIME_GONDERILDI, talep, user, hedefRol = KullaniciRolleri.SATINALMA)
        return talep
    }

    fun filter(queue: TalepQueue, talepler: List<TalepItem>, user: UserProfile?): List<TalepItem> =
        TalepKuyrugu.filtre(queue, talepler, user?.uid.orEmpty(), user?.fullName.orEmpty(), user?.role)

    fun dashboard(user: UserProfile?, talepler: List<TalepItem>, unread: Int): Pair<List<DashboardCard>, List<DashboardActivity>> {
        val role = KullaniciRolleri.normalize(user?.role)
        val uid = user?.uid.orEmpty()
        val ad = user?.fullName.orEmpty()

        val cards = when (role) {
            KullaniciRolleri.YONETIM -> listOf(
                card("Gelen Talepler", TalepKuyrugu.filtre(TalepQueue.GELEN_TALEPLER, talepler, uid, ad, role).size, "Onay bekleyen", "gelen-talepler"),
                card("Teklif Onay", TalepKuyrugu.filtre(TalepQueue.TEKLIF_ONAY, talepler, uid, ad, role).size, "Karşılaştırma", "teklif-onay"),
                card("Reddedilen", TalepKuyrugu.filtre(TalepQueue.RED_TALEPLER, talepler, uid, ad, role).size, "Geri dönen", "red-talepler"),
                card("Bildirim", unread.toString(), "Okunmamış", "bildirimler")
            )
            KullaniciRolleri.SATINALMA -> listOf(
                card("Teklif Girişi", TalepKuyrugu.filtre(TalepQueue.TEKLIF_GIR, talepler, uid, ad, role).size, "Bekleyen", "teklif-gir"),
                card("Onay Bekleyen", TalepKuyrugu.filtre(TalepQueue.ONAY_BEKLEYEN, talepler, uid, ad, role).size, "Süreçte", "onay-bekleyen"),
                card("Mal Kabul", talepler.count { TalepKuyrugu.onaylananMalzeme(it) }.toString(), "Onaylı kalem", "onaylanan-malzemeler"),
                card("Bildirim", unread.toString(), "Okunmamış", "bildirimler")
            )
            KullaniciRolleri.DEPO -> listOf(
                card("Stok", "—", "Stok modülü", "stok-durum"),
                card("Bildirim", unread.toString(), "Okunmamış", "bildirimler")
            )
            else -> listOf(
                card("Taleplerim", TalepKuyrugu.filtre(TalepQueue.TALEPLERIM, talepler, uid, ad, role).size, "Kayıtlı talep", "taleplerim"),
                card("Onay Bekleyen", TalepKuyrugu.filtre(TalepQueue.ONAY_BEKLEYEN, talepler, uid, ad, role).size, "Süreçte", "onay-bekleyen"),
                card("Onaylanan", TalepKuyrugu.filtre(TalepQueue.ONAYLANAN_TALEPLER, talepler, uid, ad, role).size, "Tamamlanan", "onaylanan-talepler"),
                card("Bildirim", unread.toString(), "Okunmamış", "bildirimler")
            )
        }

        val activities = talepler
            .filter { TalepKuyrugu.kayitli(it) }
            .sortedByDescending { it.guncellemeUtc }
            .take(5)
            .map {
                DashboardActivity(
                    title = it.talepNo.ifBlank { "Talep" },
                    subtitle = "${it.malzemeOzeti} · ${it.talepEden}",
                    status = it.durum,
                    route = "talep-detay?id=${it.id}",
                    talepId = it.id
                )
            }

        return cards to activities
    }

    fun approvedMaterials(talepler: List<TalepItem>): List<TalepItem> =
        talepler.filter { TalepKuyrugu.onaylananMalzeme(it) }.sortedByDescending { it.guncellemeUtc }

    suspend fun mutateTalep(talepId: String, transform: (TalepItem) -> TalepItem): TalepItem {
        val list = loadTalepler().toMutableList()
        val index = list.indexOfFirst { it.id.equals(talepId, true) }
        if (index < 0) throw IllegalArgumentException("Talep bulunamadı")
        val updated = transform(list[index]).copy(guncellemeUtc = System.currentTimeMillis())
        list[index] = updated
        saveTalepler(list)
        return updated
    }

    suspend fun addTeklif(
        talepId: String,
        firmaAdi: String,
        marka: String,
        vadeGunu: Int,
        teslimSuresi: String,
        odemeSekli: String,
        kalemFiyatlari: Map<String, Double>
    ): TalepItem {
        if (firmaAdi.isBlank()) throw IllegalArgumentException("Firma adı gerekli")
        return mutateTalep(talepId) { talep ->
            val fiyatlar = talep.kalemler.map { kalem ->
                val birimFiyat = kalemFiyatlari[kalem.id] ?: 0.0
                val toplam = birimFiyat * kalem.miktar
                val kdv = toplam * 0.20
                TeklifFiyat(
                    kalemId = kalem.id,
                    marka = marka,
                    birimFiyat = birimFiyat,
                    toplamTutar = toplam,
                    kdvTutari = kdv,
                    toplamKdvDahil = toplam + kdv
                )
            }
            val teklif = TeklifItem(
                id = UUID.randomUUID().toString(),
                firmaAdi = firmaAdi.trim(),
                marka = marka.trim(),
                vadeGunu = vadeGunu,
                teslimSuresi = teslimSuresi.trim(),
                odemeSekli = odemeSekli.trim(),
                fiyatlar = fiyatlar
            )
            val teklifler = talep.teklifler.filterNot { it.id == teklif.id }.toMutableList()
            teklifler.add(teklif)
            talep.copy(
                teklifler = teklifler,
                durum = TalepDurumlari.KARSILASTIRMA
            )
        }
    }

    suspend fun sendQuotesToManagement(talepId: String, user: UserProfile): TalepItem {
        val result = mutateTalep(talepId) { talep ->
            if (talep.teklifler.isEmpty()) throw IllegalArgumentException("En az bir teklif girilmelidir")
            talep.copy(durum = TalepDurumlari.YONETIM_ONAY)
        }
        bildirimler?.talepBildirimleri(BildirimTipleri.TEKLIF_ONAYDA, result, user, hedefRol = KullaniciRolleri.YONETIM)
        return result
    }

    suspend fun yonetimOnayla(talepId: String, user: UserProfile, teklifIste: Boolean): TalepItem {
        val result = mutateTalep(talepId) { talep ->
            if (!KullaniciRolleri.canManagementDecide(user.role))
                throw IllegalStateException("Yönetim onay yetkiniz yok")
            if (teklifIste) {
                talep.copy(durum = TalepDurumlari.TEKLIF_GIRISI)
            } else {
                talep.copy(
                    durum = TalepDurumlari.ONAYLANDI,
                    teklifsizYonetimOnayi = true,
                    yonetimOnayKilitli = true,
                    yonetimOnaylayanAd = user.fullName
                )
            }
        }
        if (teklifIste) {
            bildirimler?.talepBildirimleri(BildirimTipleri.TEKLIF_ISTENDI, result, user, hedefRol = KullaniciRolleri.SATINALMA)
        } else {
            bildirimler?.talepBildirimleri(BildirimTipleri.ONAYLANDI, result, user, hedefUid = result.olusturanUid)
            bildirimler?.talepBildirimleri(BildirimTipleri.ONAYLANDI, result, user, hedefRol = KullaniciRolleri.SATINALMA)
        }
        return result
    }

    suspend fun yonetimReddet(talepId: String, user: UserProfile, gerekce: String): TalepItem {
        val result = mutateTalep(talepId) { talep ->
            if (!KullaniciRolleri.canManagementDecide(user.role))
                throw IllegalStateException("Red yetkiniz yok")
            talep.copy(durum = TalepDurumlari.REDDEDILDI, redGerekcesi = gerekce.trim())
        }
        bildirimler?.talepBildirimleri(BildirimTipleri.REDDEDILDI, result, user, hedefUid = result.olusturanUid, ek = gerekce)
        return result
    }

    suspend fun yonetimTeklifOnayla(talepId: String, user: UserProfile, teklifId: String): TalepItem {
        val result = mutateTalep(talepId) { talep ->
            if (!KullaniciRolleri.canManagementDecide(user.role))
                throw IllegalStateException("Teklif onay yetkiniz yok")
            val teklif = talep.teklifler.firstOrNull { it.id.equals(teklifId, true) }
                ?: throw IllegalArgumentException("Teklif bulunamadı")
            talep.copy(
                durum = TalepDurumlari.ONAYLANDI,
                yonetimOnayKilitli = true,
                yonetimOnaylayanAd = user.fullName,
                kalemler = talep.kalemler.map { it.copy(onaylananTeklifId = teklifId) },
                teklifler = talep.teklifler.map { if (it.id.equals(teklifId, true)) it.copy(onaylandi = true) else it }
            )
        }
        val firma = result.teklifler.firstOrNull { it.id.equals(teklifId, true) }?.firmaAdi
        bildirimler?.talepBildirimleri(BildirimTipleri.ONAYLANDI, result, user, hedefRol = KullaniciRolleri.SATINALMA, firmaAdi = firma)
        if (result.olusturanUid.isNotBlank()) {
            bildirimler?.talepBildirimleri(BildirimTipleri.ONAYLANDI, result, user, hedefUid = result.olusturanUid)
        }
        return result
    }

    suspend fun teklifsizFirmaFiyatKaydet(
        talepId: String,
        girdiler: List<Triple<String, String, Double>>
    ): TalepItem {
        if (girdiler.isEmpty()) throw IllegalArgumentException("Kaydedilecek kalem bulunamadı")
        return mutateTalep(talepId) { talep ->
            if (!talep.teklifsizYonetimOnayi) throw IllegalStateException("Bu talep teklifsiz onaylı değil")
            val teklifler = talep.teklifler.toMutableList()
            val kalemOnay = talep.kalemler.associate { it.id to it.onaylananTeklifId }.toMutableMap()
            girdiler.groupBy { it.second.trim() }.forEach { (firma, satirlar) ->
                if (firma.isBlank()) throw IllegalArgumentException("Firma adı gerekli")
                val teklifId = UUID.randomUUID().toString()
                val fiyatlar = satirlar.map { (kalemId, _, birimFiyat) ->
                    val kalem = talep.kalemler.firstOrNull { it.id.equals(kalemId, true) }
                        ?: throw IllegalArgumentException("Kalem bulunamadı")
                    if (birimFiyat <= 0) throw IllegalArgumentException("Geçerli birim fiyat girin")
                    val toplam = birimFiyat * kalem.miktar
                    val kdv = toplam * 0.20
                    kalemOnay[kalemId] = teklifId
                    TeklifFiyat(
                        kalemId = kalemId,
                        birimFiyat = birimFiyat,
                        toplamTutar = toplam,
                        kdvTutari = kdv,
                        toplamKdvDahil = toplam + kdv
                    )
                }
                teklifler.add(
                    TeklifItem(
                        id = teklifId,
                        firmaAdi = firma,
                        onaylandi = true,
                        aciklama = "Yönetim onayı sonrası firma/fiyat girişi",
                        fiyatlar = fiyatlar
                    )
                )
            }
            talep.copy(
                teklifler = teklifler,
                kalemler = talep.kalemler.map { kalem ->
                    val teklifId = kalemOnay[kalem.id]
                    if (teklifId == null) kalem else kalem.copy(onaylananTeklifId = teklifId)
                }
            )
        }
    }

    suspend fun malKabul(talepId: String, kalemId: String, miktar: Double, user: UserProfile): TalepItem {
        if (!KullaniciRolleri.canMalKabul(user.role))
            throw IllegalStateException("Mal kabul yetkiniz yok")
        if (miktar <= 0) throw IllegalArgumentException("Miktar sıfırdan büyük olmalı")
        val result = mutateTalep(talepId) { talep ->
            val kalemler = talep.kalemler.map { kalem ->
                if (!kalem.id.equals(kalemId, true)) kalem
                else {
                    val yeni = kalem.kabulEdilenMiktar + miktar
                    kalem.copy(
                        kabulEdilenMiktar = yeni,
                        siparisTamamlandi = yeni >= kalem.miktar
                    )
                }
            }
            talep.copy(kalemler = kalemler)
        }
        val ozet = "$miktar kabul"
        bildirimler?.talepBildirimleri(BildirimTipleri.MAL_KABUL_EDILDI, result, user, hedefRol = KullaniciRolleri.SATINALMA, ek = ozet)
        bildirimler?.talepBildirimleri(BildirimTipleri.MAL_KABUL_EDILDI, result, user, hedefRol = KullaniciRolleri.DEPO, ek = ozet)
        if (result.olusturanUid.isNotBlank()) {
            bildirimler?.talepBildirimleri(BildirimTipleri.MAL_KABUL_EDILDI, result, user, hedefUid = result.olusturanUid, ek = ozet)
        }
        return result
    }

    private fun card(title: String, value: Any, subtitle: String, route: String) =
        DashboardCard(title, value.toString(), subtitle, route)
}
