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
import com.satinalmayonetici.android.data.ModulKatalogu
import com.satinalmayonetici.android.data.ModulYetki
import com.satinalmayonetici.android.data.TenantRow
import com.satinalmayonetici.android.data.UserRow
import com.satinalmayonetici.android.data.validateUsername
import kotlinx.coroutines.launch
import java.io.File

sealed class Screen {
    data object Login : Screen()
    data object FirmList : Screen()
    data object FirmDetail : Screen()
    data object UserEdit : Screen()
}

class YoneticiViewModel(app: Application) : AndroidViewModel(app) {
    private val api = YoneticiApp.from(app).api
    private val prefs = app.getSharedPreferences("yonetici_oturum", Context.MODE_PRIVATE)

    var screen by mutableStateOf<Screen>(Screen.Login)
        private set
    var session by mutableStateOf<AuthSession?>(null)
        private set
    var tenants by mutableStateOf<List<TenantRow>>(emptyList())
        private set
    var selected by mutableStateOf<TenantRow?>(null)
        private set
    var users by mutableStateOf<List<UserRow>>(emptyList())
        private set
    var editingUser by mutableStateOf<UserRow?>(null)
        private set
    var busy by mutableStateOf(false)
        private set
    var message by mutableStateOf<String?>(null)
    var loginError by mutableStateOf<String?>(null)

    // Firm form
    var firmaKod by mutableStateOf("")
    var firmaAd by mutableStateOf("")
    var firmaAktif by mutableStateOf(true)
    var lisansTipi by mutableStateOf("deneme")
    var manuelGun by mutableStateOf("15")
    var restorePath by mutableStateOf("")

    // User form
    var uAdi by mutableStateOf("")
    var uEposta by mutableStateOf("")
    var uAdSoyad by mutableStateOf("")
    var uRol by mutableStateOf("Saha")
    var uSaha by mutableStateOf("")
    var uSifre by mutableStateOf("")
    var uAktif by mutableStateOf(true)
    var uYetkiler by mutableStateOf<List<ModulYetki>>(emptyList())
    var isNewUser by mutableStateOf(false)

    init {
        val refresh = prefs.getString("refresh", null)
        val email = prefs.getString("email", null)
        if (!refresh.isNullOrBlank()) {
            viewModelScope.launch {
                try {
                    val s = api.refresh(refresh).copy(email = email.orEmpty())
                    session = s
                    prefs.edit().putString("refresh", s.refreshToken).apply()
                    screen = Screen.FirmList
                    reloadTenants()
                    runCatching { api.detachSelf(ensureToken()) }
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
                    prefs.edit().putString("refresh", s.refreshToken).putString("email", s.email).apply()
                } else prefs.edit().clear().apply()
                screen = Screen.FirmList
                try {
                    reloadTenants()
                    runCatching { api.detachSelf(ensureToken()) }
                } catch (ex: Exception) {
                    if (ex.message?.contains("yetkisi", true) == true) {
                        runCatching { api.bootstrapAdmin(ensureToken()) }
                        reloadTenants()
                    } else throw ex
                }
            } catch (ex: Exception) {
                loginError = ex.message ?: "Giriş başarısız"
                screen = Screen.Login
            } finally {
                busy = false
            }
        }
    }

    fun logout() {
        session = null
        tenants = emptyList()
        selected = null
        users = emptyList()
        screen = Screen.Login
        prefs.edit().clear().apply()
    }

    fun goFirmList() {
        selected = null
        users = emptyList()
        screen = Screen.FirmList
        message = null
    }

    fun newFirm() {
        selected = TenantRow()
        firmaKod = ""
        firmaAd = ""
        firmaAktif = true
        lisansTipi = "deneme"
        manuelGun = "15"
        restorePath = ""
        users = emptyList()
        screen = Screen.FirmDetail
        message = "Yeni firma formu — kaydetmeden kullanıcı eklenemez."
    }

    fun openFirm(t: TenantRow) {
        selected = t
        fillFirmForm(t)
        screen = Screen.FirmDetail
        reloadUsers()
    }

    private fun fillFirmForm(t: TenantRow) {
        firmaKod = t.kod
        firmaAd = t.ad
        firmaAktif = t.aktif
        lisansTipi = t.lisansTipi
        restorePath = "platform-backups/${t.id}/"
        message = null
    }

