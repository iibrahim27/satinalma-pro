package com.satinalmapro.android.services

import android.content.Context
import android.util.Log
import android.widget.Toast
import com.google.firebase.FirebaseApp
import com.google.firebase.messaging.FirebaseMessaging
import com.satinalmapro.android.core.saas.TenantFcmTopics
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.shared.model.UserRole

/**
 * Rol topic aboneliği — Firestore SDK kullanmaz (google-auth/gRPC native çökme riski).
 * Rol, REST oturumundan / profil cache'den gelir.
 */
class FcmSubscriptionHelper(private val context: Context) {

    private val prefs = context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    private fun firebaseReady(): Boolean =
        runCatching { FirebaseApp.getApps(context.applicationContext).isNotEmpty() }.getOrDefault(false)

    fun syncRoleTopicSubscription(roleRaw: String?, showSuccessToast: Boolean = true) {
        if (!firebaseReady()) {
            Log.w(TAG, "FCM abonelik atlandı: FirebaseApp hazır değil")
            return
        }

        val tenantId = TenantSession.tenantId()?.takeIf { it.isNotBlank() }
        if (tenantId.isNullOrBlank()) {
            Log.w(TAG, "FCM abonelik atlandı: tenantId yok")
            return
        }

        if (roleRaw.isNullOrBlank()) {
            Log.e(TAG, "FCM abonelik atlandı: rol boş")
            return
        }

        val userRole = UserRole.fromFirestore(roleRaw)
        if (userRole == null) {
            Log.e(TAG, "Geçersiz veya desteklenmeyen rol: '$roleRaw'")
            return
        }

        val topic = TenantFcmTopics.forRole(userRole, tenantId)
        Log.i(TAG, "Rol: ${userRole.displayName} → topic=$topic")
        subscribeToRoleTopic(tenantId, topic, userRole, showSuccessToast)
    }

    private fun messagingOrNull(): FirebaseMessaging? =
        runCatching { FirebaseMessaging.getInstance() }.getOrNull()

    private fun subscribeToRoleTopic(
        tenantId: String,
        topic: String,
        userRole: UserRole,
        showSuccessToast: Boolean
    ) {
        val messaging = messagingOrNull() ?: return
        unsubscribeOtherRoleTopics(tenantId, keepTopic = topic) {
            messaging.subscribeToTopic(topic)
                .addOnSuccessListener {
                    prefs.edit().putString(KEY_LAST_TOPIC, topic).putString(KEY_LAST_TENANT, tenantId).apply()
                    Log.i(TAG, "FCM topic aboneliği başarılı: topic=$topic rol=${userRole.displayName}")
                    if (showSuccessToast) {
                        val message = "Bildirim kanalına başarıyla bağlanıldı: ${userRole.displayName}"
                        // Toast yalnızca ana thread'de güvenli
                        android.os.Handler(android.os.Looper.getMainLooper()).post {
                            Toast.makeText(context.applicationContext, message, Toast.LENGTH_SHORT).show()
                        }
                    }
                }
                .addOnFailureListener { error ->
                    Log.e(TAG, "FCM topic aboneliği başarısız: topic=$topic", error)
                }
        }
    }

    fun unsubscribeAllRoleTopics() {
        if (!firebaseReady()) {
            prefs.edit().remove(KEY_LAST_TOPIC).remove(KEY_LAST_TENANT).apply()
            return
        }

        val messaging = messagingOrNull()
        val topics = mutableSetOf<String>()
        prefs.getString(KEY_LAST_TOPIC, null)?.let { topics.add(it) }
        prefs.getString(KEY_LAST_TENANT, null)?.let { tenantId ->
            topics.addAll(TenantFcmTopics.allForTenant(tenantId))
        }

        if (messaging == null || topics.isEmpty()) {
            prefs.edit().remove(KEY_LAST_TOPIC).remove(KEY_LAST_TENANT).apply()
            return
        }

        var remaining = topics.size
        topics.forEach { topic ->
            messaging.unsubscribeFromTopic(topic)
                .addOnCompleteListener { task ->
                    if (task.isSuccessful) {
                        Log.i(TAG, "FCM topic aboneliği kaldırıldı: $topic")
                    } else {
                        Log.w(TAG, "FCM topic kaldırma hatası: $topic", task.exception)
                    }
                    remaining -= 1
                    if (remaining == 0) {
                        prefs.edit().remove(KEY_LAST_TOPIC).remove(KEY_LAST_TENANT).apply()
                    }
                }
        }
    }

    private fun unsubscribeOtherRoleTopics(tenantId: String, keepTopic: String, onComplete: () -> Unit) {
        val messaging = messagingOrNull()
        val topicsToRemove = TenantFcmTopics.allForTenant(tenantId).filter { it != keepTopic }
        if (messaging == null || topicsToRemove.isEmpty()) {
            onComplete()
            return
        }

        var remaining = topicsToRemove.size
        topicsToRemove.forEach { topic ->
            messaging.unsubscribeFromTopic(topic)
                .addOnCompleteListener {
                    remaining -= 1
                    if (remaining == 0) {
                        onComplete()
                    }
                }
        }
    }

    companion object {
        private const val TAG = "FcmSubscriptionHelper"
        private const val PREFS_NAME = "fcm_subscription"
        private const val KEY_LAST_TOPIC = "last_role_topic"
        private const val KEY_LAST_TENANT = "last_tenant_id"
    }
}
