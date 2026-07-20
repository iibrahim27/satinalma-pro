package com.satinalmapro.test.automation

import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.shared.filter.ProcurementPriority
import com.satinalmapro.shared.filter.ProcurementStatus
import com.satinalmapro.shared.filter.ProcurementTab
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailPresenter
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test

/**
 * JVM unit test — Android Studio / Gradle ile çalıştırılır:
 * `./gradlew :app:testDebugUnitTest`
 */
class PurchaseModuleAutomationTest {

    private lateinit var ortam: AutomationTestEnvironment

    @Before
    fun setup() {
        ortam = AutomationTestEnvironment()
    }

    @Test
    fun senaryo1_normalTalepAkisi_teklifVeRevizeDongusu() {
        log("=== SENARYO 1: NORMAL TALEP AKIŞI ===")

        // 1 — Şef
        ortam.oturumAc(AutomationTestEnvironment.SEF)
        val sefTopic = FcmTopicRegistry.topicForRole(KullaniciRolleri.SEF)
        assertTrue("Şef $sefTopic aboneliği", FcmTopicRegistry.isSubscribed(sefTopic))

        val talep = ortam.talepOlustur(AutomationTestEnvironment.SEF, ProcurementPriority.NORMAL)
        assertEquals(ProcurementStatus.SUBMITTED, talep.status)

        val baska = ortam.talepOlustur(AutomationTestEnvironment.SAHA, ProcurementPriority.NORMAL, "Başka Malzeme")
        val sefGorur = ortam.routeTalepleri("satinalma-talepler", AutomationTestEnvironment.SEF)
        assertTrue(sefGorur.any { it.id == talep.id })
        assertFalse(sefGorur.any { it.id == baska.id })

        // 2 — Yönetim
        ortam.oturumAc(AutomationTestEnvironment.YONETIM)
        assertTrue(ortam.routeTalepleri("yonetim-gelen-talepler", AutomationTestEnvironment.YONETIM).any { it.id == talep.id })

        val ui = PurchaseRequestDetailPresenter.buildUiState(talep, AutomationTestEnvironment.YONETIM.rol)
        assertTrue(ui.isVisible(PurchaseRequestDetailAction.START_QUOTE_PROCESS))
        assertFalse(ui.isVisible(PurchaseRequestDetailAction.DIRECT_APPROVE))

        var guncel = ortam.detayAksiyonUygula(
            talep, PurchaseRequestDetailAction.START_QUOTE_PROCESS, AutomationTestEnvironment.YONETIM
        )
        assertEquals(ProcurementStatus.QUOTE_REQUESTED, guncel.status)

        val satTopic = FcmTopicRegistry.topicForRole(KullaniciRolleri.SATINALMA)
        assertTrue(FcmTopicRegistry.pushDelivered(satTopic, "teklif_istendi", guncel.id))

        // 3 — Satınalma
        ortam.oturumAc(AutomationTestEnvironment.SATINALMA)
        assertTrue(ortam.routeTalepleri("satinalma-teklif-istenen", AutomationTestEnvironment.SATINALMA).any { it.id == guncel.id })

        ortam.enterpriseTeklifEkle(guncel, "E2E Tedarikçi A", 120.0, AutomationTestEnvironment.SATINALMA)
        guncel = ortam.guncelTalep(guncel.id)
        assertTrue(
            ortam.routeTalepleri("satinalma-teklif-girilen", AutomationTestEnvironment.SATINALMA).any { it.id == guncel.id }
        )

        val teklifB = ortam.enterpriseTeklifEkle(guncel, "E2E Tedarikçi B", 95.0, AutomationTestEnvironment.SATINALMA)
        guncel = ortam.guncelTalep(guncel.id)

        assertEquals(2, ortam.quotes[guncel.id]?.size)
        assertTrue(ortam.quotes[guncel.id]!!.all { it.linePrices.isNotEmpty() })

        guncel = ortam.yonetimeTeklifGonder(guncel, AutomationTestEnvironment.SATINALMA)
        assertEquals(ProcurementStatus.MANAGEMENT_QUOTE_REVIEW, guncel.status)

        val yonTopic = FcmTopicRegistry.topicForRole(KullaniciRolleri.YONETIM)
        assertTrue(FcmTopicRegistry.pushDelivered(yonTopic, "teklif_onayda", guncel.id))

        // 4 — Revize
        guncel = ortam.detayAksiyonUygula(
            guncel,
            PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION,
            AutomationTestEnvironment.YONETIM,
            not = "Fiyatları güncelleyin"
        )
        assertEquals(ProcurementStatus.QUOTE_REQUESTED, guncel.status)
        assertTrue(FcmTopicRegistry.pushDelivered(satTopic, "teklif_duzeltme_istendi", guncel.id))

        // 5 — Güncelle ve yeniden gönder
        guncel = ortam.yonetimeTeklifGonder(ortam.guncelTalep(guncel.id), AutomationTestEnvironment.SATINALMA)
        assertEquals(ProcurementStatus.MANAGEMENT_QUOTE_REVIEW, guncel.status)

        // 6 — Onay + sipariş
        guncel = ortam.detayAksiyonUygula(
            guncel,
            PurchaseRequestDetailAction.APPROVE_QUOTE,
            AutomationTestEnvironment.YONETIM,
            quoteId = teklifB.id
        )
        assertEquals(ProcurementStatus.APPROVED, guncel.status)

        guncel = ortam.siparisOlustur(guncel, AutomationTestEnvironment.SATINALMA)
        assertEquals(ProcurementStatus.ORDERED, guncel.status)

        val depoTopic = FcmTopicRegistry.topicForRole(KullaniciRolleri.DEPO)
        assertTrue(FcmTopicRegistry.pushDelivered(depoTopic, "siparis_olusturuldu", guncel.id))

        // 7 — Depo mal kabul
        ortam.oturumAc(AutomationTestEnvironment.DEPO)
        assertTrue(ortam.sekmeGorunur(ProcurementTab.APPROVED_ORDERS, AutomationTestEnvironment.DEPO))

        val malzeme = guncel.kalemler.first().malzeme
        val oncekiStok = ortam.stocks[malzeme] ?: 0.0
        guncel = ortam.depoMalKabulTamamla(guncel)

        assertTrue(ortam.stockMovements.any { it.type == "IN" && it.requestId == guncel.id })
        assertTrue((ortam.stocks[malzeme] ?: 0.0) > oncekiStok)
        assertEquals(ProcurementStatus.COMPLETED, guncel.status)

        log("SENARYO 1 BAŞARILI")
        ortam.temizle()
    }

