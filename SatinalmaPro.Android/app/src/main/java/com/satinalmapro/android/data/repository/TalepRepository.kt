package com.satinalmapro.android.data.repository

import com.satinalmapro.android.core.JsonConfig
import com.google.gson.reflect.TypeToken
import com.satinalmapro.android.core.model.DashboardActivity
import com.satinalmapro.android.core.model.DashboardCard
import com.satinalmapro.android.core.model.ImzaAyari
import com.satinalmapro.android.core.model.OnaylananMalzemeSatiri
import com.satinalmapro.android.core.model.SatinalmaAyarlar
import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepKalem
import com.satinalmapro.android.core.model.TalepQueue
import com.satinalmapro.android.core.model.TeklifFiyat
import com.satinalmapro.android.core.model.TeklifItem
import com.satinalmapro.android.core.model.UserProfile
import com.satinalmapro.android.core.helpers.BildirimLog
import com.satinalmapro.android.core.helpers.BildirimRolPolitikasi
import com.satinalmapro.android.core.helpers.OnayBildirimYardimcisi
import com.satinalmapro.android.core.model.BildirimTipleri
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.android.core.roles.TalepDurumlari
import com.satinalmapro.android.core.roles.KalemFirmaAtamaYardimcisi
import com.satinalmapro.android.core.roles.MalKabulYardimcisi
import com.satinalmapro.android.core.roles.OnaylananMalzemeOlusturucu
import com.satinalmapro.android.core.roles.TalepKuyrugu
import com.satinalmapro.android.core.roles.TalepYetkileri
import com.satinalmapro.android.data.firebase.FirebaseAuthClient
import com.satinalmapro.android.data.firebase.FirestoreClient
import com.satinalmapro.shared.filter.ProcurementStatus
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailMutation
import com.satinalmapro.shared.filter.resolvedEnterprisePriority
import com.satinalmapro.shared.filter.resolvedEnterpriseStatus
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import java.util.UUID

