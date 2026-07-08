package com.satinalmapro.android.core.saas

data class TenantLicense(
    val tip: String = "deneme",
    val baslangicUtc: String? = null,
    val bitisUtc: String? = null,
    val aktif: Boolean = true,
    val kalanGun: Int? = null,
    val suresiDoldu: Boolean = false
) {
    val kisaDurumMetni: String
        get() {
            if (suresiDoldu || !aktif || kalanGun == null || kalanGun <= 0) {
                return "Lisans süresi doldu"
            }
            return if (kalanGun <= 7) {
                "Lisans: $kalanGun gün kaldı"
            } else {
                "Lisans: $kalanGun gün"
            }
        }

    val tipGorunen: String
        get() = when (tip) {
            "yillik" -> "Yıllık lisans"
            else -> "30 günlük deneme"
        }
}