    @Test
    fun senaryo2_acilTalep_teklifButonuGizli_direktOnay() {
        log("=== SENARYO 2: ACİL TALEP AKIŞI ===")

        var talep = ortam.talepOlustur(AutomationTestEnvironment.SAHA, ProcurementPriority.URGENT, "Acil Demir", 5.0)
        assertEquals(ProcurementPriority.URGENT, talep.priority)

        ortam.oturumAc(AutomationTestEnvironment.YONETIM)
        val ui = PurchaseRequestDetailPresenter.buildUiState(talep, AutomationTestEnvironment.YONETIM.rol)

        assertFalse("Teklif Sürecini Başlat GİZLİ olmalı", ui.isVisible(PurchaseRequestDetailAction.START_QUOTE_PROCESS))
        assertTrue(ui.isVisible(PurchaseRequestDetailAction.DIRECT_APPROVE))
        assertTrue(ui.isVisible(PurchaseRequestDetailAction.REJECT_REQUEST))

        talep = ortam.detayAksiyonUygula(
            talep, PurchaseRequestDetailAction.DIRECT_APPROVE, AutomationTestEnvironment.YONETIM
        )
        assertEquals(ProcurementStatus.APPROVED, talep.status)
        assertTrue(talep.teklifsizYonetimOnayi)

        log("SENARYO 2 BAŞARILI")
        ortam.temizle()
    }

    @Test
    fun guvenlik_negatifTestler_atolyeVeDepo() {
        log("=== GÜVENLİK NEGATİF TESTLER ===")

        assertEquals(
            FirestoreSecuritySimulator.OperationResult.PERMISSION_DENIED,
            ortam.stockMovementYazmayiDene(AutomationTestEnvironment.ATOLYE)
        )

        val talep = ortam.talepOlustur(AutomationTestEnvironment.SEF)
        ortam.enterpriseTeklifEkle(talep, "Gizli", 50.0, AutomationTestEnvironment.SATINALMA)

        assertEquals(
            FirestoreSecuritySimulator.OperationResult.PERMISSION_DENIED,
            ortam.quotesOkumayiDene(AutomationTestEnvironment.DEPO, talep.id)
        )
        assertEquals(
            FirestoreSecuritySimulator.OperationResult.ALLOWED,
            ortam.quotesOkumayiDene(AutomationTestEnvironment.SATINALMA, talep.id)
        )

        assertFalse(ortam.sekmeGorunur(ProcurementTab.MANAGEMENT_APPROVAL, AutomationTestEnvironment.ATOLYE))
        assertTrue(ortam.sekmeGorunur(ProcurementTab.STOCK_STATUS, AutomationTestEnvironment.ATOLYE))

        log("GÜVENLİK TESTLERİ BAŞARILI")
        ortam.temizle()
    }

  private fun log(msg: String) {
        println("[PurchaseModuleAutomationTest] $msg")
    }
}
