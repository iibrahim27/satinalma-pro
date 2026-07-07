package com.satinalmapro.test.automation

import com.satinalmapro.android.core.model.TalepItem
import com.satinalmapro.android.core.model.TalepKalem
import com.satinalmapro.android.core.model.TeklifFiyat
import com.satinalmapro.android.core.model.TeklifItem
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.shared.filter.ProcurementPriority
import com.satinalmapro.shared.filter.ProcurementStatus
import com.satinalmapro.shared.filter.ProcurementTab
import com.satinalmapro.shared.filter.TabFilterManager
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailMutation
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailPresenter
import com.satinalmapro.shared.filter.resolvedEnterpriseStatus
import java.util.UUID

data class TestUser(
    val uid: String,
    val adSoyad: String,
    val rol: String,
    val saha: String? = null
)

data class StockMovementRecord(
    val id: String,
    val type: String,
    val materialName: String,
    val quantity: Double,
    val requestId: String,
    val createdByUid: String
)

data class EnterpriseQuoteRecord(
    val quoteId: String,
    val firmName: String,
    val linePrices: List<Pair<String, Double>>
)

class AutomationTestEnvironment {
    val fcm = FcmTopicRegistry
    val talepler = mutableListOf<TalepItem>()
    val stocks = mutableMapOf<String, Double>()
    val stockMovements = mutableListOf<StockMovementRecord>()
    val quotes = mutableMapOf<String, MutableList<EnterpriseQuoteRecord>>()

    var aktifKullanici: TestUser? = null
    private var talepSira = 0
    private var siparisSira = 0

    companion object {
        val SAHA = TestUser("e2e-saha-uid", "E2E Saha", KullaniciRolleri.SAHA, "Test Şantiye")
        val SEF = TestUser("e2e-sef-uid", "E2E Şef", KullaniciRolleri.SEF, "Test Şantiye")
        val YONETIM = TestUser("e2e-yonetim-uid", "E2E Yönetim", KullaniciRolleri.YONETIM)
        val SATINALMA = TestUser("e2e-satinalma-uid", "E2E Satınalma", KullaniciRolleri.SATINALMA)
        val DEPO = TestUser("e2e-depo-uid", "E2E Depo", KullaniciRolleri.DEPO)
        val ATOLYE = TestUser("e2e-atolye-uid", "E2E Atölye", KullaniciRolleri.ATOLYE)
        const val TEST_ETIKETI = "[E2E-TEST]"
    }

    fun oturumAc(user: TestUser) {
        aktifKullanici = user
        fcm.openSession(user.rol, user.adSoyad)
    }

    fun guncelTalep(id: String): TalepItem = talepler.first { it.id == id }

    fun kaydet(talep: TalepItem) {
        talepler.removeAll { it.id == talep.id }
        talepler.add(0, talep.copy(guncellemeUtc = System.currentTimeMillis()))
    }

    fun routeTalepleri(route: String, user: TestUser): List<TalepItem> =
        talepler.filter { matchesRoute(route, it, user) }

    fun sekmeGorunur(tab: ProcurementTab, user: TestUser): Boolean =
        TabFilterManager.isTabVisible(user.rol, tab)

    fun talepOlustur(
        olusturan: TestUser,
        oncelik: String = ProcurementPriority.NORMAL,
        malzeme: String = "E2E Otomasyon Malzeme",
        miktar: Double = 10.0
    ): TalepItem {
        oturumAc(olusturan)
        val kalem = TalepKalem(
            id = UUID.randomUUID().toString(),
            malzeme = malzeme,
            miktar = miktar,
            birim = "Adet",
            aciklama = TEST_ETIKETI
        )
        val talepTuru = if (oncelik == ProcurementPriority.URGENT) "Acil" else "Normal"
        val talep = TalepItem(
            id = UUID.randomUUID().toString(),
            talepNo = yeniTalepNo(),
            tarih = "07.07.2026",
            talepEden = olusturan.adSoyad,
            olusturanUid = olusturan.uid,
            olusturanRol = olusturan.rol,
            santiyeAdi = olusturan.saha ?: "Test Şantiye",
            talepAciklamasi = TEST_ETIKETI,
            talepTuru = talepTuru,
            priority = oncelik,
            status = ProcurementStatus.SUBMITTED,
            durum = "İmza Sürecinde",
            kalemler = listOf(kalem)
        )
        kaydet(talep)
        fcm.pushRole(KullaniciRolleri.YONETIM, "yonetime_gonderildi", talep.id)
        return talep
    }

