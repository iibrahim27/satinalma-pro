package com.satinalmapro.android.data.local

import android.content.Context

class BiometricPreferences(context: Context) {
    private val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)

    fun isEnabled(): Boolean = prefs.getBoolean(KEY_ENABLED, false)

    fun setEnabled(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_ENABLED, enabled).apply()
    }

    companion object {
        private const val PREFS = "satinalma_biometric"
        private const val KEY_ENABLED = "biometric_enabled"
    }
}
