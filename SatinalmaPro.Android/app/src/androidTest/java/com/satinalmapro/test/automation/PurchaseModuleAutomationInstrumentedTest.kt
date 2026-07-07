package com.satinalmapro.test.automation

import android.util.Log
import androidx.test.ext.junit.runners.AndroidJUnit4
import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.shared.filter.ProcurementPriority
import com.satinalmapro.shared.filter.ProcurementStatus
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailAction
import com.satinalmapro.shared.filter.detail.PurchaseRequestDetailPresenter
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test
import org.junit.runner.RunWith

/**
 * Instrumentation test — Logcat çıktısı:
 * `adb logcat -s PurchaseModuleAutomationTest`
 *
 * Çalıştırma: `./gradlew :app:connectedDebugAndroidTest`
 */
@RunWith(AndroidJUnit4::class)
class PurchaseModuleAutomationInstrumentedTest {

    private lateinit var ortam: AutomationTestEnvironment

    @Before
    fun setup() {
        ortam = AutomationTestEnvironment()
    }

    @Test
    fun runFullPurchaseModuleAutomation() {
        log("PurchaseModuleAutomationTest başlıyor (Android/Logcat)")

        runScenario1()
        ortam.temizle()
        runScenario2()
        ortam.temizle()
        runSecurityTests()
        ortam.temizle()

        log("PurchaseModuleAutomationTest tamamlandı — tüm senaryolar geçti")
    }

    private fun runScenario1() {
        log("--- SENARYO 1: Normal talep akışı ---")

        ortam.oturumAc(AutomationTestEnvironment.SEF)
        val sefTopic = FcmTopicRegistry.topicForRole(KullaniciRolleri.SEF)
        assertTrue(FcmTopicRegistry.isSubscribed(sefTopic))
        log("OK: Şef $sefTopic aboneliği")

        var talep = ortam.talepOlustur(AutomationTestEnvironment.SEF)
        assertEquals(ProcurementStatus.SUBMITTED, talep.status)
        log("OK: Talep submitted — ${talep.talepNo}")

        ortam.oturumAc(AutomationTestEnvironment.YONETIM)
        val ui = PurchaseRequestDetailPresenter.buildUiState(talep, AutomationTestEnvironment.YONETIM.rol)
        assertTrue(ui.isVisible(PurchaseRequestDetailAction.START_QUOTE_PROCESS))
        assertFalse(ui.isVisible(PurchaseRequestDetailAction.DIRECT_APPROVE))

        talep = ortam.detayAksiyonUygula(
            talep, PurchaseRequestDetailAction.START_QUOTE_PROCESS, AutomationTestEnvironment.YONETIM
        )
        log("OK: quote_requested + FCM satinalma")

        ortam.oturumAc(AutomationTestEnvironment.SATINALMA)
        ortam.enterpriseTeklifEkle(talep, "Firma A", 100.0, AutomationTestEnvironment.SATINALMA)
        val teklifB = ortam.enterpriseTeklifEkle(
            ortam.guncelTalep(talep.id), "Firma B", 90.0, AutomationTestEnvironment.SATINALMA
        )
        talep = ortam.yonetimeTeklifGonder(ortam.guncelTalep(talep.id), AutomationTestEnvironment.SATINALMA)
        log("OK: management_quote_review + 2 quote")

        talep = ortam.detayAksiyonUygula(
            talep, PurchaseRequestDetailAction.SEND_QUOTES_FOR_REVISION,
            AutomationTestEnvironment.YONETIM, not = "Revize"
        )
        log("OK: comparison / revize")

        talep = ortam.yonetimeTeklifGonder(ortam.guncelTalep(talep.id), AutomationTestEnvironment.SATINALMA)
        talep = ortam.detayAksiyonUygula(
            talep, PurchaseRequestDetailAction.APPROVE_QUOTE,
            AutomationTestEnvironment.YONETIM, quoteId = teklifB.id
        )
        talep = ortam.siparisOlustur(talep, AutomationTestEnvironment.SATINALMA)
        log("OK: approved + ordered")

        talep = ortam.depoMalKabulTamamla(talep)
        assertEquals(ProcurementStatus.COMPLETED, talep.status)
        assertTrue(ortam.stockMovements.any { it.type == "IN" })
        log("OK: depo mal kabul + stock IN + completed")
    }

    private fun runScenario2() {
        log("--- SENARYO 2: Acil talep ---")

        var talep = ortam.talepOlustur(AutomationTestEnvironment.SAHA, ProcurementPriority.URGENT)
        ortam.oturumAc(AutomationTestEnvironment.YONETIM)
        val ui = PurchaseRequestDetailPresenter.buildUiState(talep, AutomationTestEnvironment.YONETIM.rol)

        assertFalse("ASSERT: Teklif İste gizli", ui.isVisible(PurchaseRequestDetailAction.START_QUOTE_PROCESS))
        assertTrue(ui.isVisible(PurchaseRequestDetailAction.DIRECT_APPROVE))

        talep = ortam.detayAksiyonUygula(
            talep, PurchaseRequestDetailAction.DIRECT_APPROVE, AutomationTestEnvironment.YONETIM
        )
        assertEquals(ProcurementStatus.APPROVED, talep.status)
        log("OK: acil direkt onay → approved")
    }

    private fun runSecurityTests() {
        log("--- Güvenlik negatif testler ---")

        assertEquals(
            FirestoreSecuritySimulator.OperationResult.PERMISSION_DENIED,
            ortam.stockMovementYazmayiDene(AutomationTestEnvironment.ATOLYE)
        )
        log("OK: atolye stock_movements Permission Denied")

        val talep = ortam.talepOlustur(AutomationTestEnvironment.SEF)
        ortam.enterpriseTeklifEkle(talep, "X", 1.0, AutomationTestEnvironment.SATINALMA)
        assertEquals(
            FirestoreSecuritySimulator.OperationResult.PERMISSION_DENIED,
            ortam.quotesOkumayiDene(AutomationTestEnvironment.DEPO, talep.id)
        )
        log("OK: depo quotes okuma engellendi")
    }

    private fun log(msg: String) {
        Log.i(TAG, msg)
    }

    companion object {
        private const val TAG = "PurchaseModuleAutomationTest"
    }
}
