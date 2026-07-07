package com.satinalmapro.android.services

import android.content.Context
import android.util.Log
import android.widget.Toast
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.firestore.FirebaseFirestore
import com.google.firebase.messaging.FirebaseMessaging
import com.satinalmapro.shared.model.User
import com.satinalmapro.shared.model.UserRole

/**
 * Giriş yapmış kullanıcının Firestore rolüne göre FCM topic aboneliğini yönetir.
 *
 * Çağrı noktaları:
 * - [MainActivity.onCreate] — uygulama her açıldığında
 * - Başarılı login sonrası — [syncRoleTopicSubscription]
 * - Logout öncesi — [unsubscribeAllRoleTopics]
 */
class FcmSubscriptionHelper(private val context: Context) {

    private val auth: FirebaseAuth = FirebaseAuth.getInstance()
    private val firestore: FirebaseFirestore = FirebaseFirestore.getInstance()
    private val messaging: FirebaseMessaging = FirebaseMessaging.getInstance()
    private val prefs = context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    /**
     * Firebase Auth oturumunu kontrol eder, Firestore'dan rolü okur ve ilgili FCM topic'ine abone olur.
     */
    fun syncRoleTopicSubscription(showSuccessToast: Boolean = true) {
        val currentUser = auth.currentUser
        if (currentUser == null) {
            Log.w(TAG, "FCM abonelik atlandı: Firebase Auth oturumu yok")
            return
        }

        val uid = currentUser.uid
        Log.d(TAG, "Rol okunuyor: users/$uid")

        firestore.collection(User.COLLECTION)
            .document(uid)
            .get()
            .addOnSuccessListener { snapshot ->
                if (!snapshot.exists()) {
                    Log.e(TAG, "Kullanıcı belgesi bulunamadı: users/$uid")
                    return@addOnSuccessListener
                }

                val roleRaw = snapshot.getString(FIELD_ROL)
                    ?: snapshot.getString(FIELD_ROLE)
                    ?: ""

                if (roleRaw.isBlank()) {
                    Log.e(TAG, "Kullanıcı belgesinde rol alanı boş: users/$uid")
                    return@addOnSuccessListener
                }

                val userRole = UserRole.fromFirestore(roleRaw)
                if (userRole == null) {
                    Log.e(TAG, "Geçersiz veya desteklenmeyen rol: '$roleRaw' (uid=$uid)")
                    return@addOnSuccessListener
                }

                Log.i(TAG, "Rol okundu: ${userRole.displayName} → topic=${userRole.fcmTopic}")
                subscribeToRoleTopic(userRole, showSuccessToast)
            }
            .addOnFailureListener { error ->
                Log.e(TAG, "Firestore rol okuma hatası uid=$uid", error)
            }
    }

    /**
     * Önce diğer rol topic'lerinden çıkar, ardından hedef role abone olur.
     */
    private fun subscribeToRoleTopic(userRole: UserRole, showSuccessToast: Boolean) {
        val topic = userRole.fcmTopic

        unsubscribeOtherRoleTopics(keepTopic = topic) {
            messaging.subscribeToTopic(topic)
                .addOnSuccessListener {
                    prefs.edit().putString(KEY_LAST_TOPIC, topic).apply()
                    Log.i(
                        TAG,
                        "FCM topic aboneliği başarılı: topic=$topic rol=${userRole.displayName}"
                    )
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

    /**
     * Oturum kapanırken tüm rol topic aboneliklerini kaldırır.
     */
    fun unsubscribeAllRoleTopics() {
        val topics = UserRole.allTopics.toMutableSet()
        prefs.getString(KEY_LAST_TOPIC, null)?.let { topics.add(it) }

        if (topics.isEmpty()) {
            prefs.edit().remove(KEY_LAST_TOPIC).apply()
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
                        prefs.edit().remove(KEY_LAST_TOPIC).apply()
                    }
                }
        }
    }

    private fun unsubscribeOtherRoleTopics(keepTopic: String, onComplete: () -> Unit) {
        val topicsToRemove = UserRole.allTopics.filter { it != keepTopic }
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
        private const val FIELD_ROL = "rol"
        private const val FIELD_ROLE = "role"
    }
}