    fun detayAksiyonUygula(
        talep: TalepItem,
        action: PurchaseRequestDetailAction,
        user: TestUser,
        quoteId: String? = null,
        not: String? = null
    ): TalepItem {
        oturumAc(user)
        val mutation = PurchaseRequestDetailPresenter.createMutation(action, talep, user.rol, quoteId, not)
            ?: throw IllegalStateException("Aksiyon uygulanamaz: $action")
        val guncel = mutasyonUygula(talep, mutation, user)
        kaydet(guncel)
        aksiyonSonrasiBildirim(guncel, action)
        return guncel
    }

    fun enterpriseTeklifEkle(
        talep: TalepItem,
        firma: String,
        birimFiyat: Double,
        user: TestUser
    ): TeklifItem {
        oturumAc(user)
        val teklif = TeklifItem(
            id = UUID.randomUUID().toString(),
            firmaAdi = firma,
            fiyatlar = talep.kalemler.map { k ->
                val toplam = birimFiyat * k.miktar
                val kdv = toplam * 0.20
                TeklifFiyat(
                    kalemId = k.id,
                    birimFiyat = birimFiyat,
                    toplamTutar = toplam,
                    kdvTutari = kdv,
                    toplamKdvDahil = toplam + kdv
                )
            }
        )
        val guncelTeklifler = talep.teklifler + teklif
        val quoteDoc = EnterpriseQuoteRecord(
            quoteId = teklif.id,
            firmName = firma,
            linePrices = talep.kalemler.map { it.id to birimFiyat }
        )
        quotes.getOrPut(talep.id) { mutableListOf() }.add(quoteDoc)

        val guncel = talep.copy(
            teklifler = guncelTeklifler,
            status = ProcurementStatus.QUOTE_ENTRY,
            durum = "Teklif Girişi"
        )
        kaydet(guncel)
        return teklif
    }

    fun yonetimeTeklifGonder(talep: TalepItem, user: TestUser): TalepItem {
        oturumAc(user)
        require(talep.teklifler.isNotEmpty()) { "Teklif yok" }
        val guncel = talep.copy(
            status = ProcurementStatus.MANAGEMENT_QUOTE_REVIEW,
            durum = "Yönetim Onayında"
        )
        kaydet(guncel)
        fcm.pushRole(KullaniciRolleri.YONETIM, "teklif_onayda", guncel.id)
        return guncel
    }

    fun siparisOlustur(talep: TalepItem, user: TestUser): TalepItem {
        oturumAc(user)
        val guncel = talep.copy(
            status = ProcurementStatus.ORDERED,
            durum = "Sipariş Oluşturuldu",
            siparisNo = yeniSiparisNo()
        )
        kaydet(guncel)
        fcm.pushRole(KullaniciRolleri.DEPO, "siparis_olusturuldu", guncel.id)
        fcm.pushUid(guncel.olusturanUid, "siparis_olusturuldu", guncel.id)
        return guncel
    }

    fun depoMalKabulTamamla(talep: TalepItem): TalepItem {
        oturumAc(DEPO)
        talep.kalemler.forEach { kalem ->
            val onceki = stocks[kalem.malzeme] ?: 0.0
            stocks[kalem.malzeme] = onceki + kalem.miktar
            stockMovements += StockMovementRecord(
                id = UUID.randomUUID().toString(),
                type = "IN",
                materialName = kalem.malzeme,
                quantity = kalem.miktar,
                requestId = talep.id,
                createdByUid = DEPO.uid
            )
        }
        val guncelKalemler = talep.kalemler.map {
            it.copy(kabulEdilenMiktar = it.miktar, siparisTamamlandi = true)
        }
        val guncel = talep.copy(
            kalemler = guncelKalemler,
            status = ProcurementStatus.COMPLETED
        )
        kaydet(guncel)
        fcm.pushRole(KullaniciRolleri.DEPO, "mal_kabul_edildi", guncel.id)
        fcm.pushUid(guncel.olusturanUid, "mal_kabul_edildi", guncel.id)
        return guncel
    }

    fun stockMovementYazmayiDene(user: TestUser): FirestoreSecuritySimulator.OperationResult {
        oturumAc(user)
        val sonuc = FirestoreSecuritySimulator.createStockMovement(user.rol)
        if (sonuc == FirestoreSecuritySimulator.OperationResult.ALLOWED) {
            stockMovements += StockMovementRecord(
                UUID.randomUUID().toString(), "IN", "Yetkisiz Test", 1.0, "", user.uid
            )
        }
        return sonuc
    }

    fun quotesOkumayiDene(user: TestUser, requestId: String): FirestoreSecuritySimulator.OperationResult =
        FirestoreSecuritySimulator.readProcurementQuotes(user.rol)

