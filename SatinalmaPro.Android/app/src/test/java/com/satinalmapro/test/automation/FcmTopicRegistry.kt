package com.satinalmapro.test.automation

import com.satinalmapro.android.core.roles.KullaniciRolleri
import com.satinalmapro.shared.model.UserRole

object FcmTopicRegistry {
    private val subscriptions = linkedSetOf<String>()
    private val pushes = mutableListOf<FcmPushRecord>()

    data class FcmPushRecord(
        val topic: String,
        val tip: String,
        val talepId: String?,
        val mesaj: String
    )

    fun topicForRole(rol: String?): String {
        val userRole = UserRole.fromFirestore(rol) ?: return "/topics/${rol?.lowercase().orEmpty()}"
        return "/topics/${userRole.fcmTopic}"
    }

    fun openSession(rol: String?, displayName: String) {
        val topic = topicForRole(rol)
        subscriptions.removeAll { it != topic }
        subscriptions.add(topic)
        log("FCM oturum → $displayName ($rol) abone: $topic")
    }

    fun pushTopic(topic: String, tip: String, talepId: String? = null, mesaj: String = tip) {
        pushes += FcmPushRecord(topic, tip, talepId, mesaj)
        log("FCM push → $topic | $tip | talep=$talepId")
    }

    fun pushRole(hedefRol: String?, tip: String, talepId: String? = null, mesaj: String = tip) =
        pushTopic(topicForRole(hedefRol), tip, talepId, mesaj)

    fun pushUid(hedefUid: String, tip: String, talepId: String? = null, mesaj: String = tip) =
        pushTopic("/topics/user-$hedefUid", tip, talepId, mesaj)

    fun isSubscribed(topic: String): Boolean = subscriptions.contains(topic)

    fun pushDelivered(topic: String, tip: String, talepId: String? = null): Boolean =
        pushes.any {
            it.topic.equals(topic, ignoreCase = true) &&
                it.tip == tip &&
                (talepId == null || it.talepId == talepId)
        }

    fun clear() {
        subscriptions.clear()
        pushes.clear()
    }

    private fun log(msg: String) {
        println("[PurchaseModuleAutomationTest] $msg")
    }
}

object FirestoreSecuritySimulator {
    enum class OperationResult { ALLOWED, PERMISSION_DENIED }

    fun createStockMovement(rol: String?): OperationResult =
        if (canWriteStock(rol)) OperationResult.ALLOWED else OperationResult.PERMISSION_DENIED

    fun readProcurementQuotes(rol: String?): OperationResult =
        if (canReadProcurementQuotes(rol)) OperationResult.ALLOWED else OperationResult.PERMISSION_DENIED

    private fun canWriteStock(rol: String?): Boolean {
        val r = KullaniciRolleri.normalize(rol)
        return r == KullaniciRolleri.ADMIN ||
            r == KullaniciRolleri.SATINALMA ||
            r == KullaniciRolleri.DEPO
    }

    private fun canReadProcurementQuotes(rol: String?): Boolean {
        val r = KullaniciRolleri.normalize(rol)
        return r == KullaniciRolleri.ADMIN ||
            r == KullaniciRolleri.YONETIM ||
            r == KullaniciRolleri.SATINALMA
    }
}
