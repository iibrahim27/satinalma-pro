package com.satinalmapro.shared.model

/**
 * Firestore: `users/{uid}`
 *
 * Kimlik profili — Firebase Auth UID belge kimliği olarak kullanılır.
 * Zaman damgaları epoch milisaniye ([Long]) olarak tutulur; Firestore Timestamp ↔ Long dönüşümü
 * repository katmanında yapılır.
 */
data class User(
    /** Firebase Auth UID — belge PK */
    val uid: String = "",
    /** Giriş e-postası */
    val eposta: String = "",
    /** Görünen ad (ad soyad) */
    val adSoyad: String = "",
    /** Admin, Yönetim, Satınalma, Şef, Saha, Atölye, Depo */
    val rol: String = "",
    /** Hesap aktif mi */
    val aktif: Boolean = true,
    /** Şantiye/saha kodu → sites.siteCode */
    val saha: String? = null,
    /** FK → sites/{siteId} */
    val siteId: String? = null,
    /** Legacy modül listesi */
    val moduller: List<String> = emptyList(),
    /** JSON: ModulYetkiKaydi[] */
    val modulYetkileriJson: String? = null,
    /** Son FCM token (tercihen device_tokens koleksiyonu kullanılır) */
    val fcmToken: String? = null,
    /** Denormalize okunmamış bildirim sayısı (badge) */
    val unreadNotificationCount: Int = 0,
    val createdAt: Long? = null,
    val updatedAt: Long? = null,
    val lastLoginAt: Long? = null,
    /** Oluşturan admin UID */
    val createdBy: String? = null
) {
    companion object {
        const val COLLECTION = "users"
    }
}
