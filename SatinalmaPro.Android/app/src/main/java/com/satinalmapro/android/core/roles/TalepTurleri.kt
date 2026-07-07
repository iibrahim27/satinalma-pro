package com.satinalmapro.android.core.roles

object TalepTurleri {
    const val ACIL = "Acil"
    const val ONCELIKLI = "Oncelikli"
    const val NORMAL = "Normal"

    val TUM = listOf(NORMAL, ONCELIKLI, ACIL)

    fun fromIndex(index: Int): String = when (index.coerceIn(0, 2)) {
        2 -> ACIL
        1 -> ONCELIKLI
        else -> NORMAL
    }

    fun gorunenAd(tur: String): String = when (tur) {
        ACIL -> "Acil"
        ONCELIKLI -> "Öncelikli"
        else -> "Normal"
    }
}