class TalepRepository(
    private val firestore: FirestoreClient,
    private val auth: FirebaseAuthClient,
    private val bildirimler: BildirimRepository? = null
) {
    private val gson = JsonConfig.gson
    private val listType = object : TypeToken<List<TalepItem>>() {}.type
    private val ayarType = object : TypeToken<SatinalmaAyarlar>() {}.type

    /** Durum kaynağından enterprise status senkronu — stale Status sekmeleri boşaltmasın. */
    private fun TalepItem.withSyncedStatus(): TalepItem {
        val dogru = resolvedEnterpriseStatus()
        return if (status.equals(dogru, ignoreCase = true)) this else copy(status = dogru)
    }

    private fun syncStatuses(list: List<TalepItem>): Pair<List<TalepItem>, Boolean> {
        var degisti = false
        val synced = list.map { talep ->
            val guncel = talep.withSyncedStatus()
            if (guncel.status != talep.status) degisti = true
            guncel
        }
        return synced to degisti
    }

    /**
     * Başarılı okuma: liste (boş olabilir).
     * Geçici ağ/Firestore hatası: exception — çağıran son iyi veriyi korumalı.
     * Boş listeyi "başarısız okuma" sanmayın; yalnızca gerçek `"[]"` için empty döner.
     */
    suspend fun loadTalepler(): List<TalepItem> {
        val ayarlar = loadAyarlar()
        val silinen = runCatching {
            ayarlar.silinenTalepIdleri.map { it.lowercase() }.toSet()
        }.getOrDefault(emptySet())
        val json = firestore.readDocumentJson("veri/satinalma_talepler")
            ?: throw IllegalStateException("Talep dokümanı okunamadı (boş/geçici yanıt)")
        val raw = runCatching {
            gson.fromJson<List<TalepItem?>>(json, listType) ?: emptyList()
        }.getOrElse { ex ->
            throw IllegalStateException("Talep JSON parse hatası: ${ex.message}", ex)
        }
        val normalized = raw.mapNotNull { item ->
            runCatching { item?.normalized() }.getOrNull()
        }
        val filtered = if (silinen.isEmpty()) normalized
        else normalized.filterNot { silinen.contains(it.id.lowercase()) }
        val resetUtc = ayarlar.veriSifirlamaUtc
        // Sıkı filtre: eski offline/disk listesi sıfırlama sonrası geri gelmesin.
        val postReset = if (resetUtc > 0L) {
            filtered.filter { it.guncellemeUtc >= resetUtc }
        } else {
            filtered
        }
        // Okuma sırasında buluta yazma yok: sıfırlama sonrası eski listeyi geri yükler.
        val (synced, _) = syncStatuses(postReset)
        return synced
    }

    suspend fun loadAyarlar(): SatinalmaAyarlar {
        val raw = firestore.readDocumentRaw("veri/satinalma_ayarlar") ?: return SatinalmaAyarlar()
        val fields = org.json.JSONObject(raw).optJSONObject("fields") ?: return SatinalmaAyarlar()
        val json = fields.optJSONObject("json")?.optString("stringValue").orEmpty()
        val fromJson = if (json.isBlank()) {
            SatinalmaAyarlar()
        } else {
            runCatching { gson.fromJson(json, ayarType) ?: SatinalmaAyarlar() }
                .getOrDefault(SatinalmaAyarlar())
        }
        val docStamp = fields.optJSONObject("veriSifirlamaUtc")
            ?.optString("integerValue")
            ?.toLongOrNull()
            ?: fields.optJSONObject("veriSifirlamaUtc")
                ?.optDouble("doubleValue", 0.0)
                ?.toLong()
            ?: 0L
        return fromJson.copy(veriSifirlamaUtc = maxOf(fromJson.veriSifirlamaUtc, docStamp))
    }

    suspend fun saveTalepler(talepler: List<TalepItem>) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        var (synced, _) = syncStatuses(talepler)

        // Sıfırlama sonrası: eski (pre-reset) talepleri asla buluta geri yazma.
        val resetUtc = runCatching { loadAyarlar().veriSifirlamaUtc }.getOrDefault(0L)
        if (resetUtc > 0L && synced.isNotEmpty()) {
            val keep = synced.filter { it.guncellemeUtc >= resetUtc }
            if (keep.size < synced.size) {
                BildirimLog.w(
                    "SYNC",
                    "Sıfırlama: ${synced.size - keep.size} eski talep buluta yazılmadı"
                )
            }
            if (keep.isEmpty()) {
                // Boş buluta eski listeyi geri yazma — sessiz [] yazma da yok.
                BildirimLog.w("SYNC", "Sıfırlama: tüm liste pre-reset — buluta yazılmadı")
                return
            }
            synced = keep
        }

        val json = gson.toJson(synced)
        firestore.writeDocumentJson("veri/satinalma_talepler", json, uid)
    }

    suspend fun saveAyarlar(ayarlar: SatinalmaAyarlar) {
        val uid = auth.uid ?: throw IllegalStateException("Oturum gerekli")
        val bulut = runCatching { loadAyarlar() }.getOrNull()
        val guvenli = if (bulut != null) {
            ayarlar.copy(
                veriSifirlamaUtc = maxOf(ayarlar.veriSifirlamaUtc, bulut.veriSifirlamaUtc)
            )
        } else {
            ayarlar
        }
        firestore.writeDocumentJson("veri/satinalma_ayarlar", gson.toJson(guvenli), uid)
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
        if (!KullaniciRolleri.canCreateRequest(user.role))
            throw IllegalStateException("Talep oluşturma yetkiniz yok")
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
            talepAciklamasi = aciklama.trim(),
            talepTuru = oncelik,
            olusturanUid = user.uid,
            olusturanRol = user.role,
            durum = TalepDurumlari.IMZA,
            status = ProcurementStatus.SUBMITTED,
            guncellemeUtc = System.currentTimeMillis(),
            kalemler = parsed
        )

        cloud.removeAll { it.id.equals(talep.id, true) }
        cloud.add(talep)
        saveTalepler(cloud)
        saveAyarlar(ayarlar)
        runCatching {
            bildirimler?.talepBildirimleriToplu(
                BildirimTipleri.YONETIME_GONDERILDI,
                talep,
                user,
                BildirimRolPolitikasi.yonetimeGonderildiHedefleri()
            )
        }.onFailure { BildirimLog.e("TALEP", "Yeni talep bildirimi gönderilemedi", it) }
        return talep
    }

    fun filter(queue: TalepQueue, talepler: List<TalepItem>, user: UserProfile?): List<TalepItem> =
        TalepKuyrugu.filtre(queue, talepler, user?.uid.orEmpty(), user?.fullName.orEmpty(), user?.role)

    fun dashboard(user: UserProfile?, talepler: List<TalepItem>, unread: Int): Pair<List<DashboardCard>, List<DashboardActivity>> {
        val role = KullaniciRolleri.normalize(user?.role)
        val uid = user?.uid.orEmpty()
        val ad = user?.fullName.orEmpty()

        val cards = when (role) {
            KullaniciRolleri.ADMIN -> listOf(
                card("Gelen Talepler", TalepKuyrugu.filtre(TalepQueue.GELEN_TALEPLER, talepler, uid, ad, role).size, "Onay bekleyen", "gelen-talepler"),
                card("Teklif Girişi", TalepKuyrugu.filtre(TalepQueue.TEKLIF_GIR, talepler, uid, ad, role).size, "Bekleyen", "teklif-gir"),
                card("Teklif Onay", TalepKuyrugu.filtre(TalepQueue.TEKLIF_ONAY, talepler, uid, ad, role).size, "Karşılaştırma", "teklif-onay"),
                card("Bildirim", unread.toString(), "Okunmamış", "bildirimler")
            )
            KullaniciRolleri.YONETIM -> listOf(
                card("Gelen Talepler", TalepKuyrugu.filtre(TalepQueue.GELEN_TALEPLER, talepler, uid, ad, role).size, "Onay bekleyen", "gelen-talepler"),
                card("Teklif Bekleyen", TalepKuyrugu.filtre(TalepQueue.TEKLIF_BEKLEYEN, talepler, uid, ad, role).size, "Satınalma teklifi", "teklif-bekleyen"),
                card("Teklif Onay", TalepKuyrugu.filtre(TalepQueue.TEKLIF_ONAY, talepler, uid, ad, role).size, "Karar bekleyen", "yonetim-teklif-girilen"),
                card("Onay Geçmişi", TalepKuyrugu.filtre(TalepQueue.ONAY_GECMISI, talepler, uid, ad, role).size, "Tüm onaylar", "onay-gecmisi"),
                card("Bildirim", unread.toString(), "Okunmamış", "bildirimler")
            )
            KullaniciRolleri.SATINALMA -> listOf(
                card(
                    "Gelen Talepler",
                    TalepKuyrugu.filtre(TalepQueue.GELEN_TALEPLER, talepler, uid, ad, role).size,
                    "Onay bekleyen",
                    "gelen-talepler"
                ),
                card(
                    "Teklif İstenen",
                    TalepKuyrugu.filtre(TalepQueue.SATINALMA_TEKLIF_ISTENEN, talepler, uid, ad, role).size,
                    "Teklif girişi bekleyen",
                    "satinalma-teklif-istenen"
                ),
                card(
                    "Teklif İnceleme & Onay",
                    TalepKuyrugu.filtre(TalepQueue.TEKLIF_ONAY, talepler, uid, ad, role).size,
                    "Karar bekleyen",
                    "yonetim-teklif-girilen"
                ),
                card(
                    "Düzeltme Bekleyen",
                    TalepKuyrugu.filtre(TalepQueue.SATINALMA_TEKLIF_DUZELTME, talepler, uid, ad, role).size,
                    "Yönetimden geri gönderilen",
                    "satinalma-teklif-duzeltme"
                ),
                card(
                    "Sipariş / Mal Kabul",
                    malKabulBekleyenSayisi(talepler).toString(),
                    "Mal kabul bekleyen",
                    "satinalma-siparis"
                ),
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

        return cards to emptyList()
    }

    fun approvedMaterials(talepler: List<TalepItem>): List<OnaylananMalzemeSatiri> =
        OnaylananMalzemeOlusturucu.olustur(talepler)
            .filter { OnaylananMalzemeOlusturucu.malKabulBekleyen(it) }
            .sortedByDescending { it.talepNo }

    fun siparisBekleyenMalzemeler(talepler: List<TalepItem>): List<OnaylananMalzemeSatiri> =
        OnaylananMalzemeOlusturucu.olustur(talepler)
            .filter { OnaylananMalzemeOlusturucu.siparisVerBekleyen(it) }
            .sortedByDescending { it.talepNo }

    fun malKabulBekleyenSayisi(talepler: List<TalepItem>): Int =
        OnaylananMalzemeOlusturucu.olustur(talepler).count { OnaylananMalzemeOlusturucu.malKabulBekleyen(it) }

    fun siparisBekleyenSayisi(talepler: List<TalepItem>): Int =
        OnaylananMalzemeOlusturucu.olustur(talepler).count { OnaylananMalzemeOlusturucu.siparisVerBekleyen(it) }

    suspend fun mutateTalep(talepId: String, transform: (TalepItem) -> TalepItem): TalepItem {
        val list = loadTalepler().toMutableList()
        val index = list.indexOfFirst { it.id.equals(talepId, true) }
        if (index < 0) throw IllegalArgumentException("Talep bulunamadı")
        val updated = transform(list[index])
            .copy(guncellemeUtc = System.currentTimeMillis())
            .withSyncedStatus()
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

    suspend fun updateTeklif(
        talepId: String,
        teklifId: String,
        firmaAdi: String,
        marka: String,
        vadeGunu: Int,
        teslimSuresi: String,
        odemeSekli: String,
        kalemFiyatlari: Map<String, Double>
    ): TalepItem {
        if (firmaAdi.isBlank()) throw IllegalArgumentException("Firma adı gerekli")
        return mutateTalep(talepId) { talep ->
            if (talep.yonetimOnayKilitli)
                throw IllegalStateException("Onay kilitli talepte teklif düzenlenemez")
            val index = talep.teklifler.indexOfFirst { it.id.equals(teklifId, true) }
            if (index < 0) throw IllegalArgumentException("Teklif bulunamadı")
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
            val guncel = TeklifItem(
                id = teklifId,
                firmaAdi = firmaAdi.trim(),
                marka = marka.trim(),
                vadeGunu = vadeGunu,
                teslimSuresi = teslimSuresi.trim(),
                odemeSekli = odemeSekli.trim(),
                fiyatlar = fiyatlar
            )
            val teklifler = talep.teklifler.toMutableList()
            teklifler[index] = guncel
            talep.copy(
                teklifler = teklifler,
                durum = TalepDurumlari.KARSILASTIRMA
            )
        }
    }

    suspend fun deleteTeklif(talepId: String, teklifId: String): TalepItem =
        mutateTalep(talepId) { talep ->
            if (talep.yonetimOnayKilitli)
                throw IllegalStateException("Onay kilitli talepte teklif silinemez")
            val teklifler = talep.teklifler.filterNot { it.id.equals(teklifId, true) }
            val durum = if (teklifler.isEmpty()) TalepDurumlari.TEKLIF_GIRISI else TalepDurumlari.KARSILASTIRMA
            var yonetimOnerilen = talep.yonetimOnerilenTeklifId
            var elleSecildi = talep.satinalmaOnerisiElleSecildi
            if (yonetimOnerilen.equals(teklifId, true)) {
                yonetimOnerilen = null
                elleSecildi = false
            }
            talep.copy(
                teklifler = teklifler,
                durum = durum,
                yonetimOnerilenTeklifId = yonetimOnerilen,
                satinalmaOnerisiElleSecildi = elleSecildi
            )
        }

    suspend fun satinalmaOnerisiSec(talepId: String, teklifId: String): TalepItem =
        mutateTalep(talepId) { talep ->
            if (!talep.teklifler.any { it.id.equals(teklifId, true) })
                throw IllegalArgumentException("Teklif bulunamadı")
            talep.copy(
                yonetimOnerilenTeklifId = teklifId,
                satinalmaOnerisiElleSecildi = true
            )
        }

    suspend fun satinalmaOnerisiOtomatigeAl(talepId: String): TalepItem =
        mutateTalep(talepId) { talep ->
            talep.copy(
                yonetimOnerilenTeklifId = null,
                satinalmaOnerisiElleSecildi = false
            )
        }

    suspend fun sendQuotesToManagement(talepId: String, user: UserProfile): TalepItem {
        val result = mutateTalep(talepId) { talep ->
            if (talep.teklifler.isEmpty()) throw IllegalArgumentException("En az bir teklif girilmelidir")
            talep.teklifler.forEach { teklif ->
                if (teklif.genelToplam <= 0) {
                    throw IllegalArgumentException("'${teklif.firmaAdi}' teklifinde geçerli fiyat bulunamadı")
                }
            }
            val oneri = talep.onerilenTeklif()
                ?: throw IllegalArgumentException("Geçerli bir satınalma önerisi oluşturulamadı")
            val yonetimOnerilen = if (talep.satinalmaOnerisiElleSecildi) {
                talep.yonetimOnerilenTeklifId
            } else {
                oneri.id
            }
            talep.copy(
                durum = TalepDurumlari.YONETIM_ONAY,
                yonetimOnerilenTeklifId = yonetimOnerilen,
                satinalmaOnerisiElleSecildi = talep.satinalmaOnerisiElleSecildi,
                teklifDuzeltmeNotu = ""
            )
        }
        // Yönetim + Satınalma: teklif onay kuyruğu bildirimi (işlemi yapan kişi hariç).
        runCatching {
            bildirimler?.talepBildirimleriToplu(
                BildirimTipleri.TEKLIF_ONAYDA,
                result,
                user,
                hedefler = BildirimRolPolitikasi.teklifOnaydaHedefleri()
            )
        }.onFailure {
            BildirimLog.e("TALEP", "Teklif onayda bildirimi gönderilemedi talep=${result.id}", it)
        }
        return result
    }

    suspend fun yonetimOnayla(talepId: String, user: UserProfile, teklifIste: Boolean): TalepItem {
        val mevcut = loadTalepler().firstOrNull { it.id.equals(talepId, true) }
            ?: throw IllegalArgumentException("Talep bulunamadı")
        if (mevcut.yonetimOnayKilitli)
            throw IllegalStateException("Bu talep zaten onaylanmış")
        if (teklifIste) {
            if (!TalepKuyrugu.yonetimTalepler(mevcut) && !TalepKuyrugu.yonetimTeklifBekleyen(mevcut))
                throw IllegalStateException("Bu talep için teklif isteme işlemi zaten yapılmış")
        } else if (!TalepKuyrugu.yonetimTalepler(mevcut) && !TalepKuyrugu.yonetimTeklifBekleyen(mevcut) && mevcut.talepTuru != "Acil")
            throw IllegalStateException("Bu talep için onay işlemi yapılamaz")

        val result = mutateTalep(talepId) { talep ->
            if (!KullaniciRolleri.canManagementDecide(user.role))
                throw IllegalStateException("Yönetim onay yetkiniz yok")
            if (talep.talepTuru == "Acil" || !teklifIste) {
                talep.copy(
                    durum = TalepDurumlari.ONAYLANDI,
                    teklifsizYonetimOnayi = true,
                    yonetimOnayKilitli = true
                ).withYonetimOnayKaydi(user)
            } else {
                talep.copy(
                    durum = TalepDurumlari.TEKLIF_GIRISI,
                    yonetimOnayKilitli = false,
                    teklifsizYonetimOnayi = false
                )
            }
        }
        if (result.durum == TalepDurumlari.TEKLIF_GIRISI) {
            bildirimler?.talepBildirimleri(
                BildirimTipleri.TEKLIF_ISTENDI, result, user, hedefRol = KullaniciRolleri.SATINALMA
            )
            if (result.olusturanUid.isNotBlank()) {
                bildirimler?.talepBildirimleri(
                    BildirimTipleri.TEKLIF_ISTENDI,
                    result,
                    user,
                    hedefUid = result.olusturanUid,
                    ek = OnayBildirimYardimcisi.teklifIstemeBildirimEk(user.role)
                )
            }
        } else {
            val hedefler = OnayBildirimYardimcisi.onaylandiHedefleri(result.olusturanUid, user.role)
            bildirimler?.talepBildirimleriToplu(
                BildirimTipleri.ONAYLANDI,
                result,
                user,
                hedefler,
                onaylayanRol = user.role
            )
        }
        return result
    }

    suspend fun yonetimReddet(talepId: String, user: UserProfile, gerekce: String): TalepItem {
        val result = mutateTalep(talepId) { talep ->
            if (!KullaniciRolleri.canManagementDecide(user.role))
                throw IllegalStateException("Red yetkiniz yok")
            talep.copy(durum = TalepDurumlari.REDDEDILDI, redGerekcesi = gerekce.trim())
        }
        bildirimler?.talepBildirimleriToplu(
            BildirimTipleri.REDDEDILDI,
            result,
            user,
            BildirimRolPolitikasi.reddedildiHedefleri(result.olusturanUid, user.uid),
            ek = gerekce
        )
        return result
    }

    suspend fun yonetimTeklifOnayla(talepId: String, user: UserProfile, teklifId: String): TalepItem {
        mutateTalep(talepId) { talep ->
            if (!talep.teklifler.any { it.id.equals(teklifId, true) })
                throw IllegalArgumentException("Teklif bulunamadı")
            talep.copy(
                kalemler = talep.kalemler.map { KalemFirmaAtamaYardimcisi.tekFirmayaAta(it, teklifId) }
            )
        }
        return kalemBazliOnayla(talepId, user)
    }

    suspend fun kalemTeklifiAta(talepId: String, kalemId: String, teklifId: String?): TalepItem =
        mutateTalep(talepId) { talep ->
            if (talep.yonetimOnayKilitli)
                throw IllegalStateException("Onay kilitli talepte değişiklik yapılamaz")
            if (teklifId != null && !talep.teklifler.any { it.id.equals(teklifId, true) })
                throw IllegalArgumentException("Teklif bulunamadı")
            talep.copy(
                kalemler = talep.kalemler.map { kalem ->
                    if (!kalem.id.equals(kalemId, true)) kalem
                    else if (teklifId == null) KalemFirmaAtamaYardimcisi.temizle(kalem)
                    else KalemFirmaAtamaYardimcisi.tekFirmayaAta(kalem, teklifId)
                }
            )
        }

    suspend fun kalemBazliOnayla(
        talepId: String,
        user: UserProfile,
        kalemTeklifAtamalari: Map<String, String>? = null,
        kalemFirmaAtamalari: Map<String, List<com.satinalmapro.android.core.model.KalemFirmaAtamasi>>? = null
    ): TalepItem {
        if (!KullaniciRolleri.canApproveQuotes(user.role))
            throw IllegalStateException("Teklif onay yetkiniz yok")

        val ayarlar = loadAyarlar()
        val result = mutateTalep(talepId) { talep ->
            if (talep.yonetimOnayKilitli && talep.durum != TalepDurumlari.YONETIM_ONAY)
                throw IllegalStateException("Onay kilitli talepte onay değiştirilemez")

            val kalemler = talep.kalemler.map { kalem ->
                val bolunmus = kalemFirmaAtamalari?.get(kalem.id)
                when {
                    !bolunmus.isNullOrEmpty() -> {
                        bolunmus.forEach { a ->
                            if (!talep.teklifler.any { it.id.equals(a.teklifId, true) })
                                throw IllegalArgumentException("Teklif bulunamadı: ${a.teklifId}")
                        }
                        KalemFirmaAtamaYardimcisi.uygula(kalem, bolunmus)
                    }
                    kalemTeklifAtamalari?.containsKey(kalem.id) == true -> {
                        val teklifId = kalemTeklifAtamalari[kalem.id]
                            ?: return@map KalemFirmaAtamaYardimcisi.temizle(kalem)
                        if (!talep.teklifler.any { it.id.equals(teklifId, true) })
                            throw IllegalArgumentException("Teklif bulunamadı: $teklifId")
                        KalemFirmaAtamaYardimcisi.tekFirmayaAta(kalem, teklifId)
                    }
                    KalemFirmaAtamaYardimcisi.onayliMi(kalem) ->
                        KalemFirmaAtamaYardimcisi.uygula(kalem, KalemFirmaAtamaYardimcisi.etkinAtamalar(kalem))
                    else -> kalem
                }
            }

            val onayliKalemler = kalemler.filter { KalemFirmaAtamaYardimcisi.onayliMi(it) }
            if (onayliKalemler.isEmpty())
                throw IllegalArgumentException("En az bir kalem için firma seçin")

            val tumTeklifIdleri = onayliKalemler
                .flatMap { KalemFirmaAtamaYardimcisi.etkinAtamalar(it).map { a -> a.teklifId } }
                .distinct()

            val teklifler = talep.teklifler.map { teklif ->
                teklif.copy(onaylandi = tumTeklifIdleri.any { it.equals(teklif.id, true) })
            }

            val anaTeklifId = onayliKalemler
                .flatMap { KalemFirmaAtamaYardimcisi.etkinAtamalar(it) }
                .groupBy { it.teklifId }
                .maxByOrNull { (_, list) -> list.sumOf { it.miktar } }
                ?.key
                ?: throw IllegalArgumentException("Ana teklif seçilemedi")

            val siparisNolari = talep.firmaSiparisNolari.toMutableMap()
            tumTeklifIdleri.forEach { tid ->
                if (!siparisNolari.containsKey(tid)) {
                    ayarlar.sonSiparisSira += 1
                    val yil = SimpleDateFormat("yyyy", Locale("tr", "TR")).format(Date())
                    siparisNolari[tid] = "SIP-$yil-${ayarlar.sonSiparisSira.toString().padStart(4, '0')}"
                }
            }

            talep.copy(
                durum = TalepDurumlari.ONAYLANDI,
                yonetimOnayKilitli = true,
                onaylananTeklifId = anaTeklifId,
                teklifler = teklifler,
                kalemler = kalemler,
                firmaSiparisNolari = siparisNolari,
                siparisNo = siparisNolari[anaTeklifId].orEmpty()
            ).withYonetimOnayKaydi(user)
        }
        saveAyarlar(ayarlar)

        val anaTeklif = result.teklifler.firstOrNull { it.id == result.onaylananTeklifId }
        val firmaSayisi = result.kalemler
            .flatMap { KalemFirmaAtamaYardimcisi.etkinAtamalar(it).map { a -> a.teklifId } }
            .distinct().size
        val firmaAdi = if (firmaSayisi == 1) anaTeklif?.firmaAdi else null
        val hedefler = OnayBildirimYardimcisi.onaylandiHedefleri(result.olusturanUid, user.role)
        bildirimler?.talepBildirimleriToplu(
            BildirimTipleri.ONAYLANDI,
            result,
            user,
            hedefler,
            firmaAdi = firmaAdi,
            onaylayanRol = user.role
        )
        return result
    }

    suspend fun teklifGeriGonder(talepId: String, user: UserProfile, gerekce: String?): TalepItem {
        if (!KullaniciRolleri.canManagementDecide(user.role))
            throw IllegalStateException("Geri gönderme yetkiniz yok")

        val result = mutateTalep(talepId) { talep ->
            if (!TalepKuyrugu.teklifYonetimOnayiBekliyor(talep))
                throw IllegalStateException("Bu talep için geri gönderilecek teklif onayı bulunamadı")
            talep.copy(
                durum = TalepDurumlari.TEKLIF_GIRISI,
                status = "quote_requested",
                teklifDuzeltmeNotu = gerekce?.trim().orEmpty(),
                yonetimOnayKilitli = false,
                onaylananTeklifId = null,
                kalemler = talep.kalemler.map { KalemFirmaAtamaYardimcisi.temizle(it) }
            )
        }
        bildirimler?.talepBildirimleri(
            BildirimTipleri.TEKLIF_DUZELTME_ISTENDI,
            result,
            user,
            hedefRol = KullaniciRolleri.SATINALMA,
            ek = result.teklifDuzeltmeNotu
        )
        return result
    }

    suspend fun siparisVer(talepId: String, user: UserProfile): TalepItem {
        if (!KullaniciRolleri.canPlaceOrder(user.role))
            throw IllegalStateException("Sipariş verme yetkiniz yok")

        val ayarlar = loadAyarlar()
        val result = mutateTalep(talepId) { talep ->
            if (talep.durum != TalepDurumlari.ONAYLANDI)
                throw IllegalStateException("Yalnızca onaylanmış talepler için sipariş verilebilir")

            val siparisNolari = talep.firmaSiparisNolari.toMutableMap()
            talep.kalemler
                .flatMap { KalemFirmaAtamaYardimcisi.etkinAtamalar(it).map { a -> a.teklifId } }
                .distinct()
                .forEach { teklifId ->
                    if (!siparisNolari.containsKey(teklifId)) {
                        ayarlar.sonSiparisSira += 1
                        val yil = SimpleDateFormat("yyyy", Locale("tr", "TR")).format(Date())
                        siparisNolari[teklifId] = "SIP-$yil-${ayarlar.sonSiparisSira.toString().padStart(4, '0')}"
                    }
                }

            val anaTeklifId = talep.onaylananTeklifId
                ?: talep.kalemler
                    .flatMap { KalemFirmaAtamaYardimcisi.etkinAtamalar(it) }
                    .maxByOrNull { it.miktar }
                    ?.teklifId
            val siparisNo = when {
                anaTeklifId != null && siparisNolari.containsKey(anaTeklifId) -> siparisNolari[anaTeklifId]!!
                siparisNolari.isNotEmpty() -> siparisNolari.values.first()
                else -> talep.siparisNo
            }

            talep.copy(
                durum = TalepDurumlari.SIPARIS,
                firmaSiparisNolari = siparisNolari,
                siparisNo = siparisNo
            )
        }
        saveAyarlar(ayarlar)

        bildirimler?.talepBildirimleriToplu(
            BildirimTipleri.SIPARIS_OLUSTURULDU,
            result,
            user,
            BildirimRolPolitikasi.siparisOlusturulduHedefleri(result.olusturanUid, user.uid)
        )
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

    suspend fun malKabul(
        talepId: String,
        kalemId: String,
        miktar: Double,
        user: UserProfile,
        teklifId: String? = null
    ): TalepItem {
        if (!KullaniciRolleri.canMalKabul(user.role))
            throw IllegalStateException("Mal kabul yetkiniz yok")
        if (miktar <= 0) throw IllegalArgumentException("Miktar sıfırdan büyük olmalı")
        val satir = OnaylananMalzemeOlusturucu.olustur(loadTalepler())
            .firstOrNull {
                it.talepId.equals(talepId, true) &&
                    it.kalemId.equals(kalemId, true) &&
                    (teklifId.isNullOrBlank() || it.teklifId.equals(teklifId, true))
            }
            ?: throw IllegalArgumentException("Kalem bulunamadı")
        if (satir.durum != TalepDurumlari.SIPARIS)
            throw IllegalStateException("Mal kabul için önce sipariş verilmelidir")
        val hedefTeklifId = satir.teklifId.ifBlank { teklifId.orEmpty() }
        val result = mutateTalep(talepId) { talep ->
            val kalem = talep.kalemler.firstOrNull { it.id.equals(kalemId, true) }
                ?: throw IllegalArgumentException("Kalem bulunamadı")
            val guncelKalem = if (hedefTeklifId.isNotBlank()) {
                KalemFirmaAtamaYardimcisi.kabulEkle(kalem, hedefTeklifId, miktar)
            } else {
                val yeniKabul = kalem.kabulEdilenMiktar + miktar
                kalem.copy(
                    kabulEdilenMiktar = yeniKabul,
                    siparisTamamlandi = yeniKabul >= kalem.miktar - 0.0001
                )
            }
            talep.copy(
                kalemler = talep.kalemler.map {
                    if (it.id.equals(kalemId, true)) guncelKalem else it
                }
            )
        }
        val ozet = "${satir.malzeme} · ${"%.2f".format(miktar)} ${satir.birim}"
        bildirimler?.talepBildirimleriToplu(
            BildirimTipleri.MAL_KABUL_EDILDI,
            result,
            user,
            BildirimRolPolitikasi.malKabulEdildiHedefleri(result.olusturanUid, user.uid),
            ek = ozet
        )
        return result
    }

    suspend fun sevkiyatiTamamla(
        talepId: String,
        kalemId: String,
        user: UserProfile,
        teklifId: String? = null
    ): TalepItem {
        if (!KullaniciRolleri.canMalKabul(user.role))
            throw IllegalStateException("Bu işlem için yetkiniz yok")
        val satir = OnaylananMalzemeOlusturucu.olustur(loadTalepler())
            .firstOrNull {
                it.talepId.equals(talepId, true) &&
                    it.kalemId.equals(kalemId, true) &&
                    (teklifId.isNullOrBlank() || it.teklifId.equals(teklifId, true))
            }
            ?: throw IllegalArgumentException("Kalem bulunamadı")
        if (!OnaylananMalzemeOlusturucu.sevkiyatTamamlanabilir(satir))
            throw IllegalStateException("Bu kalem için sevkiyat tamamlanamaz")
        val hedefTeklifId = satir.teklifId.ifBlank { teklifId.orEmpty() }
        return mutateTalep(talepId) { talep ->
            val kalem = talep.kalemler.firstOrNull { it.id.equals(kalemId, true) }
                ?: throw IllegalArgumentException("Kalem bulunamadı")
            val guncelKalem = if (hedefTeklifId.isNotBlank()) {
                KalemFirmaAtamaYardimcisi.sevkiyatiTamamla(kalem, hedefTeklifId)
            } else {
                kalem.copy(siparisTamamlandi = true)
            }
            talep.copy(
                kalemler = talep.kalemler.map {
                    if (it.id.equals(kalemId, true)) guncelKalem else it
                }
            )
        }
    }

    suspend fun talepSil(talepId: String, user: UserProfile) {
        val list = loadTalepler().toMutableList()
        val talep = list.firstOrNull { it.id.equals(talepId, true) }
            ?: throw IllegalArgumentException("Talep bulunamadı")
        if (!TalepYetkileri.talepSilebilir(user.role, talep, user.uid, user.fullName))
            throw IllegalStateException("Talep silme yetkiniz yok")
        list.removeAll { it.id.equals(talepId, true) }
        saveTalepler(list)
        val ayarlar = loadAyarlar()
        val silinen = ayarlar.silinenTalepIdleri.toMutableList()
        if (silinen.none { it.equals(talepId, true) }) silinen.add(talepId)
        saveAyarlar(ayarlar.copy(silinenTalepIdleri = silinen))
        runCatching { bildirimler?.talepBildirimleriniSil(talepId) }
    }

    suspend fun talepGuncelle(
        talepId: String,
        user: UserProfile,
        site: String,
        aciklama: String,
        talepTuru: String,
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
        val mevcut = loadTalepler().firstOrNull { it.id.equals(talepId, true) }
            ?: throw IllegalArgumentException("Talep bulunamadı")
        if (!TalepYetkileri.talepDuzenleyebilir(user.role, mevcut, user.uid, user.fullName))
            throw IllegalStateException("Talep düzenleme yetkiniz yok")

        val yenidenGonder = TalepYetkileri.duzenlemeSonrasiYenidenGonder(
            user.role, mevcut, user.uid, user.fullName
        )

        val result = mutateTalep(talepId) { talep ->
            val guncel = talep.copy(
                santiyeAdi = site.trim(),
                talepAciklamasi = aciklama.trim(),
                talepTuru = talepTuru,
                kalemler = parsed
            )
            if (!yenidenGonder) guncel
            else guncel.copy(
                durum = TalepDurumlari.IMZA,
                teklifler = emptyList(),
                teklifsizYonetimOnayi = false,
                yonetimOnayKilitli = false,
                redGerekcesi = "",
                onaylananTeklifId = null,
                yonetimOnerilenTeklifId = null,
                teklifDuzeltmeNotu = "",
                yonetimOnaylayanAd = "",
                siparisNo = "",
                firmaSiparisNolari = emptyMap(),
                kalemler = parsed.map { it.copy(onaylananTeklifId = null) }
            )
        }

        if (yenidenGonder) {
            runCatching { bildirimler?.talepBildirimleriniSil(talepId) }
            runCatching {
                bildirimler?.talepBildirimleriToplu(
                    BildirimTipleri.YONETIME_GONDERILDI,
                    result,
                    user,
                    BildirimRolPolitikasi.yonetimeGonderildiHedefleri()
                )
            }
        }
        return result
    }

    suspend fun applyDetailMutation(
        talepId: String,
        user: UserProfile,
        mutation: PurchaseRequestDetailMutation,
        action: PurchaseRequestDetailAction,
        note: String? = null
    ): TalepItem {
        if (!KullaniciRolleri.canManagementDecide(user.role))
            throw IllegalStateException("Yönetim yetkisi gerekli")

        // Tek firma onayı sipariş no üretimi için kalemBazliOnayla yolundan geçmeli.
        if (action == PurchaseRequestDetailAction.APPROVE_QUOTE) {
            val qid = mutation.approvedQuoteId?.takeIf { it.isNotBlank() }
                ?: throw IllegalArgumentException("Onaylanacak teklif seçilmedi")
            return yonetimTeklifOnayla(talepId, user, qid)
        }

        val result = mutateTalep(talepId) { talep ->
            talep.applyDetailMutationFields(mutation, user)
        }

        when (action) {
            PurchaseRequestDetailAction.DIRECT_APPROVE -> {
                val hedefler = OnayBildirimYardimcisi.onaylandiHedefleri(result.olusturanUid, user.role)
                bildirimler?.talepBildirimleriToplu(
                    BildirimTipleri.ONAYLANDI,
                    result,
                    user,
                    hedefler,
                    onaylayanRol = user.role
                )
            }
            PurchaseRequestDetailAction.APPROVE_QUOTE -> {
                // Yukarıda yonetimTeklifOnayla ile işlendi
            }
            PurchaseRequestDetailAction.START_QUOTE_PROCESS -> {
                bildirimler?.talepBildirimleri(
                    BildirimTipleri.TEKLIF_ISTENDI, result, user, hedefRol = KullaniciRolleri.SATINALMA
                )
                if (result.olusturanUid.isNotBlank()) {
                    bildirimler?.talepBildirimleri(
                        BildirimTipleri.TEKLIF_ISTENDI,
                        result,
                        user,
                        hedefUid = result.olusturanUid,
                        ek = OnayBildirimYardimcisi.teklifIstemeBildirimEk(user.role)
                    )
                }
            }
            PurchaseRequestDetailAction.REJECT_REQUEST,
            PurchaseRequestDetailAction.REJECT_ENTIRE_REQUEST ->
                bildirimler?.talepBildirimleriToplu(
                    BildirimTipleri.REDDEDILDI,
                    result,
                    user,
                    BildirimRolPolitikasi.reddedildiHedefleri(result.olusturanUid, user.uid),
                    ek = note.orEmpty()
                )
            PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION ->
                bildirimler?.talepBildirimleri(
                    BildirimTipleri.TEKLIF_DUZELTME_ISTENDI,
                    result,
                    user,
                    hedefRol = KullaniciRolleri.SATINALMA,
                    ek = result.teklifDuzeltmeNotu
                )
        }

        return result
    }

    private fun TalepItem.applyDetailMutationFields(
        mutation: PurchaseRequestDetailMutation,
        user: UserProfile
    ): TalepItem {
        var updated = copy(
            status = mutation.newStatus,
            priority = resolvedEnterprisePriority(),
            guncellemeUtc = mutation.updatedAtUtcMs
        )

        mutation.newLegacyDurum?.let { updated = updated.copy(durum = it) }

        if (mutation.teklifsizYonetimOnayi) {
            updated = updated.copy(teklifsizYonetimOnayi = true)
        }

        updated = updated.copy(yonetimOnayKilitli = mutation.yonetimOnayKilitli)

        mutation.rejectionReason?.let { updated = updated.copy(redGerekcesi = it) }
        mutation.quoteCorrectionNote?.let { updated = updated.copy(teklifDuzeltmeNotu = it) }

        if (mutation.clearApprovedQuote) {
            updated = updated.copy(onaylananTeklifId = null)
        }

        if (mutation.clearLineItemApprovals) {
            updated = updated.copy(kalemler = kalemler.map { KalemFirmaAtamaYardimcisi.temizle(it) })
        }

        mutation.approvedQuoteId?.let { qid ->
            updated = updated.copy(
                onaylananTeklifId = qid,
                yonetimOnerilenTeklifId = qid,
                kalemler = if (mutation.applyQuoteToAllLineItems) {
                    kalemler.map { KalemFirmaAtamaYardimcisi.tekFirmayaAta(it, qid) }
                } else kalemler,
                teklifler = teklifler.map { it.copy(onaylandi = it.id == qid) }
            )
        }

        if (mutation.newStatus == ProcurementStatus.APPROVED) {
            updated = updated.withYonetimOnayKaydi(user)
        }

        if (mutation.newStatus == ProcurementStatus.QUOTE_REQUESTED) {
            updated = updated.copy(teklifsizYonetimOnayi = false, yonetimOnayKilitli = false)
        }

        return updated
    }

    private fun card(title: String, value: Any, subtitle: String, route: String) =
        DashboardCard(title, value.toString(), subtitle, route)

    private fun onayZamani(): String =
        SimpleDateFormat("dd.MM.yyyy HH:mm", Locale("tr", "TR")).format(Date())

    private fun TalepItem.withYonetimOnayKaydi(user: UserProfile): TalepItem = copy(
        yonetimOnaylayanUid = user.uid,
        yonetimOnaylayanAd = user.fullName,
        yonetimOnaylayanEposta = user.email,
        yonetimOnayTarihi = onayZamani()
    )
}
