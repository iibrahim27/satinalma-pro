package com.satinalmayonetici.android.ui

import android.app.Application
import android.content.Context
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.satinalmayonetici.android.YoneticiApp
import com.satinalmayonetici.android.data.AuthSession
import com.satinalmayonetici.android.data.TenantRow
import kotlinx.coroutines.launch

class YoneticiViewModel(app: Application) : AndroidViewModel(app) {
    private val api = YoneticiApp.from(app).api
    private val prefs = app.getSharedPreferences("yonetici_oturum", Context.MODE_PRIVATE)

    var session by mutableStateOf<AuthSession?>(null)
        private set
    var tenants by mutableStateOf<List<TenantRow>>(emptyList())
        private set
    var selected by mutableStateOf<TenantRow?>(null)
        private set
    var busy by mutableStateOf(false)
        private set
    var message by mutableStateOf<String?>(null)
    var loginError by mutableStateOf<String?>(null)

    var lisansTipi by mutableStateOf("deneme")
    var manuelGun by mutableStateOf("15")

    init {
        val refresh = prefs.getString("refresh", null)
        val email = prefs.getString("email", null)
        if (!refresh.isNullOrBlank()) {
            viewModelScope.launch {
                try {
                    val s = api.refresh(refresh).copy(email = email.orEmpty())
                    session = s
                    prefs.edit().putString("refresh", s.refreshToken).apply()
                    reload()
                } catch (_: Exception) {
                    prefs.edit().clear().apply()
                }
            }
        }
    }

    fun login(email: String, password: String, remember: Boolean) {
        viewModelScope.launch {
            busy = true
            loginError = null
            try {
                val s = api.signIn(email, password)
                session = s
                if (remember) {
                    prefs.edit()
                        .putString("refresh", s.refreshToken)
                        .putString("email", s.email)
                        .apply()
                } else {
                    prefs.edit().clear().apply()
                }
                reload()
            } catch (ex: Exception) {
                loginError = ex.message ?: "Giriş başarısız"
            } finally {
                busy = false
            }
        }
    }

    fun logout() {
        session = null
        tenants = emptyList()
        selected = null
        prefs.edit().clear().apply()
    }

    fun select(t: TenantRow) {
        selected = t
        lisansTipi = t.lisansTipi
        message = null
    }

    fun clearSelection() {
        selected = null
        message = null
    }

    fun reload() {
        viewModelScope.launch {
            busy = true
            message = null
            try {
                val token = ensureToken()
                tenants = api.listTenants(token)
                selected = selected?.let { s -> tenants.find { it.id == s.id } }
            } catch (ex: Exception) {
                message = ex.message
                if (ex.message?.contains("yetkisi", ignoreCase = true) == true) {
                    logout()
                    loginError = ex.message
                }
            } finally {
                busy = false
            }
        }
    }

    fun renewLicense() {
        val t = selected ?: return
        viewModelScope.launch {
            busy = true
            try {
                val token = ensureToken()
                val gun = manuelGun.toIntOrNull()?.takeIf { it > 0 }
                val updated = api.saveTenant(
                    token,
                    t.copy(lisansTipi = lisansTipi, aktif = true),
                    lisansYenile = true,
                    lisansGunEkle = gun
                )
                message = "Lisans yenilendi: ${updated.ad}"
                reload()
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun addDays() {
        val t = selected ?: return
        val gun = manuelGun.toIntOrNull() ?: 0
        if (gun <= 0) {
            message = "Geçerli gün sayısı girin."
            return
        }
        viewModelScope.launch {
            busy = true
            try {
                val token = ensureToken()
                api.saveTenant(
                    token,
                    t.copy(lisansTipi = lisansTipi, aktif = true),
                    lisansGunEkle = gun,
                    lisansGunEkleModu = true
                )
                message = "$gun gün eklendi."
                reload()
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun resetData(confirmKod: String) {
        val t = selected ?: return
        if (!confirmKod.equals(t.kod, ignoreCase = true)) {
            message = "Onay kodu firma kodu ile eşleşmiyor."
            return
        }
        viewModelScope.launch {
            busy = true
            try {
                val token = ensureToken()
                val utc = api.resetTenant(token, t.id)
                message = "Veri sıfırlandı (utc=$utc)."
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    private suspend fun ensureToken(): String {
        val s = session ?: throw IllegalStateException("Oturum yok")
        if (System.currentTimeMillis() < s.expiresAtMs) return s.idToken
        val refreshed = api.refresh(s.refreshToken).copy(email = s.email)
        session = refreshed
        prefs.edit().putString("refresh", refreshed.refreshToken).apply()
        return refreshed.idToken
    }
}