    fun temizle() {
        talepler.clear()
        stocks.clear()
        stockMovements.clear()
        quotes.clear()
        aktifKullanici = null
        talepSira = 0
        siparisSira = 0
        fcm.clear()
    }

    private fun mutasyonUygula(
        talep: TalepItem,
        mutation: PurchaseRequestDetailMutation,
        user: TestUser
    ): TalepItem {
        var guncel = talep.copy(
            status = mutation.newStatus,
            priority = talep.priority.ifBlank { ProcurementPriority.fromRequestType(talep.talepTuru) },
            durum = mutation.newLegacyDurum ?: talep.durum,
            teklifsizYonetimOnayi = mutation.teklifsizYonetimOnayi,
            yonetimOnayKilitli = mutation.yonetimOnayKilitli,
            redGerekcesi = mutation.rejectionReason ?: talep.redGerekcesi,
            teklifDuzeltmeNotu = mutation.quoteCorrectionNote ?: talep.teklifDuzeltmeNotu,
            onaylananTeklifId = if (mutation.clearApprovedQuote) null else mutation.approvedQuoteId ?: talep.onaylananTeklifId
        )

        if (mutation.clearLineItemApprovals) {
            guncel = guncel.copy(
                kalemler = guncel.kalemler.map { it.copy(onaylananTeklifId = null) }
            )
        }

        if (!mutation.approvedQuoteId.isNullOrBlank() && mutation.applyQuoteToAllLineItems) {
            guncel = guncel.copy(
                kalemler = guncel.kalemler.map { it.copy(onaylananTeklifId = mutation.approvedQuoteId) },
                teklifler = guncel.teklifler.map { it.copy(onaylandi = it.id == mutation.approvedQuoteId) },
                yonetimOnerilenTeklifId = mutation.approvedQuoteId
            )
        }

        if (mutation.newStatus == ProcurementStatus.APPROVED) {
            guncel = guncel.copy(
                yonetimOnaylayanUid = user.uid,
                yonetimOnaylayanAd = user.adSoyad
            )
        }

        return guncel
    }

    private fun aksiyonSonrasiBildirim(talep: TalepItem, action: PurchaseRequestDetailAction) {
        when (action) {
            PurchaseRequestDetailAction.DIRECT_APPROVE,
            PurchaseRequestDetailAction.APPROVE_QUOTE -> {
                fcm.pushRole(KullaniciRolleri.SATINALMA, "onaylandi", talep.id)
                fcm.pushUid(talep.olusturanUid, "onaylandi", talep.id)
            }
            PurchaseRequestDetailAction.START_QUOTE_PROCESS -> {
                fcm.pushRole(KullaniciRolleri.SATINALMA, "teklif_istendi", talep.id)
                fcm.pushUid(talep.olusturanUid, "teklif_istendi", talep.id)
            }
            PurchaseRequestDetailAction.REJECT_REQUEST,
            PurchaseRequestDetailAction.REJECT_ENTIRE_REQUEST -> {
                fcm.pushRole(KullaniciRolleri.SATINALMA, "reddedildi", talep.id)
                fcm.pushUid(talep.olusturanUid, "reddedildi", talep.id)
            }
            PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION ->
                fcm.pushRole(KullaniciRolleri.SATINALMA, "teklif_duzeltme_istendi", talep.id)
            else -> Unit
        }
    }

    private fun matchesRoute(route: String, talep: TalepItem, user: TestUser): Boolean {
        val status = talep.resolvedEnterpriseStatus()
        val scoped = TabFilterManager.requiresRequesterScope(user.rol) &&
            talep.olusturanUid != user.uid
        if (scoped) return false

        return when (route) {
            "yonetim-gelen-talepler" -> status == ProcurementStatus.SUBMITTED
            "satinalma-teklif-istenen" -> status == ProcurementStatus.QUOTE_REQUESTED
            "satinalma-teklif-girilen" -> status == ProcurementStatus.QUOTE_ENTRY
            "yonetim-teklif-girilen" -> status == ProcurementStatus.MANAGEMENT_QUOTE_REVIEW
            "satinalma-siparis" -> status == ProcurementStatus.ORDERED &&
                talep.kalemler.any { !it.siparisTamamlandi }
            "satinalma-talepler" -> status in listOf(ProcurementStatus.DRAFT, ProcurementStatus.SUBMITTED) &&
                (!TabFilterManager.requiresRequesterScope(user.rol) || talep.olusturanUid == user.uid)
            else -> false
        }
    }

    private fun yeniTalepNo(): String {
        talepSira++
        return "TLP-2026-${talepSira.toString().padStart(4, '0')}"
    }

    private fun yeniSiparisNo(): String {
        siparisSira++
        return "SIP-2026-${siparisSira.toString().padStart(4, '0')}"
    }
}
