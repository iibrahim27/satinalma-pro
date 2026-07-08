package com.satinalmapro.android.services

import android.content.Context
import android.util.Log
import android.widget.Toast
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.firestore.FirebaseFirestore
import com.google.firebase.messaging.FirebaseMessaging
import com.satinalmapro.android.core.saas.TenantFcmTopics
import com.satinalmapro.android.core.saas.TenantSession
import com.satinalmapro.shared.model.UserRole

/**
 * Giriş yapmış kullanıcının Firestore rolüne göre kiracıya özel FCM topic aboneliğini yönetir.
 */
class FcmSubscriptionHelper(private val context: Context) {

    private val auth: FirebaseAuth = FirebaseAuth.getInstance()
    private val firestore: FirebaseFirestore = FirebaseFirestore.getInstance()
    private val messaging: FirebaseMessaging = FirebaseMessaging.getInstance()
    private val prefs = context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    fun syncRoleTopicSubscription(showSuccessToast: Boolean = true) {
        val currentUser = auth.currentUser
        if (currentUser == null) {
            Log.w(TAG, "FCM abonelik atlandı: Firebase Auth oturumu yok")
            return
        }

        val tenantId = runCatching { TenantSession.requireTenantId() }.getOrNull()
        if (tenantId.isNullOrBlank()) {
            Log.w(TAG, "FCM abonelik atlandı: tenantId yok")
            return
        }

        val uid = currentUser.uid
        val userPath = "tenants/$tenantId/users/$uid"
        Log.d(TAG, "Rol okunuyor: $userPath")

        firestore.document(userPath)
            .get()
            .addOnSuccessListener { snapshot ->
                if (!snapshot.exists()) {
                    Log.e(TAG, "Kullanıcı belgesi bulunamadı: $userPath")
                    return@addOnSuccessListener
                }

                val roleRaw = snapshot.getString(FIELD_ROL)
                    ?: snapshot.getString(FIELD_ROLE)
                    ?: ""

                if (roleRaw.isBlank()) {
                    Log.e(TAG, "Kullanıcı belgesinde rol alanı boş: $userPath")
                    return@addOnSuccessListener
                }

                val userRole = UserRole.fromFirestore(roleRaw)
                if (userRole == null) {
                    Log.e(TAG, "Geçersiz veya desteklenmeyen rol: '$roleRaw' (uid=$uid)")
                    return@addOnSuccessListener
                }

                val topic = TenantFcmTopics.forRole(userRole, tenantId)
                Log.i(TAG, "Rol okundu: ${userRole.displayName} → topic=$topic")
                subscribeToRoleTopic(tenantId, topic, userRole, showSuccessToast)
            }
            .addOnFailureListener { error ->
                Log.e(TAG, "Firestore rol okuma hatası uid=$uid tenant=$tenantId", error)
            }
    }

    private fun subscribeToRoleTopic(
        tenantId: String,
        topic: String,
        userRole: UserRole,
        showSuccessToast: Boolean
    ) {
        unsubscribeOtherRoleTopics(tenantId, keepTopic = topic) {
            messaging.subscribeToTopic(topic)
                .addOnSuccessListener {
                    prefs.edit().putString(KEY_LAST_TOPIC, topic).putString(KEY_LAST_TENANT, tenantId).apply()
                    Log.i(TAG, "FCM topic aboneliği başarılı: topic=$topic rol=${userRole.displayName}")
                    if (showSuccessToast) {
                        val message = "Bildirim kanalına başarıyla bağlanıldı: ${userRole.displayName}"
                        Toast.makeText(context.applicationContext, message, Toast.LENGTH_SHORT).show()
                    }
                }
                .addOnFailureListener { error ->
                    Log.e(TAG, "FCM topic aboneliği başarısız: topic=$topic", error)
                }
        }
    }

    fun unsubscribeAllRoleTopics() {
        val topics = mutableSetOf<String>()
        prefs.getString(KEY_LAST_TOPIC, null)?.let { topics.add(it) }
        prefs.getString(KEY_LAST_TENANT, null)?.let { tenantId ->
            topics.addAll(TenantFcmTopics.allForTenant(tenantId))
        }

        if (topics.isEmpty()) {
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
        val topicsToRemove = TenantFcmTopics.allForTenant(tenantId).filter { it != keepTopic }
        if (topicsToRemove.isEmpty()) {
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
        private const val FIELD_ROL = "rol"
        private const val FIELD_ROLE = "role"
    }
}