    fun reloadTenants() {
        viewModelScope.launch {
            busy = true
            try {
                tenants = api.listTenants(ensureToken())
                selected = selected?.takeIf { it.id.isBlank() }
                    ?: selected?.let { s -> tenants.find { it.id == s.id } }?.also { fillFirmForm(it) }
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun saveFirm(lisansYenile: Boolean = false, gunEkleModu: Boolean = false) {
        viewModelScope.launch {
            busy = true
            try {
                val gun = manuelGun.toIntOrNull()?.takeIf { it > 0 }
                val base = (selected ?: TenantRow()).copy(
                    kod = firmaKod.trim(),
                    ad = firmaAd.trim(),
                    aktif = if (lisansYenile || gunEkleModu) true else firmaAktif,
                    lisansTipi = lisansTipi
                )
                val saved = api.saveTenant(
                    ensureToken(),
                    base,
                    lisansYenile = lisansYenile,
                    lisansGunEkle = gun,
                    lisansGunEkleModu = gunEkleModu
                )
                message = when {
                    gunEkleModu -> "Gün eklendi."
                    lisansYenile -> "Lisans yenilendi."
                    else -> "Firma kaydedildi."
                }
                reloadTenants()
                selected = saved
                fillFirmForm(saved)
                if (saved.id.isNotBlank()) reloadUsers()
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun deleteFirm(confirmKod: String) {
        val t = selected ?: return
        if (t.id.isBlank()) return
        if (!confirmKod.equals(t.kod, true)) {
            message = "Onay kodu firma kodu ile eşleşmiyor."
            return
        }
        viewModelScope.launch {
            busy = true
            try {
                val n = api.deleteTenant(ensureToken(), t.id, confirmKod.trim())
                message = "Firma silindi ($n kullanıcı)."
                goFirmList()
                reloadTenants()
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun backupFirm(onFile: (File) -> Unit) {
        val t = selected ?: return
        if (t.id.isBlank()) {
            message = "Önce firmayı kaydedin."
            return
        }
        viewModelScope.launch {
            busy = true
            try {
                val r = api.backupTenant(ensureToken(), t.id)
                if (r.downloadUrl.isBlank()) throw IllegalStateException("Yedek URL alınamadı")
                val dest = File(getApplication<Application>().cacheDir, "${t.kod}_backup.zip")
                api.downloadToFile(r.downloadUrl, dest)
                message = "Yedek hazır (${r.sizeBytes / 1024} KB)\n${r.path}"
                onFile(dest)
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun restoreFirm() {
        val t = selected ?: return
        if (t.id.isBlank() || restorePath.isBlank()) {
            message = "Storage yolu gerekli."
            return
        }
        viewModelScope.launch {
            busy = true
            try {
                val (d, u) = api.restoreTenant(ensureToken(), t.id, restorePath.trim())
                message = "Geri yüklendi: $d veri, $u kullanıcı."
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun resetFirm(confirmKod: String) {
        val t = selected ?: return
        if (!confirmKod.equals(t.kod, true)) {
            message = "Onay kodu eşleşmiyor."
            return
        }
        viewModelScope.launch {
            busy = true
            try {
                val utc = api.resetTenant(ensureToken(), t.id)
                message = "Veri sıfırlandı (utc=$utc). Kullanıcılar ve ayarlar korundu."
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun detachSelf() {
        viewModelScope.launch {
            busy = true
            try {
                val (u, n) = api.detachSelf(ensureToken())
                message = "Firmalardan ayrıldı ($u kullanıcı, $n username)."
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun reloadUsers() {
        val t = selected ?: return
        if (t.id.isBlank()) return
        viewModelScope.launch {
            busy = true
            try {
                users = api.listUsers(ensureToken(), t.id)
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun newUser() {
        if (selected?.id.isNullOrBlank()) {
            message = "Önce firmayı kaydedin."
            return
        }
        isNewUser = true
        editingUser = UserRow()
        uAdi = ""; uEposta = ""; uAdSoyad = ""; uRol = "Saha"; uSaha = ""; uSifre = ""; uAktif = true
        uYetkiler = mergeYetkiler(ModulKatalogu.rolVarsayilanYetkiler("Saha"))
        screen = Screen.UserEdit
        message = null
    }

    fun openUser(u: UserRow) {
        isNewUser = false
        editingUser = u
        uAdi = u.kullaniciAdi
        uEposta = u.eposta
        uAdSoyad = u.adSoyad
        uRol = ModulKatalogu.normalizeRol(u.rol)
        uSaha = u.saha
        uSifre = ""
        uAktif = u.aktif
        uYetkiler = if (u.modulYetkileri.isNotEmpty()) {
            mergeYetkiler(u.modulYetkileri)
        } else {
            mergeYetkiler(ModulKatalogu.rolVarsayilanYetkiler(u.rol))
        }
        screen = Screen.UserEdit
        message = null
    }

    fun applyRoleDefaults() {
        uYetkiler = mergeYetkiler(ModulKatalogu.rolVarsayilanYetkiler(uRol))
    }

    fun clearPermissions() {
        uYetkiler = ModulKatalogu.tum.map { ModulYetki(it) }
    }

    fun updateYetki(modul: String, transform: (ModulYetki) -> ModulYetki) {
        val base = mergeYetkiler(uYetkiler)
        uYetkiler = base.map {
            if (it.modul == modul) {
                var y = transform(it)
                if (y.yazma) y = y.copy(okuma = true)
                if (!y.okuma) y = y.copy(yazma = false, sekmeler = emptyList())
                if (y.yazma && !ModulKatalogu.yazmaAtanabilir(uRol, modul)) y = y.copy(yazma = false)
                y
            } else it
        }
    }

    fun saveUser() {
        val t = selected ?: return
        val err = validateUsername(uAdi)
        if (err != null) {
            message = err
            return
        }
        if (uEposta.isBlank() || uAdSoyad.isBlank()) {
            message = "E-posta ve ad soyad zorunlu."
            return
        }
        if (isNewUser && uSifre.isBlank()) {
            message = "Yeni kullanıcı için şifre zorunlu."
            return
        }
        viewModelScope.launch {
            busy = true
            try {
                val yetkiler = uYetkiler.filter { it.okuma || it.yazma }.map { y ->
                    val all = ModulKatalogu.sekmeleriAl(y.modul)
                    val sekmeler = if (all.isNotEmpty() && y.sekmeler.size == all.size) emptyList() else y.sekmeler
                    y.copy(sekmeler = sekmeler)
                }
                val user = UserRow(
                    uid = editingUser?.uid.orEmpty(),
                    kullaniciAdi = uAdi.trim().lowercase(),
                    eposta = uEposta.trim(),
                    adSoyad = uAdSoyad.trim(),
                    rol = uRol,
                    saha = uSaha.trim(),
                    aktif = uAktif,
                    moduller = yetkiler.filter { it.okuma }.map { it.modul },
                    modulYetkileri = yetkiler
                )
                api.saveUser(ensureToken(), t.id, user, uSifre.ifBlank { null })
                message = "Kullanıcı kaydedildi."
                screen = Screen.FirmDetail
                reloadUsers()
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun deleteUser() {
        val t = selected ?: return
        val u = editingUser ?: return
        if (u.uid.isBlank()) return
        viewModelScope.launch {
            busy = true
            try {
                api.deleteUser(ensureToken(), t.id, u.uid)
                message = "Kullanıcı silindi."
                screen = Screen.FirmDetail
                reloadUsers()
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun importLegacy() {
        val t = selected ?: return
        if (t.id.isBlank()) return
        viewModelScope.launch {
            busy = true
            try {
                val (i, s, tot) = api.importLegacyUsers(ensureToken(), t.id)
                message = "Eski kullanıcı aktarımı: $i aktarıldı, $s atlandı (toplam $tot)."
                reloadUsers()
            } catch (ex: Exception) {
                message = ex.message
            } finally {
                busy = false
            }
        }
    }

    fun backFromUser() {
        screen = Screen.FirmDetail
        message = null
    }

    private fun mergeYetkiler(mevcut: List<ModulYetki>): List<ModulYetki> {
        val map = mevcut.associateBy { it.modul }
        return ModulKatalogu.tum.map { m ->
            map[m] ?: ModulYetki(m)
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
