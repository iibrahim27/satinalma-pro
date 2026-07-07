package com.satinalmapro.shared.model

import com.satinalmapro.android.core.roles.KullaniciRolleri

/**
 * Firestore `users.rol` değerleri ile FCM topic adlarını eşler.
 * Topic adları yalnızca [a-zA-Z0-9-_.~%] içerebilir — Türkçe karakter kullanılmaz.
 */
enum class UserRole(
    val firestoreValue: String,
    val fcmTopic: String,
    val displayName: String
) {
    ADMIN(KullaniciRolleri.ADMIN, "admin", "Admin"),
    YONETIM(KullaniciRolleri.YONETIM, "yonetim", "Yönetim"),
    SATINALMA(KullaniciRolleri.SATINALMA, "satinalma", "Satınalma"),
    SEF(KullaniciRolleri.SEF, "sef", "Şef"),
    SAHA(KullaniciRolleri.SAHA, "saha", "Saha"),
    ATOLYE(KullaniciRolleri.ATOLYE, "atolye", "Atölye"),
    DEPO(KullaniciRolleri.DEPO, "depo", "Depo");

    companion object {
        val allTopics: List<String> = entries.map { it.fcmTopic }

        fun fromFirestore(raw: String?): UserRole? {
            if (raw.isNullOrBlank()) return null
            val normalized = KullaniciRolleri.normalize(raw)
            return entries.firstOrNull { it.firestoreValue.equals(normalized, ignoreCase = true) }
        }
    }
}
